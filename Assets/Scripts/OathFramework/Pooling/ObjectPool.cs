using OathFramework.Utility;

namespace OathFramework.Pooling
{
    public class ObjectPool<T> where T : new()
    {
        private QList<T> pool;
        
        public ObjectPool(int poolSize = 8)
        {
            pool = new QList<T>(poolSize);
            for(int i = 0; i < poolSize; i++) {
                AddNew();
            }
        }
        
        public T Retrieve()
        {
            if(pool.Count == 0) {
                AddNew();
            }
            T obj = pool.Array[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            return obj;
        }

        public void Return(T obj) => pool.Add(obj);
        private void AddNew()     => pool.Add(new T());
    }
}
