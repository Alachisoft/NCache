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
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Common.Pooling.Internal;
using ProtoBuf.Serializers.Pooling;

namespace Alachisoft.NCache.Common.Pooling
{
    public class PoolManager
    {
        private readonly IArrayPool[] _arrayPools;
        private readonly IObjectPool[] _objectPools;

        public bool IsUsingFakePools
        {
            get;
        }

        public virtual string PoolManagerName
        {
            get
            {
                return IsUsingFakePools ? "FakeGrowablePoolManager" : "GrowablePoolManager";
            }
        }

        public PoolManager(bool hardCreateFakePools)
        {
            IsUsingFakePools = hardCreateFakePools || !ServiceConfiguration.EnableObjectPooling;

            _arrayPools = new IArrayPool[System.Enum.GetValues(typeof(ArrayPoolType)).Length];
            _objectPools = new IObjectPool[System.Enum.GetValues(typeof(ObjectPoolType)).Length];
        }

        public void AddPool<T>(ObjectPoolType poolType, ObjectPool<T> pool) where T : ILeasable
        {
            var index = (int)poolType;

            if (index < 0 || index >= _objectPools.Length)
                throw new ArgumentException("Invalid argument for type of pool provided.", nameof(poolType));

            if (pool == null)
                throw new ArgumentNullException(nameof(pool));

            // If a pool already exists, ignore it
            if (_objectPools[index] != null)
                return;

            // We ought to avoid locking here
            // since we initialize all the pools synchronously
            //lock (_objectPools)
            //{
                // A form of double check locking
                if (_objectPools[index] == null)
                    _objectPools[index] = pool;
            //}
        }

        public void AddPool<T>(ArrayPoolType poolType, ArrayPool<T> pool)
        {
            var index = (int)poolType;

            if (index < 0 || index >= _arrayPools.Length)
                throw new ArgumentException("Invalid argument for type of pool provided.", nameof(poolType));

            if (pool == null)
                throw new ArgumentNullException(nameof(pool));

            // If a pool already exists, ignore it
            if (_arrayPools[index] != null)
                return;

            // We ought to avoid locking here
            // since we initialize all the pools synchronously
            //lock (_arrayPools)
            //{
                // A form of double check locking
                if (_arrayPools[index] == null)
                    _arrayPools[index] = pool;
            //}
        }

        public void CreatePool<T>(ObjectPoolType poolType, PoolingOptions<T> options) where T : ILeasable
        {
            var index = (int)poolType;

            if (index < 0 || index >= _objectPools.Length)
                throw new ArgumentException("Invalid argument for type of pool provided.", nameof(poolType));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // If the same pool has already been added, ignore it
            if (_objectPools[index] != null)
                return;

            // We ought to avoid locking here
            // since we initialize all the pools synchronously
            //lock (_objectPools)
            //{
                // A form of double check locking
                if (_objectPools[index] == null)
                    _objectPools[index] = CreateObjectPool(options);
            //}
        }

        public void CreateSimplePool<T>(ObjectPoolType poolType, PoolingOptions<T> options) where T : ILeasable
        {
            var index = (int)poolType;

            if (index < 0 || index >= _objectPools.Length)
                throw new ArgumentException("Invalid argument for type of pool provided.", nameof(poolType));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // If the same pool has already been added, ignore it
            if (_objectPools[index] != null)
                return;

            // We ought to avoid locking here
            // since we initialize all the pools synchronously
            //lock (_objectPools)
            //{
                // A form of double check locking
                if (_objectPools[index] == null)
                    _objectPools[index] = CreateSimpleObjectPool(options);
            //}
        }

        public ProtoPoolBase<T> GetSimplePool<T>(ObjectPoolType poolType) where T : ILeasable
        {
            var index = (int)poolType;

            if (index < 0 || index >= _objectPools.Length)
                throw new ArgumentException("Invalid type of pool requested.", nameof(poolType));

            // Lock not taken on purpose since our pools are always initialized before use
            // and we don't want our operations to halt because of locking
            var pool = _objectPools[index] as ProtoPoolBase<T>;

            if (pool == null)
                throw new InvalidOperationException("Requested pool has not been added to this manager.");

            return pool;
        }


        public void CreatePool<T>(ArrayPoolType poolType,bool growable)
        {
            var index = (int)poolType;

            if (index < 0 || index >= _arrayPools.Length)
                throw new ArgumentException("Invalid argument for type of pool provided.", nameof(poolType));

            // If the same pool has already been added, ignore it
            if (_arrayPools[index] != null)
                return;

            // We ought to avoid locking here
            // since we initialize all the pools synchronously
            //lock (_arrayPools)
            //{
                // A form of double check locking
                if (_arrayPools[index] == null)
                    _arrayPools[index] = CreateArrayPool<T>(growable);
            //}
        }

        public ProtoPoolBase<T> GetPool<T>(ObjectPoolType poolType) where T : ILeasable
        {
            var index = (int)poolType;

            if (index < 0 || index >= _objectPools.Length)
                throw new ArgumentException("Invalid type of pool requested.", nameof(poolType));

            // Lock not taken on purpose since our pools are always initialized before use
            // and we don't want our operations to halt because of locking
            var pool = _objectPools[index] as ProtoPoolBase<T>;

            if (pool == null)
                throw new InvalidOperationException("Requested pool has not been added to this manager.");

            return pool;
        }

        public ArrayPool<T> GetPool<T>(ArrayPoolType poolType)
        {
            var index = (int)poolType;

            if (index < 0 || index >= _arrayPools.Length)
                throw new ArgumentException("Invalid type of pool requested.", nameof(poolType));

            // Lock not taken on purpose since our pools are always initialized before use
            // and we don't want our operations to halt because of locking
            var pool = _arrayPools[index] as ArrayPool<T>;

            if (pool == null)
                throw new InvalidOperationException("Requested pool has not been added to this manager.");

            return pool;
        }

        public virtual void Clear()
        {
            // Locking is not necessary here as the critical
            // sections that need to be locked are locked
            // within the pools themselves

            if (_objectPools?.Length > 0)
            {
                foreach (var pool in _objectPools)
                {
                    pool?.ResetPool();
                }
            }
            if (_arrayPools?.Length > 0)
            {
                foreach (var arrayPool in _arrayPools)
                {
                    arrayPool?.ResetPool();
                }
            }
        }

        public virtual PoolStats GetStats(PoolStatsRequest request)
        {
            var stats = new PoolStats();

            lock (_arrayPools)
            {
                foreach (var arrayPoolType in System.Enum.GetValues(typeof(ArrayPoolType)) as int[])
                {
                    var arrayPool = _arrayPools[arrayPoolType];

                    if (arrayPool != null)
                    {
                        stats.ArrayPoolStats[(ArrayPoolType)arrayPoolType] = arrayPool.GetStats();
                    }
                }
            }
            lock (_objectPools)
            {
                foreach (var objectPoolType in System.Enum.GetValues(typeof(ObjectPoolType)) as int[])
                {
                    var objectPool = _objectPools[objectPoolType];

                    if (objectPool != null)
                    {
                        var objectPoolStats = objectPool.GetStats();

                        if (objectPoolStats.IsSimpleObjectPool)
                        {
                            stats.SimpleObjectPoolStats[(ObjectPoolType)objectPoolType] = objectPoolStats;
                        }
                        else
                        {
                            stats.ObjectPoolStats[(ObjectPoolType)objectPoolType] = objectPoolStats;
                        }
                    }
                }
            }
            return stats;
        }

        private ObjectPool<T> CreateObjectPool<T>(PoolingOptions<T> options) where T : ILeasable
        {
            if (IsUsingFakePools)
                return new FakeObjectPool<T>(this, options);

            return new ObjectPool<T>(this, options);
        }

        private ArrayPool<T> CreateArrayPool<T>(bool growable)
        {
            if (IsUsingFakePools)
                return new FakeArrayPool<T>();

            return new ArrayPool<T>(this,growable);
        }

        private SimpleObjectPool<T> CreateSimpleObjectPool<T>(PoolingOptions<T> options) where T : ILeasable
        {
            if (IsUsingFakePools)
                return new FakeSimpleObjectPool<T>(this, options);

            return new SimpleObjectPool<T>(this, options.InitialCapacity, options.Instantiator);

        }
    }
}
