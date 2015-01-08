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
using System.Collections;

using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Web.Util;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;
using System.Text;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    internal sealed class HashmapChangedEvent : IEventTask
    {
        private string _cacheId;
        private string _clientId;
        private NewHashmap _newmap;

        public HashmapChangedEvent(string cacheId, string clientId, NewHashmap newHashmap)
        {
            this._cacheId = cacheId;
            this._clientId = clientId;
            this._newmap = newHashmap;
        }

        public void Process()
        {
            try
            {
                ClientManager clientManager = null;
                lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[this._clientId];
                if (clientManager != null)
                {
                    byte[] table = new byte[0];
                        if (_newmap != null)
                        {
                            if (this._newmap.Buffer == null)
                            {
                                NewHashmap.Serialize(this._newmap, this._cacheId, true);
                            }
                            table = this._newmap.Buffer; 
                        }

                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.HashmapChangedEventResponse hashmapChangedResponse = new Alachisoft.NCache.Common.Protobuf.HashmapChangedEventResponse();

                    hashmapChangedResponse.table = table;

                    response.hashmapChanged = hashmapChangedResponse;
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.HASHMAP_CHANGED_EVENT;

                    byte[] serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                    ConnectionManager.AssureSend(clientManager, serializedResponse, Alachisoft.NCache.Common.Enum.Priority.Critical);

                }
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "HashmapChangedEvent.Process", exc.ToString());
            }
        }
    }
}
