using Exchange.Core.Common;
using Exchange.Core.Common.Api.Binary;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Processors.Journaling;
using Exchange.Core.Utils;
using log4net;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Processors
{
    public sealed class RiskEngine : IWriteBytesMarshallable, IReportQueriesHandler
    {
        private static ILog log = LogManager.GetLogger(typeof(RiskEngine));
        // state
        public SymbolSpecificationProvider symbolSpecificationProvider { get; }
        public UserProfileService userProfileService { get; }
        public BinaryCommandsProcessor binaryCommandsProcessor { get; }
        public Dictionary<int, LastPriceCacheRecord> lastPriceCache { get; }
        public Dictionary<int, long> fees { get; }
        public Dictionary<int, long> adjustments { get; }
        public Dictionary<int, long> suspends { get; }
        public ObjectsPool objectsPool { get; }

        // sharding by symbolId
        public int shardId { get; }
        public long shardMask { get; }

        public bool cfgIgnoreRiskProcessing { get; }
        public bool cfgMarginTradingEnabled { get; }

        public ISerializationProcessor serializationProcessor { get; }

        public bool logDebug { get; }

        public RiskEngine(int shardId,
                          long numShards,
                          ISerializationProcessor serializationProcessor,
                          SharedPool sharedPool,
                          ExchangeConfiguration exchangeConfiguration)
        {
            if (LongHelpers.NumberOfSetBits(numShards) != 1)
            {
                throw new InvalidOperationException("Invalid number of shards " + numShards + " - must be power of 2");
            }
            this.shardId = shardId;
            this.shardMask = numShards - 1;
            this.serializationProcessor = serializationProcessor;

            // initialize object pools // TODO move to perf config
            Dictionary<int, int> objectsPoolConfig = new Dictionary<int, int>();
            //objectsPoolConfig.put(ObjectsPool.SYMBOL_POSITION_RECORD, 1024 * 256);
            this.objectsPool = new ObjectsPool();

            this.logDebug = exchangeConfiguration.LoggingCfg.LoggingLevels.HasFlag(LoggingLevel.LOGGING_RISK_DEBUG);

            if (exchangeConfiguration.InitStateCfg.fromSnapshot())
            {

                // TODO refactor, change to creator (simpler init)`
                State state = serializationProcessor.loadData(
                        exchangeConfiguration.InitStateCfg.SnapshotId,
                        SerializedModuleType.RISK_ENGINE,
                        shardId,
                        bytesIn =>
                        {
                            if (shardId != bytesIn.readInt())
                            {
                                throw new InvalidOperationException("wrong shardId");
                            }
                            if (shardMask != bytesIn.readLong())
                            {
                                throw new InvalidOperationException("wrong shardMask");
                            }
                            SymbolSpecificationProvider symbolSpecificationProvider = new SymbolSpecificationProvider(bytesIn);
                            UserProfileService userProfileService = new UserProfileService(bytesIn);
                            BinaryCommandsProcessor binaryCommandsProcessor = new BinaryCommandsProcessor(
                                    handleBinaryMessage,
                                    this,
                                    sharedPool,
                                    exchangeConfiguration.ReportsQueriesCfg,
                                    bytesIn,
                                    shardId);
                            Dictionary<int, LastPriceCacheRecord> lastPriceCache = SerializationUtils.readIntHashMap(bytesIn, b => new LastPriceCacheRecord(b));
                            Dictionary<int, long> fees = SerializationUtils.readIntLongHashMap(bytesIn);
                            Dictionary<int, long> adjustments = SerializationUtils.readIntLongHashMap(bytesIn);
                            Dictionary<int, long> suspends = SerializationUtils.readIntLongHashMap(bytesIn);

                            return new State(
                                    symbolSpecificationProvider,
                                    userProfileService,
                                    binaryCommandsProcessor,
                                    lastPriceCache,
                                    fees,
                                    adjustments,
                                    suspends);
                        });

                this.symbolSpecificationProvider = state.SymbolSpecificationProvider;
                this.userProfileService = state.UserProfileService;
                this.binaryCommandsProcessor = state.BinaryCommandsProcessor;
                this.lastPriceCache = state.LastPriceCache;
                this.fees = state.Fees;
                this.adjustments = state.Adjustments;
                this.suspends = state.Suspends;

            }
            else
            {
                this.symbolSpecificationProvider = new SymbolSpecificationProvider();
                this.userProfileService = new UserProfileService();
                this.binaryCommandsProcessor = new BinaryCommandsProcessor(
                        handleBinaryMessage,
                        this,
                        sharedPool,
                        exchangeConfiguration.ReportsQueriesCfg,
                        shardId);
                this.lastPriceCache = new Dictionary<int, LastPriceCacheRecord>();
                this.fees = new Dictionary<int, long>();
                this.adjustments = new Dictionary<int, long>();
                this.suspends = new Dictionary<int, long>();
            }

            OrdersProcessingConfiguration ordersProcCfg = exchangeConfiguration.OrdersProcessingCfg;
            this.cfgIgnoreRiskProcessing = ordersProcCfg.RiskProcessingMode == RiskProcessingMode.NO_RISK_PROCESSING;
            this.cfgMarginTradingEnabled = ordersProcCfg.MarginTradingMode == MarginTradingMode.MARGIN_TRADING_ENABLED;
        }

    
        /**
         * Pre-process command handler
         * 1. MOVE/CANCEL commands ignored, for specific uid marked as valid for matching engine
         * 2. PLACE ORDER checked with risk ending for specific uid
         * 3. ADD USER, BALANCE_ADJUSTMENT processed for specific uid, not valid for matching engine
         * 4. BINARY_DATA commands processed for ANY uid and marked as valid for matching engine TODO which handler marks?
         * 5. RESET commands processed for any uid
         *
         * @param cmd - command
         * @param seq - command sequence
         * @return true if caller should publish sequence even if batch was not processed yet
         */
        public bool preProcessCommand(long seq, OrderCommand cmd)
        {
            switch (cmd.Command)
            {
                case OrderCommandType.MOVE_ORDER:
                case OrderCommandType.CANCEL_ORDER:
                case OrderCommandType.REDUCE_ORDER:
                case OrderCommandType.ORDER_BOOK_REQUEST:
                    return false;

                case OrderCommandType.PLACE_ORDER:
                    if (uidForThisHandler(cmd.Uid))
                    {
                        cmd.ResultCode = placeOrderRiskCheck(cmd);
                    }
                    return false;

                case OrderCommandType.ADD_USER:
                    if (uidForThisHandler(cmd.Uid))
                    {
                        cmd.ResultCode = userProfileService.addEmptyUserProfile(cmd.Uid)
                                ? CommandResultCode.SUCCESS
                                : CommandResultCode.USER_MGMT_USER_ALREADY_EXISTS;
                    }
                    return false;

                case OrderCommandType.BALANCE_ADJUSTMENT:
                    if (uidForThisHandler(cmd.Uid))
                    {
                        cmd.ResultCode = adjustBalance(
                                cmd.Uid, cmd.Symbol, cmd.Price, cmd.OrderId, (BalanceAdjustmentType)((int)cmd.OrderType));
                    }
                    return false;

                case OrderCommandType.SUSPEND_USER:
                    if (uidForThisHandler(cmd.Uid))
                    {
                        cmd.ResultCode = userProfileService.suspendUserProfile(cmd.Uid);
                    }
                    return false;
                case OrderCommandType.RESUME_USER:
                    if (uidForThisHandler(cmd.Uid))
                    {
                        cmd.ResultCode = userProfileService.resumeUserProfile(cmd.Uid);
                    }
                    return false;

                case OrderCommandType.BINARY_DATA_COMMAND:
                case OrderCommandType.BINARY_DATA_QUERY:
                    binaryCommandsProcessor.acceptBinaryFrame(cmd); // ignore return result, because it should be set by MatchingEngineRouter
                    if (shardId == 0)
                    {
                        cmd.ResultCode = CommandResultCode.VALID_FOR_MATCHING_ENGINE;
                    }
                    return false;

                case OrderCommandType.RESET:
                    reset();
                    if (shardId == 0)
                    {
                        cmd.ResultCode = CommandResultCode.SUCCESS;
                    }
                    return false;

                case OrderCommandType.PERSIST_STATE_MATCHING:
                    if (shardId == 0)
                    {
                        cmd.ResultCode = CommandResultCode.VALID_FOR_MATCHING_ENGINE;
                    }
                    return true;// true = publish sequence before finishing processing whole batch

                case OrderCommandType.PERSIST_STATE_RISK:
                    bool isSuccess = serializationProcessor.storeData(
                            cmd.OrderId,
                            seq,
                            cmd.Timestamp,
                            SerializedModuleType.RISK_ENGINE,
                            shardId,
                            this);
                    UnsafeUtils.setResultVolatile(cmd, isSuccess, CommandResultCode.SUCCESS, CommandResultCode.STATE_PERSIST_RISK_ENGINE_FAILED);
                    return false;
            }
            return false;
        }


        private CommandResultCode adjustBalance(long uid, int currency, long amountDiff, long fundingTransactionId, BalanceAdjustmentType adjustmentType)
        {
            CommandResultCode res = userProfileService.balanceAdjustment(uid, currency, amountDiff, fundingTransactionId);
            if (res == CommandResultCode.SUCCESS)
            {
                switch (adjustmentType)
                {
                    case BalanceAdjustmentType.ADJUSTMENT: // adjust total adjustments amount
                        adjustments.AddValue(currency, -amountDiff);
                        break;

                    case BalanceAdjustmentType.SUSPEND: // adjust total suspends amount
                        suspends.AddValue(currency, -amountDiff);
                        break;
                }
            }
            return res;
        }

        private void handleBinaryMessage(IBinaryDataCommand message)
        {

            if (message is BatchAddSymbolsCommand) {

                Dictionary<int, CoreSymbolSpecification> symbols = ((BatchAddSymbolsCommand)message).symbols;
                foreach (var spec in symbols.Values)
                {
                    if (spec.Type == SymbolType.CURRENCY_EXCHANGE_PAIR || cfgMarginTradingEnabled)
                    {
                        symbolSpecificationProvider.addSymbol(spec);
                    }
                    else
                    {
                        log.Warn($"Margin symbols are not allowed: {spec}");
                    }
                }

            } else if (message is BatchAddAccountsCommand) {
                foreach (var pair in ((BatchAddAccountsCommand)message).users)
                {
                    var uid = pair.Key;
                    var accounts = pair.Value;
                    if (userProfileService.addEmptyUserProfile(uid))
                    {
                        foreach (var accPair in accounts)
                        {
                            adjustBalance(uid, accPair.Key, accPair.Value, 1_000_000_000 + accPair.Key, BalanceAdjustmentType.ADJUSTMENT);
                        }
                    }
                    else
                    {
                        log.Debug($"User already exist: {uid}");
                    }
                }
            }
        }

        R IReportQueriesHandler.handleReport<R>(IReportQuery<R> reportQuery)
        {
            return reportQuery.process(this);
        }

        public bool uidForThisHandler(long uid)
        {
            return (shardMask == 0) || ((uid & shardMask) == shardId);
        }

        private CommandResultCode placeOrderRiskCheck(OrderCommand cmd)
        {

            UserProfile userProfile = userProfileService.getUserProfile(cmd.Uid);
            if (userProfile == null)
            {
                cmd.ResultCode = CommandResultCode.AUTH_INVALID_USER;
                log.Warn($"User profile {cmd.Uid} not found");
                return CommandResultCode.AUTH_INVALID_USER;
            }

            CoreSymbolSpecification spec = symbolSpecificationProvider.getSymbolSpecification(cmd.Symbol);
            if (spec == null)
            {
                log.Warn($"Symbol {cmd.Symbol} not found");
                return CommandResultCode.INVALID_SYMBOL;
            }

            if (cfgIgnoreRiskProcessing)
            {
                // skip processing
                return CommandResultCode.VALID_FOR_MATCHING_ENGINE;
            }

            // check if account has enough funds
            CommandResultCode resultCode = placeOrder(cmd, userProfile, spec);

            if (resultCode != CommandResultCode.VALID_FOR_MATCHING_ENGINE)
            {
                log.Warn($"{cmd.OrderId} risk result={resultCode} uid={userProfile.uid}: Can not place {cmd}");
                log.Warn($"{cmd.OrderId} accounts:{userProfile.accounts}");
                return CommandResultCode.RISK_NSF;
            }

            return resultCode;
        }


        private CommandResultCode placeOrder(OrderCommand cmd,
                                             UserProfile userProfile,
                                             CoreSymbolSpecification spec)
        {


            if (spec.Type == SymbolType.CURRENCY_EXCHANGE_PAIR)
            {

                return placeExchangeOrder(cmd, userProfile, spec);

            }
            else if (spec.Type == SymbolType.FUTURES_CONTRACT)
            {

                if (!cfgMarginTradingEnabled)
                {
                    return CommandResultCode.RISK_MARGIN_TRADING_DISABLED;
                }

                if (!userProfile.positions.TryGetValue(spec.SymbolId, out SymbolPositionRecord position))
//                SymbolPositionRecord position = userProfile.positions.get(spec.SymbolId); // TODO getIfAbsentPut?
//                if (position == null)
                {
                    position = objectsPool.get(ObjectsPool.SYMBOL_POSITION_RECORD, pool => new SymbolPositionRecord());
                    position.initialize(userProfile.uid, spec.SymbolId, spec.QuoteCurrency);
                    userProfile.positions[spec.SymbolId] = position;
                }

                bool canPlaceOrder = canPlaceMarginOrder(cmd, userProfile, spec, position);
                if (canPlaceOrder)
                {
                    position.pendingHold(cmd.Action, cmd.Size);
                    return CommandResultCode.VALID_FOR_MATCHING_ENGINE;
                }
                else
                {
                    // try to cleanup position if refusing to place
                    if (position.isEmpty())
                    {
                        removePositionRecord(position, userProfile);
                    }
                    return CommandResultCode.RISK_NSF;
                }

            }
            else
            {
                return CommandResultCode.UNSUPPORTED_SYMBOL_TYPE;
            }
        }

        private CommandResultCode placeExchangeOrder(OrderCommand cmd,
                                                     UserProfile userProfile,
                                                     CoreSymbolSpecification spec)
        {

            int currency = (cmd.Action == OrderAction.BID) ? spec.QuoteCurrency : spec.BaseCurrency;

            // futures positions check for this currency
            long freeFuturesMargin = 0L;
            if (cfgMarginTradingEnabled)
            {
                foreach (SymbolPositionRecord position in userProfile.positions.Values)
                {
                    if (position.currency == currency)
                    {
                        int recSymbol = position.symbol;
                        CoreSymbolSpecification spec2 = symbolSpecificationProvider.getSymbolSpecification(recSymbol);
                        // add P&L subtract margin
                        freeFuturesMargin +=
                                (position.estimateProfit(spec2, lastPriceCache[recSymbol]) - position.calculateRequiredMarginForFutures(spec2));
                    }
                }
            }

            long size = cmd.Size;
            long orderHoldAmount;
            if (cmd.Action == OrderAction.BID)
            {

                if (cmd.OrderType == OrderType.FOK_BUDGET || cmd.OrderType == OrderType.IOC_BUDGET)
                {

                    if (cmd.ReserveBidPrice != cmd.Price)
                    {
                        //log.warn("reserveBidPrice={} less than price={}", cmd.reserveBidPrice, cmd.price);
                        return CommandResultCode.RISK_INVALID_RESERVE_BID_PRICE;
                    }

                    orderHoldAmount = CoreArithmeticUtils.calculateAmountBidTakerFeeForBudget(size, cmd.Price, spec);
                    if (logDebug) log.Debug($"hold amount budget buy {cmd.Price} = {size} * {spec.QuoteScaleK} + {size} * {spec.TakerFee}");

                }
                else
                {
                    
                    if (cmd.ReserveBidPrice < cmd.Price)
                    {
                        //log.warn("reserveBidPrice={} less than price={}", cmd.reserveBidPrice, cmd.price);
                        return CommandResultCode.RISK_INVALID_RESERVE_BID_PRICE;
                    }
                    orderHoldAmount = CoreArithmeticUtils.calculateAmountBidTakerFee(size, cmd.ReserveBidPrice, spec);
                    if (logDebug) log.Debug($"hold amount buy {orderHoldAmount} = {size} * ( {cmd.ReserveBidPrice} * {spec.QuoteScaleK} + {spec.TakerFee} )");
                }

            }
            else
            {

                if (cmd.Price * spec.QuoteScaleK < spec.TakerFee)
                {
                    // log.debug("cmd.price {} * spec.quoteScaleK {} < {} spec.takerFee", cmd.price, spec.quoteScaleK, spec.takerFee);
                    // todo also check for move command
                    return CommandResultCode.RISK_ASK_PRICE_LOWER_THAN_FEE;
                }

                orderHoldAmount = CoreArithmeticUtils.calculateAmountAsk(size, spec);
                if (logDebug) log.Debug($"hold sell {orderHoldAmount} = {size} * {spec.BaseScaleK} ");
            }

            if (logDebug)
            {
                log.Debug($"R1 uid={userProfile.uid} : orderHoldAmount={orderHoldAmount} vs serProfile.accounts.get({currency})={userProfile.accounts[currency]} + freeFuturesMargin={freeFuturesMargin}");
            }

            // speculative change balance
            long newBalance = userProfile.accounts[currency] += -orderHoldAmount;

            bool canPlace = newBalance + freeFuturesMargin >= 0;

            if (!canPlace)
            {
                // revert balance change
                userProfile.accounts[currency] += orderHoldAmount;
                //            log.warn("orderAmount={} > userProfile.accounts.get({})={}", orderAmount, currency, userProfile.accounts.get(currency));
                return CommandResultCode.RISK_NSF;
            }
            else
            {
                return CommandResultCode.VALID_FOR_MATCHING_ENGINE;
            }
        }


        /**
         * Checks:
         * 1. Users account balance
         * 2. Margin
         * 3. Current limit orders
         * <p>
         * NOTE: Current implementation does not care about accounts and positions quoted in different currencies
         *
         * @param cmd         - order command
         * @param userProfile - user profile
         * @param spec        - symbol specification
         * @param position    - users position
         * @return true if placing is allowed
         */
        private bool canPlaceMarginOrder(OrderCommand cmd,
                                            UserProfile userProfile,
                                            CoreSymbolSpecification spec,
                                            SymbolPositionRecord position)
        {

            long newRequiredMarginForSymbol = position.calculateRequiredMarginForOrder(spec, cmd.Action, cmd.Size);
            if (newRequiredMarginForSymbol == -1)
            {
                // always allow placing a new order if it would not increase exposure
                return true;
            }

            // extra margin is required

            int symbol = cmd.Symbol;
            // calculate free margin for all positions same currency
            long freeMargin = 0L;
            foreach (SymbolPositionRecord positionRecord in userProfile.positions.Values)
            {
                int recSymbol = positionRecord.symbol;
                if (recSymbol != symbol)
                {
                    if (positionRecord.currency == spec.QuoteCurrency)
                    {
                        CoreSymbolSpecification spec2 = symbolSpecificationProvider.getSymbolSpecification(recSymbol);
                        // add P&L subtract margin
                        freeMargin += positionRecord.estimateProfit(spec2, lastPriceCache[recSymbol]);
                        freeMargin -= positionRecord.calculateRequiredMarginForFutures(spec2);
                    }
                }
                else
                {
                    lastPriceCache.TryGetValue(spec.SymbolId, out LastPriceCacheRecord value);
                    freeMargin = position.estimateProfit(spec, value);
                }
            }

            //        log.debug("newMargin={} <= account({})={} + free {}",
            //                newRequiredMarginForSymbol, position.currency, userProfile.accounts.get(position.currency), freeMargin);

            // check if current balance and margin can cover new required margin for symbol position
            return newRequiredMarginForSymbol <= userProfile.accounts[position.currency] + freeMargin;
        }

        public bool handlerRiskRelease(long seq, OrderCommand cmd)
        {

            int symbol = cmd.Symbol;

            L2MarketData marketData = cmd.MarketData;
            MatcherTradeEvent mte = cmd.MatcherEvent;

            // skip events processing if no events (or if contains BINARY EVENT)
            if (marketData == null && (mte == null || mte.EventType == MatcherEventType.BINARY_EVENT))
            {
                return false;
            }

            CoreSymbolSpecification spec = symbolSpecificationProvider.getSymbolSpecification(symbol);
            if (spec == null)
            {
                throw new InvalidOperationException("Symbol not found: " + symbol);
            }

            bool takerSell = cmd.Action == OrderAction.ASK;

            if (mte != null && mte.EventType != MatcherEventType.BINARY_EVENT)
            {
                // at least one event to process, resolving primary/taker user profile
                // TODO processing order is reversed
                if (spec.Type == SymbolType.CURRENCY_EXCHANGE_PAIR)
                {

                    UserProfile takerUp = uidForThisHandler(cmd.Uid)
                            ? userProfileService.getUserProfileOrAddSuspended(cmd.Uid)
                            : null;

                    // REJECT always comes first; REDUCE is always single event
                    if (mte.EventType == MatcherEventType.REDUCE || mte.EventType == MatcherEventType.REJECT)
                    {
                        if (takerUp != null)
                        {
                            handleMatcherRejectReduceEventExchange(cmd, mte, spec, takerSell, takerUp);
                        }
                        mte = mte.NextEvent;
                    }

                    if (mte != null)
                    {
                        if (takerSell)
                        {
                            handleMatcherEventsExchangeSell(mte, spec, takerUp);
                        }
                        else
                        {
                            handleMatcherEventsExchangeBuy(mte, spec, takerUp, cmd);
                        }
                    }
                }
                else
                {

                    UserProfile takerUp = uidForThisHandler(cmd.Uid) ? userProfileService.getUserProfileOrAddSuspended(cmd.Uid) : null;

                    // for margin-mode symbols also resolve position record
                    SymbolPositionRecord takerSpr = (takerUp != null) ? takerUp.getPositionRecordOrThrowEx(symbol) : null;
                    do
                    {
                        handleMatcherEventMargin(mte, spec, cmd.Action, takerUp, takerSpr);
                        mte = mte.NextEvent;
                    } while (mte != null);
                }
            }

            // Process marked data
            if (marketData != null && cfgMarginTradingEnabled)
            {
                if (!lastPriceCache.TryGetValue(symbol, out LastPriceCacheRecord record))
                {
                    record = new LastPriceCacheRecord();
                    lastPriceCache[symbol] = record;
                }
                record.askPrice = (marketData.AskSize != 0) ? marketData.AskPrices[0] : long.MaxValue;
                record.bidPrice = (marketData.BidSize != 0) ? marketData.BidPrices[0] : 0;
            }

            return false;
        }

        private void handleMatcherEventMargin(MatcherTradeEvent ev,
                                              CoreSymbolSpecification spec,
                                              OrderAction takerAction,
                                              UserProfile takerUp,
                                              SymbolPositionRecord takerSpr)
        {
            if (takerUp != null)
            {
                if (ev.EventType == MatcherEventType.TRADE)
                {
                    // update taker's position
                    long sizeOpen = takerSpr.updatePositionForMarginTrade(takerAction, ev.Size, ev.Price);
                    long fee = spec.TakerFee * sizeOpen;
                    takerUp.accounts[spec.QuoteCurrency] += -fee;
                    fees.AddValue(spec.QuoteCurrency, fee);
                }
                else if (ev.EventType == MatcherEventType.REJECT || ev.EventType == MatcherEventType.REDUCE)
                {
                    // for cancel/rejection only one party is involved
                    takerSpr.pendingRelease(takerAction, ev.Size);
                }

                if (takerSpr.isEmpty())
                {
                    removePositionRecord(takerSpr, takerUp);
                }
            }

            if (ev.EventType == MatcherEventType.TRADE && uidForThisHandler(ev.MatchedOrderUid))
            {
                // update maker's position
                UserProfile maker = userProfileService.getUserProfileOrAddSuspended(ev.MatchedOrderUid);
                SymbolPositionRecord makerSpr = maker.getPositionRecordOrThrowEx(spec.SymbolId);
                long sizeOpen = makerSpr.updatePositionForMarginTrade(OrderActionHelper.opposite(takerAction), ev.Size, ev.Price);
                long fee = spec.MakerFee * sizeOpen;
                maker.accounts[spec.QuoteCurrency] += -fee;
                fees[spec.QuoteCurrency] += fee;
                if (makerSpr.isEmpty())
                {
                    removePositionRecord(makerSpr, maker);
                }
            }

        }

        private void handleMatcherRejectReduceEventExchange(OrderCommand cmd,
                                                            MatcherTradeEvent ev,
                                                            CoreSymbolSpecification spec,
                                                            bool takerSell,
                                                            UserProfile taker)
        {

            //log.debug("REDUCE/REJECT {} {}", cmd, ev);

            // for cancel/rejection only one party is involved
            if (takerSell)
            {

                taker.accounts[spec.BaseCurrency] += CoreArithmeticUtils.calculateAmountAsk(ev.Size, spec);

            }
            else
            {

                if (cmd.Command == OrderCommandType.PLACE_ORDER && cmd.OrderType == OrderType.FOK_BUDGET)
                {
                    taker.accounts[spec.QuoteCurrency] += CoreArithmeticUtils.calculateAmountBidTakerFeeForBudget(ev.Size, ev.Price, spec);
                }
                else
                {
                    taker.accounts[spec.QuoteCurrency] += CoreArithmeticUtils.calculateAmountBidTakerFee(ev.Size, ev.BidderHoldPrice, spec);
                }
                // TODO for OrderType.IOC_BUDGET - for REJECT should release leftover deposit after all trades calculated
            }

        }


        private void handleMatcherEventsExchangeSell(MatcherTradeEvent ev,
                                                     CoreSymbolSpecification spec,
                                                     UserProfile taker)
        {

            //log.debug("TRADE EXCH SELL {}", ev);

            long takerSizeForThisHandler = 0L;
            long makerSizeForThisHandler = 0L;

            long takerSizePriceForThisHandler = 0L;

            int quoteCurrency = spec.QuoteCurrency;

            while (ev != null)
            {
                Debug.Assert(ev.EventType == MatcherEventType.TRADE);

                // aggregate transfers for selling taker
                if (taker != null)
                {
                    takerSizePriceForThisHandler += ev.Size * ev.Price;
                    takerSizeForThisHandler += ev.Size;
                }

                // process transfers for buying maker
                if (uidForThisHandler(ev.MatchedOrderUid))
                {
                    long size = ev.Size;
                    UserProfile maker = userProfileService.getUserProfileOrAddSuspended(ev.MatchedOrderUid);

                    // buying, use bidderHoldPrice to calculate released amount based on price difference
                    long priceDiff = ev.BidderHoldPrice - ev.Price;
                    long amountDiffToReleaseInQuoteCurrency = CoreArithmeticUtils.calculateAmountBidReleaseCorrMaker(size, priceDiff, spec);
                    maker.accounts[quoteCurrency] += amountDiffToReleaseInQuoteCurrency;

                    long gainedAmountInBaseCurrency = CoreArithmeticUtils.calculateAmountAsk(size, spec);
                    maker.accounts.AddValue(spec.BaseCurrency, gainedAmountInBaseCurrency);

                    makerSizeForThisHandler += size;
                }

                ev = ev.NextEvent;
            }

            if (taker != null)
            {
                taker.accounts.AddValue(quoteCurrency, takerSizePriceForThisHandler * spec.QuoteScaleK - spec.TakerFee * takerSizeForThisHandler);
            }

            if (takerSizeForThisHandler != 0 || makerSizeForThisHandler != 0)
            {
                fees.AddValue(quoteCurrency, spec.TakerFee * takerSizeForThisHandler + spec.MakerFee * makerSizeForThisHandler);
            }
        }

        private void handleMatcherEventsExchangeBuy(MatcherTradeEvent ev,
                                                    CoreSymbolSpecification spec,
                                                    UserProfile taker,
                                                    OrderCommand cmd)
        {
            //log.debug("TRADE EXCH BUY {}", ev);

            long takerSizeForThisHandler = 0L;
            long makerSizeForThisHandler = 0L;

            long takerSizePriceSum = 0L;
            long takerSizePriceHeldSum = 0L;

            int quoteCurrency = spec.QuoteCurrency;

            while (ev != null)
            {
                Debug.Assert(ev.EventType == MatcherEventType.TRADE);

                // perform transfers for taker
                if (taker != null)
                {

                    takerSizePriceSum += ev.Size * ev.Price;
                    takerSizePriceHeldSum += ev.Size * ev.BidderHoldPrice;

                    takerSizeForThisHandler += ev.Size;
                }

                // process transfers for maker
                if (uidForThisHandler(ev.MatchedOrderUid))
                {
                    long size = ev.Size;
                    UserProfile maker = userProfileService.getUserProfileOrAddSuspended(ev.MatchedOrderUid);
                    long gainedAmountInQuoteCurrency = CoreArithmeticUtils.calculateAmountBid(size, ev.Price, spec);
                    maker.accounts.AddValue(quoteCurrency, gainedAmountInQuoteCurrency - spec.MakerFee * size);
                    makerSizeForThisHandler += size;
                }

                ev = ev.NextEvent;
            }

            if (taker != null)
            {

                if (cmd.Command == OrderCommandType.PLACE_ORDER && cmd.OrderType == OrderType.FOK_BUDGET)
                {
                    // for FOK budget held sum calculated differently
                    takerSizePriceHeldSum = cmd.Price;
                }
                // TODO IOC_BUDGET - order can be partially rejected - need held taker fee correction

                taker.accounts.AddValue(quoteCurrency, (takerSizePriceHeldSum - takerSizePriceSum) * spec.QuoteScaleK);
                taker.accounts.AddValue(spec.BaseCurrency, takerSizeForThisHandler * spec.BaseScaleK);
            }

            if (takerSizeForThisHandler != 0 || makerSizeForThisHandler != 0)
            {
                fees.AddValue(quoteCurrency, spec.TakerFee * takerSizeForThisHandler + spec.MakerFee * makerSizeForThisHandler);
            }
        }

        private void removePositionRecord(SymbolPositionRecord record, UserProfile userProfile)
        {
            userProfile.accounts[record.currency] += record.profit;
            userProfile.positions.Remove(record.symbol);
            objectsPool.Put(ObjectsPool.SYMBOL_POSITION_RECORD, record);
        }

        public void writeMarshallable(IBytesOut bytes)
        {

            bytes.writeInt(shardId).writeLong(shardMask);

            symbolSpecificationProvider.writeMarshallable(bytes);
            userProfileService.writeMarshallable(bytes);
            binaryCommandsProcessor.writeMarshallable(bytes);
            SerializationUtils.marshallIntHashMap(lastPriceCache, bytes);
            SerializationUtils.marshallIntLongHashMap(fees, bytes);
            SerializationUtils.marshallIntLongHashMap(adjustments, bytes);
            SerializationUtils.marshallIntLongHashMap(suspends, bytes);
        }

        public void reset()
        {
            userProfileService.reset();
            symbolSpecificationProvider.reset();
            binaryCommandsProcessor.reset();
            lastPriceCache.Clear();
            fees.Clear();
            adjustments.Clear();
            suspends.Clear();
        }


    }
}
