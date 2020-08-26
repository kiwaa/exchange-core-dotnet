using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core
{
    class CommandExecutionResult
    {
        public int symbol { get; set; }
        public long volume { get; set; }
        public long price { get; set; }
        public long orderId { get; set; }
        public long uid { get; set; }
        public long timestamp { get; set; }
    }
}
