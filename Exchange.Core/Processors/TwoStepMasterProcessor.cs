using Disruptor;
using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public sealed class TwoStepMasterProcessor : IEventProcessor
    {
        private static readonly int IDLE = 0;
        private static readonly int HALTED = IDLE + 1;
        private static readonly int RUNNING = HALTED + 1;

        private static readonly int MASTER_SPIN_LIMIT = 5000;

        private volatile int running = IDLE;
        private readonly IDataProvider<OrderCommand> dataProvider;
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly WaitSpinningHelper<OrderCommand> waitSpinningHelper;
        private readonly Func<long, OrderCommand, bool> eventHandler;
        private readonly IExceptionHandler<OrderCommand> exceptionHandler;
        private readonly String name;
        public ISequence Sequence { get; } = new Sequence(Sequencer.INITIAL_CURSOR_VALUE);

        private TwoStepSlaveProcessor slaveProcessor;

        public TwoStepMasterProcessor(RingBuffer<OrderCommand> ringBuffer,
                                      ISequenceBarrier sequenceBarrier,
                                      Func<long,OrderCommand, bool> eventHandler,
                                      IExceptionHandler<OrderCommand> exceptionHandler,
                                      CoreWaitStrategy coreWaitStrategy,
                                      String name)
        {
            this.dataProvider = ringBuffer;
            this.sequenceBarrier = sequenceBarrier;
            this.waitSpinningHelper = new WaitSpinningHelper<OrderCommand>(ringBuffer, sequenceBarrier, MASTER_SPIN_LIMIT, coreWaitStrategy);
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

                try
                {
                    if (running == RUNNING)
                    {
                        processEvents();
                    }
                }
                finally
                {
                    running = IDLE;
                }
            }
        }

        private void processEvents()
        {

            Thread.CurrentThread.setName("Thread-" + name);

            long nextSequence = Sequence.Value + 1L;

            long currentSequenceGroup = 0;

            // wait until slave processor has instructed to run
            while (!slaveProcessor.IsRunning)
            {
                Thread.Yield();
            }

            while (true)
            {
                OrderCommand cmd = null;
                try
                {

                    // should spin and also check another barrier
                    long availableSequence = waitSpinningHelper.tryWaitFor(nextSequence);

                    if (nextSequence <= availableSequence)
                    {
                        while (nextSequence <= availableSequence)
                        {
                            cmd = dataProvider[nextSequence];

                            // switch to next group - let slave processor start doing its handling cycle
                            if (cmd.eventsGroup != currentSequenceGroup)
                            {
                                publishProgressAndTriggerSlaveProcessor(nextSequence);
                                currentSequenceGroup = cmd.eventsGroup;
                            }

                            bool forcedPublish = eventHandler.onEvent(nextSequence, cmd);
                            nextSequence++;

                            if (forcedPublish)
                            {
                                Sequence.SetValue(nextSequence - 1);
                                waitSpinningHelper.signalAllWhenBlocking();
                            }

                            if (cmd.Command == OrderCommandType.SHUTDOWN_SIGNAL)
                            {
                                // having all sequences aligned with the ringbuffer cursor is a requirement for proper shutdown
                                // let following processors to catch up
                                publishProgressAndTriggerSlaveProcessor(nextSequence);
                            }
                        }
                        Sequence.SetValue(availableSequence);
                        waitSpinningHelper.signalAllWhenBlocking();
                    }
                }
                catch (AlertException ex)
                {
                    if (running != RUNNING)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    exceptionHandler.HandleEventException(ex, nextSequence, cmd);
                    Sequence.SetValue(nextSequence);
                    waitSpinningHelper.signalAllWhenBlocking();
                    nextSequence++;
                }

            }
        }

        private void publishProgressAndTriggerSlaveProcessor(long nextSequence)
        {
            Sequence.SetValue(nextSequence - 1);
            waitSpinningHelper.signalAllWhenBlocking();
            slaveProcessor.handlingCycle(nextSequence);
        }


        public override string ToString()
        {
            return "TwoStepMasterProcessor{" + name + "}";
        }
    }
}
