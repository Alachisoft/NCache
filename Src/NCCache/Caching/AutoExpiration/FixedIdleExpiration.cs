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
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
    /// Fixed time expiry and Idle Time to live based derivative of ExpirationHint. 
    /// Combines the effect of both.
    /// </summary>
    [Serializable]
    public class FixedIdleExpiration : AggregateExpirationHint
    {
        public FixedIdleExpiration()         
        {
            _hintType = ExpirationHintType.FixedIdleExpiration;
        }

        #region Creating FixedIdleExpiration

        public static new FixedIdleExpiration Create(PoolManager poolManager)
        {
            return poolManager.GetFixedIdleExpirationPool()?.Rent(true) ?? new FixedIdleExpiration();
        }

        public static FixedIdleExpiration Create(PoolManager poolManager, TimeSpan idleTime, DateTime absoluteTime)
        {
            var expiration = Create(poolManager);
            Construct(expiration, FixedExpiration.Create(poolManager, absoluteTime), IdleExpiration.Create(poolManager, idleTime));

            return expiration;
        }

        #endregion

        public override string ToString()
        {
            return string.Empty;
        }

        #region ILeasable

        public sealed override void ResetLeasable()
        {

        }

        public sealed override void ReturnLeasableToPool()
        {

        }

        #endregion

        #region - [Deep Cloning] -

        public sealed override ExpirationHint DeepClone(PoolManager poolManager)
        {
            var clonedHint = poolManager.GetFixedIdleExpirationPool()?.Rent(true) ?? new FixedIdleExpiration();
            DeepCloneInternal(poolManager, clonedHint);
            return clonedHint;
        }

        #endregion
    }
}