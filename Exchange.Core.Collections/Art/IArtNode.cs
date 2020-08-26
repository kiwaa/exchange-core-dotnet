using Exchange.Core.Collections.ObjPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Collections.Art
{

    public interface IArtNode<V>
    {
        V getValue(long key, int level);

        IArtNode<V> put(long key, int level, V value);

        IArtNode<V> remove(long key, int level);

        V getCeilingValue(long key, int level);

        V getFloorValue(long key, int level);

        int forEach(Action<long, V> consumer, int limit);

        int forEachDesc(Action<long, V> consumer, int limit);

        /**
         * Get number of elements
         * Slow operation - O(n) complexity
         *
         * @param limit - can provide value to operation increase performance
         * @return if returned value less than limit - it is precise size of the node
         */
        int size(int limit);

        /**
         * For testing only
         *
         * @param level level
         */
        void validateInternalState(int level);

        /**
         * For testing only
         *
         * @param prefix prefix
         * @param level  level
         * @return internal diagram part
         */
        String printDiagram(String prefix, int level);

        /**
         * For testing only
         *
         * @return list of entries
         */
        List<KeyValuePair<long, V>> entries();


        ObjectsPool getObjectsPool();
    }
}
