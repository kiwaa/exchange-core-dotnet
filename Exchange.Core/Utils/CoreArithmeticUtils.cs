using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Utils
{
    public static class CoreArithmeticUtils
    {
        public static long calculateAmountAsk(long size, CoreSymbolSpecification spec)
        {
            return size * spec.BaseScaleK;
        }

        public static long calculateAmountBid(long size, long price, CoreSymbolSpecification spec)
        {
            return size * (price * spec.QuoteScaleK);
        }

        public static long calculateAmountBidTakerFee(long size, long price, CoreSymbolSpecification spec)
        {
            return size * (price * spec.QuoteScaleK + spec.TakerFee);
        }

        public static long calculateAmountBidReleaseCorrMaker(long size, long priceDiff, CoreSymbolSpecification spec)
        {
            return size * (priceDiff * spec.QuoteScaleK + (spec.TakerFee - spec.MakerFee));
        }

        public static long calculateAmountBidTakerFeeForBudget(long size, long budgetInSteps, CoreSymbolSpecification spec)
        {

            return budgetInSteps * spec.QuoteScaleK + size * spec.TakerFee;
        }

    }
}
