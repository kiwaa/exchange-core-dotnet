using System;

namespace Exchange.Core.Common.Api
{
    public sealed partial class ApiPlaceOrder : ApiCommand
    {
        public override string ToString()
        {
            return "[ADD o" + OrderId + " s" + Symbol + " u" + Uid + " " + (Action == OrderAction.ASK ? 'A' : 'B')
                    + ":" + (OrderType == OrderType.IOC ? "IOC" : "GTC")
                    + ":" + Price + ":" + Size + "]";
            //(reservePrice != 0 ? ("(R" + reservePrice + ")") : "") +
        }
    }
}