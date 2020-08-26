using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Collections.Utils
{
    //http://hg.openjdk.java.net/jdk8/jdk8/jdk/file/687fd7c7986d/src/share/classes/java/lang/Long.java
    public static class LongHelper
    {

        /**
         * Returns the number of zero bits preceding the highest-order
         * ("leftmost") one-bit in the two's complement binary representation
         * of the specified {@code long} value.  Returns 64 if the
         * specified value has no one-bits in its two's complement representation,
         * in other words if it is equal to zero.
         *
         * <p>Note that this method is closely related to the logarithm base 2.
         * For all positive {@code long} values x:
         * <ul>
         * <li>floor(log<sub>2</sub>(x)) = {@code 63 - numberOfLeadingZeros(x)}
         * <li>ceil(log<sub>2</sub>(x)) = {@code 64 - numberOfLeadingZeros(x - 1)}
         * </ul>
         *
         * @param i the value whose number of leading zeros is to be computed
         * @return the number of zero bits preceding the highest-order
         *     ("leftmost") one-bit in the two's complement binary representation
         *     of the specified {@code long} value, or 64 if the value
         *     is equal to zero.
         * @since 1.5
         */
        public static int numberOfLeadingZeros(long i)
        {
            // HD, Figure 5-6
            if (i == 0)
                return 64;
            int n = 1;
            int x = (int)(ShiftHelper.UnsignShiftRight(i, 32));
            if (x == 0) { n += 32; x = (int)i; }
            if (ShiftHelper.UnsignShiftRight(x, 16) == 0) { n += 16; x <<= 16; }
            if (ShiftHelper.UnsignShiftRight(x, 24) == 0) { n += 8; x <<= 8; }
            if (ShiftHelper.UnsignShiftRight(x, 28) == 0) { n += 4; x <<= 4; }
            if (ShiftHelper.UnsignShiftRight(x, 30) == 0) { n += 2; x <<= 2; }
            n -= (int)ShiftHelper.UnsignShiftRight(x , 31);
            return n;
        }

        /**
    * Returns the number of zero bits following the lowest-order ("rightmost")
    * one-bit in the two's complement binary representation of the specified
    * {@code long} value.  Returns 64 if the specified value has no
    * one-bits in its two's complement representation, in other words if it is
    * equal to zero.
    *
    * @param i the value whose number of trailing zeros is to be computed
    * @return the number of zero bits following the lowest-order ("rightmost")
    *     one-bit in the two's complement binary representation of the
    *     specified {@code long} value, or 64 if the value is equal
    *     to zero.
    * @since 1.5
    */
        public static int numberOfTrailingZeros(long i)
        {
            // HD, Figure 5-14
            int x, y;
            if (i == 0) return 64;
            int n = 63;
            y = (int)i; if (y != 0) { n = n - 32; x = y; } else x = (int)(ShiftHelper.UnsignShiftRight(i, 32));
            y = x << 16; if (y != 0) { n = n - 16; x = y; }
            y = x << 8; if (y != 0) { n = n - 8; x = y; }
            y = x << 4; if (y != 0) { n = n - 4; x = y; }
            y = x << 2; if (y != 0) { n = n - 2; x = y; }
            return n - (int)(ShiftHelper.UnsignShiftRight((x << 1), 31));
        }
    }
}
