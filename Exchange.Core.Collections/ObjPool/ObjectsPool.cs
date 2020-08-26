using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Collections.ObjPool
{
    public sealed class ObjectsPool
    {

        public static readonly int ORDER = 0;

        public static readonly int DIRECT_ORDER = 1;
        public static readonly int DIRECT_BUCKET = 2;
        public static readonly int ART_NODE_4 = 8;
        public static readonly int ART_NODE_16 = 9;
        public static readonly int ART_NODE_48 = 10;
        public static readonly int ART_NODE_256 = 11;
        public static readonly int SYMBOL_POSITION_RECORD = 12;

        private readonly ArrayStack[] pools;


        public static ObjectsPool createDefaultTestPool()
        {
            // initialize object pools
            var objectsPoolConfig = new Dictionary<int, int>();
            objectsPoolConfig.Add(ObjectsPool.DIRECT_ORDER, 512);
            objectsPoolConfig.Add(ObjectsPool.DIRECT_BUCKET, 256);
            objectsPoolConfig.Add(ObjectsPool.ART_NODE_4, 256);
            objectsPoolConfig.Add(ObjectsPool.ART_NODE_16, 128);
            objectsPoolConfig.Add(ObjectsPool.ART_NODE_48, 64);
            objectsPoolConfig.Add(ObjectsPool.ART_NODE_256, 32);

            return new ObjectsPool(objectsPoolConfig);
        }

        public ObjectsPool(Dictionary<int, int> sizesConfig)
        {
            int maxStack = sizesConfig.Count > 0 ? sizesConfig.Keys.Max() : 0; // sizesConfig.keySet().stream().max(Integer::compareTo).orElse(0);
            this.pools = new ArrayStack[maxStack + 1];

            //sizesConfig.forEach((type, size) => this.pools[type] = new Stack(size));
            foreach (var pair in sizesConfig)
            {
                this.pools[pair.Key] = new ArrayStack(pair.Value);
            }
        }

        public T get<T>(int type, Func<T> supplier) where T : class
        {
            T obj = (T)pools[type].Pop();  // pollFirst is cheaper for empty pool

            if (obj == null)
            {
                //            log.debug("MISS {}", type);
                return supplier();
            }
            else
            {
                //            log.debug("HIT {} (count={})", type, pools[type].count);
                return obj;
            }
        }
        public T get<T>(int type, Func<ObjectsPool, T> constructor) where T : class
        {
            T obj = (T)pools[type].Pop();  // pollFirst is cheaper for empty pool

            if (obj == null)
            {
                //            log.debug("MISS {}", type);
                return constructor(this);
            }
            else
            {
                //            log.debug("HIT {} (count={})", type, pools[type].count);
                return obj;
            }
        }

        public void Put(int type, object obj)
        {
            //        log.debug("RETURN {} (count={})", type, pools[type].count);
            pools[type].Add(obj);
        }


    }
}
