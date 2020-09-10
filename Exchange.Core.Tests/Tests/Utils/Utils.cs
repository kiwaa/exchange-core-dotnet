using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Utils;

namespace Exchange.Core.Tests.Utils
{
    public sealed partial class TestDataFutures : IEquatable<TestDataFutures>
    {
        public Task<List<CoreSymbolSpecification>> CoreSymbolSpecifications { get; set; }
        public Task<List<BitSet>> UsersAccounts { get; set; }
        public Task<MultiSymbolGenResult> GenResult { get; set; }
        public TestDataFutures(Task<List<CoreSymbolSpecification>> coreSymbolSpecifications, Task<List<BitSet>> usersAccounts, Task<MultiSymbolGenResult> genResult)
        {
            CoreSymbolSpecifications = coreSymbolSpecifications;
            UsersAccounts = usersAccounts;
            GenResult = genResult;
        }

        public bool Equals(TestDataFutures other)
        {
              return CoreSymbolSpecifications.Equals(other.CoreSymbolSpecifications) && UsersAccounts.Equals(other.UsersAccounts) && GenResult.Equals(other.GenResult);
        }

        public static TestDataFuturesBuilder Builder()
        {
              return new TestDataFuturesBuilder();
        }

        public sealed class TestDataFuturesBuilder
        {
            private Task<List<CoreSymbolSpecification>> _coreSymbolSpecifications;
            private Task<List<BitSet>> _usersAccounts;
            private Task<MultiSymbolGenResult> _genResult;

            public TestDataFuturesBuilder coreSymbolSpecifications(Task<List<CoreSymbolSpecification>> value)
            {
                _coreSymbolSpecifications = value;
                return this;
            }
            public TestDataFuturesBuilder usersAccounts(Task<List<BitSet>> value)
            {
                _usersAccounts = value;
                return this;
            }
            public TestDataFuturesBuilder genResult(Task<MultiSymbolGenResult> value)
            {
                _genResult = value;
                return this;
            }

            public TestDataFutures build()
            {
                return new TestDataFutures(_coreSymbolSpecifications, _usersAccounts, _genResult);
            }
        }
    }
    public sealed partial class TestDataParameters : IEquatable<TestDataParameters>
    {
        public int TotalTransactionsNumber { get; set; }
        public int TargetOrderBookOrdersTotal { get; set; }
        public int NumAccounts { get; set; }
        public HashSet<int> CurrenciesAllowed { get; set; }
        public int NumSymbols { get; set; }
        public AllowedSymbolTypes AllowedSymbolTypes { get; set; }
        public Func<TestOrdersGeneratorConfig,int> PreFillMode { get; set; }
        public bool AvalancheIOC { get; set; }
        public TestDataParameters(int totalTransactionsNumber, int targetOrderBookOrdersTotal, int numAccounts, HashSet<int> currenciesAllowed, int numSymbols, AllowedSymbolTypes allowedSymbolTypes, Func<TestOrdersGeneratorConfig,int> preFillMode, bool avalancheIOC)
        {
            TotalTransactionsNumber = totalTransactionsNumber;
            TargetOrderBookOrdersTotal = targetOrderBookOrdersTotal;
            NumAccounts = numAccounts;
            CurrenciesAllowed = currenciesAllowed;
            NumSymbols = numSymbols;
            AllowedSymbolTypes = allowedSymbolTypes;
            PreFillMode = preFillMode;
            AvalancheIOC = avalancheIOC;
        }

        public bool Equals(TestDataParameters other)
        {
              return TotalTransactionsNumber.Equals(other.TotalTransactionsNumber) && TargetOrderBookOrdersTotal.Equals(other.TargetOrderBookOrdersTotal) && NumAccounts.Equals(other.NumAccounts) && CurrenciesAllowed.Equals(other.CurrenciesAllowed) && NumSymbols.Equals(other.NumSymbols) && AllowedSymbolTypes.Equals(other.AllowedSymbolTypes) && PreFillMode.Equals(other.PreFillMode) && AvalancheIOC.Equals(other.AvalancheIOC);
        }

        public static TestDataParametersBuilder Builder()
        {
              return new TestDataParametersBuilder();
        }

        public sealed class TestDataParametersBuilder
        {
            private int _totalTransactionsNumber;
            private int _targetOrderBookOrdersTotal;
            private int _numAccounts;
            private HashSet<int> _currenciesAllowed;
            private int _numSymbols;
            private AllowedSymbolTypes _allowedSymbolTypes;
            private Func<TestOrdersGeneratorConfig,int> _preFillMode;
            private bool _avalancheIOC;

            public TestDataParametersBuilder totalTransactionsNumber(int value)
            {
                _totalTransactionsNumber = value;
                return this;
            }
            public TestDataParametersBuilder targetOrderBookOrdersTotal(int value)
            {
                _targetOrderBookOrdersTotal = value;
                return this;
            }
            public TestDataParametersBuilder numAccounts(int value)
            {
                _numAccounts = value;
                return this;
            }
            public TestDataParametersBuilder currenciesAllowed(HashSet<int> value)
            {
                _currenciesAllowed = value;
                return this;
            }
            public TestDataParametersBuilder numSymbols(int value)
            {
                _numSymbols = value;
                return this;
            }
            public TestDataParametersBuilder allowedSymbolTypes(AllowedSymbolTypes value)
            {
                _allowedSymbolTypes = value;
                return this;
            }
            public TestDataParametersBuilder preFillMode(Func<TestOrdersGeneratorConfig,int> value)
            {
                _preFillMode = value;
                return this;
            }
            public TestDataParametersBuilder avalancheIOC(bool value)
            {
                _avalancheIOC = value;
                return this;
            }

            public TestDataParameters build()
            {
                return new TestDataParameters(_totalTransactionsNumber, _targetOrderBookOrdersTotal, _numAccounts, _currenciesAllowed, _numSymbols, _allowedSymbolTypes, _preFillMode, _avalancheIOC);
            }
        }
    }
    public sealed partial class TestOrdersGeneratorConfig : IEquatable<TestOrdersGeneratorConfig>
    {
        public List<CoreSymbolSpecification> CoreSymbolSpecifications { get; set; }
        public int TotalTransactionsNumber { get; set; }
        public List<BitSet> UsersAccounts { get; set; }
        public int TargetOrderBookOrdersTotal { get; set; }
        public int Seed { get; set; }
        public bool AvalancheIOC { get; set; }
        public Func<TestOrdersGeneratorConfig,int> PreFillMode { get; set; }
        public TestOrdersGeneratorConfig(List<CoreSymbolSpecification> coreSymbolSpecifications, int totalTransactionsNumber, List<BitSet> usersAccounts, int targetOrderBookOrdersTotal, int seed, bool avalancheIOC, Func<TestOrdersGeneratorConfig,int> preFillMode)
        {
            CoreSymbolSpecifications = coreSymbolSpecifications;
            TotalTransactionsNumber = totalTransactionsNumber;
            UsersAccounts = usersAccounts;
            TargetOrderBookOrdersTotal = targetOrderBookOrdersTotal;
            Seed = seed;
            AvalancheIOC = avalancheIOC;
            PreFillMode = preFillMode;
        }

        public bool Equals(TestOrdersGeneratorConfig other)
        {
              return CoreSymbolSpecifications.Equals(other.CoreSymbolSpecifications) && TotalTransactionsNumber.Equals(other.TotalTransactionsNumber) && UsersAccounts.Equals(other.UsersAccounts) && TargetOrderBookOrdersTotal.Equals(other.TargetOrderBookOrdersTotal) && Seed.Equals(other.Seed) && AvalancheIOC.Equals(other.AvalancheIOC) && PreFillMode.Equals(other.PreFillMode);
        }

        public static TestOrdersGeneratorConfigBuilder Builder()
        {
              return new TestOrdersGeneratorConfigBuilder();
        }

        public sealed class TestOrdersGeneratorConfigBuilder
        {
            private List<CoreSymbolSpecification> _coreSymbolSpecifications;
            private int _totalTransactionsNumber;
            private List<BitSet> _usersAccounts;
            private int _targetOrderBookOrdersTotal;
            private int _seed;
            private bool _avalancheIOC;
            private Func<TestOrdersGeneratorConfig,int> _preFillMode;

            public TestOrdersGeneratorConfigBuilder coreSymbolSpecifications(List<CoreSymbolSpecification> value)
            {
                _coreSymbolSpecifications = value;
                return this;
            }
            public TestOrdersGeneratorConfigBuilder totalTransactionsNumber(int value)
            {
                _totalTransactionsNumber = value;
                return this;
            }
            public TestOrdersGeneratorConfigBuilder usersAccounts(List<BitSet> value)
            {
                _usersAccounts = value;
                return this;
            }
            public TestOrdersGeneratorConfigBuilder targetOrderBookOrdersTotal(int value)
            {
                _targetOrderBookOrdersTotal = value;
                return this;
            }
            public TestOrdersGeneratorConfigBuilder seed(int value)
            {
                _seed = value;
                return this;
            }
            public TestOrdersGeneratorConfigBuilder avalancheIOC(bool value)
            {
                _avalancheIOC = value;
                return this;
            }
            public TestOrdersGeneratorConfigBuilder preFillMode(Func<TestOrdersGeneratorConfig,int> value)
            {
                _preFillMode = value;
                return this;
            }

            public TestOrdersGeneratorConfig build()
            {
                return new TestOrdersGeneratorConfig(_coreSymbolSpecifications, _totalTransactionsNumber, _usersAccounts, _targetOrderBookOrdersTotal, _seed, _avalancheIOC, _preFillMode);
            }
        }
    }
    public sealed partial class GenResult : IEquatable<GenResult>
    {
        public L2MarketData FinalOrderBookSnapshot { get; set; }
        public int FinalOrderbookHash { get; set; }
        public List<OrderCommand> CommandsFill { get; set; }
        public List<OrderCommand> CommandsBenchmark { get; set; }
        public GenResult(L2MarketData finalOrderBookSnapshot, int finalOrderbookHash, List<OrderCommand> commandsFill, List<OrderCommand> commandsBenchmark)
        {
            FinalOrderBookSnapshot = finalOrderBookSnapshot;
            FinalOrderbookHash = finalOrderbookHash;
            CommandsFill = commandsFill;
            CommandsBenchmark = commandsBenchmark;
        }

        public bool Equals(GenResult other)
        {
              return FinalOrderBookSnapshot.Equals(other.FinalOrderBookSnapshot) && FinalOrderbookHash.Equals(other.FinalOrderbookHash) && CommandsFill.Equals(other.CommandsFill) && CommandsBenchmark.Equals(other.CommandsBenchmark);
        }

        public static GenResultBuilder Builder()
        {
              return new GenResultBuilder();
        }

        public sealed class GenResultBuilder
        {
            private L2MarketData _finalOrderBookSnapshot;
            private int _finalOrderbookHash;
            private List<OrderCommand> _commandsFill;
            private List<OrderCommand> _commandsBenchmark;

            public GenResultBuilder finalOrderBookSnapshot(L2MarketData value)
            {
                _finalOrderBookSnapshot = value;
                return this;
            }
            public GenResultBuilder finalOrderbookHash(int value)
            {
                _finalOrderbookHash = value;
                return this;
            }
            public GenResultBuilder commandsFill(List<OrderCommand> value)
            {
                _commandsFill = value;
                return this;
            }
            public GenResultBuilder commandsBenchmark(List<OrderCommand> value)
            {
                _commandsBenchmark = value;
                return this;
            }

            public GenResult build()
            {
                return new GenResult(_finalOrderBookSnapshot, _finalOrderbookHash, _commandsFill, _commandsBenchmark);
            }
        }
    }
    public sealed partial class MultiSymbolGenResult : IEquatable<MultiSymbolGenResult>
    {
        public Dictionary<int,GenResult> GenResults { get; set; }
        public Task<List<ApiCommand>> ApiCommandsFill { get; set; }
        public Task<List<ApiCommand>> ApiCommandsBenchmark { get; set; }
        public int BenchmarkCommandsSize { get; set; }
        public MultiSymbolGenResult(Dictionary<int,GenResult> genResults, Task<List<ApiCommand>> apiCommandsFill, Task<List<ApiCommand>> apiCommandsBenchmark, int benchmarkCommandsSize)
        {
            GenResults = genResults;
            ApiCommandsFill = apiCommandsFill;
            ApiCommandsBenchmark = apiCommandsBenchmark;
            BenchmarkCommandsSize = benchmarkCommandsSize;
        }

        public bool Equals(MultiSymbolGenResult other)
        {
              return GenResults.Equals(other.GenResults) && ApiCommandsFill.Equals(other.ApiCommandsFill) && ApiCommandsBenchmark.Equals(other.ApiCommandsBenchmark) && BenchmarkCommandsSize.Equals(other.BenchmarkCommandsSize);
        }

        public static MultiSymbolGenResultBuilder Builder()
        {
              return new MultiSymbolGenResultBuilder();
        }

        public sealed class MultiSymbolGenResultBuilder
        {
            private Dictionary<int,GenResult> _genResults;
            private Task<List<ApiCommand>> _apiCommandsFill;
            private Task<List<ApiCommand>> _apiCommandsBenchmark;
            private int _benchmarkCommandsSize;

            public MultiSymbolGenResultBuilder genResults(Dictionary<int,GenResult> value)
            {
                _genResults = value;
                return this;
            }
            public MultiSymbolGenResultBuilder apiCommandsFill(Task<List<ApiCommand>> value)
            {
                _apiCommandsFill = value;
                return this;
            }
            public MultiSymbolGenResultBuilder apiCommandsBenchmark(Task<List<ApiCommand>> value)
            {
                _apiCommandsBenchmark = value;
                return this;
            }
            public MultiSymbolGenResultBuilder benchmarkCommandsSize(int value)
            {
                _benchmarkCommandsSize = value;
                return this;
            }

            public MultiSymbolGenResult build()
            {
                return new MultiSymbolGenResult(_genResults, _apiCommandsFill, _apiCommandsBenchmark, _benchmarkCommandsSize);
            }
        }
    }
    public sealed partial class SlowCommandRecord : IEquatable<SlowCommandRecord>
    {
        public int MinLatency { get; set; }
        public int SeqNumber { get; set; }
        public ApiCommand ApiCommand { get; set; }
        public int EventsNum { get; set; }
        public SlowCommandRecord(int minLatency, int seqNumber, ApiCommand apiCommand, int eventsNum)
        {
            MinLatency = minLatency;
            SeqNumber = seqNumber;
            ApiCommand = apiCommand;
            EventsNum = eventsNum;
        }

        public bool Equals(SlowCommandRecord other)
        {
              return MinLatency.Equals(other.MinLatency) && SeqNumber.Equals(other.SeqNumber) && ApiCommand.Equals(other.ApiCommand) && EventsNum.Equals(other.EventsNum);
        }

        public static SlowCommandRecordBuilder Builder()
        {
              return new SlowCommandRecordBuilder();
        }

        public sealed class SlowCommandRecordBuilder
        {
            private int _minLatency;
            private int _seqNumber;
            private ApiCommand _apiCommand;
            private int _eventsNum;

            public SlowCommandRecordBuilder minLatency(int value)
            {
                _minLatency = value;
                return this;
            }
            public SlowCommandRecordBuilder seqNumber(int value)
            {
                _seqNumber = value;
                return this;
            }
            public SlowCommandRecordBuilder apiCommand(ApiCommand value)
            {
                _apiCommand = value;
                return this;
            }
            public SlowCommandRecordBuilder eventsNum(int value)
            {
                _eventsNum = value;
                return this;
            }

            public SlowCommandRecord build()
            {
                return new SlowCommandRecord(_minLatency, _seqNumber, _apiCommand, _eventsNum);
            }
        }
    }
}


				
