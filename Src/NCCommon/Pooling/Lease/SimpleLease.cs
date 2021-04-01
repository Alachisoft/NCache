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

namespace Alachisoft.NCache.Common.Pooling.Lease
{
    [Serializable]
    public abstract class SimpleLease : ILeasable
    {
        [NonSerialized]
        private PoolManager _poolManager;

        public PoolManager PoolManager
        {
            get => _poolManager;
            set => _poolManager = value;
        }

        public bool IsFromPool
        {
            // No need for thread-safety here 
            // as _poolManager is set only by 
            // pool and no other entity and our 
            // pools' operations are locked.
            get => _poolManager != null;
        }

        bool ILeasable.IsInUse
        {
            get => false;
        }

        public bool IsOutOfPool { get; set; }

        public void MarkInUse(int moduleRefId)
        {
        }

        public virtual void MarkFree(int moduleRefId)
        {
            //if(IsFromPool)
            //    ReturnLeasableToPool();
        }

        public abstract void ResetLeasable();

        public abstract void ReturnLeasableToPool();

        /// <summary>
        /// Verify if this object is from input PoolManager
        /// </summary>
        /// <param name="poolManager"></param>
        /// <returns></returns>
        public bool FromPool(PoolManager poolManager)
        {
            return PoolManager != null && ReferenceEquals(PoolManager, poolManager);
        }
    }
}
