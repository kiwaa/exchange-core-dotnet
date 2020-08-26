using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api
{
    public abstract class ApiCommand
    {
        public long Timestamp { get; set; }
    }
}
