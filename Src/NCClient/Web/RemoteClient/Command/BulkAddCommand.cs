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

using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Runtime.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class BulkAddCommand : CommandBase
    {
        private Common.Protobuf.BulkAddCommand _bulkAddCommand;
        private Common.Protobuf.AddCommand _addCommand;

        private Cache _parent;
        private int _methodOverload;
        public BulkAddCommand(string[] keys, CacheItem[] items, short onDsItemsAddedCallback, Cache parent, string providerName, bool encryption, string cacheId, int methodOverload, string clientId, short updateCallbackId, short removedCallbackId, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            base.name = "BulkAddCommand";
            _parent = parent;
            _methodOverload = methodOverload;
            _bulkAddCommand = new Common.Protobuf.BulkAddCommand();
            _bulkAddCommand.datasourceItemAddedCallbackId = onDsItemsAddedCallback;
            _bulkAddCommand.providerName = providerName;
            _bulkAddCommand.requestId = base.RequestId;
            short initialUpdateCallbackId = updateCallbackId;
            short initialRemoveCallBackId = removedCallbackId;
            base.BulkKeys = keys;

            for (int i = 0; i < keys.Length; i++)
            {
                CacheItem item = items[i];

                _addCommand = new Alachisoft.NCache.Common.Protobuf.AddCommand();
                _addCommand.key = keys[i];

                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject((byte[])item.GetValue<object>());
                _addCommand.data.AddRange(ubObject.DataList);

                DateTime absExpiration = default(DateTime);
                if (item.Expiration.Absolute != Cache.NoAbsoluteExpiration)
                    absExpiration = item.Expiration.Absolute.ToUniversalTime();

                if (absExpiration.Equals(Cache.DefaultAbsolute.ToUniversalTime()))
                    _addCommand.absExpiration = 1;
                else if (absExpiration.Equals(Cache.DefaultAbsoluteLonger.ToUniversalTime()))
                    _addCommand.absExpiration = 2;
                else if (absExpiration != Cache.NoAbsoluteExpiration)
                    _addCommand.absExpiration = absExpiration.Ticks;

                if (item.SlidingExpiration.Equals(Cache.DefaultSliding))
                    _addCommand.sldExpiration = 1;
                else if (item.SlidingExpiration.Equals(Cache.DefaultSlidingLonger))
                    _addCommand.sldExpiration = 2;
                else if (item.SlidingExpiration != Cache.NoSlidingExpiration)
                    _addCommand.sldExpiration = item.SlidingExpiration.Ticks;

                _addCommand.flag = item.FlagMap.Data;
                _addCommand.priority = (int)item.Priority;
                _addCommand.clientID = clientId;
                _addCommand.CallbackType = CallbackType(callbackType);

             

                EventDataFilter itemUpdateDataFilter = updateCallbackFilter;
                EventDataFilter itemRemovedDataFilter = removeCallabackFilter;


                if (removedCallbackId <= 0)
                {
                    if (item.CacheItemRemovedCallback != null)
                    {
                        itemRemovedDataFilter = item.ItemRemovedDataFilter;
                        short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback, EventTypeInternal.ItemRemoved, itemRemovedDataFilter, callbackType);
                        removedCallbackId = callabackIds[1];
                    }
                    else if (item.ItemRemoveCallback != null)
                    {
                        removedCallbackId = _parent.GetCallbackId(item.ItemRemoveCallback);
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


                _addCommand.removeCallbackId = removedCallbackId;
                _addCommand.updateCallbackId = updateCallbackId;
                _addCommand.updateDataFilter = (short)itemUpdateDataFilter;
                _addCommand.removeDataFilter = (short)itemRemovedDataFilter;
                _bulkAddCommand.addCommand.Add(_addCommand);
                updateCallbackId =initialUpdateCallbackId  ;
                removedCallbackId = initialRemoveCallBackId ;
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
            get { return CommandType.ADD_BULK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkWrite; }
        }

        internal override bool IsSafe { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _bulkAddCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.ADD_BULK;
        }

        protected override void CreateCommand()
        {
            _bulkAddCommand.commandID = this._commandID;
            _bulkAddCommand.requestId = base.RequestId;
            _bulkAddCommand.clientLastViewId = base.ClientLastViewId;
            _bulkAddCommand.intendedRecipient = base.IntendedRecipient;
            _bulkAddCommand.version = "4200";
            _bulkAddCommand.MethodOverload = _methodOverload;
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
                        BulkAddCommand bulkCommand = (BulkAddCommand)command;
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
