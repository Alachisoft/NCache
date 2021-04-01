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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Events;
using System;
using System.Collections;
using System.IO;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Runtime.Exceptions;
namespace Alachisoft.NCache.Client
{
    internal sealed class InsertCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.InsertCommand _insertCommand;

        private DateTime _absoluteExpiration;
        private TimeSpan _slidingExpiration;
        private bool _isResyncExpiredItems;
        private short _itemUpdatedCallbackId;
        private short _itemRemovedCallbackId;
        private short _dsItemsUpdatedCallbackId;
        private CacheItemPriority _priority;
        private short _itemUpdated;
        private Hashtable _queryInfo;
        private BitSet _flagMap = new BitSet();
        private object _lockId;
        private LockAccessType _accessType;
        private ulong _version;
        private string _providerName;
        private string _ressyncProviderName;
        private int _methodOverload;
        private string _type;

        public InsertCommand(string key, byte[] value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short removeCallback, short updateCallback, short dsItemsUpdateCallbackId, bool isResyncExpiredItems, short itemUpdated, bool isAsync, Hashtable queryInfo, BitSet flagMap, object lockId, ulong version, LockAccessType accessType, string providerName, string resyncProviderName, bool encryption, string cacheId, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, int methodOverload, string clientId, string typeName, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            base.name = "InsertCommand";
            base.asyncCallbackSpecified = isAsync && itemUpdated != -1 ? true : false;
            base.isAsync = isAsync;
            base.key = key;

            _itemUpdated = itemUpdated;

            _insertCommand = new Alachisoft.NCache.Common.Protobuf.InsertCommand();
            _insertCommand.key = key;

            Alachisoft.NCache.Common.Caching.UserBinaryObject ubObject = Alachisoft.NCache.Common.Caching.UserBinaryObject.CreateUserBinaryObject(value);
            _insertCommand.data.AddRange(ubObject.DataList);
            _methodOverload = methodOverload;
            _insertCommand.requestId = base.RequestId;
            _insertCommand.removeCallbackId = removeCallback;
            _insertCommand.updateCallbackId = updateCallback;
            _insertCommand.updateDataFilter = (short)updateCallbackFilter;
            _insertCommand.removeDataFilter = (short)removeCallabackFilter;
            _insertCommand.datasourceUpdatedCallbackId = dsItemsUpdateCallbackId;
            _insertCommand.isAsync = isAsync;
            _insertCommand.priority = (int)priority;
            _insertCommand.flag = flagMap.Data;
            _insertCommand.itemVersion = version;
            if (lockId != null) _insertCommand.lockId = lockId.ToString();
            _insertCommand.lockAccessType = (int)accessType == 0 ? (int)LockAccessType.IGNORE_LOCK : (int)accessType;
            _insertCommand.providerName = providerName;
            _insertCommand.resyncProviderName = resyncProviderName;

            // Client ID: Must not have value except ClientCache.
            _insertCommand.clientID = clientId;
            _insertCommand.CallbackType = CallbackType(callbackType);

            if (absoluteExpiration.Equals(Cache.DefaultAbsolute.ToUniversalTime()))
                _insertCommand.absExpiration = 1;
            else if (absoluteExpiration.Equals(Cache.DefaultAbsoluteLonger.ToUniversalTime()))
                _insertCommand.absExpiration = 2;
            else if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                _insertCommand.absExpiration = absoluteExpiration.Ticks;

            if (slidingExpiration.Equals(Cache.DefaultSliding))
                _insertCommand.sldExpiration = 1;
            else if (slidingExpiration.Equals(Cache.DefaultSlidingLonger))
                _insertCommand.sldExpiration = 2;
            else if (slidingExpiration != Cache.NoSlidingExpiration)
                _insertCommand.sldExpiration = slidingExpiration.Ticks;

            _insertCommand.isResync = isResyncExpiredItems;
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

        internal short AsycItemUpdatedOpComplete
        {
            get { return _itemUpdated; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.INSERT; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        internal override bool IsSafe { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _insertCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.INSERT;
        }

        protected override void CreateCommand()
        {
            if (_insertCommand.sldExpiration != 0)
            {
                if (!(_insertCommand.absExpiration == 1 || _insertCommand.absExpiration == 0))
                    throw new ArgumentException("You cannot set both sliding and absolute expirations on the same cache item");
            }


            _insertCommand.commandID = this._commandID;
            _insertCommand.requestId = base.RequestId;
            _insertCommand.version = "4200";
            _insertCommand.MethodOverload = _methodOverload;


        }		
	}
}
