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
    internal sealed class BulkAddCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.BulkAddCommand _bulkAddCommand;
        private Alachisoft.NCache.Common.Protobuf.AddCommand _addCommand;

        private Alachisoft.NCache.Web.Caching.Cache _parent;
        private int _methodOverload;

        public BulkAddCommand(string[] keys, CacheItem[] items, short onDsItemsAddedCallback,
            Alachisoft.NCache.Web.Caching.Cache parent, string providerName, string cacheId, int methodOverload,
            string clientId, short updateCallbackId, short removedCallbackId, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, bool returnVersions,
            CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            base.name = "BulkAddCommand";
            _parent = parent;
            _methodOverload = methodOverload;
            _bulkAddCommand = new Alachisoft.NCache.Common.Protobuf.BulkAddCommand();
            _bulkAddCommand.datasourceItemAddedCallbackId = onDsItemsAddedCallback;
            _bulkAddCommand.providerName = providerName;
            _bulkAddCommand.returnVersions = returnVersions;
            _bulkAddCommand.requestId = base.RequestId;
            base.BulkKeys = keys;

            for (int i = 0; i < keys.Length; i++)
            {
                CacheItem item = items[i];

                _addCommand = new Alachisoft.NCache.Common.Protobuf.AddCommand();
                _addCommand.key = keys[i];

                Alachisoft.NCache.Caching.UserBinaryObject ubObject =
                    Alachisoft.NCache.Caching.UserBinaryObject.CreateUserBinaryObject((byte[]) item.Value);
                _addCommand.data.AddRange(ubObject.DataList);

                if (item.AbsoluteExpiration.Equals(Caching.Cache.DefaultAbsolute.ToUniversalTime()))
                    _addCommand.absExpiration = 1;
                else if (item.AbsoluteExpiration.Equals(Caching.Cache.DefaultAbsoluteLonger.ToUniversalTime()))
                    _addCommand.absExpiration = 2;
                else if (item.AbsoluteExpiration != Caching.Cache.NoAbsoluteExpiration)
                    _addCommand.absExpiration = item.AbsoluteExpiration.Ticks;

                if (item.SlidingExpiration.Equals(Caching.Cache.DefaultSliding))
                    _addCommand.sldExpiration = 1;
                else if (item.SlidingExpiration.Equals(Caching.Cache.DefaultSlidingLonger))
                    _addCommand.sldExpiration = 2;
                else if (item.SlidingExpiration != Caching.Cache.NoSlidingExpiration)
                    _addCommand.sldExpiration = item.SlidingExpiration.Ticks;

                _addCommand.flag = item.FlagMap.Data;
                _addCommand.group = item.Group;
                _addCommand.subGroup = item.SubGroup;
                _addCommand.isResync = item.IsResyncExpiredItems;
                _addCommand.priority = (int) item.Priority;
                _addCommand.dependency = item.Dependency == null
                    ? null
                    : Alachisoft.NCache.Common.Util.DependencyHelper.GetProtoBufDependency(item.Dependency);

                _addCommand.clientID = clientId;
                _addCommand.CallbackType = CallbackType(callbackType);


                ObjectQueryInfo objectQueryInfo = new ObjectQueryInfo();

                if (item.QueryInfo["query-info"] != null)
                    objectQueryInfo.queryInfo =
                        ProtobufHelper.GetQueryInfoObj(item.QueryInfo["query-info"] as Hashtable);

                if (item.QueryInfo["tag-info"] != null)
                    objectQueryInfo.tagInfo = ProtobufHelper.GetTagInfoObj(item.QueryInfo["tag-info"] as Hashtable);

                if (item.QueryInfo["named-tag-info"] != null)
                    objectQueryInfo.namedTagInfo =
                        ProtobufHelper.GetNamedTagInfoObj(item.QueryInfo["named-tag-info"] as Hashtable, true);


                _addCommand.objectQueryInfo = objectQueryInfo;


                EventDataFilter itemUpdateDataFilter = updateCallbackFilter;
                EventDataFilter itemRemovedDataFilter = removeCallabackFilter;


                if (item.CacheItemRemovedCallback != null)
                {
                    itemRemovedDataFilter = item.ItemRemovedCallabackDataFilter;
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback,
                        EventType.ItemRemoved, itemRemovedDataFilter);
                    removedCallbackId = callabackIds[1];
                }
                else if (item.ItemRemoveCallback != null)
                {
                    removedCallbackId = _parent.GetCallbackId(item.ItemRemoveCallback);
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


                _addCommand.removeCallbackId = removedCallbackId;
                _addCommand.updateCallbackId = updateCallbackId;
                _addCommand.updateDataFilter = (short) itemUpdateDataFilter;
                _addCommand.removeDataFilter = (short) itemRemovedDataFilter;

                _addCommand.resyncProviderName = item.ResyncProviderName;

                if (item.SyncDependency != null)
                {
                    _addCommand.syncDependency = new Alachisoft.NCache.Common.Protobuf.SyncDependency();
                    _addCommand.syncDependency.key = item.SyncDependency.Key;
                    _addCommand.syncDependency.cacheId = item.SyncDependency.CacheId;
                    _addCommand.syncDependency.server = item.SyncDependency.Server;
                    _addCommand.syncDependency.port = item.SyncDependency.Port;
                }

                _bulkAddCommand.addCommand.Add(_addCommand);
            }
        }

        private int CallbackType(CallbackType type)
        {
            if (type == Runtime.Events.CallbackType.PullBasedCallback)
                return 0;
            else if (type == Runtime.Events.CallbackType.PushBasedNotification)
                return 1;
            else
                return 1;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.ADD_BULK; }
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
            base._command.bulkAddCommand = _bulkAddCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_BULK;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.intendedRecipient = base.IntendedRecipient;
            base._command.version = "4200";
            base._command.MethodOverload = _methodOverload;
        }

        protected override CommandBase GetMergedCommand(List<CommandBase> commands)
        {
            BulkAddCommand mergedCommand = null;
            if (commands != null || commands.Count > 0)
            {
                foreach (CommandBase command in commands)
                {
                    if (command is BulkAddCommand)
                    {
                        BulkAddCommand bulkCommand = (BulkAddCommand) command;
                        if (mergedCommand == null)
                        {
                            mergedCommand = bulkCommand;
                        }
                        else
                        {
                            mergedCommand._bulkAddCommand.addCommand.AddRange(bulkCommand._bulkAddCommand.addCommand);
                        }
                    }
                }
            }

            return mergedCommand;
        }
    }
}