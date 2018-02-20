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

using System.Text;
using System.Collections;
using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.SocketServer.CallbackTasks
{
    class DataSourceUpdatedCallbackTask : ICallbackTask
    {
        private short _id;
        private object _result;
        private OpCode _opCode;
        private string _clientId;

        public DataSourceUpdatedCallbackTask(short id, object result, OpCode opCode, string clientId)
        {
            _id = id;
            _result = result;
            _opCode = opCode;
            _clientId = clientId;
        }

        public void Process()
        {
            StringBuilder keyPackage = new StringBuilder();
            keyPackage.AppendFormat("DSUPDATECALLBACK \"{0}\"{1}\"{2}\"", _id, (int)_opCode, ((Hashtable)_result).Count);

            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable)
                clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];

            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();

                response.dsUpdateCallbackRespose = Alachisoft.NCache.SocketServer.Util.EventHelper.GetDSUPdateCallbackResponse(_id, _opCode,_result);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.DS_UPDATE_CALLBACK;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                ConnectionManager.AssureSend(clientManager, serializedResponse, Common.Enum.Priority.Low);
            }
        }
    }
}
