using OathFramework.Pooling;
using System;
using System.Collections.Generic;

namespace OathFramework.Utility
{
    public class WeightedRandom<T>
    {
        private readonly FRandom rand;
        private readonly List<T> items     = new();
        private readonly List<int> weights = new();
        private int totalWeight;

        public WeightedRandom()
        {
            rand = new FRandom();
        }

        public WeightedRandom(FRandom rng)
        {
            rand = rng;
        }

        public void Add(T item, int weight)
        {
            if(weight <= 0)
                throw new ArgumentException("Weight must be positive.", nameof(weight));

            items.Add(item);
            weights.Add(weight);
            totalWeight += weight;
        }

        public void Remove(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for(int i = 0; i < items.Count; i++) {
                if(!comparer.Equals(items[i], item))
                    continue;

                totalWeight -= weights[i];
                items.RemoveAt(i);
                weights.RemoveAt(i);
                return;
            }
        }

        public T Retrieve()
        {
            if(items.Count == 0)
                return default;

            int roll             = rand.Int(totalWeight);
            int cumulativeWeight = 0;
            for(int i = 0; i < items.Count; i++) {
                cumulativeWeight += weights[i];
                if(roll < cumulativeWeight) {
                    return items[i];
                }
            }
            throw new Exception("Failed to retrieve a valid item.");
        }
        
        public int Count => items.Count;

        public Builder RetrieveBuilder() => StaticObjectPool<Builder>.Retrieve().Initialize(this);
        private void ReturnBuilder(Builder builder) => StaticObjectPool<Builder>.Return(builder);

        public class Builder
        {
            private WeightedRandom<T> parent;
            private HashSet<T> excluded = new();

            public Builder() { }

            public Builder(WeightedRandom<T> parent)
            {
                this.parent = parent;
            }

            public Builder Initialize(WeightedRandom<T> parent)
            {
                this.parent = parent;
                return this;
            }
            
            public Builder Exclude(T item)
            {
                excluded.Add(item);
                return this;
            }

            public Builder Exclude(params T[] items)
            {
                foreach(T item in items) {
                    excluded.Add(item);
                }
                return this;
            }

            public T Retrieve()
            {
                if(parent.items.Count == 0) 
                    throw new InvalidOperationException("No items to retrieve.");

                List<T> validItems     = null;
                List<int> validWeights = null;
                try {
                    validItems           = StaticObjectPool<List<T>>.Retrieve();
                    validWeights         = StaticObjectPool<List<int>>.Retrieve();
                    int validTotalWeight = 0;
                    for(int i = 0; i < parent.items.Count; i++) {
                        T item = parent.items[i];
                        if(!excluded.Contains(item)) {
                            validItems.Add(item);
                            validWeights.Add(parent.weights[i]);
                            validTotalWeight += parent.weights[i];
                        }
                    }
                    if(validItems.Count == 0)
                        throw new InvalidOperationException("No valid items to retrieve.");

                    int roll             = parent.rand.Int(validTotalWeight);
                    int cumulativeWeight = 0;
                    for(int i = 0; i < validItems.Count; i++) {
                        cumulativeWeight += validWeights[i];
                        if(roll < cumulativeWeight) {
                            return validItems[i];
                        }
                    }
                    throw new Exception("Failed to retrieve a valid item.");
                } finally {
                    if(validItems != null) {
                        validItems.Clear();
                        StaticObjectPool<List<T>>.Return(validItems);
                    }
                    if(validWeights != null) {
                        validWeights.Clear();
                        StaticObjectPool<List<int>>.Return(validWeights);
                    }
                    Clear();
                }
            }

            private void Clear()
            {
                parent.ReturnBuilder(this);
                parent = null;
                excluded.Clear();
            }
        }
    }
}
