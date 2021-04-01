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

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
    /// Time to Live and Idle Time to live based derivative of ExpirationHint. 
    /// Combines the effect of both.
    /// </summary>
    /// 
    [Serializable]
    public class TTLIdleExpiration : AggregateExpirationHint
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ttl">time to live value</param>
        /// <param name="idleTTL">idle time to live value</param>
        public TTLIdleExpiration(TimeSpan ttl, TimeSpan idleTTL):base(new TTLExpiration(), new IdleExpiration())
        {
            _hintType = ExpirationHintType.TTLIdleExpiration;
        }

        public TTLIdleExpiration()
        {
            _hintType = ExpirationHintType.TTLIdleExpiration;
        }

        #region ILeasable

        public sealed override void MarkFree(int moduleRefId)
        {
            throw new NotImplementedException();
        }
        
        public sealed override void ResetLeasable()
        {
            throw new NotImplementedException();
        }
        

        #endregion
    }
}