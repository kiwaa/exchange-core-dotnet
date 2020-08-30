using Disruptor;
using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public sealed class WaitSpinningHelper<T> where T : class
    {

        private readonly ISequenceBarrier sequenceBarrier;
        private readonly ISequencer sequencer;

        private readonly int spinLimit;
        private readonly int yieldLimit;

        // blocking mode, using same locking objects that Disruptor operates with
        private readonly bool block;
        private readonly BlockingWaitStrategy blockingDisruptorWaitStrategy;
        //private readonly Lock lock;
        private readonly Condition processorNotifyCondition;
        // next Disruptor release will have mutex (to avoid allocations)
        // private final Object mutex;

        public WaitSpinningHelper(RingBuffer<T> ringBuffer, ISequenceBarrier sequenceBarrier, int spinLimit, CoreWaitStrategy waitStrategy)
        {
            this.sequenceBarrier = sequenceBarrier;
            this.spinLimit = spinLimit;
            this.sequencer = extractSequencer(ringBuffer);
            this.yieldLimit = CoreWaitStrategyHelper.IsYield(waitStrategy) ? spinLimit / 2 : 0;

            this.block = CoreWaitStrategyHelper.IsBlock(waitStrategy);
            if (block)
            {
                this.blockingDisruptorWaitStrategy = ReflectionUtils.extractField(typeof(AbstractSequencer), (AbstractSequencer)sequencer, "waitStrategy");
                //this.lock = ReflectionUtils.extractField(BlockingWaitStrategy.class, blockingDisruptorWaitStrategy, "lock");
                this.processorNotifyCondition = ReflectionUtils.extractField(typeof(BlockingWaitStrategy), blockingDisruptorWaitStrategy, "processorNotifyCondition");
            }
            else
            {
                this.blockingDisruptorWaitStrategy = null;
                //this.lock = null;
                this.processorNotifyCondition = null;
            }
        }

        public long tryWaitFor(long seq)
        {
            sequenceBarrier.CheckAlert();

            long spin = spinLimit;
            long availableSequence;
            while ((availableSequence = sequenceBarrier.Cursor) < seq && spin > 0)
            {
                if (spin < yieldLimit && spin > 1)
                {
                    Thread.Yield();
                }
                else if (block)
                {
                    /*
                                    synchronized (mutex) {
                                        sequenceBarrier.checkAlert();
                                        mutex.wait();
                                    }
                    */
                    lock (_lock)
                    {
                        sequenceBarrier.CheckAlert();
                        // lock only if sequence barrier did not progressed since last check
                        if (availableSequence == sequenceBarrier.Cursor)
                        {
                            processorNotifyCondition.await();
                        }
                    }
                }

                spin--;
            }

            return (availableSequence < seq)
                    ? availableSequence
                    : sequencer.GetHighestPublishedSequence(seq, availableSequence);
        }

        public void signalAllWhenBlocking()
        {
            if (block)
            {
                blockingDisruptorWaitStrategy.SignalAllWhenBlocking();
            }
        }

        private static Sequencer extractSequencer<T>(RingBuffer<T> ringBuffer) where T : class
        {
            try
            {
                Field f = ReflectionUtils.getField(typeof(RingBuffer), "sequencer");
                f.setAccessible(true);
                return (Sequencer)f.get(ringBuffer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Can not access Disruptor internals: ", e);
            }
        }
    }
}
