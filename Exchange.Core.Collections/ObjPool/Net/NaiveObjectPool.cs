using System;
using System.Collections.Concurrent;

namespace Exchange.Core.Collections.ObjPool
{
    public class NaiveObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _objects;
        
        public NaiveObjectPool()
        {
            //_objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _objects = new ConcurrentBag<T>();
        }

        public T Get() => _objects.TryTake(out T item) ? item : null;

        public void Return(T item) => _objects.Add(item);
    }
}
