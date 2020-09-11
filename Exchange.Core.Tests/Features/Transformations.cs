using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Exchange.Core.Tests.Features
{
    [Binding]
    class Transformations
    {
        static Dictionary<String, long> users;
        static Dictionary<String, CoreSymbolSpecification> symbolSpecificationMap;
        static Transformations()
        {
            users = new Dictionary<String, long>();
            users["Alice"] = 1440001L;
            users["Bob"]= 1440002L;
            users["Charlie"] = 1440003L;

            symbolSpecificationMap = new Dictionary<String, CoreSymbolSpecification>();
            symbolSpecificationMap["EUR_USD"] = TestConstants.SYMBOLSPEC_EUR_USD;
            symbolSpecificationMap["ETH_XBT"] = TestConstants.SYMBOLSPEC_ETH_XBT;
        }
        [StepArgumentTransformation("(Alice|Bob|Charlie)")]
        public long GetUser(string user)
        {
            return users[user];
         }
        
        [StepArgumentTransformation("(EUR_USD|ETH_XBT)")]
        public CoreSymbolSpecification GetSymbol(string symbol)
        {
            return symbolSpecificationMap[symbol];
        }

        [StepArgumentTransformation()]
        public L2MarketDataHelper l2MarketData(Table table)
        {
            var tmp = new L2MarketDataHelper();
            if (table.Header.First()?.Trim() != "bid")
                AddL2MarketData(tmp, table.Header.ToArray());
            foreach (var row in table.Rows)
            {
                var values = row.Values.ToArray();
                AddL2MarketData(tmp, values);
            }
            return tmp;
        }

        [StepArgumentTransformation()]
        public List<List<string>> ToList(Table table)
        {
            var h = table.Header.ToList();
            var v = table.Rows.Select(x => x.Values.ToList()).ToList();
            v.Insert(0, h);
            return v;
        }

        private void AddL2MarketData(L2MarketDataHelper tmp, string[] values)
        {
            var bid = !string.IsNullOrEmpty(values[0]);
            var price = int.Parse(values[1]);
            var count = bid ? int.Parse(values[0]) : int.Parse(values[2]);
            if (bid)
                tmp.addBid(price, count);
            else
                tmp.addAsk(price, count);
        }
    }
}
