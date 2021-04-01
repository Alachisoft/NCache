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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Protobuf;

namespace Alachisoft.NCache.Caching
{
    internal static class Stash
    {
        [ThreadStatic]
        private static BitSet _bitSet;

        public static BitSet BitSet
        {
            get
            {
                if (_bitSet == null)
                    _bitSet = new BitSet();

                _bitSet.ResetLeasable();

                return _bitSet;
            }
        }

        [ThreadStatic]
        private static CacheEntry _cacheEntry;

        public static CacheEntry CacheEntry
        {
            get
            {
                if (_cacheEntry == null)
                    _cacheEntry = new CacheEntry();

                _cacheEntry.ResetLeasable();

                return _cacheEntry;
            }
        }

        [ThreadStatic]
        private static InsertResponse _insertResponse;

        public static InsertResponse InsertResponse
        {
            get
            {
                if (_insertResponse == null)
                    _insertResponse = new InsertResponse();

                _insertResponse.ResetLeasable();

                return _insertResponse;
            }
        }
    }
}
