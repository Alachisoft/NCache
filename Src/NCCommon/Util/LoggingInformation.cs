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
using System.Collections;

namespace Alachisoft.NCache.Common.Util
{
    public static class LoggingInformation
    {
        internal static Hashtable cacheLogger = new Hashtable();
        internal static Hashtable staticCacheLogger = new Hashtable();

        /// <summary>
        /// Returns the logger Name w.r.t the cache Name
        /// </summary>
        /// <param name="cacheName">CacheName to which the loggername is associated</param>
        /// <returns>returns the logger Name, if not found null is returned</returns>
        internal static string GetLoggerName(string cacheName)
        {
            if (cacheName == null || cacheName == string.Empty) return null;
            return (string)cacheLogger[cacheName];
        }

        internal static string GetStaticLoggerName(string name)
        {
            if (name == null || name == string.Empty) return null;

            return (string)staticCacheLogger[name];
        }

    }
}