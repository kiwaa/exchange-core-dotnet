using System;

namespace Exchange.Core.Common
{
    public sealed partial class MatcherTradeEvent : IEquatable<MatcherTradeEvent>
    {
        public MatcherEventType EventType { get; set; }
        public int Section { get; set; }
        public bool ActiveOrderCompleted { get; set; }
        public long MatchedOrderId { get; set; }
        public long MatchedOrderUid { get; set; }
        public bool MatchedOrderCompleted { get; set; }
        public long Price { get; set; }
        public long Size { get; set; }
        public long BidderHoldPrice { get; set; }
        public MatcherTradeEvent NextEvent { get; set; }
        public MatcherTradeEvent(MatcherEventType eventType, int section, bool activeOrderCompleted, long matchedOrderId, long matchedOrderUid, bool matchedOrderCompleted, long price, long size, long bidderHoldPrice, MatcherTradeEvent nextEvent)
        {
            EventType = eventType;
            Section = section;
            ActiveOrderCompleted = activeOrderCompleted;
            MatchedOrderId = matchedOrderId;
            MatchedOrderUid = matchedOrderUid;
            MatchedOrderCompleted = matchedOrderCompleted;
            Price = price;
            Size = size;
            BidderHoldPrice = bidderHoldPrice;
            NextEvent = nextEvent;
        }

        public bool Equals(MatcherTradeEvent other)
        {
              return EventType.Equals(other.EventType) && Section.Equals(other.Section) && ActiveOrderCompleted.Equals(other.ActiveOrderCompleted) && MatchedOrderId.Equals(other.MatchedOrderId) && MatchedOrderUid.Equals(other.MatchedOrderUid) && MatchedOrderCompleted.Equals(other.MatchedOrderCompleted) && Price.Equals(other.Price) && Size.Equals(other.Size) && BidderHoldPrice.Equals(other.BidderHoldPrice) && NextEvent.Equals(other.NextEvent);
        }

        public static MatcherTradeEventBuilder Builder()
        {
              return new MatcherTradeEventBuilder();
        }

        public sealed class MatcherTradeEventBuilder
        {
            private MatcherEventType _eventType;
            private int _section;
            private bool _activeOrderCompleted;
            private long _matchedOrderId;
            private long _matchedOrderUid;
            private bool _matchedOrderCompleted;
            private long _price;
            private long _size;
            private long _bidderHoldPrice;
            private MatcherTradeEvent _nextEvent;

            public MatcherTradeEventBuilder eventType(MatcherEventType value)
            {
                _eventType = value;
                return this;
            }
            public MatcherTradeEventBuilder section(int value)
            {
                _section = value;
                return this;
            }
            public MatcherTradeEventBuilder activeOrderCompleted(bool value)
            {
                _activeOrderCompleted = value;
                return this;
            }
            public MatcherTradeEventBuilder matchedOrderId(long value)
            {
                _matchedOrderId = value;
                return this;
            }
            public MatcherTradeEventBuilder matchedOrderUid(long value)
            {
                _matchedOrderUid = value;
                return this;
            }
            public MatcherTradeEventBuilder matchedOrderCompleted(bool value)
            {
                _matchedOrderCompleted = value;
                return this;
            }
            public MatcherTradeEventBuilder price(long value)
            {
                _price = value;
                return this;
            }
            public MatcherTradeEventBuilder size(long value)
            {
                _size = value;
                return this;
            }
            public MatcherTradeEventBuilder bidderHoldPrice(long value)
            {
                _bidderHoldPrice = value;
                return this;
            }
            public MatcherTradeEventBuilder nextEvent(MatcherTradeEvent value)
            {
                _nextEvent = value;
                return this;
            }

            public MatcherTradeEvent build()
            {
                return new MatcherTradeEvent(_eventType, _section, _activeOrderCompleted, _matchedOrderId, _matchedOrderUid, _matchedOrderCompleted, _price, _size, _bidderHoldPrice, _nextEvent);
            }
        }
    }
    public sealed partial class Order : IEquatable<Order>
    {
        public long OrderId { get; set; }
        public long Price { get; set; }
        public long Size { get; set; }
        public long Filled { get; set; }
        public long ReserveBidPrice { get; set; }
        public OrderAction Action { get; set; }
        public long Uid { get; set; }
        public long Timestamp { get; set; }
        public Order(long orderId, long price, long size, long filled, long reserveBidPrice, OrderAction action, long uid, long timestamp)
        {
            OrderId = orderId;
            Price = price;
            Size = size;
            Filled = filled;
            ReserveBidPrice = reserveBidPrice;
            Action = action;
            Uid = uid;
            Timestamp = timestamp;
        }

        public bool Equals(Order other)
        {
              return OrderId.Equals(other.OrderId) && Price.Equals(other.Price) && Size.Equals(other.Size) && Filled.Equals(other.Filled) && ReserveBidPrice.Equals(other.ReserveBidPrice) && Action.Equals(other.Action) && Uid.Equals(other.Uid) && Timestamp.Equals(other.Timestamp);
        }

        public static OrderBuilder Builder()
        {
              return new OrderBuilder();
        }

        public sealed class OrderBuilder
        {
            private long _orderId;
            private long _price;
            private long _size;
            private long _filled;
            private long _reserveBidPrice;
            private OrderAction _action;
            private long _uid;
            private long _timestamp;

            public OrderBuilder orderId(long value)
            {
                _orderId = value;
                return this;
            }
            public OrderBuilder price(long value)
            {
                _price = value;
                return this;
            }
            public OrderBuilder size(long value)
            {
                _size = value;
                return this;
            }
            public OrderBuilder filled(long value)
            {
                _filled = value;
                return this;
            }
            public OrderBuilder reserveBidPrice(long value)
            {
                _reserveBidPrice = value;
                return this;
            }
            public OrderBuilder action(OrderAction value)
            {
                _action = value;
                return this;
            }
            public OrderBuilder uid(long value)
            {
                _uid = value;
                return this;
            }
            public OrderBuilder timestamp(long value)
            {
                _timestamp = value;
                return this;
            }

            public Order build()
            {
                return new Order(_orderId, _price, _size, _filled, _reserveBidPrice, _action, _uid, _timestamp);
            }
        }
    }
    public sealed partial class CoreSymbolSpecification : IEquatable<CoreSymbolSpecification>
    {
        public int SymbolId { get; set; }
        public SymbolType Type { get; set; }
        public int BaseCurrency { get; set; }
        public int QuoteCurrency { get; set; }
        public long BaseScaleK { get; set; }
        public long QuoteScaleK { get; set; }
        public long TakerFee { get; set; }
        public long MakerFee { get; set; }
        public long MarginBuy { get; set; }
        public long MarginSell { get; set; }
        public CoreSymbolSpecification(int symbolId, SymbolType type, int baseCurrency, int quoteCurrency, long baseScaleK, long quoteScaleK, long takerFee, long makerFee, long marginBuy, long marginSell)
        {
            SymbolId = symbolId;
            Type = type;
            BaseCurrency = baseCurrency;
            QuoteCurrency = quoteCurrency;
            BaseScaleK = baseScaleK;
            QuoteScaleK = quoteScaleK;
            TakerFee = takerFee;
            MakerFee = makerFee;
            MarginBuy = marginBuy;
            MarginSell = marginSell;
        }

        public bool Equals(CoreSymbolSpecification other)
        {
              return SymbolId.Equals(other.SymbolId) && Type.Equals(other.Type) && BaseCurrency.Equals(other.BaseCurrency) && QuoteCurrency.Equals(other.QuoteCurrency) && BaseScaleK.Equals(other.BaseScaleK) && QuoteScaleK.Equals(other.QuoteScaleK) && TakerFee.Equals(other.TakerFee) && MakerFee.Equals(other.MakerFee) && MarginBuy.Equals(other.MarginBuy) && MarginSell.Equals(other.MarginSell);
        }

        public static CoreSymbolSpecificationBuilder Builder()
        {
              return new CoreSymbolSpecificationBuilder();
        }

        public sealed class CoreSymbolSpecificationBuilder
        {
            private int _symbolId;
            private SymbolType _type;
            private int _baseCurrency;
            private int _quoteCurrency;
            private long _baseScaleK;
            private long _quoteScaleK;
            private long _takerFee;
            private long _makerFee;
            private long _marginBuy;
            private long _marginSell;

            public CoreSymbolSpecificationBuilder symbolId(int value)
            {
                _symbolId = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder type(SymbolType value)
            {
                _type = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder baseCurrency(int value)
            {
                _baseCurrency = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder quoteCurrency(int value)
            {
                _quoteCurrency = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder baseScaleK(long value)
            {
                _baseScaleK = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder quoteScaleK(long value)
            {
                _quoteScaleK = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder takerFee(long value)
            {
                _takerFee = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder makerFee(long value)
            {
                _makerFee = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder marginBuy(long value)
            {
                _marginBuy = value;
                return this;
            }
            public CoreSymbolSpecificationBuilder marginSell(long value)
            {
                _marginSell = value;
                return this;
            }

            public CoreSymbolSpecification build()
            {
                return new CoreSymbolSpecification(_symbolId, _type, _baseCurrency, _quoteCurrency, _baseScaleK, _quoteScaleK, _takerFee, _makerFee, _marginBuy, _marginSell);
            }
        }
    }
}


				
