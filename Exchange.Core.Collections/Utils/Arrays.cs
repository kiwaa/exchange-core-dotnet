using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Collections.Utils
{
    public static class Arrays
    {
        public static void fill<T>(T[] a, T value)
        {
            for (int i = 0; i < a.Length; i++)
                a[i] = value;
        }
    }
}
