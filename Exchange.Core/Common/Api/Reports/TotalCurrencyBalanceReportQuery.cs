using Exchange.Core.Processors;
using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System.Collections.Generic;
using System.Linq;

namespace Exchange.Core.Common.Api.Reports
{
    public class TotalCurrencyBalanceReportQuery : IReportQuery<TotalCurrencyBalanceReportResult>
    {
        public TotalCurrencyBalanceReportQuery()
        {
            // do nothing
        }
        public TotalCurrencyBalanceReportQuery(IBytesIn bytesIn)
        {
            // do nothing
        }

        public TotalCurrencyBalanceReportResult createResult(IEnumerable<IBytesIn> sections)
        {
            return TotalCurrencyBalanceReportResult.merge(sections);
        }

        public int getReportTypeCode()
        {
            return (int)ReportType.TOTAL_CURRENCY_BALANCE;
        }

        public TotalCurrencyBalanceReportResult process(MatchingEngineRouter matchingEngine)
        {
            Dictionary<int,long> currencyBalance = new Dictionary<int, long>();

            foreach (var ob in matchingEngine.orderBooks.Where(ob=>ob.Value.getSymbolSpec().Type == SymbolType.CURRENCY_EXCHANGE_PAIR))
            {
                CoreSymbolSpecification spec = ob.Value.getSymbolSpec();

                currencyBalance.AddValue(
                        spec.BaseCurrency,
                        ob.Value.askOrdersStream(false).Select(ord=>CoreArithmeticUtils.calculateAmountAsk(ord.Size - ord.Filled, spec)).Sum());

                currencyBalance.AddValue(
                        spec.QuoteCurrency,
                        ob.Value.bidOrdersStream(false).Select(ord=>CoreArithmeticUtils.calculateAmountBidTakerFee(ord.Size - ord.Filled, ord.ReserveBidPrice, spec)).Sum());

            }

            return TotalCurrencyBalanceReportResult.ofOrderBalances(currencyBalance);
        }

        public TotalCurrencyBalanceReportResult process(RiskEngine riskEngine)
        {
            // prepare fast price cache for profit estimation with some price (exact value is not important, except ask==bid condition)
            Dictionary<int, LastPriceCacheRecord> dummyLastPriceCache = new Dictionary<int, LastPriceCacheRecord>();
            foreach (var pair in riskEngine.lastPriceCache)
            {
                dummyLastPriceCache[pair.Key] = pair.Value.averagingRecord();
            }

            Dictionary<int, long> currencyBalance = new Dictionary<int, long>();

            Dictionary<int, long> symbolOpenInterestLong = new Dictionary<int, long>();
            Dictionary<int, long> symbolOpenInterestShort = new Dictionary<int, long>();

            SymbolSpecificationProvider symbolSpecificationProvider = riskEngine.symbolSpecificationProvider;

            foreach (var pair in riskEngine.userProfileService.userProfiles)
            {
                var userProfile = pair.Value;

                foreach (var acc in userProfile.accounts)
                {
                    currencyBalance.AddValue(acc.Key, acc.Value);
                }
                foreach (var (symbolId, positionRecord) in userProfile.positions)
                {
                    CoreSymbolSpecification spec = symbolSpecificationProvider.getSymbolSpecification(symbolId);
//                    LastPriceCacheRecord avgPrice = dummyLastPriceCache.getIfAbsentPut(symbolId, LastPriceCacheRecord.dummy);
                    if (!dummyLastPriceCache.TryGetValue(symbolId, out LastPriceCacheRecord avgPrice))
                    {
                        dummyLastPriceCache[symbolId] = avgPrice = LastPriceCacheRecord.dummy;
                    }
                    currencyBalance.AddValue(positionRecord.currency, positionRecord.estimateProfit(spec, avgPrice));

                    if (positionRecord.direction == PositionDirection.LONG)
                    {
                        symbolOpenInterestLong.AddValue(symbolId, positionRecord.openVolume);
                    }
                    else if (positionRecord.direction == PositionDirection.SHORT)
                    {
                        symbolOpenInterestShort.AddValue(symbolId, positionRecord.openVolume);
                    }

                }
            }

            return new TotalCurrencyBalanceReportResult(
                            currencyBalance,
                            new Dictionary<int, long>(riskEngine.fees),
                            new Dictionary<int, long>(riskEngine.adjustments),
                            new Dictionary<int, long>(riskEngine.suspends),
                            null,
                            symbolOpenInterestLong,
                            symbolOpenInterestShort);
        }

        public void writeMarshallable(IBytesOut bytes)
        {
            // do nothing
        }
    }
}