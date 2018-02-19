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
using System.Collections;
using System.Text;
using System.IO;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Web.Caching.Util;

namespace Alachisoft.NCache.Web.Command
{
    internal class GetServerMappingCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetServerMappingCommand _getServerMappingCommand;

        internal GetServerMappingCommand()
        {
            base.name = "GetServerMappingCommand";

            _getServerMappingCommand = new Alachisoft.NCache.Common.Protobuf.GetServerMappingCommand();
            _getServerMappingCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_SERVER_MAPPING; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getServerMappingCommand = _getServerMappingCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_SERVER_MAPPING;
        }

        public override byte[] ToByte()
        {
            if (_commandBytes == null)
            {
                this.CreateCommand();
                this.SerializeCommand();
            }
            return _commandBytes;
        }

        public override void SerializeCommand()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ///Write discarding buffer that socketserver reads
                byte[] discardingBuffer = new byte[20];
                stream.Write(discardingBuffer, 0, discardingBuffer.Length);

                byte[] size = new byte[Connection.CmdSizeHolderBytesCount];
                stream.Write(size, 0, size.Length);

                ProtoBuf.Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Command>(stream, this._command);
                int messageLen = (int)stream.Length - (size.Length + discardingBuffer.Length);

                size = HelperFxn.ToBytes(messageLen.ToString());
                stream.Position = discardingBuffer.Length;
                stream.Write(size, 0, size.Length);

                this._commandBytes = stream.ToArray();
                stream.Close();
            }
        }
    }
}
