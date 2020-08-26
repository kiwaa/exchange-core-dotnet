using System;

namespace Exchange.Core.Common.Api
{
    public sealed partial class ApiAddUser : ApiCommand
    {
        public override string ToString()
        {
            return "[ADDUSER " + Uid + "]";
        }

    }
}