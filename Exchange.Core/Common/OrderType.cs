namespace Exchange.Core.Common
{
    public enum OrderType
    {
        // Good till Cancel - equivalent to regular limit order
        GTC = 0,

        // Immediate or Cancel - equivalent to strict-risk market order
        IOC = 1, // with price cap
        IOC_BUDGET = 2, // with total amount cap

        // Fill or Kill - execute immediately completely or not at all
        FOK = 3, // with price cap
        FOK_BUDGET = 4 // total amount cap
    }
}