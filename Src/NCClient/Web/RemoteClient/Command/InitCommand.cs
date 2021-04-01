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
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Client
{
    internal sealed class InitCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.InitCommand _initCommand;
        private byte[] _userName;
        private byte[] _password;
        private string _clientID;
        private string _licenceCode;
        private string _clientIp;
       

        //+Moiz: LiveUpgrade task 2-12-2013
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
     

       
        public InitCommand(string clientid, string id, string clientLocalIP, IPAddress requestedServerAddress, Runtime.Caching.ClientInfo clientInfo, int operationTimeout)
        {
            _initCommand = new Alachisoft.NCache.Common.Protobuf.InitCommand();

          
            _currentVersion = Alachisoft.NCache.Common.ProductVersion.ProductInfo;
            _initCommand.cacheId = id;
            _initCommand.clientId = clientid;
            _initCommand.isDotnetClient = true;
            _initCommand.requestId = base.RequestId;
            _initCommand.clientInfo = new ClientInfo();
            _initCommand.clientInfo.machineName = clientInfo.MachineName;
            _initCommand.clientInfo.processId = clientInfo.ProcessID;
            _initCommand.clientInfo.appName = clientInfo.AppName;
            _initCommand.clientVersion = clientInfo.ClientVersion;
            _initCommand.clientInfo.clientId = clientInfo.ClientID;
          

            //Protobuf. Product Version is assigned values 
            if (_initCommand.productVersion == null)
                _initCommand.productVersion = new Common.Protobuf.ProductVersion();

            _initCommand.productVersion.AddiotionalData = _currentVersion.AdditionalData;
            _initCommand.productVersion.EditionID = _currentVersion.EditionID;
            _initCommand.productVersion.MajorVersion1 = this.ParseToByteArray(_currentVersion.MajorVersion1);
            _initCommand.productVersion.MajorVersion2 = this.ParseToByteArray(_currentVersion.MajorVersion2);
            _initCommand.productVersion.MinorVersion1 = this.ParseToByteArray(_currentVersion.MinorVersion1);
            _initCommand.productVersion.MinorVersion2 = this.ParseToByteArray(_currentVersion.MinorVersion2);
            _initCommand.productVersion.ProductName = _currentVersion.ProductName;
            
            _initCommand.operationTimeout = operationTimeout;
            _initCommand.clientVersion = 5000;
            _initCommand.OperationSystem = GetOSPlatform().ToString();

        }

        internal override CommandType CommandType
        {
            get { return CommandType.INIT; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        internal override bool SupportsAacknowledgement
        {
            get
            {
                return false;
            }
        }

        internal override bool IsKeyBased { get { return false; } }


        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.initCommand = _initCommand;

            base._command.commandVersion = 2;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.INIT;
        }

        protected override void SerializeCommand()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ///Write discarding buffer that socketserver reads
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

        private OSInfo GetOSPlatform()
        {
            OSInfo currentOS = OSInfo.Windows;
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                currentOS = OSInfo.Linux;
#endif
            return currentOS;
        }
    }
}
