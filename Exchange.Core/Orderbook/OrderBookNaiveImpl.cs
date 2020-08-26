using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Orderbook
{
    public sealed class OrderBookNaiveImpl : IOrderBook
    {
        public CommandResultCode cancelOrder(OrderCommand cmd)
        {
            throw new NotImplementedException();
        }

        public void fillAsks(int size, L2MarketData data)
        {
            throw new NotImplementedException();
        }

        public void fillBids(int size, L2MarketData data)
        {
            throw new NotImplementedException();
        }

        public List<Order> findUserOrders(long uid)
        {
            throw new NotImplementedException();
        }

        public OrderBookImplType getImplementationType()
        {
            throw new NotImplementedException();
        }

        public IOrder getOrderById(long orderId)
        {
            throw new NotImplementedException();
        }

        public int getOrdersNum(OrderAction action)
        {
            throw new NotImplementedException();
        }

        public CoreSymbolSpecification getSymbolSpec()
        {
            throw new NotImplementedException();
        }

        public int getTotalAskBuckets(int limit)
        {
            throw new NotImplementedException();
        }

        public int getTotalBidBuckets(int limit)
        {
            throw new NotImplementedException();
        }

        public long getTotalOrdersVolume(OrderAction action)
        {
            throw new NotImplementedException();
        }

        public CommandResultCode moveOrder(OrderCommand cmd)
        {
            throw new NotImplementedException();
        }

        public void newOrder(OrderCommand cmd)
        {
            throw new NotImplementedException();
        }

        public CommandResultCode reduceOrder(OrderCommand cmd)
        {
            throw new NotImplementedException();
        }

        public void validateInternalState()
        {
            throw new NotImplementedException();
        }
    }
}
