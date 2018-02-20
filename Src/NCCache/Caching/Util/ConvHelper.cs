// Copyright (c) 2018 Alachisoft
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

using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Config.Dom;
#if !NETCORE
using System.Management;
#endif

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Summary description for Util.
    /// </summary>
    internal sealed class ConvHelper
    {
        public static readonly DateTime NoAbsoluteExpiration = DateTime.MaxValue.ToUniversalTime();
        public static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;

        public static readonly DateTime AbsoluteDefaultExpiration = DateTime.MinValue.AddYears(1);
        public static readonly DateTime AbsoluteLongerExpiration = DateTime.MinValue.AddYears(2);
        public static readonly DateTime AbsoluteLongestExpiration = DateTime.MinValue.AddYears(3);

        public static readonly TimeSpan SlidingDefaultExpiration = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 1));
        public static readonly TimeSpan SlidingLongerExpiration = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 2));
        public static readonly TimeSpan SlidingLongestExpiration = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 3));
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
        public static void ValidateExpiration(Alachisoft.NCache.Config.Dom.ExpirationPolicy policy)
        {
            if (policy.SlidingExpiration.DefaultEnabled || policy.SlidingExpiration.LongerEnabled || policy.AbsoluteExpiration.DefaultEnabled || policy.AbsoluteExpiration.LongerEnabled)
            {
                if (policy.AbsoluteExpiration.Default < 5 && policy.AbsoluteExpiration.DefaultEnabled)
#if !NETCORE
                    throw new ManagementException("Absolute Default expiration value is less than 5 seconds.");
#elif NETCORE
                    throw new Exception("Absolute Default expiration value is less than 5 seconds."); //TODO: ALACHISOFT (System.Management has some issues)
#endif
                if (policy.AbsoluteExpiration.Longer < 5 && policy.AbsoluteExpiration.LongerEnabled)
#if !NETCORE
                    throw new ManagementException("Absolute Longer expiration value is less than 5 seconds.");
#elif NETCORE
                    throw new Exception("Absolute Longer expiration value is less than 5 seconds."); //TODO: ALACHISOFT (System.Management has some issues)
#endif
                if (policy.SlidingExpiration.Default < 5 && policy.SlidingExpiration.DefaultEnabled)
#if !NETCORE
                    throw new ManagementException("Sliding Default expiration value is less than 5 seconds.");
#elif NETCORE
                    throw new Exception("Sliding Default expiration value is less than 5 seconds."); //TODO: ALACHISOFT (System.Management has some issues)
#endif
                if (policy.SlidingExpiration.Longer < 5 && policy.SlidingExpiration.LongerEnabled)
#if !NETCORE
                    throw new ManagementException("Sliding Longer expiration value is less than 5 seconds.");
#elif NETCORE
                    throw new Exception("Sliding Longer expiration value is less than 5 seconds."); //TODO: ALACHISOFT (System.Management has some issues)
#endif
            }
        }

        public static ExpirationHint MakeExpirationHint(ExpirationPolicy policy, long ticks, bool isAbsolute)
        {
            
            if (ticks == 0) return null;

            if (!isAbsolute)
            {
                TimeSpan slidingExpiration = new TimeSpan(ticks);
                ValidateExpiration(policy);
                if (slidingExpiration != SlidingDefaultExpiration && slidingExpiration != SlidingLongerExpiration)
                {

                    if (slidingExpiration.CompareTo(TimeSpan.Zero) < 0)
                        throw new ArgumentOutOfRangeException("slidingExpiration");

                    if (slidingExpiration.CompareTo(DateTime.Now.AddYears(1) - DateTime.Now) >= 0)
                        throw new ArgumentOutOfRangeException("slidingExpiration");
                }
                if (slidingExpiration == SlidingDefaultExpiration || slidingExpiration == SlidingLongerExpiration)
                {
                    if (policy.SlidingExpiration.DefaultEnabled || policy.SlidingExpiration.LongerEnabled)
                    {
                        if (policy.SlidingExpiration.LongerEnabled && slidingExpiration == SlidingLongerExpiration)
                        {
                            return new IdleExpiration(new TimeSpan(policy.SlidingExpiration.Longer * TICKS));
                        }
                        else if (slidingExpiration == SlidingDefaultExpiration && policy.SlidingExpiration.DefaultEnabled)
                        {
                            return new IdleExpiration(new TimeSpan(policy.SlidingExpiration.Default * TICKS));
                        }
                        else
                            return null;
                    }
                    else
                        return null;
                }
                else
                {
                    return new IdleExpiration(slidingExpiration);
                }
            }
            else
            {
                DateTime absoluteExpiration = new DateTime(ticks, DateTimeKind.Utc);

                if (absoluteExpiration == AbsoluteDefaultExpiration.ToUniversalTime() || absoluteExpiration == AbsoluteLongerExpiration.ToUniversalTime())
                {
                    if (policy.AbsoluteExpiration.DefaultEnabled||policy.AbsoluteExpiration.LongerEnabled)
                    {
                       
                        if (policy.AbsoluteExpiration.LongerEnabled && absoluteExpiration ==AbsoluteLongerExpiration.ToUniversalTime() )// If not enabled try to check if Longer Expiration is enabled
                            return new FixedExpiration(DateTime.Now.AddSeconds(policy.AbsoluteExpiration.Longer).ToUniversalTime());
                        else if (policy.AbsoluteExpiration.DefaultEnabled && absoluteExpiration == AbsoluteDefaultExpiration.ToUniversalTime())
                            return new FixedExpiration(DateTime.Now.AddSeconds(policy.AbsoluteExpiration.Default).ToUniversalTime());
                        else
                            return null;
                    }
                    else
                        return null;
                }
                else
                    return new FixedExpiration(absoluteExpiration);
            }
        }
    }
}
