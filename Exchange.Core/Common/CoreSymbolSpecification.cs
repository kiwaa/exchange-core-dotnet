using OpenHFT.Chronicle.WireMock;

namespace Exchange.Core.Common
{
    public sealed partial class CoreSymbolSpecification : IStateHash
    {
        public CoreSymbolSpecification(IBytesIn bytes)
        {
            this.SymbolId = bytes.readInt();
            this.Type = (SymbolType)bytes.readByte();
            this.BaseCurrency = bytes.readInt();
            this.QuoteCurrency = bytes.readInt();
            this.BaseScaleK = bytes.readLong();
            this.QuoteScaleK = bytes.readLong();
            this.TakerFee = bytes.readLong();
            this.MakerFee = bytes.readLong();
            this.MarginBuy = bytes.readLong();
            this.MarginSell = bytes.readLong();
        }

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

        public int stateHash()
        {
            return (int)(97 * SymbolId +
                    997 * (int)Type +
                    9997 * BaseCurrency +
                    99997 * QuoteCurrency +
                    999997 * BaseScaleK +
                    9999997 * QuoteScaleK +
                    99999997 * TakerFee +
                    999999997 * MakerFee +
                    9999999997 * MarginBuy +
                    99999999997 * MarginSell);
        }

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
