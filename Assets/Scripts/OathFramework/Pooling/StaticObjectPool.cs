using OathFramework.Utility;

namespace OathFramework.Pooling
{
    public static class StaticObjectPool<T> where T : new()
    {
        private static QList<T> pool;
        private const int PoolSize = 8;
        
        static StaticObjectPool()
        {
            pool = new QList<T>(PoolSize);
            for(int i = 0; i < PoolSize; i++) {
                AddNew();
            }
        }
        
        public static T Retrieve()
        {
            if(pool.Count == 0) {
                AddNew();
            }

            T obj = pool.Array[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            return obj;
        }

        public static void Return(T obj) => pool.Add(obj);
        private static void AddNew()     => pool.Add(new T());
    }
}
