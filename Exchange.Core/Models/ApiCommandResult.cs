using Exchange.Core.Common.Api;
using Exchange.Core.Common.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core
{
    public class ApiCommandResult
    {
        public ApiCommand Command { get; set; }
        public CommandResultCode ResultCode { get; set; }
        public long Seq { get; set; }

        public ApiCommandResult(ApiCommand command, CommandResultCode resultCode, long seq)
        {
            Command = command;
            ResultCode = resultCode;
            Seq = seq;
        }
    }
}
