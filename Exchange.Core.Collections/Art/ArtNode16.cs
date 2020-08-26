using Exchange.Core.Collections.ObjPool;
using Exchange.Core.Collections.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Collections.Art
{

    /**
     * This node type is used for storing between 5 and
     * 16 child pointers. Like the Node4, the keys and pointers
     * are stored in separate arrays at corresponding positions, but
     * both arrays have space for 16 entries. A key can be found
     * efficiently with binary search or, on modern hardware, with
     * parallel comparisons using SIMD instructions.
     */
    public sealed class ArtNode16<V> : IArtNode<V> where V : class
    {

        private static readonly int NODE4_SWITCH_THRESHOLD = 3;

        // keys are ordered
        public short[] keys { get; private set; } = new short[16];
        public Object[] nodes { get; private set; } = new Object[16];
        public long nodeKey { get; private set; }
        public int nodeLevel { get; private set; }

        public byte numChildren { get; private set; }

        private readonly ObjectsPool objectsPool;

        public ArtNode16(ObjectsPool objectsPool)
        {
            this.objectsPool = objectsPool;
        }

        public void initFromNode4(ArtNode4<V> node4, short subKey, Object newElement)
        {

            byte sourceSize = node4.numChildren;
            this.nodeLevel = node4.nodeLevel;
            this.nodeKey = node4.nodeKey;
            this.numChildren = (byte)(sourceSize + 1);
            int inserted = 0;
            for (int i = 0; i < sourceSize; i++)
            {
                int key = node4.keys[i];
                if (inserted == 0 && key > subKey)
                {
                    keys[i] = subKey;
                    nodes[i] = newElement;
                    inserted = 1;
                }
                keys[i + inserted] = node4.keys[i];
                nodes[i + inserted] = node4.nodes[i];
            }
            if (inserted == 0)
            {
                keys[sourceSize] = subKey;
                nodes[sourceSize] = newElement;
            }

            // put original node back into pool
            Arrays.fill(node4.nodes, null);
            objectsPool.Put(ObjectsPool.ART_NODE_4, node4);
        }

        public void initFromNode48(ArtNode48<V> node48)
        {
            //        log.debug("48->16 nodeLevel={} (nodekey={})", node48.nodeLevel, node48.nodeKey);
            this.numChildren = node48.numChildren;
            this.nodeLevel = node48.nodeLevel;
            this.nodeKey = node48.nodeKey;
            byte idx = 0;
            for (short i = 0; i < 256; i++)
            {
                sbyte j = node48.indexes[i];
                if (j != -1)
                {
                    this.keys[idx] = i;
                    this.nodes[idx] = node48.nodes[j];
                    idx++;
                }
                if (idx == numChildren)
                {
                    break;
                }
            }

            Arrays.fill(node48.nodes, null);
            Arrays.fill(node48.indexes, (sbyte)-1);
            objectsPool.Put(ObjectsPool.ART_NODE_48, node48);
        }

        public V getValue(long key, int level)
        {
            if (level != nodeLevel && ((key ^ nodeKey) & (-1L << (nodeLevel + 8))) != 0)
            {
                return null;
            }
            short nodeIndex = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            for (int i = 0; i < numChildren; i++)
            {
                short index = keys[i];
                if (index == nodeIndex)
                {
                    Object node = nodes[i];
                    return nodeLevel == 0
                            ? (V)node
                            : ((IArtNode<V>)node).getValue(key, nodeLevel - 8);
                }
                if (nodeIndex < index)
                {
                    // can give up searching because keys are in sorted order
                    break;
                }
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
            short nodeIndex = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            int pos = 0;
            while (pos < numChildren)
            {
                if (nodeIndex == keys[pos])
                {
                    // just update
                    if (nodeLevel == 0)
                    {
                        nodes[pos] = value;
                    }
                    else
                    {
                        IArtNode<V> resizedNode = ((IArtNode<V>)nodes[pos]).put(key, nodeLevel - 8, value);
                        if (resizedNode != null)
                        {
                            // TODO put old into the pool
                            // update resized node if capacity has increased
                            nodes[pos] = resizedNode;
                        }
                    }
                    return null;
                }
                if (nodeIndex < keys[pos])
                {
                    // can give up searching because keys are in sorted order
                    break;
                }
                pos++;
            }

            // not found, put new element
            if (numChildren != 16)
            {
                // capacity less than 16 - can simply insert node
                int copyLength = numChildren - pos;
                if (copyLength != 0)
                {
                    Array.Copy(keys, pos, keys, pos + 1, copyLength);
                    Array.Copy(nodes, pos, nodes, pos + 1, copyLength);
                }
                keys[pos] = nodeIndex;
                if (nodeLevel == 0)
                {
                    nodes[pos] = value;
                }
                else
                {
                    ArtNode4<V> newSubNode = objectsPool.get(ObjectsPool.ART_NODE_4, pool => new ArtNode4<V>(pool));
                    newSubNode.initFirstKey(key, value);
                    nodes[pos] = newSubNode;
                    newSubNode.put(key, nodeLevel - 8, value);
                }
                numChildren++;
                return null;
            }
            else
            {
                // no space left, create a Node48 with new element
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

                ArtNode48<V> node48 = objectsPool.get(ObjectsPool.ART_NODE_48, pool => new ArtNode48<V>(pool));
                node48.initFromNode16(this, nodeIndex, newElement);

                return node48;
            }
        }

        public IArtNode<V> remove(long key, int level)
        {
            if (level != nodeLevel && ((key ^ nodeKey) & (-1L << (nodeLevel + 8))) != 0)
            {
                return this;
            }
            short nodeIndex = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);
            Object node = null;
            int pos = 0;
            while (pos < numChildren)
            {
                if (nodeIndex == keys[pos])
                {
                    // found
                    node = nodes[pos];
                    break;
                }
                if (nodeIndex < keys[pos])
                {
                    // can give up searching because keys are in sorted order
                    return this;
                }
                pos++;
            }

            if (node == null)
            {
                // not found
                return this;
            }

            // removing
            if (nodeLevel == 0)
            {
                removeElementAtPos(pos);
            }
            else
            {
                IArtNode<V> resizedNode = ((IArtNode<V>)node).remove(key, nodeLevel - 8);
                if (resizedNode != node)
                {
                    // update resized node if capacity has decreased
                    nodes[pos] = resizedNode;
                    if (resizedNode == null)
                    {
                        removeElementAtPos(pos);
                    }
                }
            }

            // switch to ArtNode4 if too small
            if (numChildren == NODE4_SWITCH_THRESHOLD)
            {
                ArtNode4<V> newNode = objectsPool.get(ObjectsPool.ART_NODE_4, pool => new ArtNode4<V>(pool));
                newNode.initFromNode16(this);
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
            //        log.debug("level={} nodeLevel={} nodekey={} looking for key={} mask={}",
            //                level, nodeLevel, String.format("%Xh", nodeKey), String.format("%Xh", key), String.format("%Xh", mask));
            //
            //        log.debug("key & mask = {} > nodeKey & mask = {}",
            //                String.format("%Xh", key & mask), String.format("%Xh", nodeKey & mask));

            // special processing for compacted nodes
            if ((level != nodeLevel))
            {
                // try first
                long mask = -1L << (nodeLevel + 8);
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

            short nodeIndex = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);

            for (int i = 0; i < numChildren; i++)
            {
                short index = keys[i];
                //            log.debug("try index={} (looking for {}) key={}", String.format("%X", index), String.format("%X", nodeIndex), String.format("%X", key));
                // any equal or higher is ok
                if (index == nodeIndex)
                {
                    V res = nodeLevel == 0
                            ? (V)nodes[i]
                            : ((IArtNode<V>)nodes[i]).getCeilingValue(key, nodeLevel - 8);
                    if (res != null)
                    {
                        // return if found ceiling, otherwise will try next one
                        return res;
                    }
                }
                if (index > nodeIndex)
                {
                    // exploring first higher key
                    return nodeLevel == 0
                            ? (V)nodes[i]
                            : ((IArtNode<V>)nodes[i]).getCeilingValue(0, nodeLevel - 8); // take lowest existing key
                }
            }
            return null;
        }

        public V getFloorValue(long key, int level)
        {
            //        log.debug("key = {}", String.format("%Xh", key));
            //        log.debug("level={} nodeLevel={} nodekey={} looking for key={} mask={}",
            //                level, nodeLevel, String.format("%Xh", nodeKey), String.format("%Xh", key), String.format("%Xh", mask));

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

            short nodeIndex = (short)((ShiftHelper.UnsignShiftRight(key, nodeLevel)) & 0xFF);

            for (int i = numChildren - 1; i >= 0; i--)
            {
                short index = keys[i];
                if (index == nodeIndex)
                {
                    V res = nodeLevel == 0
                            ? (V)nodes[i]
                            : ((IArtNode<V>)nodes[i]).getFloorValue(key, nodeLevel - 8);
                    if (res != null)
                    {
                        // return if found ceiling, otherwise will try next one
                        return res;
                    }
                }
                if (index < nodeIndex)
                {
                    // exploring first lower key
                    return nodeLevel == 0
                            ? (V)nodes[i]
                            : ((IArtNode<V>)nodes[i]).getFloorValue(long.MaxValue, nodeLevel - 8); // take highest existing key
                }
            }
            return null;
        }

        public int forEach(Action<long, V> consumer, int limit)
        {
            if (nodeLevel == 0)
            {
                long keyBase = (ShiftHelper.UnsignShiftRight(nodeKey, 8)) << 8;
                int n = Math.Min(numChildren, limit);
                for (int i = 0; i < n; i++)
                {
                    consumer(keyBase + keys[i], (V)nodes[i]);
                }
                return n;
            }
            else
            {
                int numLeft = limit;
                for (int i = 0; i < numChildren && numLeft > 0; i++)
                {
                    numLeft -= ((IArtNode<V>)nodes[i]).forEach(consumer, numLeft);
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
                for (int i = numChildren - 1; i >= 0 && numFound < limit; i--)
                {
                    consumer(keyBase + keys[i], (V)nodes[i]);
                    numFound++;
                }
                return numFound;
            }
            else
            {
                int numLeft = limit;
                for (int i = numChildren - 1; i >= 0 && numLeft > 0; i--)
                {
                    numLeft -= ((IArtNode<V>)nodes[i]).forEachDesc(consumer, numLeft);
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
                for (int i = numChildren - 1; i >= 0 && numLeft > 0; i--)
                {
                    numLeft -= ((IArtNode<V>)nodes[i]).size(numLeft);
                }
                return limit - numLeft;
            }
        }

        public void validateInternalState(int level)
        {
            if (nodeLevel > level) throw new System.InvalidOperationException("unexpected nodeLevel");
            if (numChildren > 16 || numChildren <= NODE4_SWITCH_THRESHOLD)
                throw new System.InvalidOperationException("unexpected numChildren");
            short last = -1;
            for (int i = 0; i < 16; i++)
            {
                Object node = nodes[i];
                if (i < numChildren)
                {
                    if (node == null) throw new System.InvalidOperationException("null node");
                    if (keys[i] < 0 || keys[i] >= 256) throw new System.InvalidOperationException("key out of range");
                    if (keys[i] == last) throw new System.InvalidOperationException("duplicate key");
                    if (keys[i] < last) throw new System.InvalidOperationException("wrong key order");
                    last = keys[i];
                    if (node is IArtNode<V>)
                    {
                        if (nodeLevel == 0) throw new System.InvalidOperationException("unexpected node type");
                        var artNode = (IArtNode<V>)node;
                        artNode.validateInternalState(nodeLevel - 8);
                    }
                    else
                    {
                        if (nodeLevel != 0) throw new System.InvalidOperationException("unexpected node type");
                    }

                }
                else
                {
                    if (node != null) throw new System.InvalidOperationException("not released node");
                }
            }
        }

        public String printDiagram(String prefix, int level)
        {
            return LongAdaptiveRadixTreeMap<V>.printDiagram(prefix, level, nodeLevel, nodeKey, numChildren, idx => keys[idx], idx => nodes[idx]);
        }

        public List<KeyValuePair<long, V>> entries()
        {
            long keyPrefix = nodeKey & (-1L << 8);
            List<KeyValuePair<long, V>> list = new List<KeyValuePair<long, V>>();
            for (int i = 0; i < numChildren; i++)
            {
                if (nodeLevel == 0)
                {
                    list.Add(new KeyValuePair<long, V>(keyPrefix + keys[i], (V)nodes[i]));
                }
                else
                {
                    list.AddRange(((IArtNode<V>)nodes[i]).entries());
                }
            }
            return list;
        }

        private void removeElementAtPos(int pos)
        {
            int ppos = pos + 1;
            int copyLength = numChildren - ppos;
            if (copyLength != 0)
            {
                Array.Copy(keys, ppos, keys, pos, copyLength);
                Array.Copy(nodes, ppos, nodes, pos, copyLength);
            }
            numChildren--;
            nodes[numChildren] = null;
        }

        public ObjectsPool getObjectsPool()
        {
            return objectsPool;
        }

        public override String ToString()
        {
            return "ArtNode16{" +
                    "nodeKey=" + nodeKey +
                    ", nodeLevel=" + nodeLevel +
                    ", numChildren=" + numChildren +
                    '}';
        }
    }

}
