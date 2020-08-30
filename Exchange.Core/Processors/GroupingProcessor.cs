using Disruptor;
using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public sealed class GroupingProcessor : IEventProcessor
    {
        private static readonly int IDLE = 0;
        private static readonly int HALTED = IDLE + 1;
        private static readonly int RUNNING = HALTED + 1;

        private static readonly int GROUP_SPIN_LIMIT = 1000;

        // TODO move into configuration
        private static readonly int L2_PUBLISH_INTERVAL_NS = 10_000_000;

        private volatile int running = IDLE;
        private readonly RingBuffer<OrderCommand> ringBuffer;
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly WaitSpinningHelper<OrderCommand> waitSpinningHelper;
        private ISequence Sequence { get; } = new Sequence(Sequencer.INITIAL_CURSOR_VALUE);

        private readonly SharedPool sharedPool;

        private readonly int msgsInGroupLimit;
        private readonly long maxGroupDurationNs;

        public GroupingProcessor(RingBuffer<OrderCommand> ringBuffer,
                                 ISequenceBarrier sequenceBarrier,
                                 PerformanceConfiguration perfCfg,
                                 CoreWaitStrategy coreWaitStrategy,
                                 SharedPool sharedPool)
        {

            if (perfCfg.MsgsInGroupLimit > perfCfg.RingBufferSize / 4)
            {
                throw new InvalidOperationException("msgsInGroupLimit should be less than quarter ringBufferSize");
            }

            this.ringBuffer = ringBuffer;
            this.sequenceBarrier = sequenceBarrier;
            this.waitSpinningHelper = new WaitSpinningHelper<OrderCommand>(ringBuffer, sequenceBarrier, GROUP_SPIN_LIMIT, coreWaitStrategy);
            this.msgsInGroupLimit = perfCfg.MsgsInGroupLimit;
            this.maxGroupDurationNs = perfCfg.MaxGroupDurationNs;
            this.sharedPool = sharedPool;
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
            else
            {
                // This is a little bit of guess work.  The running state could of changed to HALTED by
                // this point.  However, Java does not have compareAndExchange which is the only way
                // to get it exactly correct.
                if (running == RUNNING)
                {
                    throw new InvalidOperationException("Thread is already running");
                }
            }
        }

        private void processEvents()
        {
            long nextSequence = Sequence.Value + 1L;

            long groupCounter = 0;
            long msgsInGroup = 0;

            long groupLastNs = 0;

            long l2dataLastNs = 0;
            bool triggerL2DataRequest = false;

            int tradeEventChainLengthTarget = sharedPool.getChainLength();
            MatcherTradeEvent tradeEventHead = null;
            MatcherTradeEvent tradeEventTail = null;
            int tradeEventCounter = 0; // counter

            bool groupingEnabled = true;

            while (true)
            {
                try
                {

                    // should spin and also check another barrier
                    long availableSequence = waitSpinningHelper.tryWaitFor(nextSequence);

                    if (nextSequence <= availableSequence)
                    {
                        while (nextSequence <= availableSequence)
                        {

                            OrderCommand cmd = ringBuffer.get(nextSequence);

                            nextSequence++;

                            if (cmd.Command == OrderCommandType.GROUPING_CONTROL)
                            {
                                groupingEnabled = cmd.OrderId == 1;
                                cmd.ResultCode = CommandResultCode.SUCCESS;
                            }

                            if (!groupingEnabled)
                            {
                                // TODO pooling
                                cmd.MatcherEvent = null;
                                cmd.MarketData = null;
                                continue;
                            }

                            // some commands should trigger R2 stage to avoid unprocessed events that could affect accounting state
                            if (cmd.Command == OrderCommandType.RESET
                                    || cmd.Command == OrderCommandType.PERSIST_STATE_MATCHING
                                    || cmd.Command == OrderCommandType.GROUPING_CONTROL)
                            {
                                groupCounter++;
                                msgsInGroup = 0;
                            }

                            // report/binary commands also should trigger R2 stage, but only for last message
                            if ((cmd.Command == OrderCommandType.BINARY_DATA_COMMAND || cmd.Command == OrderCommandType.BINARY_DATA_QUERY) && cmd.Symbol == -1)
                            {
                                groupCounter++;
                                msgsInGroup = 0;
                            }

                            cmd.eventsGroup = groupCounter;


                            if (triggerL2DataRequest)
                            {
                                triggerL2DataRequest = false;
                                cmd.serviceFlags = 1;
                            }
                            else
                            {
                                cmd.serviceFlags = 0;
                            }

                            // cleaning attached events
                            if (EVENTS_POOLING && cmd.MatcherEvent != null)
                            {

                                // update tail
                                if (tradeEventTail == null)
                                {
                                    tradeEventHead = cmd.MatcherEvent; //?
                                }
                                else
                                {
                                    tradeEventTail.NextEvent = cmd.MatcherEvent;
                                }

                                tradeEventTail = cmd.MatcherEvent;
                                tradeEventCounter++;

                                // find last element in the chain and update tail accordingly
                                while (tradeEventTail.NextEvent != null)
                                {
                                    tradeEventTail = tradeEventTail.NextEvent;
                                    tradeEventCounter++;
                                }

                                if (tradeEventCounter >= tradeEventChainLengthTarget)
                                {
                                    // chain is big enough -> send to the shared pool
                                    tradeEventCounter = 0;
                                    sharedPool.putChain(tradeEventHead);
                                    tradeEventTail = null;
                                    tradeEventHead = null;
                                }

                            }
                            cmd.MatcherEvent = null;

                            // TODO collect to shared buffer
                            cmd.MarketData = null;

                            msgsInGroup++;

                            // switch group after each N messages
                            // avoid changing groups when PERSIST_STATE_MATCHING is already executing
                            if (msgsInGroup >= msgsInGroupLimit && cmd.Command != OrderCommandType.PERSIST_STATE_RISK)
                            {
                                groupCounter++;
                                msgsInGroup = 0;
                            }

                        }
                        Sequence.SetValue(availableSequence);
                        waitSpinningHelper.signalAllWhenBlocking();
                        groupLastNs = System.nanoTime() + maxGroupDurationNs;

                    }
                    else
                    {
                        long t = System.nanoTime();
                        if (msgsInGroup > 0 && t > groupLastNs)
                        {
                            // switch group after T microseconds elapsed, if group is non empty
                            groupCounter++;
                            msgsInGroup = 0;
                        }

                        if (t > l2dataLastNs)
                        {
                            // TODO fix order best price updating mechanism,
                            //  this does not work for multi-symbol configuration

                            l2dataLastNs = t + L2_PUBLISH_INTERVAL_NS; // trigger L2 data every 10ms
                            triggerL2DataRequest = true;
                        }
                    }

                }
                catch (AlertException ex) {
                    if (running != RUNNING)
                    {
                        break;
                    }
                } catch (Exception ex) {
                    Sequence.SetValue(nextSequence);
                    waitSpinningHelper.signalAllWhenBlocking();
                    nextSequence++;
                }
                }
            }

        public override string ToString()
            {
                return "GroupingProcessor{" +
                        "GL=" + msgsInGroupLimit +
                        '}';
            }
        }
    }
