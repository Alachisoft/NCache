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

using Alachisoft.NCache.Common;
using System;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class ClearCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.ClearCommand _clearCommand;
        private short _cacheCleared;
        private int _methodOverload;

        internal ClearCommand(short cacheCleared, bool isAsync, BitSet flagMap, short onDsClearedId, string providerName, int methodOverload)
        {
            base.name = "ClearCommand";
            base.asyncCallbackSpecified = isAsync && cacheCleared != -1 ? true : false;
            base.isAsync = isAsync;

            _clearCommand = new Alachisoft.NCache.Common.Protobuf.ClearCommand();
            _clearCommand.datasourceClearedCallbackId = onDsClearedId;
            _clearCommand.isAsync = isAsync;
            _clearCommand.flag = flagMap.Data;
            _clearCommand.requestId = base.RequestId;
            _clearCommand.providerName = providerName;

            _cacheCleared = cacheCleared;
            _methodOverload = methodOverload;
        }

        internal short AsyncCacheClearedOpComplete
        {
            get { return _cacheCleared; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.CLEAR; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _clearCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.CLEAR;
        }

        protected override void CreateCommand()
        {

            _clearCommand.requestId = base.RequestId;
            _clearCommand.MethodOverload = _methodOverload;
        }
    }
}
