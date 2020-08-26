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
     * As the number of entries in a node increases,
     * searching the key array becomes expensive. Therefore, nodes
     * with more than 16 pointers do not store the keys explicitly.
     * Instead, a 256-element array is used, which can be indexed
     * with key bytes directly. If a node has between 17 and 48 child
     * pointers, this array stores indexes into a second array which
     * contains up to 48 pointers. This indirection saves space in
     * comparison to 256 pointers of 8 bytes, because the indexes
     * only require 6 bits (we use 1 byte for simplicity).
     */
    public sealed class ArtNode48<V> : IArtNode<V> where V : class
    {

        private static readonly int NODE16_SWITCH_THRESHOLD = 12;

        // just keep indexes
        public sbyte[] indexes { get; private set; } = new sbyte[256];
        public Object[] nodes { get; private set; } = new Object[48];
        private long freeBitMask;

        public long nodeKey { get; private set; }
        public int nodeLevel { get; private set; }

        public byte numChildren { get; private set; }

        private readonly ObjectsPool objectsPool;

        public ArtNode48(ObjectsPool objectsPool)
        {
            this.objectsPool = objectsPool;
        }

        public void initFromNode16(ArtNode16<V> node16, short subKey, Object newElement)
        {

            sbyte sourceSize = 16;
            Arrays.fill(this.indexes, (sbyte)-1);
            this.numChildren = (byte)(sourceSize + 1);
            this.nodeLevel = node16.nodeLevel;
            this.nodeKey = node16.nodeKey;

            for (sbyte i = 0; i < sourceSize; i++)
            {
                this.indexes[node16.keys[i]] = i;
                this.nodes[i] = node16.nodes[i];
            }

            this.indexes[subKey] = sourceSize;
            this.nodes[sourceSize] = newElement;
            this.freeBitMask = (1L << (sourceSize + 1)) - 1;

            Arrays.fill(node16.nodes, null);
            objectsPool.Put(ObjectsPool.ART_NODE_16, node16);
        }

        public void initFromNode256(ArtNode256<V> node256)
        {
            Arrays.fill(this.indexes, (sbyte)-1);
            this.numChildren = (byte)node256.numChildren;
            this.nodeLevel = node256.nodeLevel;
            this.nodeKey = node256.nodeKey;
            sbyte idx = 0;
            for (int i = 0; i < 256; i++)
            {
                Object node = node256.nodes[i];
                if (node != null)
                {
                    this.indexes[i] = idx;
                    this.nodes[idx] = node;
                    idx++;
                    if (idx == numChildren)
                    {
                        break;
                    }
                }
            }
            this.freeBitMask = (1L << (numChildren)) - 1;

            Arrays.fill(node256.nodes, null);
            objectsPool.Put(ObjectsPool.ART_NODE_256, node256);
        }

        public V getValue(long key, int level)
        {
            if (level != nodeLevel && ((key ^ nodeKey) & (-1L << (nodeLevel + 8))) != 0)
            {
                return null;
            }
            short idx = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            sbyte nodeIndex = indexes[idx];
            if (nodeIndex != -1)
            {
                Object node = nodes[nodeIndex];
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
            sbyte nodeIndex = indexes[idx];
            if (nodeIndex != -1)
            {
                // found
                if (nodeLevel == 0)
                {
                    nodes[nodeIndex] = value;
                }
                else
                {
                    IArtNode<V> resizedNode = ((IArtNode<V>)nodes[nodeIndex]).put(key, nodeLevel - 8, value);
                    if (resizedNode != null)
                    {
                        // update resized node if capacity has increased
                        // TODO put old into the pool
                        nodes[nodeIndex] = resizedNode;
                    }
                }
                return null;
            }

            // not found, put new element

            if (numChildren != 48)
            {
                // capacity less than 48 - can simply insert node
                sbyte freePosition = (sbyte)LongHelper.numberOfTrailingZeros(~freeBitMask);
                indexes[idx] = freePosition;

                if (nodeLevel == 0)
                {
                    nodes[freePosition] = value;
                }
                else
                {
                    ArtNode4<V> newSubNode = objectsPool.get(ObjectsPool.ART_NODE_4, pool => new ArtNode4<V>(pool));
                    newSubNode.initFirstKey(key, value);
                    nodes[freePosition] = newSubNode;
                }
                numChildren++;
                freeBitMask = freeBitMask ^ (1L << freePosition);
                return null;

            }
            else
            {
                // no space left, create a ArtNode256 containing a new item
                Object newElement;
                if (nodeLevel == 0)
                {
                    newElement = value;
                }
                else
                {
                    ArtNode4<V> newSubNode = objectsPool.get(ObjectsPool.ART_NODE_4, pool => new ArtNode4<V>(pool));
                    newSubNode.initFirstKey(key, value);
                    newElement = newSubNode;
                }

                ArtNode256<V> node256 = objectsPool.get(ObjectsPool.ART_NODE_256, pool => new ArtNode256<V>(pool));
                node256.initFromNode48(this, idx, newElement);

                return node256;
            }
        }

        public IArtNode<V> remove(long key, int level)
        {
            if (level != nodeLevel && ((key ^ nodeKey) & (-1L << (nodeLevel + 8))) != 0)
            {
                return this;
            }
            short idx = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            sbyte nodeIndex = indexes[idx];
            if (nodeIndex == -1)
            {
                return this;
            }

            if (nodeLevel == 0)
            {
                nodes[nodeIndex] = null;
                indexes[idx] = -1;
                numChildren--;
                freeBitMask = freeBitMask ^ (1L << nodeIndex);
            }
            else
            {
                IArtNode<V> node = (IArtNode<V>)nodes[nodeIndex];
                IArtNode<V> resizedNode = node.remove(key, nodeLevel - 8);
                if (resizedNode != node)
                {
                    // TODO put old into the pool
                    // update resized node if capacity has decreased
                    nodes[nodeIndex] = resizedNode;
                    if (resizedNode == null)
                    {
                        numChildren--;
                        indexes[idx] = -1;
                        freeBitMask = freeBitMask ^ (1L << nodeIndex);
                    }
                }
            }

            if (numChildren == NODE16_SWITCH_THRESHOLD)
            {
                ArtNode16<V> newNode = objectsPool.get(ObjectsPool.ART_NODE_16, pool => new ArtNode16<V>(pool));
                newNode.initFromNode48(this);
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
                    // can reset key, because compacted nodekey is higher
                    key = 0;
                }
            }

            short idx = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);

            sbyte index = indexes[idx];
            if (index != -1)
            {
                // if exact key found
                V res = nodeLevel == 0 ? (V)nodes[index] : ((IArtNode<V>)nodes[index]).getCeilingValue(key, nodeLevel - 8);
                if (res != null)
                {
                    // return if found ceiling, otherwise will try next one
                    return res;
                }
            }

            // if exact key not found - searching for first higher key
            while (++idx < 256)
            {
                index = indexes[idx];
                if (index != -1)
                {
                    if (nodeLevel == 0)
                    {
                        // found
                        return (V)nodes[index];
                    }
                    else
                    {
                        // find first lowest key
                        return ((IArtNode<V>)nodes[index]).getCeilingValue(0, nodeLevel - 8);
                    }
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

            sbyte index = indexes[idx];
            if (index != -1)
            {
                // if exact key found
                V res = nodeLevel == 0 ? (V)nodes[index] : ((IArtNode<V>)nodes[index]).getFloorValue(key, nodeLevel - 8);
                if (res != null)
                {
                    // return if found ceiling, otherwise will try prev one
                    return res;
                }
            }

            // if exact key not found - searching for first lower key
            while (--idx >= 0)
            {
                index = indexes[idx];
                if (index != -1)
                {
                    if (nodeLevel == 0)
                    {
                        // found
                        return (V)nodes[index];
                    }
                    else
                    {
                        // find first highest key
                        return ((IArtNode<V>)nodes[index]).getFloorValue(long.MaxValue, nodeLevel - 8);
                    }
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
                    sbyte index = indexes[i];
                    if (index != -1)
                    {
                        consumer(keyBase + i, (V)nodes[index]);
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
                    sbyte index = indexes[i];
                    if (index != -1)
                    {
                        numLeft -= ((IArtNode<V>)nodes[index]).forEach(consumer, numLeft);
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
                    sbyte index = indexes[i];
                    if (index != -1)
                    {
                        consumer(keyBase + i, (V)nodes[index]);
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
                    sbyte index = indexes[i];
                    if (index != -1)
                    {
                        numLeft -= ((IArtNode<V>)nodes[index]).forEachDesc(consumer, numLeft);
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
                    sbyte index = indexes[i];
                    if (index != -1)
                    {
                        numLeft -= ((IArtNode<V>)nodes[index]).size(numLeft);
                    }
                }
                return limit - numLeft;
            }
        }


        public void validateInternalState(int level)
        {
            if (nodeLevel > level) throw new System.InvalidOperationException("unexpected nodeLevel");
            int found = 0;
            HashSet<int> keysSet = new HashSet<int>();
            long expectedBitMask = 0;
            for (int i = 0; i < 256; i++)
            {
                sbyte idx = indexes[i];
                if (idx != -1)
                {
                    if (idx > 47 || idx < -1) throw new System.InvalidOperationException("wrong index");
                    keysSet.Add((int)idx);
                    found++;
                    if (nodes[idx] == null) throw new System.InvalidOperationException("null node");
                    expectedBitMask ^= (1L << idx);
                }
            }
            if (freeBitMask != expectedBitMask) throw new System.InvalidOperationException("freeBitMask is wrong");
            if (found != numChildren) throw new System.InvalidOperationException("wrong numChildren");
            if (keysSet.Count != numChildren)
            {
                throw new System.InvalidOperationException("duplicate keys keysSet=" + keysSet + " numChildren=" + numChildren);
            }
            if (numChildren > 48 || numChildren <= NODE16_SWITCH_THRESHOLD)
                throw new System.InvalidOperationException("unexpected numChildren");
            for (int i = 0; i < 48; i++)
            {
                Object node = nodes[i];
                if (keysSet.Contains(i))
                {
                    if (node == null)
                    {
                        throw new System.InvalidOperationException("null node");
                    }
                    else
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
                    }
                }
                else
                {
                    if (node != null) throw new System.InvalidOperationException("not released node");
                }
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
                    list.Add(new KeyValuePair<long, V>(keyPrefix + keys[i], (V)nodes[indexes[keys[i]]]));
                }
                else
                {
                    list.AddRange(((IArtNode<V>)nodes[indexes[keys[i]]]).entries());
                }
            }
            return list;
        }

        public String printDiagram(String prefix, int level)
        {
            short[] keys = createKeysArray();
            return LongAdaptiveRadixTreeMap<V>.printDiagram(prefix, level, nodeLevel, nodeKey, numChildren, idx => keys[idx], idx => nodes[indexes[keys[idx]]]);
        }

        public ObjectsPool getObjectsPool()
        {
            return objectsPool;
        }

        public override string ToString()
        {
            return "ArtNode48{" +
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
                if (indexes[i] != -1)
                {
                    keys[j++] = i;
                }
            }
            return keys;
        }
    }
}
