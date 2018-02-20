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
// limitations under the License

using System;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Web.SessionStateManagement.LocationAffinity

{
    class TagUtil
    {
        public const string SESSION_TAG = "NC_ASP.net_session_data";

        public static CacheItem CreateTaggedCacheItem(object value)
        {
            CacheItem cacheItem = new CacheItem(value);
            cacheItem.Tags = new Runtime.Caching.Tag[] { new Runtime.Caching.Tag(SESSION_TAG) };

            return cacheItem;
        }

        public static  CacheItem CreateTaggedCacheItem(object value, CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            CacheItem cacheItem = new CacheItem(value);

            cacheItem.Tags = new Runtime.Caching.Tag[] { new Runtime.Caching.Tag(SESSION_TAG) };
            cacheItem.Dependency = dependency;
            cacheItem.AbsoluteExpiration = absoluteExpiration;
            cacheItem.SlidingExpiration = slidingExpiration;
            cacheItem.Priority = priority;

            return cacheItem;
        }
    }
}
