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

using Alachisoft.NCache.Runtime;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Caching;

namespace Alachisoft.NCache.Client
{
    internal class MessageEventItem : MessageEventItemBase
    {
        internal string CQId { get; set; }
        internal EventCacheItem Item { get; set; }
        internal EventCacheItem OldItem { get; set; }
        internal EventDataFilter DataFilter { get; set; }
        
        internal CacheItemRemovedReason Reason { get; set; }

        internal EventCacheItem ConvertToEventCacheItem(Common.Protobuf.EventCacheItem entry)
        {
            EventCacheItem cacheItem = new EventCacheItem();

            try
            {
                cacheItem.Group = entry.group;
                cacheItem.SubGroup = entry.subGroup;
                cacheItem.CacheItemVersion = new CacheItemVersion();             
                cacheItem.CacheItemVersion.Version = entry.itemVersion;                
                cacheItem.CacheItemPriority = (CacheItemPriority)entry.priority;
                //cacheItem.ResyncOptions = new Runtime.Caching.ResyncOptions(false, "");
                cacheItem.Flagmap = new Common.BitSet((byte)entry.flagMap);

                if (entry.value.Count > 0)
                {
                    List<byte[]> val = entry.value;
                    UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(entry.value.ToArray());
                    if (ubObject != null)
                    {
                        cacheItem.SetValue(ubObject.GetFullObject());
                    }
                }

                cacheItem.EntryType = Util.MiscUtil.ProtoItemTypeToEntryType(entry.itemType);
            }
            catch (System.Exception e)
            {
                throw e;
            }

            return cacheItem;
        }        
    }
}
