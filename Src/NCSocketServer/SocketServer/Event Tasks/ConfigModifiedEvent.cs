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
using System;
using System.Collections;
using Alachisoft.NCache.Caching.Util;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    internal sealed class ConfigModifiedEvent : IEventTask
    {
        private HotConfig _config;
        private string _cacheId;
        private string _clientid;

        internal ConfigModifiedEvent(HotConfig config, string cacheId, string clientid)
        {
            _config = config;
            _cacheId = cacheId;
            _clientid = clientid;
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientid];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.ConfigModifiedEventResponse configModified = new Alachisoft.NCache.Common.Protobuf.ConfigModifiedEventResponse();

                configModified.hotConfig = _config.ToString();

                response.configModified = configModified;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.CONFIG_MODIFIED_EVENT;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.CONFIG_MODIFIED_EVENT);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }
        }
    }
}
