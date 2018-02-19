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

using Alachisoft.NCache.Web.Caching.Util;

namespace Alachisoft.NCache.Web.Command
{
    class GetRunningServersCommand : CommandBase
    {
        private Common.Protobuf.GetRunningServersCommand _getRunningServersCommand;
       
        private byte[] _userName;
        private byte[] _password;
       
        private Alachisoft.NCache.Common.ProductVersion _currentVersion;
       
        public GetRunningServersCommand(string id)
        {
            base.name = "GetRunningServersCommand";
            _currentVersion = Alachisoft.NCache.Common.ProductVersion.ProductInfo;
            _getRunningServersCommand = new Alachisoft.NCache.Common.Protobuf.GetRunningServersCommand();
            _getRunningServersCommand.cacheId = id;

            _getRunningServersCommand.isDotnetClient = true;
            _getRunningServersCommand.requestId = base.RequestId;

        
            if (_getRunningServersCommand.productVersion == null)
                _getRunningServersCommand.productVersion = new Common.Protobuf.ProductVersion();

                _getRunningServersCommand.productVersion.AddiotionalData = _currentVersion.AdditionalData;
                _getRunningServersCommand.productVersion.EditionID = _currentVersion.EditionID;
                _getRunningServersCommand.productVersion.MajorVersion1 = HelperFxn.ParseToByteArray(_currentVersion.MajorVersion1);
                _getRunningServersCommand.productVersion.MajorVersion2 = HelperFxn.ParseToByteArray(_currentVersion.MajorVersion2);
                _getRunningServersCommand.productVersion.MinorVersion1 = HelperFxn.ParseToByteArray(_currentVersion.MinorVersion1);
                _getRunningServersCommand.productVersion.MinorVersion2 = HelperFxn.ParseToByteArray(_currentVersion.MinorVersion2);
                _getRunningServersCommand.productVersion.ProductName = _currentVersion.ProductName;
            
        
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_RUNNING_SERVERS; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getRunningServersCommand = _getRunningServersCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_RUNNING_SERVERS;
        }
    }
}
