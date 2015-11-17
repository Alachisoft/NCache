// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Summary description for Util.
    /// </summary>
    internal sealed class ExpirationHelper
    {
        public static readonly DateTime NoAbsoluteExpiration = DateTime.MaxValue.ToUniversalTime();
        public static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;


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
        public static ExpirationHint MakeExpirationHint(long ticks, bool isAbsolute)
        {
            if (ticks == 0) return null;

            if (!isAbsolute)
            {
                TimeSpan slidingExpiration = new TimeSpan(ticks);
                if (slidingExpiration.CompareTo(TimeSpan.Zero) < 0)
                    throw new ArgumentOutOfRangeException("slidingExpiration");
                if (slidingExpiration.CompareTo(DateTime.Now.AddYears(1) - DateTime.Now) >= 0)
                    throw new ArgumentOutOfRangeException("slidingExpiration");
                return new IdleExpiration(slidingExpiration);
            }
            else
            {
                DateTime absoluteExpiration = new DateTime(ticks, DateTimeKind.Utc);
                return new FixedExpiration(absoluteExpiration);
            }
        }
    }
}
