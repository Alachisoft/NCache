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

using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Dependencies;

namespace Alachisoft.NCache.EntityFrameworkCore.NCache
{
    internal class CachingOptionsUtil
    {
        internal static void CopyMetadata(ref Alachisoft.NCache.Web.Caching.CacheItem cacheItem, CachingOptions options, CacheDependency cacheDependency = null)
        {
            Logger.Log(
                "Copying options '" + options.ToLog() + "' into cache item metadata with CacheDependency '" + cacheDependency + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );

            // Set Expiration
            if (options.ExpirationType == ExpirationType.Absolute)
            {
                cacheItem.AbsoluteExpiration = options.AbsoluteExpirationTime;
            }
            else if (options.ExpirationType == ExpirationType.Sliding)
            {
                cacheItem.SlidingExpiration = options.SlidingExpirationTime;
            }

            // Set Priority
            cacheItem.Priority = options.Priority;

            // Set Cache Dependency

            if (options.CreateDbDependency)
            {
                cacheItem.Dependency = cacheDependency;
            }

            // Set Tags
            if (options.QueryIdentifier != null)
            {
                cacheItem.Tags = new Tag[] { options.QueryIdentifier };
            }
        }

        internal static Tag[] GetTags(string[] stringTags)
        {
            Tag[] tags = new Tag[stringTags.Length];
            for (int i = 0; i < stringTags.Length; i++)
            {
                tags[i] = new Tag(stringTags[i]);
            }
            return tags;
        }
    }
}
