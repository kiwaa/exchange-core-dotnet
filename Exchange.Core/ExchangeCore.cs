using Disruptor;
using Disruptor.Dsl;
using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using Exchange.Core.Processors;
using Exchange.Core.Processors.Journaling;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core
{
    public sealed class ExchangeCore
    {
        private static ILog log = LogManager.GetLogger(typeof(ExchangeCore));

        private readonly Disruptor<OrderCommand> disruptor;

        private readonly ExchangeApi api;

        private readonly ISerializationProcessor serializationProcessor;

        private readonly ExchangeConfiguration exchangeConfiguration;

        // core can be started and stopped only once
        private bool started = false;
        private bool stopped = false;

        // enable MatcherTradeEvent pooling
        public static readonly bool EVENTS_POOLING = false;

        /**
         * Exchange core constructor.
         *
         * @param resultsConsumer       - custom consumer of processed commands
         * @param exchangeConfiguration - exchange configuration
         */
        public ExchangeCore(Action<OrderCommand, long> resultsConsumer,
                            ExchangeConfiguration exchangeConfiguration)
        {

            log.Debug($"Building exchange core from configuration: {exchangeConfiguration}");

            this.exchangeConfiguration = exchangeConfiguration;

            PerformanceConfiguration perfCfg = exchangeConfiguration.PerformanceCfg;

            int ringBufferSize = perfCfg.RingBufferSize;

            this.disruptor = new Disruptor<OrderCommand>(
                    () => new OrderCommand(),
                    ringBufferSize,
                    perfCfg.TaskScheduler,
                    ProducerType.Multi, // multiple gateway threads are writing
                    CoreWaitStrategyHelper.GetDisruptorWaitStrategy(perfCfg.WaitStrategy));

            this.api = new ExchangeApi(disruptor.RingBuffer, perfCfg.BinaryCommandsLz4CompressorFactory());

            TaskScheduler threadFactory = perfCfg.TaskScheduler;
            Func<CoreSymbolSpecification, ObjectsPool, OrderBookEventsHelper, LoggingConfiguration, IOrderBook> orderBookFactory = perfCfg.OrderBookFactory;
            CoreWaitStrategy coreWaitStrategy = perfCfg.WaitStrategy;

            int matchingEnginesNum = perfCfg.MatchingEnginesNum;
            int riskEnginesNum = perfCfg.RiskEnginesNum;

            SerializationConfiguration serializationCfg = exchangeConfiguration.SerializationCfg;

            // creating serialization processor
            serializationProcessor = serializationCfg.SerializationProcessorFactory(exchangeConfiguration);

            // creating shared objects pool
            int poolInitialSize = (matchingEnginesNum + riskEnginesNum) * 8;
            int chainLength = EVENTS_POOLING ? 1024 : 1;
            SharedPool sharedPool = new SharedPool(poolInitialSize * 4, poolInitialSize, chainLength);

            // creating and attaching exceptions handler
            var exceptionHandler = new DisruptorExceptionHandler("main", disruptor);

            disruptor.SetDefaultExceptionHandler(exceptionHandler);

            // advice completable future to use the same CPU socket as disruptor
            //ExecutorService loaderExecutor = Executors.newFixedThreadPool(matchingEnginesNum + riskEnginesNum, threadFactory);

            // start creating matching engines
            Dictionary<int, MatchingEngineRouter> matchingEngineFutures = Enumerable.Range(0, matchingEnginesNum)
                .ToDictionary(shardId => shardId,
                              shardId => new MatchingEngineRouter(shardId, matchingEnginesNum, serializationProcessor, orderBookFactory, sharedPool, exchangeConfiguration));
            // TODO create processors in same thread we will execute it??

            // start creating risk engines
            Dictionary<int, RiskEngine> riskEngines = Enumerable.Range(0, riskEnginesNum)
                .ToDictionary(shardId => shardId,
                              shardId => new RiskEngine(shardId, riskEnginesNum, serializationProcessor, sharedPool, exchangeConfiguration));

            IEventHandler<OrderCommand>[] matchingEngineHandlers = matchingEngineFutures.Values
                .Select(mer => new MatchingEngineProcessor(mer))
                .ToArray();


            List<TwoStepMasterProcessor> procR1 = new List<TwoStepMasterProcessor>(riskEnginesNum);
            List<TwoStepSlaveProcessor> procR2 = new List<TwoStepSlaveProcessor>(riskEnginesNum);

            // 1. grouping processor (G)
            EventHandlerGroup<OrderCommand> afterGrouping =
                    disruptor.HandleEventsWith(new GroupingProcessorFactory(perfCfg, coreWaitStrategy, sharedPool));

            // 2. [journaling (J)] in parallel with risk hold (R1) + matching engine (ME)

            bool enableJournaling = serializationCfg.EnableJournaling;
            IEventHandler<OrderCommand> jh = enableJournaling ? new JournalingProcessor(serializationProcessor) : null;

            if (enableJournaling)
            {
                afterGrouping.HandleEventsWith(jh);
            }

            var reList = riskEngines.Values.ToList();
            for (int i = 0; i < reList.Count; i++)
            {
                afterGrouping.HandleEventsWith(new TwoStepMasterProcessorFactory(procR1, reList[i], exceptionHandler, coreWaitStrategy, "R1_" + i));
            }

            disruptor.After(procR1.ToArray()).HandleEventsWith(matchingEngineHandlers);

            // 3. risk release (R2) after matching engine (ME)
            EventHandlerGroup<OrderCommand> afterMatchingEngine = disruptor.After(matchingEngineHandlers);
            for (int i = 0; i < reList.Count; i++)
            {
                afterMatchingEngine.HandleEventsWith(new TwoStepSlaveProcessorFactory(procR2, reList[i], exceptionHandler, "R2_" + i));
            }

            // 4. results handler (E) after matching engine (ME) + [journaling (J)]
            EventHandlerGroup<OrderCommand> mainHandlerGroup = enableJournaling
                    ? disruptor.After(arraysAddHandler(matchingEngineHandlers, jh))
                    : afterMatchingEngine;

            ResultsHandler resultsHandler = new ResultsHandler(resultsConsumer);

            mainHandlerGroup.HandleEventsWith(new ResultsProcessor(resultsHandler, api));

            // attach slave processors to master processor
            foreach (var i in Enumerable.Range(0, riskEnginesNum))
                procR1[i].slaveProcessor = procR2[i];

            //try
            //{
            //    loaderExecutor.shutdown();
            //    loaderExecutor.awaitTermination(1, TimeUnit.SECONDS);
            //}
            //catch (Exception ex)
            //{
            //    throw new RuntimeException(ex);
            //}
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void startup()
        {
            if (!started)
            {
                log.Debug("Starting disruptor...");
                disruptor.Start();
                started = true;

                serializationProcessor.replayJournalFullAndThenEnableJouraling(exchangeConfiguration.InitStateCfg, api);
            }
        }

        /**
         * Provides ExchangeApi instance.
         *
         * @return ExchangeApi instance (always same object)
         */
        public ExchangeApi getApi()
        {
            return api;
        }

        /**
         * shut down disruptor
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void shutdown()
        {
            shutdown(Timeout.InfiniteTimeSpan);
        }

        /**
         * Will throw IllegalStateException if an exchange core can not stop gracefully.
         *
         * @param timeout  the amount of time to wait for all events to be processed. <code>-1</code> will give an infinite timeout
         * @param timeUnit the unit the timeOut is specified in
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void shutdown(TimeSpan timeout)
        {
            if (!stopped)
            {
                stopped = true;
                // TODO stop accepting new events first
                try
                {
                    log.Info("Shutdown disruptor...");
                    disruptor.RingBuffer.PublishEvent(ShutdownSignalTranlator.Instance);
                    disruptor.Shutdown(timeout);
                    log.Info("Disruptor stopped");
                }
                catch (TimeoutException e)
                {
                    throw new InvalidOperationException("could not stop a disruptor gracefully. Not all events may be executed.");
                }
            }
        }

        private static IEventHandler<OrderCommand>[] arraysAddHandler(IEventHandler<OrderCommand>[] handlers, IEventHandler<OrderCommand> extraHandler)
        {
            IEventHandler<OrderCommand>[] result = new IEventHandler<OrderCommand>[handlers.Length + 1];
            Array.Copy(handlers, result, handlers.Length);
            result[handlers.Length] = extraHandler;
            return result;
        }

        private static EventHandler<OrderCommand>[] newEventHandlersArray(int size)
        {
            return new EventHandler<OrderCommand>[size];
        }


        private class DisruptorExceptionHandler : Disruptor.IExceptionHandler<object>
        {
            private string _name;
            private Disruptor<OrderCommand> _disruptor;

            public DisruptorExceptionHandler(string name, Disruptor<OrderCommand> disruptor)
            {
                _name = name;
                _disruptor = disruptor;
            }

            public void HandleEventException(Exception ex, long seq, object evt)
            {
                // "main",
                log.Error($"Exception thrown on sequence={seq}", ex);
                // TODO re-throw exception on publishing
                _disruptor.RingBuffer.PublishEvent(ShutdownSignalTranlator.Instance);
                _disruptor.Shutdown();
            }

            public void HandleOnShutdownException(Exception ex)
            {
                throw new NotImplementedException();
            }

            public void HandleOnStartException(Exception ex)
            {
                throw new NotImplementedException();
            }
        }


        //private static IEventTranslator<OrderCommand> SHUTDOWN_SIGNAL_TRANSLATOR = (cmd, seq) => {
        //};

        private class ShutdownSignalTranlator : IEventTranslator<OrderCommand>
        {
            public static readonly ShutdownSignalTranlator Instance = new ShutdownSignalTranlator();
            private ShutdownSignalTranlator()
            {

            }
            public void TranslateTo(OrderCommand cmd, long seq)
            {
                cmd.Command = OrderCommandType.SHUTDOWN_SIGNAL;
                cmd.ResultCode = CommandResultCode.NEW;
            }
        }

        private class MatchingEngineProcessor : IEventHandler<OrderCommand>
        {
            private readonly MatchingEngineRouter mer;
            public MatchingEngineProcessor(MatchingEngineRouter mer)
            {
                this.mer = mer;
            }
            public void OnEvent(OrderCommand cmd, long seq, bool eob)
            {
                mer.processOrder(seq, cmd);
            }
        }

        private class JournalingProcessor : IEventHandler<OrderCommand>
        {
            private ISerializationProcessor serializationProcessor;
            public JournalingProcessor(ISerializationProcessor serializationProcessor)
            {
                this.serializationProcessor = serializationProcessor;
            }

            public void OnEvent(OrderCommand data, long sequence, bool endOfBatch)
            {
                serializationProcessor.writeToJournal(data, sequence, endOfBatch);
            }
        }

        private class ResultsProcessor : IEventHandler<OrderCommand>
        {
            private ResultsHandler resultsHandler;
            private ExchangeApi api;

            public ResultsProcessor(ResultsHandler resultsHandler, ExchangeApi api)
            {
                this.resultsHandler = resultsHandler;
                this.api = api;
            }

            public void OnEvent(OrderCommand cmd, long seq, bool endOfBatch)
            {
                resultsHandler.OnEvent(cmd, seq, endOfBatch);
                api.processResult(seq, cmd); // TODO SLOW ?(volatile operations)
            }
        }

        private class GroupingProcessorFactory : IEventProcessorFactory<OrderCommand>
        {
            private PerformanceConfiguration perfCfg;
            private CoreWaitStrategy coreWaitStrategy;
            private SharedPool sharedPool;

            public GroupingProcessorFactory(PerformanceConfiguration perfCfg, CoreWaitStrategy coreWaitStrategy, SharedPool sharedPool)
            {
                this.perfCfg = perfCfg;
                this.coreWaitStrategy = coreWaitStrategy;
                this.sharedPool = sharedPool;
            }

            public IEventProcessor CreateEventProcessor(RingBuffer<OrderCommand> ringBuffer, ISequence[] barrierSequences)
            {
                return new GroupingProcessor(ringBuffer, ringBuffer.NewBarrier(barrierSequences), perfCfg, coreWaitStrategy, sharedPool);
            }
        }

        private class TwoStepMasterProcessorFactory : IEventProcessorFactory<OrderCommand>
        {
            private List<TwoStepMasterProcessor> procR1;
            private RiskEngine riskEngine;
            private DisruptorExceptionHandler exceptionHandler;
            private CoreWaitStrategy coreWaitStrategy;
            private string v;

            public TwoStepMasterProcessorFactory(List<TwoStepMasterProcessor> procR1, RiskEngine riskEngine, DisruptorExceptionHandler exceptionHandler, CoreWaitStrategy coreWaitStrategy, string v)
            {
                this.procR1 = procR1;
                this.riskEngine = riskEngine;
                this.exceptionHandler = exceptionHandler;
                this.coreWaitStrategy = coreWaitStrategy;
                this.v = v;
            }

            public IEventProcessor CreateEventProcessor(RingBuffer<OrderCommand> ringBuffer, ISequence[] barrierSequences)
            {
                TwoStepMasterProcessor r1 = new TwoStepMasterProcessor(ringBuffer, ringBuffer.NewBarrier(barrierSequences), riskEngine.preProcessCommand, exceptionHandler, coreWaitStrategy, v);
                procR1.Add(r1);
                return r1;
            }
        }

        private class TwoStepSlaveProcessorFactory : IEventProcessorFactory<OrderCommand>
        {
            private List<TwoStepSlaveProcessor> procR2;
            private RiskEngine riskEngine;
            private DisruptorExceptionHandler exceptionHandler;
            private string v;

            public TwoStepSlaveProcessorFactory(List<TwoStepSlaveProcessor> procR2, RiskEngine riskEngine, DisruptorExceptionHandler exceptionHandler, string v)
            {
                this.procR2 = procR2;
                this.riskEngine = riskEngine;
                this.exceptionHandler = exceptionHandler;
                this.v = v;
            }

            public IEventProcessor CreateEventProcessor(RingBuffer<OrderCommand> ringBuffer, ISequence[] barrierSequences)
            {
                TwoStepSlaveProcessor r2 = new TwoStepSlaveProcessor(ringBuffer, ringBuffer.NewBarrier(barrierSequences), riskEngine.handlerRiskRelease, exceptionHandler, v);
                procR2.Add(r2);
                return r2;
            }
        }

        public static ExchangeCoreBuilder Builder()
        {
            return new ExchangeCoreBuilder();
        }

        public class ExchangeCoreBuilder
        {
            private Action<OrderCommand, long> _resultConsumer;
            private ExchangeConfiguration _exchangeConfiguration;

            public ExchangeCore build()
            {
                return new ExchangeCore(_resultConsumer, _exchangeConfiguration);
            }

            public ExchangeCoreBuilder exchangeConfiguration(ExchangeConfiguration exchangeConfiguration)
            {
                _exchangeConfiguration = exchangeConfiguration;
                return this;
            }

            public ExchangeCoreBuilder resultsConsumer(Action<OrderCommand, long> resultsConsumer)
            {
                _resultConsumer = resultsConsumer;
                return this;
            }
        }
    }

}