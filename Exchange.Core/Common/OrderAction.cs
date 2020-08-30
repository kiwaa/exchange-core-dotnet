namespace Exchange.Core.Common
{
    public enum OrderAction
    {
        BID,
        ASK
    }

    public static class OrderActionHelper
    {
        public static OrderAction opposite(OrderAction action)
        {
            return action == OrderAction.ASK ? OrderAction.BID : OrderAction.ASK;
        }

    }
}