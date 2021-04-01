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
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common;
using System.Collections;
using Alachisoft.NCache.Common.Util;

using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching
{
    internal class BinaryDataFormatService : IDataFormatService
    {
        private CacheRuntimeContext _context;

        public BinaryDataFormatService(CacheRuntimeContext context)
        {
            this._context = context;
        }

        public object GetClientData(object data, ref BitSet flag, LanguageContext languageContext)
        {
            return data;
        }

        public object GetCacheData(object data, BitSet flag)
        {
            ICollection dataList = data as ICollection;
            if (dataList == null) return data;

            return UserBinaryObject.CreateUserBinaryObject(dataList, _context.TransactionalPoolManager);
        }

        public void GetEntryClone(CacheEntry cacheEntry, out CacheEntry entry, out Array userPayload, out long payLoadSize)
        {
            entry = cacheEntry.DeepClone(_context.TransactionalPoolManager);
            entry.MarkInUse(NCModulesConstants.Global);
            userPayload = null;
            payLoadSize = 0;
        }

        /// <summary>
        /// Mainly written for cache data as JSON for backing source. Please do not call this method from anywhere 
        /// other than the derived classes of <see cref="Runtime.Caching.ProviderItemBase"/>.
        /// </summary>
        public T GetCacheData<T>(object data, BitSet flag, UserObjectType userObjectType)
        {
            var dataList = data as ICollection;

            // If data was requested by socket server
            if (dataList != null && typeof(object).IsAssignableFrom(typeof(T)))
                return (T)(object)UserBinaryObject.CreateUserBinaryObject(dataList);

            return SerializationUtil.SafeDeserializeInProc<T>(data, string.Empty, flag, userObjectType,false);
        }
    }
}
