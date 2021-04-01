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
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    /// <summary>
    /// This event is fired when cache is stoped
    /// </summary>
	internal sealed class CacheStoppedEvent : IEventTask
	{
		private string _cacheId;
        private string _clientId;

		internal CacheStoppedEvent(string cacheId,string clientId)
		{
			_cacheId = cacheId;
            _clientId = clientId;
		}

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                response.cacheStopped = Alachisoft.NCache.SocketServer.Util.EventHelper.GetCacheStoppedEventResponse(_cacheId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.CACHE_STOPPED_EVENT;
                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.CACHE_STOPPED_EVENT);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }
        }
    }
}
