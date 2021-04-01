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
    internal sealed class GetOptimalServerCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetOptimalServerCommand _getOptimalServerCommand;

        private byte[] _userName;
        private byte[] _password;
        private Alachisoft.NCache.Common.ProductVersion _currentVersion;


        #region Helper Methods
        //This function is needed to parse byte to byte[] because all values in protobuf.ProductVersion are byte[]
        private byte[] ParseToByteArray(byte value)
        {
            byte[] tempArray = new byte[1];
            tempArray[0] = value;
            return tempArray;
        }
        #endregion

        public GetOptimalServerCommand(string id, byte[] userName, byte[] password)
        {
            _currentVersion = Alachisoft.NCache.Common.ProductVersion.ProductInfo;
            base.name = "GetOptimalServerCommand";

            _getOptimalServerCommand = new Alachisoft.NCache.Common.Protobuf.GetOptimalServerCommand();
            _getOptimalServerCommand.cacheId = id;
            _getOptimalServerCommand.binaryUserId = userName;
            _getOptimalServerCommand.binaryPassword = password;

            _getOptimalServerCommand.isDotnetClient = true;
            _getOptimalServerCommand.requestId = base.RequestId;
            //Protobuf. Product Version is assigned values 

            if (_getOptimalServerCommand.productVersion == null)
                _getOptimalServerCommand.productVersion = new Common.Protobuf.ProductVersion();

            _getOptimalServerCommand.productVersion.AddiotionalData = _currentVersion.AdditionalData;
            _getOptimalServerCommand.productVersion.EditionID = _currentVersion.EditionID;
            _getOptimalServerCommand.productVersion.MajorVersion1 = this.ParseToByteArray(_currentVersion.MajorVersion1);
            _getOptimalServerCommand.productVersion.MajorVersion2 = this.ParseToByteArray(_currentVersion.MajorVersion2);
            _getOptimalServerCommand.productVersion.MinorVersion1 = this.ParseToByteArray(_currentVersion.MinorVersion1);
            _getOptimalServerCommand.productVersion.MinorVersion2 = this.ParseToByteArray(_currentVersion.MinorVersion2);
            _getOptimalServerCommand.productVersion.ProductName = _currentVersion.ProductName;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_OPTIMAL_SERVER; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        internal override bool IsKeyBased { get { return false; } }

        internal override bool SupportsAacknowledgement
        {
            get
            {
                return false;
            }
        }


        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getOptimalServerCommand = _getOptimalServerCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_OPTIMAL_SERVER;
        }

        protected override void SerializeCommand()
        {
            using (MemoryStream stream = new MemoryStream())
            {
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

                stream.Seek(20, SeekOrigin.Begin);
                stream.Write(acknowledgementBuffer, 0, acknowledgementBuffer.Length);
                _commandBytes = stream.GetBuffer();
            }
            return _commandBytes;
        }
    }
}
