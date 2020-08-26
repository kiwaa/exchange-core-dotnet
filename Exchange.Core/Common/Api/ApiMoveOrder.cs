using System;

namespace Exchange.Core.Common.Api
{
    public sealed partial class ApiMoveOrder : ApiCommand
    {
        public override string ToString()
        {
            return "[MOVE " + OrderId + " " + NewPrice + " u" + Uid + " s" + Symbol + "]";
        }

    }
}