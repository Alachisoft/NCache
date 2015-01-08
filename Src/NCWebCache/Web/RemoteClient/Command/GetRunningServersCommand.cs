using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Web.Command;
using Common = Alachisoft.NCache.Common;
namespace Alachisoft.NCache.Web.Command
{
    class GetRunningServersCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetRunningServersCommand _getRunningServersCommand;
       
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
                _getRunningServersCommand.productVersion.MajorVersion1 = this.ParseToByteArray(_currentVersion.MajorVersion1);
                _getRunningServersCommand.productVersion.MajorVersion2 = this.ParseToByteArray(_currentVersion.MajorVersion2);
                _getRunningServersCommand.productVersion.MinorVersion1 = this.ParseToByteArray(_currentVersion.MinorVersion1);
                _getRunningServersCommand.productVersion.MinorVersion2 = this.ParseToByteArray(_currentVersion.MinorVersion2);
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
