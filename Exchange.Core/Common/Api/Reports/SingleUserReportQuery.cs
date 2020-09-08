using Exchange.Core.Processors;
using OpenHFT.Chronicle.WireMock;
using System.Collections.Generic;
using System.Linq;

namespace Exchange.Core.Common.Api.Reports
{
    public sealed partial class SingleUserReportQuery : IReportQuery<SingleUserReportResult>
    {
        private long uid;

        public SingleUserReportQuery(long uid)
        {
            this.uid = uid;
        }
        public SingleUserReportQuery(IBytesIn bytesIn)
        {
            this.uid = bytesIn.readLong();
        }

        public SingleUserReportResult createResult(IEnumerable<IBytesIn> sections)
        {
            return SingleUserReportResult.merge(sections);
        }


        public int getReportTypeCode()
        {
            return (int)ReportType.SINGLE_USER_REPORT;
        }

        public SingleUserReportResult process(MatchingEngineRouter matchingEngine)
        {
            Dictionary<int, List<Order>> orders = new Dictionary<int, List<Order>>();

            foreach (var ob in matchingEngine.orderBooks)
            {
                List<Order> userOrders = ob.Value.findUserOrders(this.uid);
                // dont put empty results, so that the report result merge procedure would be simple
                if (userOrders.Any())
                {
                    orders[ob.Value.getSymbolSpec().SymbolId] = userOrders;
                }
            }

            //log.debug("ME{}: orders: {}", matchingEngine.getShardId(), orders);
            return SingleUserReportResult.createFromMatchingEngine(uid, orders);
        }

        public SingleUserReportResult process(RiskEngine riskEngine)
        {
            if (!riskEngine.uidForThisHandler(this.uid))
            {
                return null;
            }
            UserProfile userProfile = riskEngine.userProfileService.getUserProfile(this.uid);

            if (userProfile != null)
            {
                Dictionary<int, Position> positions = new Dictionary<int, Position>(userProfile.positions.Count);
                foreach (var (symbol, pos) in userProfile.positions)
                {
                    positions[symbol] = new Position(
                            pos.currency,
                            pos.direction,
                            pos.openVolume,
                            pos.openPriceSum,
                            pos.profit,
                            pos.pendingSellSize,
                            pos.pendingBuySize);
                }

                return SingleUserReportResult.createFromRiskEngineFound(
                        uid,
                        userProfile.userStatus,
                        userProfile.accounts,
                        positions);
            }
            else
            {
                // not found
                return SingleUserReportResult.createFromRiskEngineNotFound(uid);
            }
        }

        public void writeMarshallable(IBytesOut bytes)
        {
            bytes.writeLong(uid);
        }
    }
}