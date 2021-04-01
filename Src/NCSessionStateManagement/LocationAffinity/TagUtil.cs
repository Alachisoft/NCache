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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Client;

using Runtime = Alachisoft.NCache.Runtime;


namespace Alachisoft.NCache.Web.SessionStateManagement.LocationAffinity
{
    class TagUtil
    {
        public const string SESSION_TAG = "NC_ASP.net_session_data";

        public static CacheItem CreateTaggedCacheItem(object value)
        {
            CacheItem cacheItem = new CacheItem(value);
           

            return cacheItem;
        }

        public static  CacheItem CreateTaggedCacheItem(object value,  DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            CacheItem cacheItem = new CacheItem(value);

       
            if(absoluteExpiration!=null)
            {
                cacheItem.Expiration = new Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Absolute, TimeSpan.FromTicks(absoluteExpiration.Ticks));

            }
            else if (slidingExpiration != null)
            {
                cacheItem.Expiration = new Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Sliding, slidingExpiration);

            }
            cacheItem.Priority = priority;

            return cacheItem;
        }
    }
}
