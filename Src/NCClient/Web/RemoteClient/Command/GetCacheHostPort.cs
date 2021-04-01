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
    internal sealed class GetCacheHostPort : CommandBase
    {
        Common.Protobuf.GetCacheManagementPortCommand _getCacheManagementPort;

        public GetCacheHostPort(string id, byte[] useName, byte[] password)
        {
            base.name = "GetCacheManagementPort";
            _getCacheManagementPort = new Alachisoft.NCache.Common.Protobuf.GetCacheManagementPortCommand();
            _getCacheManagementPort.cacheId = id;
            _getCacheManagementPort.binaryUserId = useName;
            _getCacheManagementPort.binaryPassword = password;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_CACHE_MANAGEMENT_PORT; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getCacheManagementPortCommand = _getCacheManagementPort;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_CACHE_MANAGEMENT_PORT;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.intendedRecipient = base.IntendedRecipient;
        }

        internal override bool SupportsAacknowledgement
        {
            get
            {
                return false;
            }
        }

        protected override void SerializeCommand()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                //Write discarding buffer that socketserver reads
                byte[] discardingBuffer = new byte[20];
                stream.Write(discardingBuffer, 0, discardingBuffer.Length);
                byte[] acknowledgementBuffer = (SupportsAacknowledgement && inquiryEnabled) ? new byte[20] : new byte[0];
                stream.Write(acknowledgementBuffer, 0, acknowledgementBuffer.Length);
                byte[] size = new byte[Connection.CmdSizeHolderBytesCount];
                stream.Write(size, 0, size.Length);
                ProtoBuf.Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Command>(stream, this._command);
                int messageLen = (int)stream.Length - (size.Length + discardingBuffer.Length + acknowledgementBuffer.Length);

                size = HelperFxn.ToBytes(messageLen.ToString());
                stream.Position = discardingBuffer.Length + acknowledgementBuffer.Length;
                stream.Write(size, 0, size.Length);

                this._commandBytes = stream.ToArray();
                stream.Close();
            }
        }

        internal override byte[] ToByte(long acknowledgement, bool inquiryEnabledOnConnection)
        {
            if (_commandBytes == null || inquiryEnabled != inquiryEnabledOnConnection)
            {
                inquiryEnabled = inquiryEnabledOnConnection;
                this.CreateCommand();
                _command.commandID = _commandID;
                this.SerializeCommand();
            }

            if (SupportsAacknowledgement && inquiryEnabled)
            {
                byte[] acknowledgementBuffer = HelperFxn.ToBytes(acknowledgement.ToString());
                MemoryStream stream = new MemoryStream(_commandBytes, 0, _commandBytes.Length, true, true);
                //That's the area after the discarding buffer.
                stream.Seek(20, SeekOrigin.Begin);
                stream.Write(acknowledgementBuffer, 0, acknowledgementBuffer.Length);
                _commandBytes = stream.GetBuffer();
            }
            return _commandBytes;
        }
    }
}
