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
    /// Time to Live based derivative of ExpirationHint.
    /// </summary>
    [Serializable]
    public class TTLExpiration : FixedExpiration
    {
        public TTLExpiration()
        {
            _hintType = ExpirationHintType.TTLExpiration;
        }

        #region Creating TTLExpiration

        public static new TTLExpiration Create(PoolManager poolManager)
        {
            return poolManager.GetTTLExpirationPool()?.Rent(true) ?? new TTLExpiration();
        }

        public static TTLExpiration Create(PoolManager poolManager, TimeSpan timeToLive)
        {
            var expiration = Create(poolManager);
            Construct(expiration, timeToLive);

            return expiration;
        }

        protected static void Construct(TTLExpiration expiration, TimeSpan timeToLive)
        {
            Construct(expiration, DateTime.Now.Add(timeToLive));
        }

        #endregion

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
            var clonedHint = poolManager.GetTTLExpirationPool()?.Rent(true) ?? new TTLExpiration();
            DeepCloneInternal(poolManager, clonedHint);
            return clonedHint;
        }

        #endregion
    }
}
