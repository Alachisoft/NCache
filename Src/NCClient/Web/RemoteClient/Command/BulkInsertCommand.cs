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

using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Runtime.Events;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Caching.Events;
using System;

namespace Alachisoft.NCache.Client
{
    internal sealed class BulkInsertCommand : CommandBase
    {
        Common.Protobuf.BulkInsertCommand _bulkInsertCommand;
        Common.Protobuf.InsertCommand _insertCommand;
        private Cache _parent;
        private int _methodOverload;

        public BulkInsertCommand(string[] keys, CacheItem[] items, short onDataSourceItemUpdateCallbackId, Cache parent, string providerName, bool encryption, string cacheId, int methodOverload, string clientId, short updateCallbackId, short removeCallbackId, EventDataFilter updateCallbackDataFilter, EventDataFilter removeCallbackDataFilter, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            base.name = "BulkInsertCommand";

            _parent = parent;
            base.BulkKeys = keys;
            _bulkInsertCommand = new Common.Protobuf.BulkInsertCommand();
            _bulkInsertCommand.datasourceUpdatedCallbackId = onDataSourceItemUpdateCallbackId;
            _bulkInsertCommand.providerName = providerName;
            _bulkInsertCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
            short initialUpdateCallbackId = updateCallbackId;
            short initialRemoveCallBackId = removeCallbackId;
            for (int i = 0; i < keys.Length; i++)
            {
                CacheItem item = items[i];

                _insertCommand = new Common.Protobuf.InsertCommand();
                _insertCommand.key = keys[i];

                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject((byte[])item.GetValue<object>());
                _insertCommand.data.AddRange(ubObject.DataList);

                DateTime absExpiration = default(DateTime);
                if (item.Expiration.Absolute != Cache.NoAbsoluteExpiration)
                    absExpiration = item.Expiration.Absolute.ToUniversalTime();

                if (absExpiration.Equals(Cache.DefaultAbsolute.ToUniversalTime()))
                    _insertCommand.absExpiration = 1;
                else if (absExpiration.Equals(Cache.DefaultAbsoluteLonger.ToUniversalTime()))
                    _insertCommand.absExpiration = 2;
                else if (absExpiration != Cache.NoAbsoluteExpiration)
                    _insertCommand.absExpiration = absExpiration.Ticks;

                if (item.SlidingExpiration.Equals(Cache.DefaultSliding))
                    _insertCommand.sldExpiration = 1;
                else if (item.SlidingExpiration.Equals(Cache.DefaultSlidingLonger))
                    _insertCommand.sldExpiration = 2;
                else if (item.SlidingExpiration != Cache.NoSlidingExpiration)
                    _insertCommand.sldExpiration = item.SlidingExpiration.Ticks;

                _insertCommand.flag = item.FlagMap.Data;
                _insertCommand.priority = (int)item.Priority;
                //_insertCommand.dependency = item.Dependency == null ? null : Common.Util.DependencyHelper.GetProtoBufDependency(item.Dependency);

                // Client ID: Must not have value except ClientCache.
                _insertCommand.clientID = clientId;
                _insertCommand.CallbackType = CallbackType(callbackType);

                
                EventDataFilter itemUpdateDataFilter = updateCallbackDataFilter;
                EventDataFilter itemRemovedDataFilter = removeCallbackDataFilter;

                if (removeCallbackId <= 0) 
                {
                    if (item.CacheItemRemovedCallback != null)
                    {
                        itemRemovedDataFilter = item.ItemRemovedDataFilter;
                        short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback, EventTypeInternal.ItemRemoved, itemRemovedDataFilter, callbackType);
                        removeCallbackId = callabackIds[1];
                    }
                    else if (item.ItemRemoveCallback != null)
                    {
                        removeCallbackId = _parent.GetCallbackId(item.ItemRemoveCallback);
                        itemRemovedDataFilter = EventDataFilter.None;
                    }
                }
                if (updateCallbackId <= 0)
                {
                    if (item.CacheItemUpdatedCallback != null)
                    {
                        itemUpdateDataFilter = item.ItemUpdatedDataFilter;
                        short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemUpdatedCallback, EventTypeInternal.ItemUpdated, itemUpdateDataFilter, callbackType);
                        updateCallbackId = callabackIds[0];
                    }
                    else if (item.ItemUpdateCallback != null)
                    {
                        updateCallbackId = _parent.GetCallbackId(item.ItemUpdateCallback);
                        itemUpdateDataFilter = EventDataFilter.None;
                    }
                }
                _insertCommand.removeCallbackId = removeCallbackId;
                _insertCommand.updateCallbackId = updateCallbackId;
                _insertCommand.updateDataFilter = (short)itemUpdateDataFilter;
                _insertCommand.removeDataFilter = (short)itemRemovedDataFilter;
               
                _bulkInsertCommand.insertCommand.Add(_insertCommand);
                updateCallbackId = initialUpdateCallbackId;
                removeCallbackId = initialRemoveCallBackId;
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

        internal override bool IsSafe { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _bulkInsertCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.INSERT_BULK;
        }

        protected override void CreateCommand()
        {
            _bulkInsertCommand.commandID = this._commandID;
            _bulkInsertCommand.requestId = base.RequestId;
            _bulkInsertCommand.clientLastViewId = base.ClientLastViewId;
            _bulkInsertCommand.intendedRecipient = base.IntendedRecipient;
            _bulkInsertCommand.version = "4200";
            _bulkInsertCommand.MethodOverload = _methodOverload;
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
                        BulkInsertCommand bulkCommand = (BulkInsertCommand)command;
                        if (mergedCommand == null)
                        {
                            mergedCommand = bulkCommand;
                        }
                        else
                        {
                            mergedCommand._bulkInsertCommand.insertCommand.AddRange(bulkCommand._bulkInsertCommand.insertCommand);
                        }
                    }
                }
            }
            return mergedCommand;
        }
    }
}
