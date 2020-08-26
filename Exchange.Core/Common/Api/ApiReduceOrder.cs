namespace Exchange.Core.Common.Api
{
    public sealed partial class ApiReduceOrder : ApiCommand
    {
        public override string ToString()
        {
            return "[REDUCE " + OrderId + " by " + ReduceSize + "]";
        }
    }
}
