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
using ProtoBuf.Serializers.Pooling;
using Alachisoft.NCache.Common.Pooling.Util;
using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Common.Pooling.ArrayPool;

namespace Alachisoft.NCache.Common.Pooling
{
    public class ArrayPool<T> : BufferPoolBase<T>, IArrayPool
    {
        #region ---------------------------- [ Fields ] ----------------------------

        private readonly ArrayOjectPool<T>[] _buckets;
        //private readonly ObjectPool<ArrayWrapper<T>> _wrapperPool;

        #endregion

        #region -------------------------- [ Properties ] --------------------------

        public virtual string PoolName
        {
            get => "ArrayPool";
        }

        #endregion

        #region ------------------------- [ Constructors ] -------------------------

        internal ArrayPool()
        {
        }

        internal ArrayPool(PoolManager poolManager, bool growable)
        {
            _buckets = new ArrayOjectPool<T>[BucketingUtil.TotalBuckets];

            for (var i = 0; i < _buckets.Length; i++)
                InitializeBucket(i, poolManager,growable);
        }

        #endregion

        #region --------------------------- [ Behavior ] ---------------------------

        public override T[] Rent(int length, bool clean = false)
        {
            var array = _buckets[BucketingUtil.GetBucket(length)].RentArray(clean);
            return array;
        }

        public override void Return(T[] array)
        {
            var length = array.Length;
            var bucketId = BucketingUtil.GetBucket(length);

            if (!BucketingUtil.IsValidLength(bucketId, length))
                throw new ArgumentException("Provided array does not belong to the pool.", nameof(array));

            _buckets[bucketId].Return(array);
        }

        public virtual void ResetPool()
        {
            if (_buckets?.Length > 0)
            {
                foreach (var bucket in _buckets)
                {
                    bucket?.ResetPool();
                }
            }
        }

        #endregion

        #region ------------------------ [ ArrayPoolStats ] ------------------------

        ArrayPoolStats IArrayPool.GetStats()
        {
            var stats = new ArrayPoolStats()
            //{
            //    WrapperPoolStats = (_wrapperPool as IObjectPool)?.GetStats()
            //}
            ;
            if (_buckets?.Length > 0)
            {
                for (var i = 0; i < _buckets.Length; i++)
                {
                    var objectPool = _buckets[i] as IObjectPool;

                    if (objectPool != null)
                    {
                        stats[i] = objectPool.GetStats();
                    }
                }
            }
            return stats;
        }

        #endregion

        #region ------------------------ [ Helper Methods ] ------------------------

        private void InitializeBucket(int bucketId, PoolManager poolManager, bool growable)
        {
            var arrayLength = BucketingUtil.GetLength(bucketId);
            var options = GetArrayPoolingOptions(arrayLength,growable);

            _buckets[bucketId] = new ArrayOjectPool<T>(poolManager, options,growable);
        }

        private PoolingOptions<ArrayWrapper<T>> GetArrayPoolingOptions(int arrayLength,bool growable)
        {
            var instantiator = new ArrayWrapperNonBucketedInstantiator<T>(arrayLength);
            var options = new PoolingOptions<ArrayWrapper<T>>(instantiator)
            {
                InitialCapacity = 0,
                ServingCapabilityBorderline = 0.8f
            };
            return options;
        }

        private PoolingOptions<ArrayWrapper<T>> GetWrapperPoolPoolingOptions()
        {
            var instantiator = new ArrayWrapperNonBucketedInstantiator<T>();
            var options = new PoolingOptions<ArrayWrapper<T>>(instantiator);

            return options;
        }

        #endregion
    }
}
