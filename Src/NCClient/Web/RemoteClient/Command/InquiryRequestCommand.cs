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
    internal sealed class InquiryRequestCommand : CommandBase
    {
        private Common.Protobuf.InquiryRequestCommand _inquiryRequestCommand;

        internal InquiryRequestCommand(long requestID, long commandID, string destinationIP)
        {
            base.name = "InquiryRequestCommand";
            _inquiryRequestCommand = new Common.Protobuf.InquiryRequestCommand();
            _inquiryRequestCommand.inquiryRequestId = requestID;
            _inquiryRequestCommand.inquiryCommandId = commandID;
            _inquiryRequestCommand.serverIP = destinationIP;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.INQUIRY_REQUEST; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _inquiryRequestCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.INQUIRY_REQUEST;
        }

        protected override void CreateCommand()
        {
            //base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            //base._command.requestID = base.RequestId;
            //base._command.inquiryRequestCommand = _inquiryRequestCommand;
            //base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.INQUIRY_REQUEST;

            _inquiryRequestCommand.requestId = base.RequestId;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }
    }
}