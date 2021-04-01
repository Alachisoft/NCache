//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using Alachisoft.NCache.Common.Pooling.Exception;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling.Options;
using Alachisoft.NCache.Common.Pooling.Stats;
using ProtoBuf.Serializers.Pooling;
using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.Pooling
{
    public class SimpleObjectPool<T> : ProtoPoolBase<T>, IObjectPool where T : ILeasable
    {
        private Queue<T> _objects;
        protected IPooledObjectInstantiator<T> _objectInstantiator;
        private int _capacity;

        protected PoolManager _poolManager;

        public int Count => _objects.Count;

        int IObjectPool.Capacity => _capacity;

        public virtual string PoolName
        {
            get => "SimpleObjectPool";
        }

        public SimpleObjectPool(PoolManager poolManager, int capacity, IPooledObjectInstantiator<T> objectInstantiator)
        {
            _capacity = capacity;
            _poolManager = poolManager;
            _objects = new Queue<T>(capacity);
            _objectInstantiator = objectInstantiator ?? throw new ArgumentNullException("objectInstantiator");
        }

        private bool GetObject(out T item)
        {
            bool found = false;

            lock (_objects)
            {
                if (_objects.Count == 0)
                {
                    item = GetInstanceSafe();
                }
                else
                {
                    item = _objects.Dequeue();
                    found = true;
                }
            }

            return found;
        }

        private void PutObject(T item)
        {
                item?.ResetLeasable();

                lock (_objects)
                {
                    if (!item.IsOutOfPool) return;
                    //stay within pool size limit
                    if (_objects.Count < _capacity)
                    {
                        item.IsOutOfPool = false;
                        _objects.Enqueue(item);
                    }
                }
        }

        public override void Return(T item)
        {
            PutObject(item);
        }

        public override T Rent(bool initialize = false)
        {
            bool foundFromPool = GetObject(out T item);
            if (!foundFromPool) item.ResetLeasable();
            item.IsOutOfPool = true;
            return item;
        }

        public override void Return(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (item.IsFromPool)
                {
                    item.ResetLeasable();
                }
                lock (_objects)
                {
                    if (_objects.Count < _capacity)
                    {
                        if (!item.IsOutOfPool) continue;
                        item.IsOutOfPool = false;
                        _objects.Enqueue(item);
                    }
                }
            }
        }

        public override T[] Rent(int numberOfItems, bool initialize = false)
        {
            T[] items = new T[numberOfItems];

            int index = 0;

            lock (_objects)
            {
                for (; index < numberOfItems; index++)
                {
                    if (_objects.Count == 0) break;
                    var item = _objects.Dequeue();
                    item.IsOutOfPool = true;
                    items[index] = item;
                }
            }

            if (initialize)
            {
                for (int i = 0; i < index; i++)
                    items[i].ResetLeasable();
            }

            if (index < numberOfItems)
            {
                for (; index < numberOfItems; index++)
                {
                    var item = GetInstanceSafe();
                    item.IsOutOfPool = true;
                    items[index] = item;
                }
            }

            return items;
        }

        public object Rent()
        {
            return Rent(true);
        }

        public virtual void ResetPool()
        {
            lock (_objects)
            {
                _objects = new Queue<T>(_capacity);
            }
        }

        private T GetInstanceSafe()
        {
            var instance = _objectInstantiator.Instantiate();

            if (instance == null)
                throw new ObjectInstantiationException($"Failed to initialize {typeof(T)} instance");

            instance.PoolManager = _poolManager;
            return instance;
        }


        ObjectPoolStats IObjectPool.GetStats()
        {
            lock (_objects)
            {
                return new ObjectPoolStats
                {
                    Count = Count,
                    Capacity = _capacity,
                    IsSimpleObjectPool = true,
                };
            }
        }

    }
}
