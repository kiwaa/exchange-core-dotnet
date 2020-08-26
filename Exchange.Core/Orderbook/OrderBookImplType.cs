using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Orderbook
{
    public enum OrderBookImplType
    {
        NAIVE = 0,
        DIRECT = 2

        //public static OrderBookImplType of(byte code)
        //{
        //    switch (code)
        //    {
        //        case 0:
        //            return NAIVE;
        //        case 2:
        //            return DIRECT;
        //        default:
        //            throw new IllegalArgumentException("unknown OrderBookImplType:" + code);
        //    }
        //}
    }
}
