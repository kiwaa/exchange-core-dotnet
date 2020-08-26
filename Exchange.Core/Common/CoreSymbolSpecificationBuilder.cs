namespace Exchange.Core.Common
{
    public sealed partial class CoreSymbolSpecificationBuilder
    {
    //    public CoreSymbolSpecification(BytesIn bytes)
    //    {
    //        this.symbolId = bytes.readInt();
    //        this.type = SymbolType.of(bytes.readByte());
    //        this.baseCurrency = bytes.readInt();
    //        this.quoteCurrency = bytes.readInt();
    //        this.baseScaleK = bytes.readLong();
    //        this.quoteScaleK = bytes.readLong();
    //        this.takerFee = bytes.readLong();
    //        this.makerFee = bytes.readLong();
    //        this.marginBuy = bytes.readLong();
    //        this.marginSell = bytes.readLong();
    //    }

    //    /* NOT SUPPORTED YET:

    //    //    order book limits -- for FUTURES only
    //    //    public final long highLimit;
    //    //    public final long lowLimit;

    //    //    swaps -- not by
    //    //    public final long longSwap;
    //    //    public final long shortSwap;

    //    // activity (inactive, active, expired)

    //      */

    //    @Override
    //public void writeMarshallable(BytesOut bytes)
    //    {
    //        bytes.writeInt(symbolId);
    //        bytes.writeByte(type.getCode());
    //        bytes.writeInt(baseCurrency);
    //        bytes.writeInt(quoteCurrency);
    //        bytes.writeLong(baseScaleK);
    //        bytes.writeLong(quoteScaleK);
    //        bytes.writeLong(takerFee);
    //        bytes.writeLong(makerFee);
    //        bytes.writeLong(marginBuy);
    //        bytes.writeLong(marginSell);
    //    }

    //    @Override
    //public int stateHash()
    //    {
    //        return Objects.hash(
    //                symbolId,
    //                type.getCode(),
    //                baseCurrency,
    //                quoteCurrency,
    //                baseScaleK,
    //                quoteScaleK,
    //                takerFee,
    //                makerFee,
    //                marginBuy,
    //                marginSell);
    //    }

    //    @Override
    //public boolean equals(Object o)
    //    {
    //        if (this == o) return true;
    //        if (o == null || getClass() != o.getClass()) return false;
    //        CoreSymbolSpecification that = (CoreSymbolSpecification)o;
    //        return symbolId == that.symbolId &&
    //                baseCurrency == that.baseCurrency &&
    //                quoteCurrency == that.quoteCurrency &&
    //                baseScaleK == that.baseScaleK &&
    //                quoteScaleK == that.quoteScaleK &&
    //                takerFee == that.takerFee &&
    //                makerFee == that.makerFee &&
    //                marginBuy == that.marginBuy &&
    //                marginSell == that.marginSell &&
    //                type == that.type;
    //    }
    }
}
