namespace Exchange.Core
{
    public interface IEventsHandler
    {
        void tradeEvent(TradeEvent tradeEvent);

        void reduceEvent(ReduceEvent reduceEvent);

        void rejectEvent(RejectEvent rejectEvent);

        void commandResult(ApiCommandResult commandResult);

        void orderBook(OrderBook orderBook);
    }
}