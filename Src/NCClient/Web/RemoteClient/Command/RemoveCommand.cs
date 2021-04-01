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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Locking;
using System;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class RemoveCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.RemoveCommand _removeCommand;

        private byte _flag;
        private short _itemRemoved;
        private short _onDsItemRemovedId;
        private object _lockId;
        private ulong _version;
        private string _providername;
        private LockAccessType _accessType;
        private int _methodOverload;

        public RemoveCommand(string key, BitSet flagMap, short itemRemoved, bool isAsync, short onDsItemRemovedId, object lockId, ulong version, LockAccessType accessType, string providerName, int methodOverload)
        {
            base.name = "RemoveCommand";
            base.asyncCallbackSpecified = isAsync && itemRemoved != -1 ? true : false;
            base.isAsync = isAsync;
            base.key = key;

            _removeCommand = new Alachisoft.NCache.Common.Protobuf.RemoveCommand();
            _removeCommand.key = key;
            _removeCommand.isAsync = isAsync;
            _removeCommand.providerName = providerName;

            flagMap.SetBit(BitSetConstants.LockedItem);
            _removeCommand.flag = flagMap.Data;

            _removeCommand.datasourceItemRemovedCallbackId = onDsItemRemovedId;
            if (lockId != null) _removeCommand.lockId = lockId.ToString();
            _removeCommand.lockAccessType = (int)accessType;
            _removeCommand.version = version;
            _removeCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
            _itemRemoved = itemRemoved;
        }

        internal short AsyncItemRemovedOpComplete
        {
            get { return this._itemRemoved; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.REMOVE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _removeCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.REMOVE;
        }

        protected override void CreateCommand()
        {
            //base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            //base._command.requestID = base.RequestId;
            //base._command.removeCommand = _removeCommand;
            //base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE;
            //base._command.MethodOverload = _methodOverload;

            _removeCommand.requestId = base.RequestId;
            _removeCommand.commandID = base.CommandID;
            _removeCommand.MethodOverload = _methodOverload;
        }
    }
}
