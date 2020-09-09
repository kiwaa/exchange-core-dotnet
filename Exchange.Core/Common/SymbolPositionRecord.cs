using Exchange.Core.Processors;
using log4net;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public sealed class SymbolPositionRecord : IWriteBytesMarshallable, IStateHash 
    {
        private static ILog log = LogManager.GetLogger(typeof(SymbolPositionRecord));

        public long uid { get; set; }

    public int symbol { get; set; }
        public int currency { get; set; }

        // open positions state (for margin trades only)
        public PositionDirection direction { get; set; } = PositionDirection.EMPTY;
    public long openVolume { get; set; } = 0;
    public long openPriceSum { get; set; } = 0; //
    public long profit { get; set; } = 0;

    // pending orders total size
    // increment before sending order to matching engine
    // decrement after receiving trade confirmation from matching engine
    public long pendingSellSize { get; set; } = 0;
    public long pendingBuySize { get; set; } = 0;

    public void initialize(long uid, int symbol, int currency)
    {
        this.uid = uid;

        this.symbol = symbol;
        this.currency = currency;

        this.direction = PositionDirection.EMPTY;
        this.openVolume = 0;
        this.openPriceSum = 0;
        this.profit = 0;

        this.pendingSellSize = 0;
        this.pendingBuySize = 0;
    }

        public SymbolPositionRecord()
        {

        }
    public SymbolPositionRecord(long uid, IBytesIn bytes)
    {
        this.uid = uid;

        this.symbol = bytes.readInt();
        this.currency = bytes.readInt();

        this.direction = (PositionDirection)bytes.readByte();
        this.openVolume = bytes.readLong();
        this.openPriceSum = bytes.readLong();
        this.profit = bytes.readLong();

        this.pendingSellSize = bytes.readLong();
        this.pendingBuySize = bytes.readLong();
    }


    /**
     * Check if position is empty (no pending orders, no open trades) - can remove it from hashmap
     *
     * @return true if position is empty (no pending orders, no open trades)
     */
    public bool isEmpty()
    {
        return direction == PositionDirection.EMPTY
                && pendingSellSize == 0
                && pendingBuySize == 0;
    }

    public void pendingHold(OrderAction orderAction, long size)
    {
        if (orderAction == OrderAction.ASK)
        {
            pendingSellSize += size;
        }
        else
        {
            pendingBuySize += size;
        }
    }

    public void pendingRelease(OrderAction orderAction, long size)
    {
        if (orderAction == OrderAction.ASK)
        {
            pendingSellSize -= size;
        }
        else
        {
            pendingBuySize -= size;
        }

        //        if (pendingSellSize < 0 || pendingBuySize < 0) {
        //            log.error("uid {} : pendingSellSize:{} pendingBuySize:{}", uid, pendingSellSize, pendingBuySize);
        //        }
    }

    public long estimateProfit(CoreSymbolSpecification spec, LastPriceCacheRecord lastPriceCacheRecord)
    {
        switch (direction)
        {
            case PositionDirection.EMPTY:
                return profit;
            case PositionDirection.LONG:
                return profit + ((lastPriceCacheRecord != null && lastPriceCacheRecord.bidPrice != 0)
                        ? (openVolume * lastPriceCacheRecord.bidPrice - openPriceSum)
                        : spec.MarginBuy * openVolume); // unknown price - no liquidity - require extra margin
            case PositionDirection.SHORT:
                return profit + ((lastPriceCacheRecord != null && lastPriceCacheRecord.askPrice != long.MaxValue)
                        ? (openPriceSum - openVolume * lastPriceCacheRecord.askPrice)
                        : spec.MarginSell * openVolume); // unknown price - no liquidity - require extra margin
            default:
                throw new InvalidOperationException();
        }
    }

    /**
     * Calculate required margin based on specification and current position/orders
     *
     * @param spec core symbol specification
     * @return required margin
     */
    public long calculateRequiredMarginForFutures(CoreSymbolSpecification spec)
    {
        long specMarginBuy = spec.MarginBuy;
        long specMarginSell = spec.MarginSell;

        long signedPosition = openVolume * (int)direction;
        long currentRiskBuySize = pendingBuySize + signedPosition;
        long currentRiskSellSize = pendingSellSize - signedPosition;

        long marginBuy = specMarginBuy * currentRiskBuySize;
        long marginSell = specMarginSell * currentRiskSellSize;
        // marginBuy or marginSell can be negative, but not both of them
        return Math.Max(marginBuy, marginSell);
    }

    /**
     * Calculate required margin based on specification and current position/orders
     * considering extra size added to current position (or outstanding orders)
     *
     * @param spec   symbols specification
     * @param action order action
     * @param size   order size
     * @return -1 if order will reduce current exposure (no additional margin required), otherwise full margin for symbol position if order placed/executed
     */
    public long calculateRequiredMarginForOrder(CoreSymbolSpecification spec, OrderAction action, long size)
    {
        long specMarginBuy = spec.MarginBuy;
        long specMarginSell = spec.MarginSell;

        long signedPosition = openVolume * (int)direction;
        long currentRiskBuySize = pendingBuySize + signedPosition;
        long currentRiskSellSize = pendingSellSize - signedPosition;

        long marginBuy = specMarginBuy * currentRiskBuySize;
        long marginSell = specMarginSell * currentRiskSellSize;
        // either marginBuy or marginSell can be negative (because of signedPosition), but not both of them
        long currentMargin = Math.Max(marginBuy, marginSell);

        if (action == OrderAction.BID)
        {
            marginBuy += spec.MarginBuy * size;
        }
        else
        {
            marginSell += spec.MarginSell * size;
        }

        // marginBuy or marginSell can be negative, but not both of them
        long newMargin = Math.Max(marginBuy, marginSell);

        return (newMargin <= currentMargin) ? -1 : newMargin;
    }


    /**
     * Update position for one user
     * 1. Un-hold pending size
     * 2. Reduce opposite position accordingly (if exists)
     * 3. Increase forward position accordingly (if size left in the trading event)
     *
     * @param action order action
     * @param size   order size
     * @param price  order price
     * @return opened size
     */
    public long updatePositionForMarginTrade(OrderAction action, long size, long price)
    {

        // 1. Un-hold pending size
        pendingRelease(action, size);

        // 2. Reduce opposite position accordingly (if exists)
        long sizeToOpen = closeCurrentPositionFutures(action, size, price);

        // 3. Increase forward position accordingly (if size left in the trading event)
        if (sizeToOpen > 0)
        {
            openPositionMargin(action, sizeToOpen, price);
        }
        return sizeToOpen;
    }

    private long closeCurrentPositionFutures(OrderAction action, long tradeSize, long tradePrice)
    {

        // log.debug("{} {} {} {} cur:{}-{} profit={}", uid, action, tradeSize, tradePrice, position, totalSize, profit);

        if (direction == PositionDirection.EMPTY || direction == PositionDirectionHelper.of(action))
        {
            // nothing to close
            return tradeSize;
        }

        if (openVolume > tradeSize)
        {
            // current position is bigger than trade size - just reduce position accordingly, don't fix profit
            openVolume -= tradeSize;
            openPriceSum -= tradeSize * tradePrice;
            return 0;
        }

        // current position smaller than trade size, can close completely and calculate profit
        profit += (openVolume * tradePrice - openPriceSum) * (int)direction;
        openPriceSum = 0;
        direction = PositionDirection.EMPTY;
        long sizeToOpen = tradeSize - openVolume;
        openVolume = 0;

        // validateInternalState();

        return sizeToOpen;
    }

    private void openPositionMargin(OrderAction action, long sizeToOpen, long tradePrice)
    {
        openVolume += sizeToOpen;
        openPriceSum += tradePrice * sizeToOpen;
        direction = PositionDirectionHelper.of(action);

        // validateInternalState();
    }

        public void writeMarshallable(IBytesOut bytes)
        {
            bytes.writeInt(symbol);
            bytes.writeInt(currency);
            bytes.writeByte((sbyte)direction);
            bytes.writeLong(openVolume);
            bytes.writeLong(openPriceSum);
            bytes.writeLong(profit);
            bytes.writeLong(pendingSellSize);
            bytes.writeLong(pendingBuySize);
        }

        public void reset()
    {

        // log.debug("records: {}, Pending B{} S{} total size: {}", records.size(), pendingBuySize, pendingSellSize, totalSize);

        pendingBuySize = 0;
        pendingSellSize = 0;

        openVolume = 0;
        openPriceSum = 0;
        direction = PositionDirection.EMPTY;
    }

    public void validateInternalState()
    {
        if (direction == PositionDirection.EMPTY && (openVolume != 0 || openPriceSum != 0))
        {
            log.Error($"uid {uid} : position:{direction} totalSize:{openVolume} openPriceSum:{openPriceSum}");
            throw new InvalidOperationException();
        }
        if (direction != PositionDirection.EMPTY && (openVolume <= 0 || openPriceSum <= 0))
        {
            log.Error($"uid {uid} : position:{direction} totalSize:{openVolume} openPriceSum:{openPriceSum}");
            throw new InvalidOperationException();
        }

        if (pendingSellSize < 0 || pendingBuySize < 0)
        {
            log.Error($"uid {uid} : pendingSellSize:{pendingSellSize} pendingBuySize:{pendingBuySize}");
            throw new InvalidOperationException();
        }
    }

    public int stateHash()
    {
        return (int)(97 * symbol +
                997 * currency +
                9997 * (int)direction +
                99997 * openVolume +
                999997 * openPriceSum +
                9999997 * profit +
                99999997 * pendingSellSize +
                999999997 * pendingBuySize);
    }

    public override string ToString()
    {
        return "SPR{" +
                "u" + uid +
                " sym" + symbol +
                " cur" + currency +
                " pos" + direction +
                " Σv=" + openVolume +
                " Σp=" + openPriceSum +
                " pnl=" + profit +
                " pendingS=" + pendingSellSize +
                " pendingB=" + pendingBuySize +
                '}';
    }
}
}
