using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Collections.ObjPool
{
    public sealed class NaiveObjectsPool
    {

        public static readonly int ORDER = 0;

        public static readonly int DIRECT_ORDER = 1;
        public static readonly int DIRECT_BUCKET = 2;
        public static readonly int ART_NODE_4 = 8;
        public static readonly int ART_NODE_16 = 9;
        public static readonly int ART_NODE_48 = 10;
        public static readonly int ART_NODE_256 = 11;
        public static readonly int SYMBOL_POSITION_RECORD = 12;

        private Dictionary<Type, object> _pools = new Dictionary<Type, object>();

        public static NaiveObjectsPool createDefaultTestPool()
        {
            // initialize object pools
            //var objectsPoolConfig = new Dictionary<int, int>();
            //objectsPoolConfig.Add(ObjectsPool.DIRECT_ORDER, 512);
            //objectsPoolConfig.Add(ObjectsPool.DIRECT_BUCKET, 256);
            //objectsPoolConfig.Add(ObjectsPool.ART_NODE_4, 256);
            //objectsPoolConfig.Add(ObjectsPool.ART_NODE_16, 128);
            //objectsPoolConfig.Add(ObjectsPool.ART_NODE_48, 64);
            //objectsPoolConfig.Add(ObjectsPool.ART_NODE_256, 32);

            return new NaiveObjectsPool();
        }

        public NaiveObjectsPool()
        {
            //this.pools = new Dictionary<Type, ;

            //sizesConfig.forEach((type, size) => this.pools[type] = new Stack(size));
        }

        //public T get<T>(int type, Func<T> supplier) where T : class
        //{
        //    T obj = (T)pools[type].Pop();  // pollFirst is cheaper for empty pool

        //    if (obj == null)
        //    {
        //        //            log.debug("MISS {}", type);
        //        return supplier();
        //    }
        //    else
        //    {
        //        //            log.debug("HIT {} (count={})", type, pools[type].count);
        //        return obj;
        //    }
        //}
        public T get<T>(int type, Func<NaiveObjectsPool, T> constructor) where T : class
        {
            if (!_pools.TryGetValue(typeof(T), out object pool))
            {
                pool = new NaiveObjectPool<T>();
                _pools.Add(typeof(T), pool);
            }
            T obj = ((NaiveObjectPool<T>)pool).Get();

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

        public void Put<T>(int type, T obj) where T : class
        {
            if (!_pools.TryGetValue(typeof(T), out object pool))
            {
                pool = new NaiveObjectPool<T>();
                _pools.Add(typeof(T), pool);
            }
            //        log.debug("RETURN {} (count={})", type, pools[type].count);
            //pools[type].Add(obj);
             ((NaiveObjectPool<T>)pool).Return(obj);
        }
    }
}
