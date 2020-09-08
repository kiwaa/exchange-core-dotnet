using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Utils
{
    public static class DictionaryExtensions
    {
        public static long AddValue(this Dictionary<int, long> dict, int key, long value)
        {
            if (!dict.TryGetValue(key, out long old))
            {
                // nop
            }
            return dict[key] = old + value;
        }
    }
}
