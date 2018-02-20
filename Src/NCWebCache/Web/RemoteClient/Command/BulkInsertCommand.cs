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

using System.Collections.Generic;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Caching.Util;
using System.Collections;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class BulkInsertCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.BulkInsertCommand _bulkInsertCommand;
        Alachisoft.NCache.Common.Protobuf.InsertCommand _insertCommand;
        private Alachisoft.NCache.Web.Caching.Cache _parent;
        private int _methodOverload;

        public BulkInsertCommand(string[] keys, CacheItem[] items, short onDataSourceItemUpdateCallbackId,
            Alachisoft.NCache.Web.Caching.Cache parent, string providerName, string cacheId, int methodOverload,
            string clientId, short updateCallbackId, short removeCallbackId, EventDataFilter updateCallbackDataFilter,
            EventDataFilter removeCallbackDataFilter, bool returnVersions,
            CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            base.name = "BulkInsertCommand";

            _parent = parent;
            base.BulkKeys = keys;
            _bulkInsertCommand = new Alachisoft.NCache.Common.Protobuf.BulkInsertCommand();
            _bulkInsertCommand.datasourceUpdatedCallbackId = onDataSourceItemUpdateCallbackId;
            _bulkInsertCommand.providerName = providerName;
            _bulkInsertCommand.returnVersions = returnVersions;
            _bulkInsertCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;

            for (int i = 0; i < keys.Length; i++)
            {
                CacheItem item = items[i];

                _insertCommand = new Alachisoft.NCache.Common.Protobuf.InsertCommand();
                _insertCommand.key = keys[i];

                Alachisoft.NCache.Caching.UserBinaryObject ubObject =
                    Alachisoft.NCache.Caching.UserBinaryObject.CreateUserBinaryObject((byte[]) item.Value);
                _insertCommand.data.AddRange(ubObject.DataList);

                if (item.AbsoluteExpiration.Equals(Caching.Cache.DefaultAbsolute.ToUniversalTime()))
                    _insertCommand.absExpiration = 1;
                else if (item.AbsoluteExpiration.Equals(Caching.Cache.DefaultAbsoluteLonger.ToUniversalTime()))
                    _insertCommand.absExpiration = 2;
                else if (item.AbsoluteExpiration != Caching.Cache.NoAbsoluteExpiration)
                    _insertCommand.absExpiration = item.AbsoluteExpiration.Ticks;


                if (item.SlidingExpiration.Equals(Caching.Cache.DefaultSliding))
                    _insertCommand.sldExpiration = 1;
                else if (item.SlidingExpiration.Equals(Caching.Cache.DefaultSlidingLonger))
                    _insertCommand.sldExpiration = 2;
                else if (item.SlidingExpiration != Caching.Cache.NoSlidingExpiration)
                    _insertCommand.sldExpiration = item.SlidingExpiration.Ticks;

                _insertCommand.flag = item.FlagMap.Data;
                _insertCommand.group = item.Group;
                _insertCommand.subGroup = item.SubGroup;
                _insertCommand.isResync = item.IsResyncExpiredItems;
                _insertCommand.priority = (int) item.Priority;
                _insertCommand.dependency = item.Dependency == null
                    ? null
                    : Alachisoft.NCache.Common.Util.DependencyHelper.GetProtoBufDependency(item.Dependency);

                _insertCommand.clientID = clientId;
                _insertCommand.CallbackType = CallbackType(callbackType);

                ObjectQueryInfo objectQueryInfo = new ObjectQueryInfo();

                if (item.QueryInfo["query-info"] != null)
                    objectQueryInfo.queryInfo =
                        ProtobufHelper.GetQueryInfoObj(item.QueryInfo["query-info"] as Hashtable);

                if (item.QueryInfo["tag-info"] != null)
                    objectQueryInfo.tagInfo = ProtobufHelper.GetTagInfoObj(item.QueryInfo["tag-info"] as Hashtable);

                if (item.QueryInfo["named-tag-info"] != null)
                    objectQueryInfo.namedTagInfo =
                        ProtobufHelper.GetNamedTagInfoObj(item.QueryInfo["named-tag-info"] as Hashtable, true);


                _insertCommand.objectQueryInfo = objectQueryInfo;

                EventDataFilter itemUpdateDataFilter = updateCallbackDataFilter;
                EventDataFilter itemRemovedDataFilter = removeCallbackDataFilter;

                if (item.CacheItemRemovedCallback != null)
                {
                    itemRemovedDataFilter = item.ItemRemovedCallabackDataFilter;
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback,
                        EventType.ItemRemoved, itemRemovedDataFilter);
                    removeCallbackId = callabackIds[1];
                }
                else if (item.ItemRemoveCallback != null)
                {
                    removeCallbackId = _parent.GetCallbackId(item.ItemRemoveCallback);
                    itemRemovedDataFilter = EventDataFilter.DataWithMetadata;
                }

                if (item.CacheItemUpdatedCallback != null)
                {
                    itemUpdateDataFilter = item.ItemUpdatedCallabackDataFilter;
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemUpdatedCallback,
                        EventType.ItemUpdated, itemUpdateDataFilter);
                    updateCallbackId = callabackIds[0];
                }
                else if (item.ItemUpdateCallback != null)
                {
                    updateCallbackId = _parent.GetCallbackId(item.ItemUpdateCallback);
                    itemUpdateDataFilter = EventDataFilter.None;
                }

                _insertCommand.removeCallbackId = removeCallbackId;
                _insertCommand.updateCallbackId = updateCallbackId;
                _insertCommand.updateDataFilter = (short) itemUpdateDataFilter;
                _insertCommand.removeDataFilter = (short) itemRemovedDataFilter;
                _insertCommand.resyncProviderName = item.ResyncProviderName;

                if (item.SyncDependency != null)
                {
                    _insertCommand.syncDependency = new Alachisoft.NCache.Common.Protobuf.SyncDependency();
                    _insertCommand.syncDependency.key = item.SyncDependency.Key;
                    _insertCommand.syncDependency.cacheId = item.SyncDependency.CacheId;
                    _insertCommand.syncDependency.server = item.SyncDependency.Server;
                    _insertCommand.syncDependency.port = item.SyncDependency.Port;
                }

                _bulkInsertCommand.insertCommand.Add(_insertCommand);
            }
        }

        private int CallbackType(CallbackType type)
        {
            if (type == Runtime.Events.CallbackType.PullBasedCallback)
                return 0;
            else if (type == Runtime.Events.CallbackType.PushBasedNotification)
                return 1;
            else
                return 1; // default is push
        }

        internal override CommandType CommandType
        {
            get { return CommandType.INSERT_BULK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkWrite; }
        }

        internal override bool IsSafe
        {
            get { return false; }
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
            base._command.MethodOverload = _methodOverload;
        }

        protected override CommandBase GetMergedCommand(List<CommandBase> commands)
        {
            BulkInsertCommand mergedCommand = null;
            if (commands != null || commands.Count > 0)
            {
                foreach (CommandBase command in commands)
                {
                    if (command is BulkInsertCommand)
                    {
                        BulkInsertCommand bulkCommand = (BulkInsertCommand) command;
                        if (mergedCommand == null)
                        {
                            mergedCommand = bulkCommand;
                        }
                        else
                        {
                            mergedCommand._bulkInsertCommand.insertCommand.AddRange(bulkCommand._bulkInsertCommand
                                .insertCommand);
                        }
                    }
                }
            }

            return mergedCommand;
        }
    }
}