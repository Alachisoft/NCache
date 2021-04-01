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

using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class RaiseCustomEventCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.RaiseCustomEventCommand _raiseCustomeEventCommand;

        private int _methodOverload;
        public RaiseCustomEventCommand(object notifId, object data, int methodOverload)
        {
            base.name = "RaiseCustomEventCommand";
            _raiseCustomeEventCommand = new Alachisoft.NCache.Common.Protobuf.RaiseCustomEventCommand();
            _raiseCustomeEventCommand.notifIf = (byte[])notifId;
            _raiseCustomeEventCommand.data = (byte[])data;
            _raiseCustomeEventCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.RAISE_CUSTOM_EVENT; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _raiseCustomeEventCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.RAISE_CUSTOM_EVENT;
        }

        protected override void CreateCommand()
        {
            _raiseCustomeEventCommand.requestId = base.RequestId;
            _raiseCustomeEventCommand.MethodOverload = _methodOverload;
        }
    }
}
