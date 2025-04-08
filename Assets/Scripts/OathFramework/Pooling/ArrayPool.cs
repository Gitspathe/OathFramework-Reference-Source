using OathFramework.Utility;

namespace OathFramework.Pooling
{
    public class ArrayPool<T>
    {
        private QList<T[]> pool;
        private int initialArraySize;
        
        public ArrayPool(int arraySize = 8, int poolSize = 8)
        {
            pool = new QList<T[]>(poolSize);
            initialArraySize = arraySize;
            for(int i = 0; i < poolSize; i++) {
                AddNew();
            }
        }

        public int Count => pool.Count;
        
        public T[] Retrieve()
        {
            if(pool.Count == 0) {
                AddNew();
            }
            T[] obj = pool.Array[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            return obj;
        }

        public void Return(T[] obj) => pool.Add(obj);
        private void AddNew()       => pool.Add(new T[initialArraySize]);
    }
}
