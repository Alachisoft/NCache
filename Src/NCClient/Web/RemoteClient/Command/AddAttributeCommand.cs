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

using System;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class AddAttributeCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.AddAttributeCommand _addAttributeCommand;
        int _methodOverload = 0;

        internal AddAttributeCommand(string key, DateTime absoluteExpiration , int methodOverload)
        {
            base.name = "AddAttributeCommand";
            base.key = key;

            _methodOverload = methodOverload;
            _addAttributeCommand = new Alachisoft.NCache.Common.Protobuf.AddAttributeCommand();
            if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                _addAttributeCommand.absExpiration = absoluteExpiration.ToUniversalTime().Ticks;

            _addAttributeCommand.key = key;
            _addAttributeCommand.requestId = base.RequestId;
        
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _addAttributeCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.ADD_ATTRIBUTE;
        }
        
        protected override void CreateCommand()
        {

            _addAttributeCommand.requestId = base.RequestId;
            _addAttributeCommand.MethodOverload = _methodOverload;

        }

        internal override CommandType CommandType
        {
            get { return CommandType.ADD_ATTRIBUTE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }
    }
}
