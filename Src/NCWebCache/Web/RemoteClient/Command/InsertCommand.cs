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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class InsertCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.InsertCommand _insertCommand;

        private string _group;
        private string _subGroup;
        private DateTime _absoluteExpiration;
        private TimeSpan _slidingExpiration;
        private bool _isResyncExpiredItems;
        private short _itemUpdatedCallbackId;
        private short _itemRemovedCallbackId;
        private short _dsItemsUpdatedCallbackId;
        private CacheItemPriority _priority;
        private CacheDependency _dependency;
        private CacheSyncDependency _syncDependency;
        private short _itemUpdated;
        private Hashtable _queryInfo;
        private BitSet _flagMap = new BitSet();
        private object _lockId;
        private LockAccessType _accessType;
        private ulong _version;
        private string _providerName;
        private string _ressyncProviderName;
        private int _methodOverload;

        public InsertCommand(string key, byte[] value, CacheDependency dependency, CacheSyncDependency syncDependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short removeCallback,
            short updateCallback, short dsItemsUpdateCallbackId, bool isResyncExpiredItems, string group,
            string subGroup, short itemUpdated, bool isAsync, Hashtable queryInfo, BitSet flagMap, object lockId,
            ulong version, LockAccessType accessType, string providerName, string resyncProviderName, string cacheId,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, int methodOverload,
            string clientId, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            base.name = "InsertCommand";
            base.asyncCallbackSpecified = isAsync && itemUpdated != -1 ? true : false;
            base.isAsync = isAsync;
            base.key = key;

            _itemUpdated = itemUpdated;

            _insertCommand = new Alachisoft.NCache.Common.Protobuf.InsertCommand();
            _insertCommand.key = key;

            Alachisoft.NCache.Caching.UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value);
            _insertCommand.data.AddRange(ubObject.DataList);
            _methodOverload = methodOverload;
            _insertCommand.requestId = base.RequestId;
            _insertCommand.removeCallbackId = removeCallback;
            _insertCommand.updateCallbackId = updateCallback;
            _insertCommand.updateDataFilter = (short) updateCallbackFilter;
            _insertCommand.removeDataFilter = (short) removeCallabackFilter;
            _insertCommand.datasourceUpdatedCallbackId = dsItemsUpdateCallbackId;
            _insertCommand.isAsync = isAsync;
            _insertCommand.priority = (int) priority;
            _insertCommand.flag = flagMap.Data;
            _insertCommand.itemVersion = version;
            if (lockId != null) _insertCommand.lockId = lockId.ToString();
            _insertCommand.lockAccessType = (int) accessType;
            _insertCommand.providerName = providerName;
            _insertCommand.resyncProviderName = resyncProviderName;

            _insertCommand.clientID = clientId;
            _insertCommand.CallbackType = CallbackType(callbackType);

            if (syncDependency != null)
            {
                _insertCommand.syncDependency = new Alachisoft.NCache.Common.Protobuf.SyncDependency();
                _insertCommand.syncDependency.cacheId = syncDependency.CacheId;
                _insertCommand.syncDependency.key = syncDependency.Key;
                _insertCommand.syncDependency.server = syncDependency.Server;
                _insertCommand.syncDependency.port = syncDependency.Port;
            }

            if (absoluteExpiration.Equals(Caching.Cache.DefaultAbsolute.ToUniversalTime()))
                _insertCommand.absExpiration = 1;
            else if (absoluteExpiration.Equals(Caching.Cache.DefaultAbsoluteLonger.ToUniversalTime()))
                _insertCommand.absExpiration = 2;
            else if (absoluteExpiration != Caching.Cache.NoAbsoluteExpiration)
                _insertCommand.absExpiration = absoluteExpiration.Ticks;

            if (slidingExpiration.Equals(Caching.Cache.DefaultSliding))
                _insertCommand.sldExpiration = 1;
            else if (slidingExpiration.Equals(Caching.Cache.DefaultSlidingLonger))
                _insertCommand.sldExpiration = 2;
            else if (slidingExpiration != Caching.Cache.NoSlidingExpiration)
                _insertCommand.sldExpiration = slidingExpiration.Ticks;


            _insertCommand.isResync = isResyncExpiredItems;
            _insertCommand.group = group;
            _insertCommand.subGroup = subGroup;

            ObjectQueryInfo objectQueryInfo = new ObjectQueryInfo();

            if (queryInfo["query-info"] != null)
                objectQueryInfo.queryInfo = ProtobufHelper.GetQueryInfoObj(queryInfo["query-info"] as Hashtable);

            if (queryInfo["tag-info"] != null)
                objectQueryInfo.tagInfo = ProtobufHelper.GetTagInfoObj(queryInfo["tag-info"] as Hashtable);

            if (queryInfo["named-tag-info"] != null)
                objectQueryInfo.namedTagInfo =
                    ProtobufHelper.GetNamedTagInfoObj(queryInfo["named-tag-info"] as Hashtable, true);


            _insertCommand.objectQueryInfo = objectQueryInfo;

            if (dependency != null)
                _insertCommand.dependency =
                    Alachisoft.NCache.Common.Util.DependencyHelper.GetProtoBufDependency(dependency);
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

        internal override bool IsSafe
        {
            get { return false; }
        }


        protected override void CreateCommand()
        {
            if (_insertCommand.group == null && _insertCommand.subGroup != null)
                throw new ArgumentException("Group must be specified for Sub Group");

            if (_insertCommand.sldExpiration != 0)
            {
                if (!(_insertCommand.absExpiration == 1 || _insertCommand.absExpiration == 0))
                    throw new ArgumentException(
                        "You cannot set both sliding and absolute expirations on the same cache item");
            }


            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.insertCommand = _insertCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT;
            base._command.version = "4200";
            base._command.MethodOverload = _methodOverload;
        }
    }
}