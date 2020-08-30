using Exchange.Core.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public sealed class SharedPool
    {

        private readonly BlockingCollection<MatcherTradeEvent> eventChainsBuffer;

        private int chainLength { get; }

        public static SharedPool createTestSharedPool()
        {
            return new SharedPool(8, 4, 256);
        }

        /**
         * Create new shared pool
         *
         * @param poolMaxSize     - max size of pool. Will skip new chains if chains buffer is full.
         * @param poolInitialSize - initial number of pre-generated chains. Recommended to set higher than number of modules - (RE+ME)*2.
         * @param chainLength     - target chain length. Longer chain means rare requests for new chains. However longer chains can cause event placeholders starvation.
         */
        public SharedPool(int poolMaxSize, int poolInitialSize, int chainLength)
        {

            if (poolInitialSize > poolMaxSize)
            {
                throw new InvalidOperationException("too big poolInitialSize");
            }

            this.eventChainsBuffer = new BlockingCollection<MatcherTradeEvent>(poolMaxSize);
            this.chainLength = chainLength;

            for (int i = 0; i < poolInitialSize; i++)
            {
                this.eventChainsBuffer.Add(MatcherTradeEvent.createEventChain(chainLength));
            }
        }

        /**
         * Request next chain from buffer
         * Threadsafe
         *
         * @return chain, otherwise null
         */
        public MatcherTradeEvent getChain()
        {
            //MatcherTradeEvent poll = eventChainsBuffer.poll();
            //        log.debug("<<< POLL CHAIN HEAD  size={}", poll == null ? 0 : poll.getChainSize());
            if (!eventChainsBuffer.TryTake(out MatcherTradeEvent poll))
            {
                poll = MatcherTradeEvent.createEventChain(chainLength);
            }

            return poll;
        }

        /**
         * Offers next chain.
         * Threadsafe (single producer safety is sufficient)
         *
         * @param head - pointer to the first element
         */
        public void putChain(MatcherTradeEvent head)
        {
            bool offer = eventChainsBuffer.TryAdd(head);
            //        log.debug(">>> OFFER CHAIN HEAD  size={} orrder={}", head.getChainSize(), offer);
        }

    }
}
