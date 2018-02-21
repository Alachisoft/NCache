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

using Alachisoft.NCache.EntityFrameworkCore.NCache;
using Alachisoft.NCache.Runtime.Dependencies;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    internal static class NCacheWrapperExtensions
    {
        internal static List<TItem> Set<TItem>(this NCacheWrapper cache, object key, Dictionary<string, TItem> value, CachingOptions options, Alachisoft.NCache.Runtime.Dependencies.CacheDependency dbDependency, StoreAs storingAs)
        {
            Logger.Log(
                "About to set values with options " + options.ToLog() + ", DbDependency '" + dbDependency + "' and StoringAs '" + storingAs + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );

            // Add entities if stroing as seperateEntities
            if (storingAs == StoreAs.SeperateEntities)
            {
                Logger.Log("Values are about to be set as separate entities.", Microsoft.Extensions.Logging.LogLevel.Trace);
                cache.Set(value.Keys.ToArray(), value.Values.ToArray(), options, dbDependency, storingAs);
            }
            // from here onwards is the enumerator logic and now it is being done in "else" after we have moved to tags based result set regeneration
            else
            {
                Logger.Log("Values are about to be set as collection.", Microsoft.Extensions.Logging.LogLevel.Trace);

                // Add query enumerator
                CacheEntry entry = cache.CreateEntry(key);

                // Setting options
                if (options != null)
                {
                    entry.SetOptions(options);
                }

                // Setting Value
                if (storingAs == StoreAs.Collection)
                {
                    entry.Value = value.Values.ToList();
                }

                // Mind that this is not the user specified option but the end storing methodology
                entry.StoredAs = storingAs;

                // Set dependencies in the entry
                var aggregateDependency = new AggregateCacheDependency();
                if (dbDependency != null)
                    aggregateDependency.Add(dbDependency);

                entry.Dependencies = aggregateDependency;

                cache.Set(key, entry, options, dbDependency, storingAs);
            }
            return value.Values.ToList();
        }

        internal static TItem Set<TItem>(this NCacheWrapper cache, object key, TItem value, CachingOptions options, Alachisoft.NCache.Runtime.Dependencies.CacheDependency dbDependency, StoreAs storingAs)
        {
            Logger.Log(
                "Setting item '" + value + "' against key '" + key + "' with DbDependency '" + dbDependency + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            Alachisoft.NCache.Web.Caching.CacheItem cacheItem = new Alachisoft.NCache.Web.Caching.CacheItem(value);
            CachingOptionsUtil.CopyMetadata(ref cacheItem, options, dbDependency);
            cache.Insert(key, cacheItem);
            return value;
        }

        internal static TItem[] Set<TItem>(this NCacheWrapper cache, object[] keys, TItem[] values, CachingOptions options, Alachisoft.NCache.Runtime.Dependencies.CacheDependency dbDependency, StoreAs storingAs)
        {
            Logger.Log(
                "Setting items in bulk against respective keys with DbDependency '" + dbDependency + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );

            Alachisoft.NCache.Web.Caching.CacheItem[] cacheItems = new Alachisoft.NCache.Web.Caching.CacheItem[values.Count()];
            for (int i = 0; i < values.Count(); i++)
            {
                cacheItems[i] = new Alachisoft.NCache.Web.Caching.CacheItem(values[i]);
                CachingOptionsUtil.CopyMetadata(ref cacheItems[i], options, dbDependency);
            }
            if (keys.Length > 0)
            {
                cache.InsertBulk(keys, cacheItems);
            }
            return values;
        }

        internal static TItem Set<TItem>(this NCacheWrapper cache, object key, TItem value, CachingOptions options)
        {
            Logger.Log(
                "Setting item '" + value + "' against key '" + key + "' with no DbDependency.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            Alachisoft.NCache.Web.Caching.CacheItem cacheItem = new Alachisoft.NCache.Web.Caching.CacheItem(value);
            CachingOptionsUtil.CopyMetadata(ref cacheItem, options);
            cache.Insert(key, cacheItem);
            return value;
        }

        internal static TItem SetAsCacheEntry<TItem>(this NCacheWrapper cache, object key, TItem value, CachingOptions options)
        {
            return SetAsCacheEntry(cache, key, value, options, null);
        }

        internal static TItem SetAsCacheEntry<TItem>(this NCacheWrapper cache, object key, TItem value, CachingOptions options, CacheDependency dbDependency)
        {
            Logger.Log(
                "Setting CacheEntry '" + value + "' against key '" + key + "' with DbDependency '" + dbDependency + "'",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );

            CacheEntry entry = cache.CreateEntry(key);

            if (options != null)
            {
                entry.SetOptions(options);
            }
            if (dbDependency != null)
            {
                entry.Dependencies = dbDependency;
            }
            entry.Value = value;

            cache.Set(key, entry, options, dbDependency, options.StoreAs);

            return value;
        }
    }
}
