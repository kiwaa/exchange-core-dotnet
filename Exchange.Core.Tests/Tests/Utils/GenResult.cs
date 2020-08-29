using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Utils
{
    public sealed partial class GenResult
    {
        public IEnumerable<OrderCommand> getCommands()
        {
            return CommandsFill.Union(CommandsBenchmark);
        }

        public int size()
        {
            return CommandsFill.Count + CommandsBenchmark.Count;
        }

    }
}
