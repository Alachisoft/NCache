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
using Alachisoft.NCache.Common;
using System.Collections;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching
{
    public class BinaryDataFormatService : IDataFormatService
    {

        public object GetClientData(object data, ref BitSet flag, LanguageContext languageContext)
        {
            return data;
        }

        public object GetCacheData(object data, BitSet flag)
        {
            ICollection dataList = data as ICollection;
            if (dataList == null) return data;

            return UserBinaryObject.CreateUserBinaryObject(dataList);
        }

        public void GetEntryClone(CacheEntry cacheEntry, out CacheEntry entry, out Array userPayload, out long payLoadSize)
        {
            entry = cacheEntry.Clone() as CacheEntry;
            userPayload = null;
            payLoadSize = 0;
        }
    }
}
