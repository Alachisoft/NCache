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

namespace Alachisoft.NCache.EntityFrameworkCore.NCache
{
    public static class CacheEntryExtensions
    {
        /// <summary>
        /// Applies the values of an existing <see cref="CachingOptions"/> to the entry.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="options"></param>
        internal static CacheEntry SetOptions(this CacheEntry entry, CachingOptions options)
        {
            Logger.Log(
                "Setting options '" + options.ToLog() + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            entry.ExpirationType = options.ExpirationType;
            entry.AbsoluteExpirationTime = options.AbsoluteExpirationTime;
            entry.SlidingExpirationTime = options.SlidingExpirationTime;
            entry.Priority = options.Priority;
            entry.QueryIDentifier = options.QueryIdentifier == null ? default(string) : options.QueryIdentifier.ToString();

            entry.CreateDbDependency = options.CreateDbDependency;

            return entry;
        }
    }
}
