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

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// CacheItemAttributes contains the information about the cache attributes.
    /// </summary>
    public class CacheItemAttributes 
    {
        /// <summary> Absolute expiration for the object. </summary>
        private DateTime _absoluteExpiration = DateTime.MaxValue.ToUniversalTime();

        /// <summary>
        /// Gets/Sets absolute expiration. You can add an item to the cache with absolute expiration by 
        /// specifying the exact date and time 
        /// at which the item should be invalidated. When this time is elapsed, the item will be removed from the cache.
        /// </summary>
        public DateTime AbsoluteExpiration
        {
            get { return _absoluteExpiration; }
            set { _absoluteExpiration = value; }
        }
        
        /// <summary>
        /// Gets/Sets the flag which indicates whether item should be reloaded on expiration
        /// if ReadThru provider is specified.
        /// </summary>
        internal bool ResyncRequired { get; set; }


    }
}
