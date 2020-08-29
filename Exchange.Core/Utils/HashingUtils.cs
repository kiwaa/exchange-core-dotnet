using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Utils
{
    public sealed class HashingUtils
    {

        public static int stateHash(BitSet bitSet)
        {
            return bitSet.GetHashCode();
        }

        public static int stateHash<T>(Dictionary<long, T> hashMap) where T : class, IStateHash
        {
            long mutableLong = 0L;
            foreach (var pair in hashMap)
                mutableLong += 97 * pair.Key + 997 * pair.Value.stateHash();
            return mutableLong.GetHashCode();
        }
        public static int stateHash<T>(Dictionary<int, T> hashMap) where T : class, IStateHash
        {
            int mutableLong = 0;
            foreach (var pair in hashMap)
                mutableLong += 97 * pair.Key + 997 * pair.Value.stateHash();
            return mutableLong.GetHashCode();
        }


        public static int stateHashStream<T>(IEnumerable<T> stream) where T : class, IStateHash
        {
            int h = 0;
            IEnumerator<T> iterator = stream.GetEnumerator();
            while (iterator.MoveNext())
            {
                h = h * 31 + iterator.Current.stateHash();
            }
            return h;
        }

        /**
         * Checks if both streams contain same elements in same order
         *
         * @param s1 stream 1
         * @param s2 stream 2
         * @return true if streams contain same elements in same order
         */
        public static bool checkStreamsEqual<T>(IEnumerable<T> s1, IEnumerable<T> s2)
        {
            IEnumerator <T> iter1 = s1.GetEnumerator(), iter2 = s2.GetEnumerator();
            while (iter1.MoveNext() && iter2.MoveNext())
            {
                if (!iter1.Current.Equals(iter2.Current))
                {
                    return false;
                }
            }
            return iter1.Current == null && iter2.Current == null;
        }

    }
}
