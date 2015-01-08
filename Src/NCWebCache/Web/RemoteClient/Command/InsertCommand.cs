// Copyright (c) 2015 Alachisoft
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
using System.Text;
using System.Collections;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;

using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime;
using System.IO;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Web.Caching.Util;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Runtime.Events;
using Web = Alachisoft.NCache.Web;
namespace Alachisoft.NCache.Web.Command
{
	internal sealed class InsertCommand : CommandBase
	{
        private Alachisoft.NCache.Common.Protobuf.InsertCommand _insertCommand;

		private DateTime _absoluteExpiration;
        private TimeSpan _slidingExpiration;
        private short _itemUpdatedCallbackId;
        private short _itemRemovedCallbackId;
		private CacheItemPriority _priority;
private Hashtable _queryInfo;
        private BitSet _flagMap = new BitSet();
        private object _lockId;
        private LockAccessType _accessType;
        private ulong _version;

        public InsertCommand(string key, byte[] value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short removeCallback, short updateCallback, Hashtable queryInfo, BitSet flagMap, object lockId, LockAccessType accessType, string cacheId, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter)
        {
            base.name = "InsertCommand";
            base.key = key;

            _insertCommand = new Alachisoft.NCache.Common.Protobuf.InsertCommand();
            _insertCommand.key = key;

            Alachisoft.NCache.Caching.UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value);
            _insertCommand.data.AddRange(ubObject.DataList);

			_insertCommand.requestId = base.RequestId;
            _insertCommand.removeCallbackId = removeCallback;
            _insertCommand.updateCallbackId = updateCallback;
            _insertCommand.updateDataFilter = (short)updateCallbackFilter;
            _insertCommand.removeDataFilter = (short)removeCallabackFilter;
            _insertCommand.priority = (int)priority;
            _insertCommand.flag = flagMap.Data;
            if(lockId != null) _insertCommand.lockId = lockId.ToString();
            _insertCommand.lockAccessType = (int)accessType;
            

            if (absoluteExpiration != Web.Caching.Cache.NoAbsoluteExpiration) 
                _insertCommand.absExpiration = absoluteExpiration.Ticks;

            if (slidingExpiration != Web.Caching.Cache.NoSlidingExpiration) 
            _insertCommand.sldExpiration = slidingExpiration.Ticks;

           

            ObjectQueryInfo objectQueryInfo = new ObjectQueryInfo();

            if (queryInfo["query-info"] != null)
                objectQueryInfo.queryInfo = ProtobufHelper.GetQueryInfoObj(queryInfo["query-info"] as Hashtable);

                _insertCommand.objectQueryInfo = objectQueryInfo;
            
        }

        internal override CommandType CommandType
        {
            get { return CommandType.INSERT; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {

            if (_insertCommand.sldExpiration != 0 && _insertCommand.absExpiration != 0)
                throw new ArgumentException("You cannot set both sliding and absolute expirations on the same cache item");


            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.insertCommand = _insertCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT;
            base._command.version = "4200";

        }		
	}
}
