using System;

namespace Exchange.Core.Common.Api
{
    public sealed partial class ApiCancelOrder : ApiCommand
    {

        public override string ToString()
        {
            return "[CANCEL " + OrderId + " u" + Uid + " s" + Symbol + "]";
        }

    }
}