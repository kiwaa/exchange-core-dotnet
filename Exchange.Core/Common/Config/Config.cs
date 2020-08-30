using Exchange.Core.Common.Api.Binary;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Orderbook;
using Exchange.Core.Processors.Journaling;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Common.Config
{
    public sealed partial class PerformanceConfiguration : IEquatable<PerformanceConfiguration>
    {
        public int RingBufferSize { get; set; }
        public int MatchingEnginesNum { get; set; }
        public int RiskEnginesNum { get; set; }
        public int MsgsInGroupLimit { get; set; }
        public int MaxGroupDurationNs { get; set; }
        public TaskScheduler TaskScheduler { get; set; }
        public CoreWaitStrategy WaitStrategy { get; set; }
        public Func<CoreSymbolSpecification,ObjectsPool,OrderBookEventsHelper,LoggingConfiguration,IOrderBook> OrderBookFactory { get; set; }
        public Func<LZ4Compressor> BinaryCommandsLz4CompressorFactory { get; set; }
        public PerformanceConfiguration(int ringBufferSize, int matchingEnginesNum, int riskEnginesNum, int msgsInGroupLimit, int maxGroupDurationNs, TaskScheduler taskScheduler, CoreWaitStrategy waitStrategy, Func<CoreSymbolSpecification,ObjectsPool,OrderBookEventsHelper,LoggingConfiguration,IOrderBook> orderBookFactory, Func<LZ4Compressor> binaryCommandsLz4CompressorFactory)
        {
            RingBufferSize = ringBufferSize;
            MatchingEnginesNum = matchingEnginesNum;
            RiskEnginesNum = riskEnginesNum;
            MsgsInGroupLimit = msgsInGroupLimit;
            MaxGroupDurationNs = maxGroupDurationNs;
            TaskScheduler = taskScheduler;
            WaitStrategy = waitStrategy;
            OrderBookFactory = orderBookFactory;
            BinaryCommandsLz4CompressorFactory = binaryCommandsLz4CompressorFactory;
        }

        public bool Equals(PerformanceConfiguration other)
        {
              return RingBufferSize.Equals(other.RingBufferSize) && MatchingEnginesNum.Equals(other.MatchingEnginesNum) && RiskEnginesNum.Equals(other.RiskEnginesNum) && MsgsInGroupLimit.Equals(other.MsgsInGroupLimit) && MaxGroupDurationNs.Equals(other.MaxGroupDurationNs) && TaskScheduler.Equals(other.TaskScheduler) && WaitStrategy.Equals(other.WaitStrategy) && OrderBookFactory.Equals(other.OrderBookFactory) && BinaryCommandsLz4CompressorFactory.Equals(other.BinaryCommandsLz4CompressorFactory);
        }

        public static PerformanceConfigurationBuilder Builder()
        {
              return new PerformanceConfigurationBuilder();
        }

        public sealed class PerformanceConfigurationBuilder
        {
            private int _ringBufferSize;
            private int _matchingEnginesNum;
            private int _riskEnginesNum;
            private int _msgsInGroupLimit;
            private int _maxGroupDurationNs;
            private TaskScheduler _taskScheduler;
            private CoreWaitStrategy _waitStrategy;
            private Func<CoreSymbolSpecification,ObjectsPool,OrderBookEventsHelper,LoggingConfiguration,IOrderBook> _orderBookFactory;
            private Func<LZ4Compressor> _binaryCommandsLz4CompressorFactory;

            public PerformanceConfigurationBuilder ringBufferSize(int value)
            {
                _ringBufferSize = value;
                return this;
            }
            public PerformanceConfigurationBuilder matchingEnginesNum(int value)
            {
                _matchingEnginesNum = value;
                return this;
            }
            public PerformanceConfigurationBuilder riskEnginesNum(int value)
            {
                _riskEnginesNum = value;
                return this;
            }
            public PerformanceConfigurationBuilder msgsInGroupLimit(int value)
            {
                _msgsInGroupLimit = value;
                return this;
            }
            public PerformanceConfigurationBuilder maxGroupDurationNs(int value)
            {
                _maxGroupDurationNs = value;
                return this;
            }
            public PerformanceConfigurationBuilder taskScheduler(TaskScheduler value)
            {
                _taskScheduler = value;
                return this;
            }
            public PerformanceConfigurationBuilder waitStrategy(CoreWaitStrategy value)
            {
                _waitStrategy = value;
                return this;
            }
            public PerformanceConfigurationBuilder orderBookFactory(Func<CoreSymbolSpecification,ObjectsPool,OrderBookEventsHelper,LoggingConfiguration,IOrderBook> value)
            {
                _orderBookFactory = value;
                return this;
            }
            public PerformanceConfigurationBuilder binaryCommandsLz4CompressorFactory(Func<LZ4Compressor> value)
            {
                _binaryCommandsLz4CompressorFactory = value;
                return this;
            }

            public PerformanceConfiguration build()
            {
                return new PerformanceConfiguration(_ringBufferSize, _matchingEnginesNum, _riskEnginesNum, _msgsInGroupLimit, _maxGroupDurationNs, _taskScheduler, _waitStrategy, _orderBookFactory, _binaryCommandsLz4CompressorFactory);
            }
        }
    }
    public sealed partial class SerializationConfiguration : IEquatable<SerializationConfiguration>
    {
        public bool EnableJournaling { get; set; }
        public Func<ExchangeConfiguration,ISerializationProcessor> SerializationProcessorFactory { get; set; }
        public SerializationConfiguration(bool enableJournaling, Func<ExchangeConfiguration,ISerializationProcessor> serializationProcessorFactory)
        {
            EnableJournaling = enableJournaling;
            SerializationProcessorFactory = serializationProcessorFactory;
        }

        public bool Equals(SerializationConfiguration other)
        {
              return EnableJournaling.Equals(other.EnableJournaling) && SerializationProcessorFactory.Equals(other.SerializationProcessorFactory);
        }

        public static SerializationConfigurationBuilder Builder()
        {
              return new SerializationConfigurationBuilder();
        }

        public sealed class SerializationConfigurationBuilder
        {
            private bool _enableJournaling;
            private Func<ExchangeConfiguration,ISerializationProcessor> _serializationProcessorFactory;

            public SerializationConfigurationBuilder enableJournaling(bool value)
            {
                _enableJournaling = value;
                return this;
            }
            public SerializationConfigurationBuilder serializationProcessorFactory(Func<ExchangeConfiguration,ISerializationProcessor> value)
            {
                _serializationProcessorFactory = value;
                return this;
            }

            public SerializationConfiguration build()
            {
                return new SerializationConfiguration(_enableJournaling, _serializationProcessorFactory);
            }
        }
    }
    public sealed partial class InitialStateConfiguration : IEquatable<InitialStateConfiguration>
    {
        public string ExchangeId { get; set; }
        public long SnapshotId { get; set; }
        public long SnapshotBaseSeq { get; set; }
        public long JournalTimestampNs { get; set; }
        public InitialStateConfiguration(string exchangeId, long snapshotId, long snapshotBaseSeq, long journalTimestampNs)
        {
            ExchangeId = exchangeId;
            SnapshotId = snapshotId;
            SnapshotBaseSeq = snapshotBaseSeq;
            JournalTimestampNs = journalTimestampNs;
        }

        public bool Equals(InitialStateConfiguration other)
        {
              return ExchangeId.Equals(other.ExchangeId) && SnapshotId.Equals(other.SnapshotId) && SnapshotBaseSeq.Equals(other.SnapshotBaseSeq) && JournalTimestampNs.Equals(other.JournalTimestampNs);
        }

        public static InitialStateConfigurationBuilder Builder()
        {
              return new InitialStateConfigurationBuilder();
        }

        public sealed class InitialStateConfigurationBuilder
        {
            private string _exchangeId;
            private long _snapshotId;
            private long _snapshotBaseSeq;
            private long _journalTimestampNs;

            public InitialStateConfigurationBuilder exchangeId(string value)
            {
                _exchangeId = value;
                return this;
            }
            public InitialStateConfigurationBuilder snapshotId(long value)
            {
                _snapshotId = value;
                return this;
            }
            public InitialStateConfigurationBuilder snapshotBaseSeq(long value)
            {
                _snapshotBaseSeq = value;
                return this;
            }
            public InitialStateConfigurationBuilder journalTimestampNs(long value)
            {
                _journalTimestampNs = value;
                return this;
            }

            public InitialStateConfiguration build()
            {
                return new InitialStateConfiguration(_exchangeId, _snapshotId, _snapshotBaseSeq, _journalTimestampNs);
            }
        }
    }
    public sealed partial class OrdersProcessingConfiguration : IEquatable<OrdersProcessingConfiguration>
    {
        public RiskProcessingMode RiskProcessingMode { get; set; }
        public MarginTradingMode MarginTradingMode { get; set; }
        public OrdersProcessingConfiguration(RiskProcessingMode riskProcessingMode, MarginTradingMode marginTradingMode)
        {
            RiskProcessingMode = riskProcessingMode;
            MarginTradingMode = marginTradingMode;
        }

        public bool Equals(OrdersProcessingConfiguration other)
        {
              return RiskProcessingMode.Equals(other.RiskProcessingMode) && MarginTradingMode.Equals(other.MarginTradingMode);
        }

        public static OrdersProcessingConfigurationBuilder Builder()
        {
              return new OrdersProcessingConfigurationBuilder();
        }

        public sealed class OrdersProcessingConfigurationBuilder
        {
            private RiskProcessingMode _riskProcessingMode;
            private MarginTradingMode _marginTradingMode;

            public OrdersProcessingConfigurationBuilder riskProcessingMode(RiskProcessingMode value)
            {
                _riskProcessingMode = value;
                return this;
            }
            public OrdersProcessingConfigurationBuilder marginTradingMode(MarginTradingMode value)
            {
                _marginTradingMode = value;
                return this;
            }

            public OrdersProcessingConfiguration build()
            {
                return new OrdersProcessingConfiguration(_riskProcessingMode, _marginTradingMode);
            }
        }
    }
    public sealed partial class ReportsQueriesConfiguration : IEquatable<ReportsQueriesConfiguration>
    {
        public Dictionary<int,Func<IBytesIn,object>> ReportConstructors { get; set; }
        public Dictionary<int,Func<IBytesIn,IBinaryDataCommand>> BinaryCommandConstructors { get; set; }
        public ReportsQueriesConfiguration(Dictionary<int,Func<IBytesIn,object>> reportConstructors, Dictionary<int,Func<IBytesIn,IBinaryDataCommand>> binaryCommandConstructors)
        {
            ReportConstructors = reportConstructors;
            BinaryCommandConstructors = binaryCommandConstructors;
        }

        public bool Equals(ReportsQueriesConfiguration other)
        {
              return ReportConstructors.Equals(other.ReportConstructors) && BinaryCommandConstructors.Equals(other.BinaryCommandConstructors);
        }

        public static ReportsQueriesConfigurationBuilder Builder()
        {
              return new ReportsQueriesConfigurationBuilder();
        }

        public sealed class ReportsQueriesConfigurationBuilder
        {
            private Dictionary<int,Func<IBytesIn,object>> _reportConstructors;
            private Dictionary<int,Func<IBytesIn,IBinaryDataCommand>> _binaryCommandConstructors;

            public ReportsQueriesConfigurationBuilder reportConstructors(Dictionary<int,Func<IBytesIn,object>> value)
            {
                _reportConstructors = value;
                return this;
            }
            public ReportsQueriesConfigurationBuilder binaryCommandConstructors(Dictionary<int,Func<IBytesIn,IBinaryDataCommand>> value)
            {
                _binaryCommandConstructors = value;
                return this;
            }

            public ReportsQueriesConfiguration build()
            {
                return new ReportsQueriesConfiguration(_reportConstructors, _binaryCommandConstructors);
            }
        }
    }
}


				
