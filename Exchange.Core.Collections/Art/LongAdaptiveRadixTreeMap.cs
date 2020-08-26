using Exchange.Core.Collections.Art;
using Exchange.Core.Collections.ObjPool;
using Exchange.Core.Collections.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exchange.Core.Collections
{
    /**
     * Adaptive Radix Tree (ART) Java implementation
     * <p>
     * based on original paper:
     * <p>
     * The Adaptive Radix Tree:
     * ARTful Indexing for Main-Memory Databases
     * <p>
     * Viktor Leis, Alfons Kemper, Thomas Neumann
     * Fakultat fur Informatik
     * Technische Universitat Munchen
     * Boltzmannstrae 3, D-85748 Garching
     * <p>
     * https://db.in.tum.de/~leis/papers/ART.pdf
     * <p>
     * Target operations:
     * - GET or (PUT + GET_LOWER/HIGHER) - placing/moving/bulkload order - often GET, more rare PUT ??cache
     * - REMOVE - cancel or move - last order in the bucket
     * - TRAVERSE from LOWER - filling L2 market data, in hot area (Node256 or Node48).
     * - REMOVE price during matching - !! can use RANGE removal operation - rare, but latency critical
     * - GET or PUT if not exists - inserting back own orders, very rare
     */
    public sealed class LongAdaptiveRadixTreeMap<V> where V : class
    {

        private static readonly int INITIAL_LEVEL = 56;

        private IArtNode<V> root = null;

        private ObjectsPool objectsPool;

        public LongAdaptiveRadixTreeMap(ObjectsPool objectsPool)
        {
            this.objectsPool = objectsPool;
        }

        public LongAdaptiveRadixTreeMap()
        {
            objectsPool = ObjectsPool.createDefaultTestPool();
        }

        public V get(long key)
        {
            return root != null
                    ? root.getValue(key, INITIAL_LEVEL)
                    : null;
        }

        public void put(long key, V value)
        {
            if (root == null)
            {
                ArtNode4<V> node = objectsPool.get(ObjectsPool.ART_NODE_4, pool => new ArtNode4<V>(pool));
                node.initFirstKey(key, value);
                root = node;
            }
            else
            {

                IArtNode<V> upSizedNode = root.put(key, INITIAL_LEVEL, value);
                if (upSizedNode != null)
                {
                    // TODO put old into the pool
                    root = upSizedNode;
                }
            }
        }

        public V getOrInsert(long key, Func<V> supplier)
        {
            // TODO implement
            return null;
        }

        public void getOrInsertFromNode(IArtNode<V> node, Func<V> supplier)
        {
            // TODO implement
        }

        public void remove(long key)
        {
            if (root != null)
            {
                IArtNode<V> downSizeNode = root.remove(key, INITIAL_LEVEL);
                // ignore null because can not remove root
                if (downSizeNode != root)
                {
                    // TODO put old into the pool
                    root = downSizeNode;
                }
            }
        }

        public void clear()
        {
            // produces garbage
            root = null;
        }

        /**
         * remove keys range
         *
         * @param keyFromInclusive from key inclusive
         * @param keyToExclusive   to key exclusive
         */
        public void removeRange(long keyFromInclusive, long keyToExclusive)
        {
            // TODO
            throw new NotImplementedException();
            //throw new UnsupportedOperationException();
        }


        // TODO putAndGetHigherValue
        // TODO putAndGetLowerValue

        // TODO moveToAnotherKey(long oldKey, long newKey) - throw exception if not found

        public V getHigherValue(long key)
        {
            if (root != null && key != long.MaxValue)
            {
                return root.getCeilingValue(key + 1, INITIAL_LEVEL);
            }
            else
            {
                return null;
            }
        }

        public V getLowerValue(long key)
        {
            if (root != null && key != 0)
            {
                return root.getFloorValue(key - 1, INITIAL_LEVEL);
            }
            else
            {
                return null;
            }
        }

        public int forEach(Action<long, V> consumer, int limit)
        {
            if (root != null)
            {
                return root.forEach(consumer, limit);
            }
            else
            {
                return 0;
            }
        }

        public int forEachDesc(Action<long, V> consumer, int limit)
        {
            if (root != null)
            {
                return root.forEachDesc(consumer, limit);
            }
            else
            {
                return 0;
            }
        }

        public int size(int limit)
        {
            if (root != null)
            {
                return Math.Min(root.size(limit), limit);
            }
            else
            {
                return 0;
            }
        }

        public List<KeyValuePair<long, V>> entriesList()
        {
            if (root != null)
            {
                return root.entries();
            }
            else
            {
                return new List<KeyValuePair<long, V>>(); // Collections.emptyList();
            }
        }

        public void validateInternalState()
        {
            if (root != null)
            {
                // TODO initial level
                root.validateInternalState(INITIAL_LEVEL);
            }
        }

        public String printDiagram()
        {
            if (root != null)
            {
                return root.printDiagram("", INITIAL_LEVEL);
            }
            else
            {
                return "";
            }
        }


        public static IArtNode<V> branchIfRequired(long key, V value, long nodeKey, int nodeLevel, IArtNode<V> caller)
        {
            long keyDiff = key ^ nodeKey;

            // check if there is common part
            if ((keyDiff & (-1L << nodeLevel)) == 0)
            {
                return null;
            }

            // on which level
            int newLevel = (63 - LongHelper.numberOfLeadingZeros(keyDiff)) & 0xF8;
            if (newLevel == nodeLevel)
            {
                return null;
            }

            ObjectsPool objectsPool = caller.getObjectsPool();
            ArtNode4<V> newSubNode = objectsPool.get(ObjectsPool.ART_NODE_4, pool => new ArtNode4<V>(pool));
            newSubNode.initFirstKey(key, value);

            ArtNode4<V> newNode = objectsPool.get(ObjectsPool.ART_NODE_4, pool => new ArtNode4<V>(pool));
            newNode.initTwoKeys(nodeKey, caller, key, newSubNode, newLevel);

            return newNode;
        }

        //    static boolean keyNotMatches(long key, int level, long nodeKey, int nodeLevel) {
        //        return (level != nodeLevel && ((key ^ nodeKey) & (-1L << (nodeLevel + 8))) != 0);
        //    }
        // TODO remove based on leaf  (having reference) ?

        public static String printDiagram(String prefix,
                                   int level,
                                   int nodeLevel,
                                   long nodeKey,
                                   short numChildren,
                                   Func<short, short> subKeys,
                                   Func<short, object> nodes)
        {

            String baseKeyPrefix;
            String baseKeyPrefix1;
            int lvlDiff = level - nodeLevel;
            //        log.debug("nodeKey={} level={} nodeLevel={} lvlDiff={}", String.format("%X", nodeKey), level, nodeLevel, lvlDiff);

            if (lvlDiff != 0)
            {
                int chars = lvlDiff >> 2;
                //            baseKeyPrefix = String.format("[%0" + chars + "X]", nodeKey & ((1L << lvlDiff) - 1L) << nodeLevel);
                long mask = ((1L << lvlDiff) - 1L);
                //            log.debug("       mask={}", String.format("%X", mask));
                //            log.debug("       nodeKey >> level = {}", String.format("%X", nodeKey >> (nodeLevel + 8)));
                //            log.debug("       nodeKey >> level  & mask= {}", String.format("%X", (nodeKey >> (nodeLevel + 8)) & mask));
                baseKeyPrefix = charRepeat('─', chars - 2) + String.Format("[%0" + chars + "X]", (nodeKey >> (nodeLevel + 8)) & mask);
                baseKeyPrefix1 = charRepeat(' ', chars * 2);
            }
            else
            {
                baseKeyPrefix = "";
                baseKeyPrefix1 = "";
            }
            //       log.debug("baseKeyPrefix={}", baseKeyPrefix);


            StringBuilder sb = new StringBuilder();
            for (short i = 0; i < numChildren; i++)
            {
                Object node = nodes(i);
                String key = String.Format("%s%02X", baseKeyPrefix, subKeys(i));
                String x = (i == 0 ? (numChildren == 1 ? "──" : "┬─") : (i + 1 == numChildren ? (prefix + "└─") : (prefix + "├─")));

                if (nodeLevel == 0)
                {
                    sb.Append(x + key + " = " + node);
                }
                else
                {
                    sb.Append(x + key + "" + (((IArtNode<V>)node).printDiagram(prefix + (i + 1 == numChildren ? "    " : "│   ") + baseKeyPrefix1, nodeLevel - 8)));
                }
                if (i < numChildren - 1)
                {
                    sb.Append("\n");
                }
                else if (nodeLevel == 0)
                {
                    sb.Append("\n" + prefix);
                }
            }
            return sb.ToString();
        }

        private static String charRepeat(char x, int n)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Append(x);
            }
            return sb.ToString();
        }


        //public class Entry<V>
        //{
        //    public long Key { get; }
        //    public V Value { get; set; }

        //    public Entry(long key, V value)
        //    {
        //        Key = key;
        //        Value = value;
        //    }

        //}

    }
}
