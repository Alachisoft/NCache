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
using System.Collections.Generic;
using System.Collections;
namespace Alachisoft.NCache.SocketServer.EventTask
{
    internal sealed class LoggingInfoModifiedEvent : IEventTask
    {
        private bool _enableErrorLog;
        private bool _enableDetailedLog;
        private string _clientid;

        public LoggingInfoModifiedEvent(bool enableErrorLogs, bool enableDetailedLogs, string clientId)
        {
            this._enableErrorLog = enableErrorLogs;
            this._enableDetailedLog = enableDetailedLogs;
            this._clientid = clientId;
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientid];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.LoggingInfoModifiedEventResponse loggingInfoModified = new Alachisoft.NCache.Common.Protobuf.LoggingInfoModifiedEventResponse();

                loggingInfoModified.enableDetailedErrorsLog = _enableDetailedLog;
                loggingInfoModified.enableErrorsLog = _enableErrorLog;

                response.loggingInfoModified = loggingInfoModified;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.LOGGING_INFO_MODIFIED_EVENT;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.LOGGING_INFO_MODIFIED_EVENT);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }
        }
    }
}