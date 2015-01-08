using System;
using System.IO;
using Alachisoft.NCache.Web.Caching.Util;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common;
using Common = Alachisoft.NCache.Common;
namespace Alachisoft.NCache.Web.Command
{
    internal sealed class GetOptimalServerCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetOptimalServerCommand _getOptimalServerCommand;

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
    

        public GetOptimalServerCommand(string id)
        {
            _currentVersion = Alachisoft.NCache.Common.ProductVersion.ProductInfo;
            base.name = "GetOptimalServerCommand";

            _getOptimalServerCommand = new Alachisoft.NCache.Common.Protobuf.GetOptimalServerCommand();
            _getOptimalServerCommand.cacheId = id;

            _getOptimalServerCommand.isDotnetClient = true;
            _getOptimalServerCommand.requestId = base.RequestId;

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

        protected override void CreateCommand()
        {
       
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getOptimalServerCommand = _getOptimalServerCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_OPTIMAL_SERVER;

        }
    }
}