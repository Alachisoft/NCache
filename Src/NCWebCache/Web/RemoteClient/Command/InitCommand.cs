// Copyright (c) 2015 Alachisoft
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
using System.IO;
using Alachisoft.NCache.Web.Caching.Util;

using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Protobuf.Util;
using Common = Alachisoft.NCache.Common;
namespace Alachisoft.NCache.Web.Command
{
    internal sealed class InitCommand : CommandBase
	{
        private Alachisoft.NCache.Common.Protobuf.InitCommand _initCommand;
        private byte[] _userName;
        private byte[] _password;
        private string _clientID;
        private string _licenceCode;
   
        private string _clientIp;

      
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


        public InitCommand(string clientid, string id, string clientLocalIP)
        {
             _initCommand = new Alachisoft.NCache.Common.Protobuf.InitCommand();
             base.name = "InitCommand";
            _currentVersion = Alachisoft.NCache.Common.ProductVersion.ProductInfo;
            _initCommand.cacheId = id;
            _initCommand.clientId = clientid;
            _initCommand.requestId = base.RequestId;
            
           
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
                               
            // from NCache 4.1 SP2 private patch 2 ownward client version will also be sent
            //Client version has following format :
            //[2 digits for major version][1 digit for service paack][1 digit for private patch]
            //e.g. 4122 means 4.1 major , 2 for service pack 2 and last 4 for private patch 4
            
            _initCommand.clientVersion = 4200; //changed for 4.2 
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
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.initCommand = _initCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.INIT;

        }
	}
}
