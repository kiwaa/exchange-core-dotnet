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
 * The smallest node type can store up to 4 child
 * pointers and uses an array of length 4 for keys and another
 * array of the same length for pointers. The keys and pointers
 * are stored at corresponding positions and the keys are sorted.
 */
    public class ArtNode4<V> : IArtNode<V> where V : class
    {
        // keys are ordered
        public short[] keys { get; private set; } = new short[4];
        public Object[] nodes { get; private set; } = new Object[4];
        private readonly ObjectsPool objectsPool;

        public long nodeKey { get; private set; }
        public int nodeLevel { get; private set; }

        public byte numChildren { get; private set; }

        public ArtNode4(ObjectsPool objectsPool)
        {
            this.objectsPool = objectsPool;
        }

        // terminal node has always nodeLevel=0
        public void initFirstKey(long key, V value)
        {
            // create compact node
            this.numChildren = 1;
            this.keys[0] = (short)(key & 0xFF);
            //this.nodes[0] = (level == 0) ? value : new ArtNode4<>(key, level - 8, value);
            this.nodes[0] = value;
            this.nodeKey = key;
            this.nodeLevel = 0;
        }

        // split-compact operation constructor
        public void initTwoKeys(long key1, Object value1, long key2, Object value2, int level)
        {
            //        log.debug("new level={} key1={} key2={}", level, key1, key2);
            // create compact node
            this.numChildren = 2;
            short idx1 = (short)((key1 >> level) & 0xFF);
            short idx2 = (short)((key2 >> level) & 0xFF);
            // ! smallest key first
            if (key1 < key2)
            {
                this.keys[0] = idx1;
                this.nodes[0] = value1;
                this.keys[1] = idx2;
                this.nodes[1] = value2;
            }
            else
            {
                this.keys[0] = idx2;
                this.nodes[0] = value2;
                this.keys[1] = idx1;
                this.nodes[1] = value1;
            }
            this.nodeKey = key1; // leading part the same for both keys
            this.nodeLevel = level;
        }

        // downsize 16->4
        public void initFromNode16(ArtNode16<V> artNode16)
        {
            // put original node back into pool
            objectsPool.Put(ObjectsPool.ART_NODE_16, artNode16);

            this.numChildren = artNode16.numChildren;
            Array.Copy(artNode16.keys, 0, this.keys, 0, numChildren);
            Array.Copy(artNode16.nodes, 0, this.nodes, 0, numChildren);
            this.nodeLevel = artNode16.nodeLevel;
            this.nodeKey = artNode16.nodeKey;

            Arrays.fill(artNode16.nodes, null);
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

            //        log.debug(" ------ PUT {}", String.format("%X", key));
            //        log.debug("level={} nodeLevel={}", level, nodeLevel);
            //        log.debug("key={} nodeKey={}", key, nodeKey);

            if (level != nodeLevel)
            {
                IArtNode<V> branch = LongAdaptiveRadixTreeMap<V>.branchIfRequired(key, value, nodeKey, nodeLevel, this);
                if (branch != null)
                {
                    return branch;
                }
            }

            //        log.debug("PUT key:{} level:{} value:{}", key, level, value);

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

            //        log.debug("pos:{}", pos);

            // new element
            if (numChildren != 4)
            {
                // capacity less than 4 - can simply insert node
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
                }
                numChildren++;
                return null;
            }
            else
            {
                // no space left, create a Node16 with new item
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

                ArtNode16<V> node16 = objectsPool.get(ObjectsPool.ART_NODE_16, pool => new ArtNode16<V>(pool));
                node16.initFromNode4(this, nodeIndex, newElement);

                return node16;
            }
        }

        public override string ToString()
        {
            return "ArtNode4{" +
                    "nodeKey=" + nodeKey +
                    ", nodeLevel=" + nodeLevel +
                    ", numChildren=" + numChildren +
                    '}';
        }

        public IArtNode<V> remove(long key, int level)
        {

            //        String prefix = StringUtils.repeat(" ", (56 - level) / 4);
            //        log.debug(prefix + " ------ REMOVE {}", String.format("%X", key));
            //          56 48 40 32 24 16  8  0
            // rem key  00 00 11 22 33 44 55 66

            //        log.debug(prefix + "level={} nodeLevel={}", level, nodeLevel);
            //        log.debug(prefix + "key={} nodeKey={}", key, nodeKey);


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
                    // TODO put old into the pool
                    // update resized node if capacity has decreased
                    nodes[pos] = resizedNode;
                    if (resizedNode == null)
                    {
                        removeElementAtPos(pos);
                        if (numChildren == 1)
                        {
                            //                        log.debug(prefix + "CAN MERGE! nodeLevel={} level={}", nodeLevel, level);
                            // todo put 'this' into pul
                            IArtNode<V> lastNode = (IArtNode<V>)nodes[0];
                            //   lastNode.setNodeLevel(nodeLevel);
                            return lastNode;
                        }
                    }
                }
            }

            if (numChildren == 0)
            {
                // indicate if removed last one
                Arrays.fill(nodes, null);
                objectsPool.Put(ObjectsPool.ART_NODE_4, this);
                return null;
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

            // special processing for compacted nodes
            if ((level != nodeLevel))
            {
                // try first
                long mask = -1L << (nodeLevel + 8);
                //            log.debug("key & mask = {} > nodeKey & mask = {}",
                //                    String.format("%Xh", key & mask), String.format("%Xh", nodeKey & mask));
                long keyWithMask = key & mask;
                long nodeKeyWithMask = nodeKey & mask;
                if (nodeKeyWithMask < keyWithMask)
                {
                    // compacted part is lower - no need to search for ceiling entry here
                    return null;
                }
                else if (keyWithMask != nodeKeyWithMask)
                {
                    // accept first existing entry, because compacted nodekey is higher
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

        public void validateInternalState(int level)
        {
            if (nodeLevel > level) throw new System.InvalidOperationException("unexpected nodeLevel");
            if (numChildren > 4 || numChildren < 1) throw new System.InvalidOperationException("unexpected numChildren");
            short last = -1;
            for (int i = 0; i < 4; i++)
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
                        IArtNode<V> artNode = (IArtNode<V>)node;
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

        public string printDiagram(string prefix, int level)
        {
            //        log.debug(">>>> {} level={} nodelevel={} nodekey={}", prefix, level, nodeLevel, nodeKey);
            return LongAdaptiveRadixTreeMap<V>.printDiagram(prefix, level, nodeLevel, nodeKey, numChildren,
                    idx => keys[idx], idx => nodes[idx]);
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

        public ObjectsPool getObjectsPool()
        {
            return objectsPool;
        }

    }
}