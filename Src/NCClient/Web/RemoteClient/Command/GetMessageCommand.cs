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
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class GetMessageCommand : CommandBase
    {
        private readonly Common.Protobuf.GetMessageCommand _getMessgaeCommand;

        public GetMessageCommand(BitSet flagMap)
        {
            name = "GetMessageCommand";
            _getMessgaeCommand = new Common.Protobuf.GetMessageCommand();
            _getMessgaeCommand.flag = flagMap.Data;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GETMESSAGE; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _getMessgaeCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.GET_MESSAGE;
        }

        protected override void CreateCommand()
        {
         
            _getMessgaeCommand.requestId = RequestId;
            _getMessgaeCommand.clientLastViewId = ClientLastViewId;
            _getMessgaeCommand.intendedRecipient = IntendedRecipient;
            _getMessgaeCommand.version = "4200";
            _getMessgaeCommand.commandVersion = 1; // NCache 4.1 Onwards
        }
    }
}
