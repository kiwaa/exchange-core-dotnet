using Disruptor;
using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private readonly object _gate;
        //private readonly Condition processorNotifyCondition;
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
                var field = typeof(MultiProducerSequencer).GetField("_waitStrategy", BindingFlags.NonPublic | BindingFlags.Instance);
                this.blockingDisruptorWaitStrategy = (BlockingWaitStrategy)field.GetValue(sequencer);
                var f2 = typeof(BlockingWaitStrategy).GetField("_gate", BindingFlags.NonPublic | BindingFlags.Instance);
                _gate = f2.GetValue(blockingDisruptorWaitStrategy);
                //this.processorNotifyCondition = ReflectionUtils.extractField(typeof(BlockingWaitStrategy), blockingDisruptorWaitStrategy, "processorNotifyCondition");
            }
            else
            {
                this.blockingDisruptorWaitStrategy = null;
                //this.lock = null;
                //this.processorNotifyCondition = null;
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
                    lock (_gate)
                    {
                        sequenceBarrier.CheckAlert();
                        //    // lock only if sequence barrier did not progressed since last check
                        if (availableSequence == sequenceBarrier.Cursor)
                        {
                            Monitor.Wait(_gate);
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

        private static ISequencer extractSequencer<T>(RingBuffer<T> ringBuffer) where T : class
        {
            try
            {
                FieldInfo f = typeof(RingBuffer<T>).GetField("_sequencerDispatcher", BindingFlags.NonPublic | BindingFlags.Instance);
                //f.setAccessible(true);
                var disp = (SequencerDispatcher)f.GetValue(ringBuffer);
                return disp.Sequencer;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Can not access Disruptor internals: ", e);
            }
        }
    }
}
