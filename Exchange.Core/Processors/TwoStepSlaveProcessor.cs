using Disruptor;
using Exchange.Core.Common;
using Exchange.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public sealed class TwoStepSlaveProcessor : IEventProcessor
    {
        private static readonly int IDLE = 0;
        private static readonly int HALTED = IDLE + 1;
        private static readonly int RUNNING = HALTED + 1;

        private volatile int running = IDLE;
        private readonly IDataProvider<OrderCommand> dataProvider;
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly WaitSpinningHelper<OrderCommand> waitSpinningHelper;
        private readonly Func<long, OrderCommand, bool> eventHandler;
        public ISequence Sequence { get; } = new Sequence(Sequencer.INITIAL_CURSOR_VALUE);
        private readonly IExceptionHandler<OrderCommand> exceptionHandler;
        private readonly String name;

        private long nextSequence = -1;

        public TwoStepSlaveProcessor(RingBuffer<OrderCommand> ringBuffer,
                                     ISequenceBarrier sequenceBarrier,
                                    Func<long, OrderCommand, bool> eventHandler,
                                     IExceptionHandler<OrderCommand> exceptionHandler,
                                     String name)
        {
            this.dataProvider = ringBuffer;
            this.sequenceBarrier = sequenceBarrier;
            this.waitSpinningHelper = new WaitSpinningHelper<OrderCommand>(ringBuffer, sequenceBarrier, 0, CoreWaitStrategy.SECOND_STEP_NO_WAIT);
            this.eventHandler = eventHandler;
            this.exceptionHandler = exceptionHandler;
            this.name = name;
        }

        public void Halt()
        {
            running = HALTED;
            sequenceBarrier.Alert();
        }

        public bool IsRunning => running != IDLE;

        /**
         * It is ok to have another thread rerun this method after a halt().
         *
         * @throws IllegalStateException if this object instance is already running in a thread
         */
        public void Run()
        {
            if (Interlocked.CompareExchange(ref running, IDLE, RUNNING) != RUNNING)
            {
                sequenceBarrier.ClearAlert();
            }
            else if (running == RUNNING)
            {
                throw new InvalidOperationException("Thread is already running (S)");
            }

            nextSequence = Sequence.Value + 1L;
        }

        public void handlingCycle(long processUpToSequence)
        {
            while (true)
            {
                OrderCommand evnt = null;
                try
                {
                    long availableSequence = waitSpinningHelper.tryWaitFor(nextSequence);

                    // process batch
                    while (nextSequence <= availableSequence && nextSequence < processUpToSequence)
                    {
                        evnt = dataProvider[nextSequence];
                        eventHandler(nextSequence, evnt); // TODO check if nextSequence is correct (not nextSequence+-1)?
                        nextSequence++;
                    }

                    // exit if finished processing entire group (up to specified sequence)
                    if (nextSequence == processUpToSequence)
                    {
                        Sequence.SetValue(processUpToSequence - 1);
                        waitSpinningHelper.signalAllWhenBlocking();
                        return;
                    }

                }
                catch (Exception ex)
                {
                    exceptionHandler.HandleEventException(ex, nextSequence, evnt);
                    Sequence.SetValue(nextSequence);
                    waitSpinningHelper.signalAllWhenBlocking();
                    nextSequence++;
                }
            }
        }

        public override string ToString()
        {
            return "TwoStepSlaveProcessor{" + name + "}";
        }

    }
}
