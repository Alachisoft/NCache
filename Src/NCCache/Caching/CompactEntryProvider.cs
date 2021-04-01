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

namespace Alachisoft.NCache.Caching
{
    public class CompactEntryProvider : ObjectProvider
    {
        public CompactEntryProvider() : base() { }
        public CompactEntryProvider(int initialSize) : base(initialSize) { }

        protected override IRentableObject CreateObject()
        {
            return new CompactCacheEntry();
        }

        protected override void ResetObject(object obj)
        {
            CompactCacheEntry entry = obj as CompactCacheEntry;
            if (entry != null) entry.Reset();
        }

        public override Type ObjectType
        {
            get
            {
                if (_objectType == null) _objectType = typeof(CompactCacheEntry);
                return _objectType;
            }
        }

        public override string Name
        {
            get { return "CompactEntryProvider"; }
        }
    }
}