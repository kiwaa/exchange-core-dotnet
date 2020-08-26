using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Collections.Utils
{
    public static class ShiftHelper
    {
        public static long UnsignShiftRight(long value, int num)
        {
            return (long)(((ulong)value) >> num);
        }

        public static int UnsignShiftRight(int value, int num)
        {
            return (int)(((uint)value) >> num);
        }
    }
}
