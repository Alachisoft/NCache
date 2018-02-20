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
using System.Collections;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Web
{
    /// <summary>
    /// Summary description for Util.
    /// </summary>
    internal sealed class WebCacheHelper
    {
        /// <summary>
        /// Converts between NCache item remove reason and web item remove reason.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static CacheItemRemovedReason GetWebItemRemovedReason(ItemRemoveReason reason)
        {
            switch (reason)
            {
                case ItemRemoveReason.DependencyChanged:
                    return CacheItemRemovedReason.DependencyChanged;

                case ItemRemoveReason.Expired:
                    return CacheItemRemovedReason.Expired;

                case ItemRemoveReason.Underused:
                    return CacheItemRemovedReason.Underused;
            }

            return CacheItemRemovedReason.Removed;
        }


        /// <summary>
        /// combines the absolute and sliding expiry params and returns a single
        /// expiration hint value.
        /// </summary>
        /// <param name="absoluteExpiration">the absolute expiration datetime</param>
        /// <param name="slidingExpiration">the sliding expiration time</param>
        /// <returns>expiration hint</returns>
        /// <remarks>If you set the <paramref name="slidingExpiration"/> parameter to less than TimeSpan.Zero, 
        /// or the equivalent of more than one year, an <see cref="ArgumentOutOfRangeException"/> is thrown. 
        /// You cannot set both sliding and absolute expirations on the same cached item. 
        /// If you do so, an <see cref="ArgumentException"/> is thrown.</remarks>
        public static ExpirationHint MakeFixedIdleExpirationHint(DateTime absoluteExpiration,
            TimeSpan slidingExpiration)
        {
            if (Caching.Cache.DefaultAbsolute.Equals(absoluteExpiration) &&
                Caching.Cache.NoSlidingExpiration.Equals(slidingExpiration))
            {
                return null;
            }

            if (Caching.Cache.DefaultAbsolute.Equals(absoluteExpiration))
            {
                if (slidingExpiration.CompareTo(TimeSpan.Zero) < 0)
                    throw new ArgumentOutOfRangeException("slidingExpiration");

                if (slidingExpiration.CompareTo(DateTime.Now.AddYears(1) - DateTime.Now) >= 0)
                    throw new ArgumentOutOfRangeException("slidingExpiration");

                return new IdleExpiration(slidingExpiration);
            }

            if (Caching.Cache.NoSlidingExpiration.Equals(slidingExpiration))
            {
                return new FixedExpiration(absoluteExpiration);
            }

            throw new ArgumentException("You cannot set both sliding and absolute expirations on the same cache item.");
        }

        public static void EvaluateTagsParameters(Hashtable queryInfo, string group)
        {
            if (queryInfo != null)
            {
                if (!String.IsNullOrEmpty(group) && queryInfo["tag-info"] != null)

                    throw new ArgumentException("You cannot set both groups and tags on the same cache item.");
            }
        }

        public static byte EvaluateExpirationParameters(DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            if (Caching.Cache.NoAbsoluteExpiration.Equals(absoluteExpiration) &&
                Caching.Cache.NoSlidingExpiration.Equals(slidingExpiration))
            {
                return 2;
            }

            if (Caching.Cache.NoAbsoluteExpiration.Equals(absoluteExpiration))
            {
                if (slidingExpiration != Caching.Cache.DefaultSliding &&
                    slidingExpiration != Caching.Cache.DefaultSlidingLonger)
                {
                    if (slidingExpiration.CompareTo(TimeSpan.Zero) < 0)
                        throw new ArgumentOutOfRangeException("slidingExpiration");

                    if (slidingExpiration.CompareTo(DateTime.Now.AddYears(1) - DateTime.Now) >= 0)
                        throw new ArgumentOutOfRangeException("slidingExpiration");
                }

                return 0;
            }

            if (Caching.Cache.NoSlidingExpiration.Equals(slidingExpiration))
            {
                return 1;
            }

            throw new ArgumentException("You cannot set both sliding and absolute expirations on the same cache item.");
        }
    }
}