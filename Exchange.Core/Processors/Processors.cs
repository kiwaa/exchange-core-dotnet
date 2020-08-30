using Exchange.Core.Orderbook;
using System;
using System.Collections.Generic;

namespace Exchange.Core.Processors
{
    public sealed partial class DeserializedData : IEquatable<DeserializedData>
    {
        public BinaryCommandsProcessor BinaryCommandsProcessor { get; set; }
        public Dictionary<int,IOrderBook> OrderBooks { get; set; }
        public DeserializedData(BinaryCommandsProcessor binaryCommandsProcessor, Dictionary<int,IOrderBook> orderBooks)
        {
            BinaryCommandsProcessor = binaryCommandsProcessor;
            OrderBooks = orderBooks;
        }

        public bool Equals(DeserializedData other)
        {
              return BinaryCommandsProcessor.Equals(other.BinaryCommandsProcessor) && OrderBooks.Equals(other.OrderBooks);
        }

        public static DeserializedDataBuilder Builder()
        {
              return new DeserializedDataBuilder();
        }

        public sealed class DeserializedDataBuilder
        {
            private BinaryCommandsProcessor _binaryCommandsProcessor;
            private Dictionary<int,IOrderBook> _orderBooks;

            public DeserializedDataBuilder binaryCommandsProcessor(BinaryCommandsProcessor value)
            {
                _binaryCommandsProcessor = value;
                return this;
            }
            public DeserializedDataBuilder orderBooks(Dictionary<int,IOrderBook> value)
            {
                _orderBooks = value;
                return this;
            }

            public DeserializedData build()
            {
                return new DeserializedData(_binaryCommandsProcessor, _orderBooks);
            }
        }
    }
    public sealed partial class State : IEquatable<State>
    {
        public SymbolSpecificationProvider SymbolSpecificationProvider { get; set; }
        public UserProfileService UserProfileService { get; set; }
        public BinaryCommandsProcessor BinaryCommandsProcessor { get; set; }
        public Dictionary<int,LastPriceCacheRecord> LastPriceCache { get; set; }
        public Dictionary<int,long> Fees { get; set; }
        public Dictionary<int,long> Adjustments { get; set; }
        public Dictionary<int,long> Suspends { get; set; }
        public State(SymbolSpecificationProvider symbolSpecificationProvider, UserProfileService userProfileService, BinaryCommandsProcessor binaryCommandsProcessor, Dictionary<int,LastPriceCacheRecord> lastPriceCache, Dictionary<int,long> fees, Dictionary<int,long> adjustments, Dictionary<int,long> suspends)
        {
            SymbolSpecificationProvider = symbolSpecificationProvider;
            UserProfileService = userProfileService;
            BinaryCommandsProcessor = binaryCommandsProcessor;
            LastPriceCache = lastPriceCache;
            Fees = fees;
            Adjustments = adjustments;
            Suspends = suspends;
        }

        public bool Equals(State other)
        {
              return SymbolSpecificationProvider.Equals(other.SymbolSpecificationProvider) && UserProfileService.Equals(other.UserProfileService) && BinaryCommandsProcessor.Equals(other.BinaryCommandsProcessor) && LastPriceCache.Equals(other.LastPriceCache) && Fees.Equals(other.Fees) && Adjustments.Equals(other.Adjustments) && Suspends.Equals(other.Suspends);
        }

        public static StateBuilder Builder()
        {
              return new StateBuilder();
        }

        public sealed class StateBuilder
        {
            private SymbolSpecificationProvider _symbolSpecificationProvider;
            private UserProfileService _userProfileService;
            private BinaryCommandsProcessor _binaryCommandsProcessor;
            private Dictionary<int,LastPriceCacheRecord> _lastPriceCache;
            private Dictionary<int,long> _fees;
            private Dictionary<int,long> _adjustments;
            private Dictionary<int,long> _suspends;

            public StateBuilder symbolSpecificationProvider(SymbolSpecificationProvider value)
            {
                _symbolSpecificationProvider = value;
                return this;
            }
            public StateBuilder userProfileService(UserProfileService value)
            {
                _userProfileService = value;
                return this;
            }
            public StateBuilder binaryCommandsProcessor(BinaryCommandsProcessor value)
            {
                _binaryCommandsProcessor = value;
                return this;
            }
            public StateBuilder lastPriceCache(Dictionary<int,LastPriceCacheRecord> value)
            {
                _lastPriceCache = value;
                return this;
            }
            public StateBuilder fees(Dictionary<int,long> value)
            {
                _fees = value;
                return this;
            }
            public StateBuilder adjustments(Dictionary<int,long> value)
            {
                _adjustments = value;
                return this;
            }
            public StateBuilder suspends(Dictionary<int,long> value)
            {
                _suspends = value;
                return this;
            }

            public State build()
            {
                return new State(_symbolSpecificationProvider, _userProfileService, _binaryCommandsProcessor, _lastPriceCache, _fees, _adjustments, _suspends);
            }
        }
    }
}


				
