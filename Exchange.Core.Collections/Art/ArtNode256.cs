using Exchange.Core.Collections.ObjPool;
using Exchange.Core.Collections.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Collections.Art
{

    /**
     * The largest node type is simply an array of 256
     * pointers and is used for storing between 49 and 256 entries.
     * With this representation, the next node can be found very
     * efficiently using a single lookup of the key byte in that array.
     * No additional indirection is necessary. If most entries are not
     * null, this representation is also very space efficient because
     * only pointers need to be stored.
     */
    public sealed class ArtNode256<V> : IArtNode<V> where V : class
    {

        private static readonly int NODE48_SWITCH_THRESHOLD = 37;

        // direct addressing
        public Object[] nodes { get; private set; } = new Object[256];
        public long nodeKey { get; private set; }
        public int nodeLevel { get; private set; }
        public short numChildren { get; private set; }

        private readonly ObjectsPool objectsPool;

        public ArtNode256(ObjectsPool objectsPool)
        {
            this.objectsPool = objectsPool;
        }

        public void initFromNode48(ArtNode48<V> artNode48, short subKey, Object newElement)
        {

            this.nodeLevel = artNode48.nodeLevel;
            this.nodeKey = artNode48.nodeKey;
            short sourceSize = 48;
            for (short i = 0; i < 256; i++)
            {
                sbyte index = artNode48.indexes[i];
                if (index != -1)
                {
                    this.nodes[i] = artNode48.nodes[index];
                }
            }
            this.nodes[subKey] = newElement;
            this.numChildren = (short)(sourceSize + 1);

            Arrays.fill(artNode48.nodes, null);
            Arrays.fill(artNode48.indexes, (sbyte)-1);
            objectsPool.Put(ObjectsPool.ART_NODE_48, artNode48);
        }

        public V getValue(long key, int level)
        {
            if (level != nodeLevel && ((key ^ nodeKey) & (-1L << (nodeLevel + 8))) != 0)
            {
                return null;
            }
            short idx = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            Object node = nodes[idx];
            if (node != null)
            {
                return nodeLevel == 0
                        ? (V)node
                        : ((IArtNode<V>)node).getValue(key, nodeLevel - 8);
            }
            return null;
        }

        public IArtNode<V> put(long key, int level, V value)
        {
            if (level != nodeLevel)
            {
                IArtNode<V> branch = LongAdaptiveRadixTreeMap<V>.branchIfRequired(key, value, nodeKey, nodeLevel, this);
                if (branch != null)
                {
                    return branch;
                }
            }
            short idx = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            if (nodes[idx] == null)
            {
                // new object will be inserted
                numChildren++;
            }

            if (nodeLevel == 0)
            {
                nodes[idx] = value;
            }
            else
            {
                IArtNode<V> node = (IArtNode<V>)nodes[idx];
                if (node != null)
                {
                    IArtNode<V> resizedNode = node.put(key, nodeLevel - 8, value);
                    if (resizedNode != null)
                    {
                        // TODO put old into the pool
                        // update resized node if capacity has increased
                        nodes[idx] = resizedNode;
                    }
                }
                else
                {
                    ArtNode4<V> newSubNode = objectsPool.get(ObjectsPool.ART_NODE_4, pool => new ArtNode4<V>(pool));
                    newSubNode.initFirstKey(key, value);
                    nodes[idx] = newSubNode;
                }
            }

            // never need to increase the size
            return null;
        }

        public IArtNode<V> remove(long key, int level)
        {
            if (level != nodeLevel && ((key ^ nodeKey) & (-1L << (nodeLevel + 8))) != 0)
            {
                return this;
            }
            short idx = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            if (nodes[idx] == null)
            {
                return this;
            }

            if (nodeLevel == 0)
            {
                nodes[idx] = null;
                numChildren--;
            }
            else
            {
                IArtNode<V> node = (IArtNode<V>)nodes[idx];
                IArtNode<V> resizedNode = node.remove(key, nodeLevel - 8);
                if (resizedNode != node)
                {
                    // TODO put old into the pool
                    // update resized node if capacity has decreased
                    nodes[idx] = resizedNode;
                    if (resizedNode == null)
                    {
                        numChildren--;
                    }
                }
            }

            if (numChildren == NODE48_SWITCH_THRESHOLD)
            {
                ArtNode48<V> newNode = objectsPool.get(ObjectsPool.ART_NODE_48, pool => new ArtNode48<V>(pool));
                newNode.initFromNode256(this);
                return newNode;
            }
            else
            {
                return this;
            }
        }

        public V getCeilingValue(long key, int level)
        {
            //        log.debug("key = {}", String.format("%Xh", key));
            // special processing for compacted nodes
            if ((level != nodeLevel))
            {
                // try first
                long mask = -1L << (nodeLevel + 8);
                //            log.debug("key & mask = {} > nodeKey & mask = {}", String.format("%Xh", key & mask), String.format("%Xh", nodeKey & mask));
                long keyWithMask = key & mask;
                long nodeKeyWithMask = nodeKey & mask;
                if (nodeKeyWithMask < keyWithMask)
                {
                    // compacted part is lower - no need to search for ceiling entry here
                    return null;
                }
                else if (keyWithMask != nodeKeyWithMask)
                {
                    // find first lowest key, because compacted nodekey is higher
                    key = 0;
                }
            }

            short idx = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            Object node = nodes[idx];
            if (node != null)
            {
                // if exact key found
                V res = nodeLevel == 0 ? (V)node : ((IArtNode<V>)node).getCeilingValue(key, nodeLevel - 8);
                if (res != null)
                {
                    // return if found ceiling, otherwise will try next one
                    return res;
                }
            }

            // if exact key not found - searching for first higher key
            while (++idx < 256)
            {
                //            log.debug("idx+ = {}", String.format("%Xh", idx));
                node = nodes[idx];
                if (node != null)
                {
                    return (nodeLevel == 0)
                            ? (V)node
                            : ((IArtNode<V>)node).getCeilingValue(0, nodeLevel - 8);// find first lowest key
                }
            }
            // no keys found
            return null;
        }

        public V getFloorValue(long key, int level)
        {
            //        log.debug("key = {}", String.format("%Xh", key));
            // special processing for compacted nodes
            if ((level != nodeLevel))
            {
                // try first
                long mask = -1L << (nodeLevel + 8);
                //            log.debug("key & mask = {} > nodeKey & mask = {}",
                //                    String.format("%Xh", key & mask), String.format("%Xh", nodeKey & mask));
                long keyWithMask = key & mask;
                long nodeKeyWithMask = nodeKey & mask;
                if (nodeKeyWithMask > keyWithMask)
                {
                    // compacted part is higher - no need to search for floor entry here
                    return null;
                }
                else if (keyWithMask != nodeKeyWithMask)
                {
                    // find highest value, because compacted nodekey is lower
                    key = long.MaxValue;
                }
            }

            short idx = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            Object node = nodes[idx];
            if (node != null)
            {
                // if exact key found
                V res = nodeLevel == 0 ? (V)node : ((IArtNode<V>)node).getFloorValue(key, nodeLevel - 8);
                if (res != null)
                {
                    // return if found floor, otherwise will try prev one
                    return res;
                }
            }

            // if exact key not found - searching for first lower key
            while (--idx >= 0)
            {
                //            log.debug("idx+ = {}", String.format("%Xh", idx));
                node = nodes[idx];
                if (node != null)
                {
                    return (nodeLevel == 0)
                            ? (V)node
                            : ((IArtNode<V>)node).getFloorValue(long.MaxValue, nodeLevel - 8);// find first highest key
                }
            }
            // no keys found
            return null;
        }

        public int forEach(Action<long, V> consumer, int limit)
        {
            if (nodeLevel == 0)
            {
                long keyBase = (ShiftHelper.UnsignShiftRight(nodeKey, 8)) << 8;
                int numFound = 0;
                for (short i = 0; i < 256; i++)
                {
                    if (numFound == limit)
                    {
                        return numFound;
                    }
                    V node = (V)nodes[i];
                    if (node != null)
                    {
                        consumer(keyBase + i, node);
                        numFound++;
                    }
                }
                return numFound;
            }
            else
            {
                int numLeft = limit;
                for (short i = 0; i < 256 && numLeft > 0; i++)
                {
                    IArtNode<V> node = (IArtNode<V>)nodes[i];
                    if (node != null)
                    {
                        numLeft -= node.forEach(consumer, numLeft);
                    }
                }
                return limit - numLeft;
            }
        }

        public int forEachDesc(Action<long, V> consumer, int limit)
        {
            if (nodeLevel == 0)
            {
                long keyBase = (ShiftHelper.UnsignShiftRight(nodeKey, 8)) << 8;
                int numFound = 0;
                for (short i = 255; i >= 0; i--)
                {
                    if (numFound == limit)
                    {
                        return numFound;
                    }
                    V node = (V)nodes[i];
                    if (node != null)
                    {
                        consumer(keyBase + i, node);
                        numFound++;
                    }
                }
                return numFound;
            }
            else
            {
                int numLeft = limit;
                for (short i = 255; i >= 0 && numLeft > 0; i--)
                {
                    IArtNode<V> node = (IArtNode<V>)nodes[i];
                    if (node != null)
                    {
                        numLeft -= node.forEachDesc(consumer, numLeft);
                    }
                }
                return limit - numLeft;
            }
        }

        public int size(int limit)
        {
            if (nodeLevel == 0)
            {
                return numChildren;
            }
            else
            {
                int numLeft = limit;
                for (short i = 0; i < 256 && numLeft > 0; i++)
                {
                    IArtNode<V> node = (IArtNode<V>)nodes[i];
                    if (node != null)
                    {
                        numLeft -= node.size(numLeft);
                    }
                }
                return limit - numLeft;
            }
        }

        public void validateInternalState(int level)
        {
            if (nodeLevel > level) throw new System.InvalidOperationException("unexpected nodeLevel");
            int found = 0;
            for (int i = 0; i < 256; i++)
            {
                Object node = nodes[i];
                if (node != null)
                {
                    if (node is IArtNode<V>)
                    {
                        if (nodeLevel == 0) throw new System.InvalidOperationException("unexpected node type");
                        IArtNode<V> artNode = (IArtNode<V>)node;
                        artNode.validateInternalState(nodeLevel - 8);
                    }
                    else
                    {
                        if (nodeLevel != 0) throw new System.InvalidOperationException("unexpected node type");
                    }
                    found++;
                }
            }
            if (found != numChildren)
            {
                throw new System.InvalidOperationException("wrong numChildren");
            }
            if (numChildren <= NODE48_SWITCH_THRESHOLD || numChildren > 256)
            {
                throw new System.InvalidOperationException("unexpected numChildren");
            }
        }

        public List<KeyValuePair<long, V>> entries()
        {
            long keyPrefix = nodeKey & (-1L << 8);
            List<KeyValuePair<long, V>> list = new List<KeyValuePair<long, V>>();
            short[] keys = createKeysArray();
            for (int i = 0; i < numChildren; i++)
            {
                if (nodeLevel == 0)
                {
                    list.Add(new KeyValuePair<long, V>(keyPrefix + keys[i], (V)nodes[keys[i]]));
                }
                else
                {
                    list.AddRange(((IArtNode<V>)nodes[keys[i]]).entries());
                }
            }
            return list;
        }

        public String printDiagram(String prefix, int level)
        {
            short[] keys = createKeysArray();
            return LongAdaptiveRadixTreeMap<V>.printDiagram(prefix, level, nodeLevel, nodeKey, numChildren, idx => keys[idx], idx => nodes[keys[idx]]);
        }

        public ObjectsPool getObjectsPool()
        {
            return objectsPool;
        }

        public String toString()
        {
            return "ArtNode256{" +
                    "nodeKey=" + nodeKey +
                    ", nodeLevel=" + nodeLevel +
                    ", numChildren=" + numChildren +
                    '}';
        }

        private short[] createKeysArray()
        {
            short[] keys = new short[numChildren];
            int j = 0;
            for (short i = 0; i < 256; i++)
            {
                if (nodes[i] != null)
                {
                    keys[j++] = i;
                }
            }
            return keys;
        }
    }
}
