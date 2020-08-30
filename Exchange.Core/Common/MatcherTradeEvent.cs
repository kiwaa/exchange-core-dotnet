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

        public static MatcherTradeEvent createEventChain(int chainLength)
        {
            MatcherTradeEvent head = new MatcherTradeEvent();
            MatcherTradeEvent prev = head;
            for (int j = 1; j < chainLength; j++)
            {
                MatcherTradeEvent nextEvent = new MatcherTradeEvent();
                prev.NextEvent = nextEvent;
                prev = nextEvent;
            }
            return head;
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

        // testing only
        public MatcherTradeEvent findTail()
        {
            MatcherTradeEvent tail = this;
            while (tail.NextEvent != null)
            {
                tail = tail.NextEvent;
            }
            return tail;
        }

    }
}
