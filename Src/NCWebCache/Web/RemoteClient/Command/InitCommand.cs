// Copyright (c) 2017 Alachisoft
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

using System.IO;
using Alachisoft.NCache.Web.Caching.Util;
using Alachisoft.NCache.Web.Communication;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class InitCommand : CommandBase
	{
        private Common.Protobuf.InitCommand _initCommand;
        private string _clientID;
        private string _licenceCode;
        private string _clientIp;
        private Common.ProductVersion _currentVersion;


        public InitCommand(string clientid, string id)
        {
            _initCommand = new Common.Protobuf.InitCommand();
            name = "InitCommand";
            _currentVersion = Common.ProductVersion.ProductInfo;
            _initCommand.cacheId = id;
            _initCommand.clientId = clientid;
            _initCommand.isDotnetClient = true;
            _initCommand.requestId = RequestId;

            if (_initCommand.productVersion == null)
                _initCommand.productVersion = new Common.Protobuf.ProductVersion();
            _initCommand.productVersion.AddiotionalData = _currentVersion.AdditionalData;
            _initCommand.productVersion.EditionID = _currentVersion.EditionID;
            _initCommand.productVersion.MajorVersion1 = HelperFxn.ParseToByteArray(_currentVersion.MajorVersion1);
            _initCommand.productVersion.MajorVersion2 = HelperFxn.ParseToByteArray(_currentVersion.MajorVersion2);
            _initCommand.productVersion.MinorVersion1 = HelperFxn.ParseToByteArray(_currentVersion.MinorVersion1);
            _initCommand.productVersion.MinorVersion2 = HelperFxn.ParseToByteArray(_currentVersion.MinorVersion2);
            _initCommand.productVersion.ProductName = _currentVersion.ProductName;


            _initCommand.clientVersion = 4610;


        }

        internal override CommandType CommandType
        {
            get { return CommandType.INIT; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        protected override void CreateCommand()
        {
            _command = new Common.Protobuf.Command();
            _command.requestID = RequestId;
            _command.initCommand = _initCommand;
            _command.type = Common.Protobuf.Command.Type.INIT;

        }

        public override byte[] ToByte()
        {
            if (_commandBytes == null)
            {
                CreateCommand();
                SerializeCommand();
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

                ProtoBuf.Serializer.Serialize(stream, _command);
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
