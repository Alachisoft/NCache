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

using System;
using System.Collections.Generic;
using ProtoBuf.Serializers.Pooling;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Common.Pooling.Options;
using Alachisoft.NCache.Common.Pooling.Exception;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Pooling.ArrayPool;

namespace Alachisoft.NCache.Common.Pooling
{
    public class ObjectPool<T> : ProtoPoolBase<T>, IObjectPool where T : ILeasable
    {
        #region ---------------------------- [ Fields ] ----------------------------

        private readonly int _initialCapacity;
        private readonly float _servingCapabilityBorderline;

        private readonly PoolManager _poolManager;
        private readonly IPooledObjectInstantiator<T> _instantiator;

        protected ClusteredArray<T> _pooledObjectStack;
        protected readonly object _operationLock = new object();

        protected int _top;
        private long _hits;
        private long _misses;
        private long _totalRequests;

        #endregion

        #region ------------------------- [ Properties ] ---------------------------

        public int Count
        {
            get; protected set;
        }

        public int Capacity
        {
            get; private set;
        }

        public virtual string PoolName
        {
            get => "GrowableObjectPool";
        }

        protected PoolManager PoolManager
        {
            get;
        }

        protected IPooledObjectInstantiator<T> Instantiator
        {
            get;
        }

        protected virtual bool ShouldGrow
        {
            get
            {
                if (_misses == 0)
                    return false;

                float servingCapability = (float)_hits / _totalRequests;
                return servingCapability <= _servingCapabilityBorderline;
            }
        }

        protected virtual bool IncludeMisses { get { return true; } }

        #endregion

        #region ------------------------- [ Constructors ] -------------------------

        internal ObjectPool(PoolManager poolManager, PoolingOptions<T> options)
        {
            PoolManager = poolManager;
            Instantiator = options.Instantiator;

            _initialCapacity = options.InitialCapacity;
            _servingCapabilityBorderline = options.ServingCapabilityBorderline;

            ResetPool();
        }

        #endregion

        #region --------------------------- [ Behavior ] ---------------------------

        public override T Rent(bool initialize = false)
        {
            T item;
            var popped = false;

            lock (_operationLock)
            {
                if (popped = TryPop(out item))
                {
                    _hits++;
                }
                else
                {
                    _misses++;
                }
                _totalRequests++;

                if (ShouldGrow)
                    GrowPool();
            }

            if (!popped)
            {
                item = GetInstanceSafe();

                if (initialize)
                    item.ResetLeasable();
            }

            item.IsOutOfPool = true;
            return item;
        }

        public override T[] Rent(int numberOfItems, bool initialize = false)
        {
            if (numberOfItems < 0)
                throw new ArgumentException("Invalid number of items requested for renting.", nameof(numberOfItems));

            if (numberOfItems == 0)
                return new T[0];

            var index = 0;
            var items = new T[numberOfItems];

            lock (_operationLock)
            {
                for (; index < numberOfItems; index++)
                {
                    if (!TryPop(out items[index]))
                    {
                        break;
                    }
                }
                _hits += index;
                _misses += numberOfItems - index;
                _totalRequests += numberOfItems;
            }

            if (initialize)
            {
                for (; index < numberOfItems; index++)
                {
                    var item = GetInstanceSafe();
                    item.IsOutOfPool = true;
                    items[index] = item;
                    items[index].ResetLeasable();
                }
            }
            else
            {
                for (; index < numberOfItems; index++)
                {
                    items[index] = GetInstanceSafe();
                    if (initialize)
                        items[index].ResetLeasable();
                }
            }

            if (ShouldGrow)
            {
                lock (_operationLock)
                {
                    if (ShouldGrow)
                    {
                        GrowPool();
                    }
                }
            }
            return items;
        }

        public override void Return(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!item.IsFromPool)
                return;

            if (!item.IsInUse)
            {
                item.ResetLeasable();
                lock (_operationLock)
                {
                    TryPush(item);
                }
            }
        }

        public override void Return(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            lock (_operationLock)
            {
                foreach (var item in items)
                {
                    if (item == null)
                        throw new ArgumentNullException("One of the items returned to pool is null.");

                    if (!item.IsFromPool)
                        continue;

                    if (!item.IsInUse)
                    {
                        item.ResetLeasable();
                        TryPush(item);
                    }
                }
            }
        }

        public virtual void ResetPool()
        {
            lock (_operationLock)
            {
                Count = _initialCapacity;
                Capacity = _initialCapacity;

                _pooledObjectStack = new ClusteredArray<T>(Capacity);

                for (int i = 0; i < Capacity; i++)
                {
                    var instance = _pooledObjectStack[i] = GetInstanceSafe();
                    instance.ResetLeasable();
                }

                _top = Capacity;
                _hits = 0;
                _misses = 0;
                _totalRequests = 0;
            }
        }

        #region ------------------ [ IObjectPool Implementation ] ------------------

        object IObjectPool.Rent() => Rent(true);

        #endregion

        #endregion

        #region ----------------------- [ ObjectPoolStats ] ------------------------

        ObjectPoolStats IObjectPool.GetStats()
        {
            lock (_operationLock)
            {
                return new ObjectPoolStats
                {
                    Top = _top,
                    Hits = _hits,
                    Count = Count,
                    Misses = _misses,
                    Capacity = Capacity,
                    TotalRequests = _totalRequests,
                    ServingCapabilityBorderline = _servingCapabilityBorderline,
                };
            }
        }

        #endregion

        #region ------------------------ [ Helper Methods ] ------------------------

        protected virtual bool TryPush(T item)
        {
            var oldTop = _top;
            //item is already part of pool; avoides duplicate returns
            if (!item.IsOutOfPool) return false;

            if (_top < _pooledObjectStack.Length)
            {
                item.IsOutOfPool = false;
                _pooledObjectStack[_top++] = item; Count++;
            }
            return oldTop != _top;
        }

        protected virtual bool TryPop(out T item)
        {
            var oldTop = _top;
            item = default(T);

            if (_top > 0)
            {
                item = _pooledObjectStack[--_top];
                _pooledObjectStack[_top] = default(T);
                Count--;
                item.IsOutOfPool = true;
            }
            return oldTop != _top;
        }

        private void GrowPool()
        {
            long newCapacity = 2 * Capacity;
            var newItemsCount = newCapacity - Capacity;

            if (IncludeMisses)
                newCapacity += _misses;


            if ((newCapacity - Capacity) > 100000)
            {
                newCapacity = Capacity + 100000;
                newItemsCount = 100000;
            }

            int sizeToGrow = (int) newCapacity;
            if (sizeToGrow < 0) return;

            _pooledObjectStack.Resize((int)newCapacity);

            var newTop = _top + (int)newItemsCount;

            for (var i = _top; i < newTop; i++)
            {
                var instance = _pooledObjectStack[i % sizeToGrow] = GetInstanceSafe();
                instance.ResetLeasable();
            }

            _top = newTop >= sizeToGrow ? sizeToGrow : newTop;
            Count = _top; Capacity = sizeToGrow;

            _hits = 0;
            _misses = 0;
            _totalRequests = 0;
        }

        protected T GetInstanceSafe()
        {
            var instance = Instantiator.Instantiate();

            if (instance == null)
                throw new ObjectInstantiationException($"Failed to initialize {typeof(T)} instance");

            instance.PoolManager = PoolManager;
            return instance;
        }

        #endregion
    }

    internal class ArrayOjectPool<T> : ObjectPool<ArrayWrapper<T>>
    {
        bool _growable = true;
        public sealed override string PoolName
        {
            get => "GrowableArrayObjectPool";
        }

        internal ArrayOjectPool(PoolManager poolManager, PoolingOptions<ArrayWrapper<T>> options, bool growable) : base(poolManager, options)
        {
            _growable = growable;
        }

        public T[] RentArray(bool initialize = false)
        {
            lock(_operationLock)
            {
                var wrapper = base.Rent(initialize);
                T[] array = wrapper.Array;
                wrapper.Array = null;
                return array;
            }
        }
        protected override bool TryPop(out ArrayWrapper<T> item)
        {
            var oldTop = _top;
            item = default(ArrayWrapper<T>);

            if (_top > 0)
            {
                item = _pooledObjectStack[--_top];
                Count--;
            }
            return oldTop != _top;
        }

        internal bool Return(T[] item)
        {
            lock (_operationLock)
            {
                return TryPush(item);
            }
        }

        private bool TryPush(T[] item)
        {
            var oldTop = _top;

            if (_top < _pooledObjectStack.Length)
            {
                if (_pooledObjectStack[_top] == null)
                    _pooledObjectStack[_top] = GetInstanceSafe();

                _pooledObjectStack[_top++].Array = item;
                Count++;
            }
            return oldTop != _top;
        }

        protected override bool ShouldGrow
        {
            get
            {
                if (!_growable && Capacity > Common.Util.ServiceConfiguration.TransactionalPoolCapacity) return false;
                return base.ShouldGrow;
            }
        }

        protected override bool IncludeMisses => false;
    }
}
