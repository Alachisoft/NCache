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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Pooling;
#if !NETCORE
using System.Management;
#endif

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Summary description for Util.
    /// </summary>
    public sealed class ConvHelper
    {
        public static readonly DateTime NoAbsoluteExpiration = DateTime.MaxValue.ToUniversalTime();
        public static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;

        static long TICKS = 10000000;

        public static ExpirationHint MakeFixedIdleExpirationHint(DateTime dt, TimeSpan ts)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ticks"></param>
        /// <param name="isSliding"></param>
        /// <returns></returns>
        /// 
        public static ExpirationHint MakeExpirationHint(PoolManager poolManger, long ticks, bool isAbsolute)
        {
            if (ticks == 0) return null;
            IdleExpiration idleExpiration = null;
            if (!isAbsolute)
            {
                TimeSpan slidingExpiration = new TimeSpan(ticks);


                if (slidingExpiration.CompareTo(TimeSpan.Zero) < 0)
                    throw new ArgumentOutOfRangeException("slidingExpiration");

                if (slidingExpiration.CompareTo(DateTime.Now.AddYears(1) - DateTime.Now) >= 0)
                    throw new ArgumentOutOfRangeException("slidingExpiration");


                idleExpiration = IdleExpiration.Create(poolManger, slidingExpiration);
                return idleExpiration;
            }
            else
            {
                DateTime absoluteExpiration = new DateTime(ticks, DateTimeKind.Utc);
                return FixedExpiration.Create(poolManger, absoluteExpiration);
            }
        }
    }
}
