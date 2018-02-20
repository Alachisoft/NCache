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

using System;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class AddCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.AddCommand _addCommand;
        private short _itemAdded;
        private int _methodOverload;

        internal AddCommand(string key, byte[] value, CacheDependency dependency, CacheSyncDependency syncDependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short removeCallback,
            short updateCallback, short dsItemAddedCallback, bool isResyncExpiredItems, string group, string subGroup,
            short itemAdded, bool isAsync, Hashtable queryInfo, BitSet flagMap, string providerName,
            string resyncProviderName, string cacheId, EventDataFilter updateDataFilter,
            EventDataFilter removeDataFilter, int methodOverload, string clientId)
        {
            base.name = "AddCommand";
            base.asyncCallbackSpecified = isAsync && itemAdded != -1 ? true : false;
            base.isAsync = isAsync;
            base.key = key;

            _itemAdded = itemAdded;


            _addCommand = new Alachisoft.NCache.Common.Protobuf.AddCommand();

            if (absoluteExpiration.Equals(Caching.Cache.DefaultAbsolute.ToUniversalTime()))
                _addCommand.absExpiration = 1;
            else if (absoluteExpiration.Equals(Caching.Cache.DefaultAbsoluteLonger.ToUniversalTime()))
                _addCommand.absExpiration = 2;
            else if (absoluteExpiration != Caching.Cache.NoAbsoluteExpiration)
                _addCommand.absExpiration = absoluteExpiration.Ticks;

            if (slidingExpiration.Equals(Caching.Cache.DefaultSliding))
                _addCommand.sldExpiration = 1;
            else if (slidingExpiration.Equals(Caching.Cache.DefaultSlidingLonger))
                _addCommand.sldExpiration = 2;
            else if (slidingExpiration != Caching.Cache.NoSlidingExpiration)
                _addCommand.sldExpiration = slidingExpiration.Ticks;
            Alachisoft.NCache.Caching.UserBinaryObject ubObject =
                Alachisoft.NCache.Caching.UserBinaryObject.CreateUserBinaryObject(value);

            _addCommand.key = key;
            _addCommand.data.AddRange(ubObject.DataList);
            _addCommand.requestId = base.RequestId;
            _addCommand.updateCallbackId = updateCallback;
            _addCommand.removeCallbackId = removeCallback;
            _addCommand.datasourceItemAddedCallbackId = dsItemAddedCallback;
            _addCommand.isAsync = isAsync;
            _addCommand.priority = (int) priority;
            _addCommand.isResync = isResyncExpiredItems;
            _addCommand.group = group;
            _addCommand.subGroup = subGroup;
            _addCommand.flag = flagMap.Data;
            _addCommand.providerName = providerName;
            _addCommand.resyncProviderName = resyncProviderName;
            _addCommand.updateDataFilter = (short) updateDataFilter;
            _addCommand.removeDataFilter = (short) removeDataFilter;

            _addCommand.clientID = clientId;

            if (syncDependency != null)
            {
                _addCommand.syncDependency = new SyncDependency();
                _addCommand.syncDependency.cacheId = syncDependency.CacheId;
                _addCommand.syncDependency.key = syncDependency.Key;
                _addCommand.syncDependency.server = syncDependency.Server;
                _addCommand.syncDependency.port = syncDependency.Port;
            }

            ObjectQueryInfo objectQueryInfo = new ObjectQueryInfo();

            if (queryInfo["query-info"] != null)
                objectQueryInfo.queryInfo = ProtobufHelper.GetQueryInfoObj(queryInfo["query-info"] as Hashtable);

            if (queryInfo["tag-info"] != null)
                objectQueryInfo.tagInfo = ProtobufHelper.GetTagInfoObj(queryInfo["tag-info"] as Hashtable);

            if (queryInfo["named-tag-info"] != null)
                objectQueryInfo.namedTagInfo =
                    ProtobufHelper.GetNamedTagInfoObj(queryInfo["named-tag-info"] as Hashtable, true);


            _addCommand.objectQueryInfo = objectQueryInfo;

            if (dependency != null)
                _addCommand.dependency =
                    Alachisoft.NCache.Common.Util.DependencyHelper.GetProtoBufDependency(dependency);
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

        internal override bool IsSafe
        {
            get { return false; }
        }

        protected override void CreateCommand()
        {
            if (_addCommand.group == null && _addCommand.subGroup != null)
                throw new ArgumentException("Group must be specified for Sub Group");

            if (_addCommand.sldExpiration != 0)
            {
                if (!(_addCommand.absExpiration == 1 || _addCommand.absExpiration == 0))
                    throw new ArgumentException(
                        "You cannot set both sliding and absolute expirations on the same cache item");
            }


            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.addCommand = _addCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.ADD;
            base._command.version = "4200";
            base._command.MethodOverload = _methodOverload;
        }
    }
}