using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public enum SymbolType
    {
        CURRENCY_EXCHANGE_PAIR = 0,
        FUTURES_CONTRACT = 1,
        OPTION = 2

    //public static SymbolType of(int code)
    //{
    //    return Arrays.stream(values())
    //            .filter(c->c.code == (byte)code)
    //            .findFirst()
    //            .orElseThrow(()-> new IllegalStateException("unknown SymbolType code: " + code));
    //}
    }
}
