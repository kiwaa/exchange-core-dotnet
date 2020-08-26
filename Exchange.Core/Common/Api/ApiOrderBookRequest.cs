using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api
{
    public sealed partial class ApiOrderBookRequest
    {
        public override string ToString()
        {
            return "[OB " + Symbol + " " + Size + "]";
        }
    }
}
