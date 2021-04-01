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
using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Common
{
    public class ExpirationUtil
    {

        public static Expiration GetExpiration(DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            #region Creating Sliding Expiration

            var modernSlidingExpiration = new Expiration(ExpirationType.None);

            if (slidingExpiration != TimeSpan.Zero)
            {
                modernSlidingExpiration = new Expiration(ExpirationType.Sliding, slidingExpiration);
            }

            #endregion

            #region Creating Absolute Expiration

            var modernAbsoluteExpiration = new Expiration(ExpirationType.None);

            if (absoluteExpiration.ToUniversalTime() != DateTime.MaxValue.ToUniversalTime())
            {
                modernAbsoluteExpiration = new Expiration(ExpirationType.Absolute, absoluteExpiration.ToLocalTime() - DateTime.Now.ToLocalTime());
            }

            #endregion

            return modernAbsoluteExpiration.Type == ExpirationType.None ? modernSlidingExpiration : modernAbsoluteExpiration;
        }
    }
}
