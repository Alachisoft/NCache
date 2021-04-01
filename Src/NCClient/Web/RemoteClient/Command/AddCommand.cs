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
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Client
{
    internal sealed class AddCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.AddCommand _addCommand;
        private short _itemAdded;
        private int _methodOverload;

        internal AddCommand(string key, byte[] value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short removeCallback, short updateCallback, short dsItemAddedCallback, bool isResyncExpiredItems, short itemAdded, bool isAsync, Hashtable queryInfo, BitSet flagMap, string providerName, string resyncProviderName, bool encryption, string cacheId, EventDataFilter updateDataFilter, EventDataFilter removeDataFilter, int methodOverload, string clientId, string typeName)
        {
            base.name = "AddCommand";
            base.asyncCallbackSpecified = isAsync && itemAdded != -1 ? true : false;
            base.isAsync = isAsync;
            base.key = key;

            _itemAdded = itemAdded;

            _addCommand = new Alachisoft.NCache.Common.Protobuf.AddCommand();

            if (absoluteExpiration.Equals(Cache.DefaultAbsolute.ToUniversalTime()))
                _addCommand.absExpiration = 1;
            else if (absoluteExpiration.Equals(Cache.DefaultAbsoluteLonger.ToUniversalTime()))
                _addCommand.absExpiration = 2;
            else if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                _addCommand.absExpiration = absoluteExpiration.Ticks;

            if (slidingExpiration.Equals(Cache.DefaultSliding))
                _addCommand.sldExpiration = 1;
            else if (slidingExpiration.Equals(Cache.DefaultSlidingLonger))
                _addCommand.sldExpiration = 2;
            else if (slidingExpiration != Cache.NoSlidingExpiration)
                _addCommand.sldExpiration = slidingExpiration.Ticks;

            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value);

            _addCommand.key = key;
            _addCommand.data.AddRange(ubObject.DataList);
            _addCommand.requestId = base.RequestId;
            _addCommand.updateCallbackId = updateCallback;
            _addCommand.removeCallbackId = removeCallback;
            _addCommand.datasourceItemAddedCallbackId = dsItemAddedCallback;
            _addCommand.isAsync = isAsync;
            _addCommand.priority = (int)priority;
            _addCommand.isResync = isResyncExpiredItems;
            _addCommand.flag = flagMap.Data;
            _addCommand.providerName = providerName;
            _addCommand.resyncProviderName = resyncProviderName;
            _addCommand.updateDataFilter = (short)updateDataFilter;
            _addCommand.removeDataFilter = (short)removeDataFilter;

            // Client ID: Must not have value except ClientCache.
            _addCommand.clientID = clientId;
            _methodOverload = methodOverload;
        }

        internal short AsycItemAddedOpComplete
        {
            get { return _itemAdded; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.ADD; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        internal override bool IsSafe { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _addCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.ADD;
        }
        
        protected override void CreateCommand()
        {
           
            if (_addCommand.sldExpiration != 0)
            {
                if (!(_addCommand.absExpiration == 1 || _addCommand.absExpiration == 0))
                    throw new ArgumentException("You cannot set both sliding and absolute expirations on the same cache item");
            }

            _addCommand.commandID = this._commandID;
            _addCommand.requestId = base.RequestId;
            _addCommand.version = "4200";
            _addCommand.MethodOverload = _methodOverload;
        }
    }
}
