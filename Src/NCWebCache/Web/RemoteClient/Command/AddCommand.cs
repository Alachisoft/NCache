// Copyright (c) 2017 Alachisoft
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
using System.IO;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Runtime;

using Alachisoft.NCache.Runtime.Events;


namespace Alachisoft.NCache.Web.Command
{
    internal sealed class AddCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.AddCommand _addCommand;
        internal AddCommand(string key, byte[] value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short removeCallback, short updateCallback, Hashtable queryInfo, BitSet flagMap, string cacheId,EventDataFilter updateDataFilter,EventDataFilter removeDataFilter)
        {
            base.name = "AddCommand";
            base.key = key;


            _addCommand = new Alachisoft.NCache.Common.Protobuf.AddCommand();

            if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                _addCommand.absExpiration = absoluteExpiration.Ticks;

            if (slidingExpiration != Cache.NoSlidingExpiration)
                _addCommand.sldExpiration = slidingExpiration.Ticks;

            Alachisoft.NCache.Caching.UserBinaryObject ubObject = Alachisoft.NCache.Caching.UserBinaryObject.CreateUserBinaryObject(value);

            _addCommand.key = key;
            _addCommand.data.AddRange(ubObject.DataList);
            _addCommand.requestId = base.RequestId;
            _addCommand.updateCallbackId = updateCallback;
            _addCommand.removeCallbackId = removeCallback;
            _addCommand.priority = (int)priority;
            _addCommand.flag = flagMap.Data;
            _addCommand.updateDataFilter = (short)updateDataFilter;
            _addCommand.removeDataFilter = (short)removeDataFilter;

            ObjectQueryInfo objectQueryInfo = new ObjectQueryInfo();

            if (queryInfo["query-info"] != null)
                objectQueryInfo.queryInfo = ProtobufHelper.GetQueryInfoObj(queryInfo["query-info"] as Hashtable);


                _addCommand.objectQueryInfo = objectQueryInfo;

        }

        internal override CommandType CommandType
        {
            get { return CommandType.ADD; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            if (_addCommand.sldExpiration != 0 && _addCommand.absExpiration != 0)
                throw new ArgumentException("You cannot set both sliding and absolute expirations on the same cache item");

            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.addCommand = _addCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.ADD;
            base._command.version = "4200";
            
        }
    }
}
