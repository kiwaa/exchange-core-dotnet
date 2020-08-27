using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public sealed partial class MatcherTradeEvent
    {
        public MatcherTradeEvent()
        {

        }

        // testing only
        public static List<MatcherTradeEvent> asList(MatcherTradeEvent next)
        {
            List<MatcherTradeEvent> list = new List<MatcherTradeEvent>();
            while (next != null)
            {
                list.Add(next);
                next = next.NextEvent;
            }
            return list;
        }
    }
}
