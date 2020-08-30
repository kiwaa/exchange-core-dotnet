using Exchange.Core.Collections.ObjPool;
using Exchange.Core.Common;
using Exchange.Core.Common.Api.Binary;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using Exchange.Core.Processors.Journaling;
using Exchange.Core.Utils;
using log4net;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Processors
{
    public sealed class MatchingEngineRouter : IWriteBytesMarshallable, IReportQueriesHandler
    {
        private static ILog log = LogManager.GetLogger(typeof(MatchingEngineRouter));

        // state
        public BinaryCommandsProcessor binaryCommandsProcessor { get; }

        // symbol->OB
        public Dictionary<int, IOrderBook> orderBooks { get; }

        public Func<CoreSymbolSpecification, ObjectsPool, OrderBookEventsHelper, LoggingConfiguration, IOrderBook> orderBookFactory { get; }

        public OrderBookEventsHelper eventsHelper { get; }

        // local objects pool for order books
        public ObjectsPool objectsPool { get; }

        // sharding by symbolId
        public int shardId { get; }
        public long shardMask { get; }

        public bool cfgMarginTradingEnabled { get; }

        public ISerializationProcessor serializationProcessor { get; }

        public LoggingConfiguration loggingCfg { get; }
        public bool logDebug { get; }

        public MatchingEngineRouter(int shardId,
                                    long numShards,
                                    ISerializationProcessor serializationProcessor,
                                    Func<CoreSymbolSpecification, ObjectsPool, OrderBookEventsHelper, LoggingConfiguration, IOrderBook> orderBookFactory,
                                    SharedPool sharedPool,
                                    ExchangeConfiguration exchangeCfg)
        {

            //if (long.bitCount(numShards) != 1)
            if (LongHelpers.NumberOfSetBits(numShards) != 1)
            {
                throw new InvalidOperationException("Invalid number of shards " + numShards + " - must be power of 2");
            }
            this.shardId = shardId;
            this.shardMask = numShards - 1;
            this.serializationProcessor = serializationProcessor;
            this.orderBookFactory = orderBookFactory;
            this.eventsHelper = new OrderBookEventsHelper(sharedPool.getChain);

            this.loggingCfg = exchangeCfg.LoggingCfg;
            this.logDebug = loggingCfg.LoggingLevels.HasFlag(LoggingLevel.LOGGING_MATCHING_DEBUG);

            // initialize object pools // TODO move to perf config
            //Dictionary<int, int> objectsPoolConfig = new Dictionary<int, int>();
            //objectsPoolConfig[ObjectsPool.DIRECT_ORDER] = 1024 * 1024;
            //objectsPoolConfig[ObjectsPool.DIRECT_BUCKET] = 1024 * 64;
            //objectsPoolConfig[ObjectsPool.ART_NODE_4] = 1024 * 32;
            //objectsPoolConfig[ObjectsPool.ART_NODE_16] = 1024 * 16;
            //objectsPoolConfig[ObjectsPool.ART_NODE_48] = 1024 * 8;
            //objectsPoolConfig[ObjectsPool.ART_NODE_256] = 1024 * 4;
            this.objectsPool = new ObjectsPool();
            if (exchangeCfg.InitStateCfg.fromSnapshot())
            {
                DeserializedData deserialized = serializationProcessor.loadData(
                        exchangeCfg.InitStateCfg.SnapshotId,
                        SerializedModuleType.MATCHING_ENGINE_ROUTER,
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

                            BinaryCommandsProcessor bcp = new BinaryCommandsProcessor(
                                handleBinaryMessage,
                                this,
                                sharedPool,
                                exchangeCfg.ReportsQueriesCfg,
                                bytesIn,
                                shardId + 1024);

                            Dictionary<int, IOrderBook> ob = SerializationUtils.readIntHashMap(
                                bytesIn,
                                bytes => IOrderBook.create(bytes, objectsPool, eventsHelper, loggingCfg));

                            return DeserializedData.Builder().binaryCommandsProcessor(bcp).orderBooks(ob).build();
                        });

                this.binaryCommandsProcessor = deserialized.BinaryCommandsProcessor;
                this.orderBooks = deserialized.OrderBooks;

            }
            else
            {
                this.binaryCommandsProcessor = new BinaryCommandsProcessor(
                        handleBinaryMessage,
                        this,
                        sharedPool,
                        exchangeCfg.ReportsQueriesCfg,
                        shardId + 1024);

                this.orderBooks = new Dictionary<int, IOrderBook>();
            }

            OrdersProcessingConfiguration ordersProcCfg = exchangeCfg.OrdersProcessingCfg;
            this.cfgMarginTradingEnabled = ordersProcCfg.MarginTradingMode == MarginTradingMode.MARGIN_TRADING_ENABLED;
        }

        public void processOrder(long seq, OrderCommand cmd)
        {

            OrderCommandType command = cmd.Command;

            if (command == OrderCommandType.MOVE_ORDER
                    || command == OrderCommandType.CANCEL_ORDER
                    || command == OrderCommandType.PLACE_ORDER
                    || command == OrderCommandType.REDUCE_ORDER
                    || command == OrderCommandType.ORDER_BOOK_REQUEST)
            {
                // process specific symbol group only
                if (symbolForThisHandler(cmd.Symbol))
                {
                    processMatchingCommand(cmd);
                }
            }
            else if (command == OrderCommandType.BINARY_DATA_QUERY || command == OrderCommandType.BINARY_DATA_COMMAND)
            {

                CommandResultCode resultCode = binaryCommandsProcessor.acceptBinaryFrame(cmd);
                if (shardId == 0)
                {
                    cmd.ResultCode = resultCode;
                }

            }
            else if (command == OrderCommandType.RESET)
            {
                // process all symbols groups, only processor 0 writes result
                orderBooks.Clear();
                binaryCommandsProcessor.reset();
                if (shardId == 0)
                {
                    cmd.ResultCode = CommandResultCode.SUCCESS;
                }

            }
            else if (command == OrderCommandType.NOP)
            {
                if (shardId == 0)
                {
                    cmd.ResultCode = CommandResultCode.SUCCESS;
                }

            }
            else if (command == OrderCommandType.PERSIST_STATE_MATCHING)
            {
                bool isSuccess = serializationProcessor.storeData(
                        cmd.OrderId,
                        seq,
                        cmd.Timestamp,
                        SerializedModuleType.MATCHING_ENGINE_ROUTER,
                        shardId,
                        this);
                // Send ACCEPTED because this is a first command in series. Risk engine is second - so it will return SUCCESS
                UnsafeUtils.setResultVolatile(cmd, isSuccess, CommandResultCode.ACCEPTED, CommandResultCode.STATE_PERSIST_MATCHING_ENGINE_FAILED);
            }

        }

        private void handleBinaryMessage(Object message)
        {

            if (message is BatchAddSymbolsCommand)
            {
                Dictionary<int, CoreSymbolSpecification> symbols = ((BatchAddSymbolsCommand)message).symbols;
                foreach (var tmp in symbols.Values)
                    addSymbol(tmp);
            }
            else if (message is BatchAddAccountsCommand)
            {
                // do nothing
            }
        }

        R IReportQueriesHandler.handleReport<R>(IReportQuery<R> reportQuery)
        {
            return reportQuery.process(this);
        }


        private bool symbolForThisHandler(long symbol)
        {
            return (shardMask == 0) || ((symbol & shardMask) == shardId);
        }


        private void addSymbol(CoreSymbolSpecification spec)
        {

            //        log.debug("ME add symbolSpecification: {}", symbolSpecification);

            if (spec.Type != SymbolType.CURRENCY_EXCHANGE_PAIR && !cfgMarginTradingEnabled)
            {
                log.Warn($"Margin symbols are not allowed: {spec}");
            }

            // if (orderBooks.get(spec.SymbolId) == null)
            if (!orderBooks.TryGetValue(spec.SymbolId, out _))
            {
                orderBooks[spec.SymbolId] = orderBookFactory(spec, objectsPool, eventsHelper, loggingCfg);
            }
            else
            {
                log.Warn($"OrderBook for symbol id={spec.SymbolId} already exists! Can not add symbol: {spec}");
            }
        }

        private void processMatchingCommand(OrderCommand cmd)
        {
            if (!orderBooks.TryGetValue(cmd.Symbol, out IOrderBook orderBook))
            {
                cmd.ResultCode = CommandResultCode.MATCHING_INVALID_ORDER_BOOK_ID;
            }
            else
            {
                cmd.ResultCode = IOrderBook.processCommand(orderBook, cmd);

                // posting market data for risk processor makes sense only if command execution is successful, otherwise it will be ignored (possible garbage from previous cycle)
                // TODO don't need for EXCHANGE mode order books?
                // TODO doing this for many order books simultaneously can introduce hiccups
                if ((cmd.serviceFlags & 1) != 0 && cmd.Command != OrderCommandType.ORDER_BOOK_REQUEST && cmd.ResultCode == CommandResultCode.SUCCESS)
                {
                    cmd.MarketData = orderBook.getL2MarketDataSnapshot(8);
                }
            }
        }

        //public void writeMarshallable(BytesOut bytes)
        //{
        //    bytes.writeInt(shardId).writeLong(shardMask);
        //    binaryCommandsProcessor.writeMarshallable(bytes);

        //    // write orderBooks
        //    SerializationUtils.marshallIntHashMap(orderBooks, bytes);
        //}
    }

}
