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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Web.Caching;
using System.IO;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Web.Caching.Util;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class DeleteCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.DeleteCommand _deleteCommand;

        private byte _flag;
        private short _onDsItemRemovedId;
        private object _lockId;
       private LockAccessType _accessType;



        public DeleteCommand(string key, BitSet flagMap, object lockId, LockAccessType accessType)
        {
            base.name = "DeleteCommand";
            base.key = key;

            _deleteCommand = new Alachisoft.NCache.Common.Protobuf.DeleteCommand();
            _deleteCommand.key = key;

            flagMap.SetBit(BitSetConstants.LockedItem);
            _deleteCommand.flag = flagMap.Data;
            if (lockId != null) _deleteCommand.lockId = lockId.ToString();
            _deleteCommand.lockAccessType = (int)accessType;
            _deleteCommand.requestId = base.RequestId;
        }


        internal override CommandType CommandType
        {
            get { return CommandType.DELETE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.deleteCommand = _deleteCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.DELETE;

           
        }
    }
}
