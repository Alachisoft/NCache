//  Copyright (c) 2018 Alachisoft
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

using System;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class ContainsBulkCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.ContainsBulkCommand _containsBulkCommand;
        private int _methodOverload;

        internal ContainsBulkCommand(string[] keys, int methodOverload)
        {
            base.name = "ContainsBulkCommand";

            _containsBulkCommand = new Alachisoft.NCache.Common.Protobuf.ContainsBulkCommand();
            _containsBulkCommand.requestId = base.RequestId;
            _containsBulkCommand.keys.AddRange(keys);
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.CONTAINS_BULK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkRead; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Extended.Serializer.Serialize(stream, _containsBulkCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.CONTAINS_BULK;
        }

        protected override void CreateCommand()
        {
            //base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            //base._command.requestID = base.RequestId;
            //base._command.containsBulkCommand = _containsBulkCommand;
            //base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.CONTAINS_BULK;
            //base._command.MethodOverload = _methodOverload;
            _containsBulkCommand.requestId = base.RequestId;
            _containsBulkCommand.MethodOverload = _methodOverload;
        }
    }
}