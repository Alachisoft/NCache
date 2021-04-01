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
    /// Class that provides values to specify expiration of items in cache.
    /// </summary>
    public class Expiration
    {
        /// <summary>
        /// Value of time in the form of <see cref="TimeSpan"/> that shows 
        /// after how much time, the item in cache is to be expired.
        /// </summary>
        public TimeSpan ExpireAfter
        {
            get;
            set;
        }

        /// <summary>
        /// The type of expiration to be used while expiring items in cache. The 
        /// value of this type varies from item to item in cache.
        /// </summary>
        public ExpirationType Type
        {
            get;
        }

        /// <summary>
        /// Instantiates <see cref="Expiration"/> to provide expiration values for items in cache.
        /// </summary>
        /// <param name="expirationType">Flag indicating type of expiration to be used while 
        /// expiring items in cache.</param>
        /// <param name="expireAfter">Value of time in the form of <see cref="TimeSpan"/> that 
        /// shows after how much time, the item in cache is to be expired.</param>
        /// <example>This example demonstrates how to create an instance of <see cref="Expiration"/> with
        /// sliding expiration of 5 minutes.
        /// <code>
        /// Expiration slidingExpiration = new Expiration(ExpirationType.Sliding, TimeSpan.FromMinutes(5));
        /// </code>
        /// </example>
        public Expiration(ExpirationType expirationType, TimeSpan expireAfter = default(TimeSpan))
        {
            Type = expirationType;
            ExpireAfter = expireAfter;
        }

        internal Expiration() : this(ExpirationType.None)
        {
        }

        internal DateTime Absolute
        {
            get
            {
                switch (Type)
                {
                    case ExpirationType.Absolute:
                        return DateTime.Now.AddTicks(ExpireAfter.Ticks);

                    default:
                        return ExpirationConstants.AbsoluteNoneExpiration;
                }
            }
        }

        internal TimeSpan Sliding
        {
            get
            {
                switch (Type)
                {
                    case ExpirationType.Sliding:
                        return ExpireAfter;

                    default:
                        return ExpirationConstants.SlidingNoneExpiration;
                }
            }
        }
    }
}
