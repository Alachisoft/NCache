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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Caching;
namespace Alachisoft.NCache.Caching
{
    public class EventCacheEntry : ICompactSerializable
    {
        string _group;
        string _subGroup;
        ulong _version;
        int _priority;
        bool _resyncExpiredItems;
        string _reSyncProviderCacheItem;
        object _value;
        BitSet _flags;
        EntryType _entryType;

        public string Group 
        {
            get { return _group; }
            set { _group = value; }
        }
        public string SubGroup
        {
            get { return _subGroup; }
            set { _subGroup = value; }
        }
        public ulong Version
        {
            get { return _version; }
            set { _version = value; }
        }
        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }
        public bool ReSyncExpiredItems
        {
            get { return _resyncExpiredItems; }
            set { _resyncExpiredItems = value; }
        }
        public string ReSyncProviderCacheItem
        {
            get { return _reSyncProviderCacheItem; }
            set { _reSyncProviderCacheItem = value; }
        }

        public static EventCacheEntry ReadItemInfo(CompactReader reader)
        {
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                EventCacheEntry item = new EventCacheEntry();
                item.Deserialize(reader);
                return item ;
            }
            return null;
        }

        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        internal static void WriteItemInfo(CompactWriter writer, EventCacheEntry item)
        {
            if (item == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                item.Serialize(writer);
            }
        }

        public BitSet Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        public EntryType EntryType
        {
            get { return _entryType; }
            set { _entryType = value; }
        }

        public EventCacheEntry(CacheEntry cacheEntry)
        {
            if (cacheEntry.GroupInfo != null)
            {
                Group = cacheEntry.GroupInfo.Group;
                SubGroup = cacheEntry.GroupInfo.SubGroup;
            }
            Version = cacheEntry.Version;
            Priority = (int)cacheEntry.Priority;
            if (cacheEntry.ExpirationHint != null)
                ReSyncExpiredItems = cacheEntry.ExpirationHint.NeedsReSync;
            ReSyncProviderCacheItem = cacheEntry.ResyncProviderName;
            EntryType = cacheEntry.Type;
        }

        public EventCacheEntry()
        {

        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            Group = reader.ReadObject() as string;
            SubGroup = reader.ReadObject() as string;
            Version = reader.ReadUInt64();
            Priority = reader.ReadInt32();
            ReSyncExpiredItems = reader.ReadBoolean();
            ReSyncProviderCacheItem = reader.ReadObject() as string;
            Flags = reader.ReadObject() as BitSet;
            Value = reader.ReadObject();
            EntryType = (EntryType)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(Group);
            writer.WriteObject(SubGroup);
            writer.Write(Version);
            writer.Write(Priority);
            writer.Write(ReSyncExpiredItems);
            writer.WriteObject(ReSyncProviderCacheItem);
            writer.WriteObject(Flags);
            writer.WriteObject(Value);
            writer.WriteObject(EntryType);
        } 
        #endregion
    }
}
