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

using System.Collections;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Runtime.Events;


namespace Alachisoft.NCache.Web.Command
{
    internal sealed class BulkInsertCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.BulkInsertCommand _bulkInsertCommand;
        Alachisoft.NCache.Common.Protobuf.InsertCommand _insertCommand;
        private Cache _parent;

        public BulkInsertCommand(string[] keys, CacheItem[] items,  Cache parent, string cacheId)
        {
            base.name = "BulkInsertCommand";
            _parent = parent;
            base.BulkKeys = keys;
            _bulkInsertCommand = new Alachisoft.NCache.Common.Protobuf.BulkInsertCommand();
            _bulkInsertCommand.requestId = base.RequestId;

            for (int i = 0; i < keys.Length; i++)
            {
                CacheItem item = items[i];

                _insertCommand = new Alachisoft.NCache.Common.Protobuf.InsertCommand();
                _insertCommand.key = keys[i];

                Alachisoft.NCache.Caching.UserBinaryObject ubObject = Alachisoft.NCache.Caching.UserBinaryObject.CreateUserBinaryObject((byte[])item.Value);
                _insertCommand.data.AddRange(ubObject.DataList);

				if (item.AbsoluteExpiration != Cache.NoAbsoluteExpiration)
					_insertCommand.absExpiration = item.AbsoluteExpiration.Ticks;

				if (item.SlidingExpiration != Cache.NoSlidingExpiration)
					_insertCommand.sldExpiration = item.SlidingExpiration.Ticks;

                _insertCommand.flag = item.FlagMap.Data;
               _insertCommand.priority = (int)item.Priority;

                ObjectQueryInfo objectQueryInfo = new ObjectQueryInfo();

                if (item.QueryInfo["query-info"] != null)
                    objectQueryInfo.queryInfo = ProtobufHelper.GetQueryInfoObj(item.QueryInfo["query-info"] as Hashtable);

                _insertCommand.objectQueryInfo = objectQueryInfo;

                short removeCallbackId = -1;
                short updateCallbackId = -1;
                EventDataFilter itemUpdateDataFilter = EventDataFilter.None;
                EventDataFilter itemRemovedDataFilter = EventDataFilter.None;

                if (item.CacheItemRemovedCallback != null)
                {
                    itemRemovedDataFilter = item.ItemRemovedCallabackDataFilter;
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback, EventType.ItemRemoved, itemRemovedDataFilter);
                    removeCallbackId = callabackIds[1];
                }
                

                if (item.CacheItemUpdatedCallback != null)
                {
                    itemUpdateDataFilter = item.ItemUpdatedCallabackDataFilter;
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemUpdatedCallback, EventType.ItemUpdated, itemUpdateDataFilter);
                    updateCallbackId = callabackIds[0];
                }
                

                _insertCommand.removeCallbackId = removeCallbackId;
                _insertCommand.updateCallbackId = updateCallbackId;
                _insertCommand.updateDataFilter = (short)itemUpdateDataFilter;
                _insertCommand.removeDataFilter = (short)itemRemovedDataFilter;

                _bulkInsertCommand.insertCommand.Add(_insertCommand);
            }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.INSERT_BULK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.BulkWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.bulkInsertCommand = _bulkInsertCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT_BULK;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.intendedRecipient = base.IntendedRecipient;
            base._command.version = "4200";
        }
    }
}
