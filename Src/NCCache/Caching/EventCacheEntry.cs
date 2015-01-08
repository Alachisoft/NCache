// Copyright (c) 2015 Alachisoft
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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Caching
{
    public class EventCacheEntry : ICompactSerializable
    {
        int _priority;
        object _value;
        BitSet _flags;


        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }
     
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }
        public BitSet Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        public EventCacheEntry(CacheEntry cacheEntry)
        {
            Priority = (int)cacheEntry.Priority;
        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            Priority = reader.ReadInt32();
            Flags = new BitSet(reader.ReadByte());
            Value = reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(Priority);
            writer.Write(Flags.Data);
            writer.WriteObject(Value);
        } 
        #endregion
    }
}
