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
using System.Threading;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.EvictionPolicies
{

    /// <summary>
    /// Provides a thread-safe wrapper over the eviction policy.
    /// </summary>
    class EvictionPolicySyncWrapper : IEvictionPolicy
    {
        IEvictionPolicy _evctPolicy;
        ReaderWriterLock _sync = new ReaderWriterLock();

        public EvictionPolicySyncWrapper(IEvictionPolicy evictionPolicy)
        {
            _evctPolicy = evictionPolicy;
        }

        public ReaderWriterLock Sync
        {
            get { return _sync; }
        }
        #region IEvictionPolicy Members

        public void Notify(object key, EvictionHint oldhint, EvictionHint newHint)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _evctPolicy.Notify(key, oldhint, newHint);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public void Clear()
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _evctPolicy.Clear();
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public EvictionHint CompatibleHint(EvictionHint eh,PoolManager poolManager)
        {
            return _evctPolicy.CompatibleHint(eh,poolManager);
        }

        public long Execute(CacheBase cache, CacheRuntimeContext context, long count)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                return _evctPolicy.Execute(cache,context, count);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public void Remove(object key, EvictionHint hint)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _evctPolicy.Remove(key, hint);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public float EvictRatio
        {
            get
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    return _evctPolicy.EvictRatio;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }

            set
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _evctPolicy.EvictRatio = value; ;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }
        }

        #endregion

        public long IndexInMemorySize
        {
            get { throw new NotImplementedException("EvictionPolicySyncWrapper.IndexInMemorySize"); }            
        }
    }
}
