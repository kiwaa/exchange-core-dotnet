using Exchange.Core.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Utils
{
    public static class PreFillMode
    {
        public static readonly Func<TestOrdersGeneratorConfig, int> ORDERS_NUMBER = (config) => config.TargetOrderBookOrdersTotal;
        public static readonly Func<TestOrdersGeneratorConfig, int> ORDERS_NUMBER_PLUS_QUARTER = (config) => config.TargetOrderBookOrdersTotal * 5 / 4;

        //ORDERS_NUMBER = (TestOrdersGeneratorConfig::getTargetOrderBookOrdersTotal),
        //ORDERS_NUMBER_PLUS_QUARTER = (config -> config.targetOrderBookOrdersTotal * 5 / 4); // used for snapshot tests to let some margin positions open

        //final Function<TestOrdersGeneratorConfig, Integer> calculateReadySeqFunc;
    }
}
