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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Serialization.Formatters;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.CallbackTasks
{
    internal sealed class CompactTypeRegisterCallback : ICallbackTask
    {
        private Hashtable _types = null;
        
        internal CompactTypeRegisterCallback(Hashtable types)
        {
            this._types = types;           
        }
        #region ICallbackTask Members

        public void Process()
        {
            ClientManager clientManager = null;
            IDictionaryEnumerator ide = ConnectionManager.ConnectionTable.GetEnumerator();
            while (ide.MoveNext())
            {
                lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[ide.Key.ToString()];
                if (clientManager != null)
                {
                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.CompactTypeRegisterEvent compactTypeRegisterCallback = new Alachisoft.NCache.Common.Protobuf.CompactTypeRegisterEvent();
                    compactTypeRegisterCallback.compactTypes = CompactBinaryFormatter.ToByteBuffer(_types, null);

                    response.compactTypeRegisterEvent = compactTypeRegisterCallback;
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.COMPACT_TYPE_REGISTER_EVENT;

                    IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.COMPACT_TYPE_REGISTER_EVENT);

                    ConnectionManager.AssureSend(clientManager, serializedResponse, false);                    
                }
            }            
        }

        #endregion
    }
}
