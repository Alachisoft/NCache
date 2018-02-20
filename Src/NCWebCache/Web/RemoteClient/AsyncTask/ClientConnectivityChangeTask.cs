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

using System;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Communication;

namespace Alachisoft.NCache.Web.AsyncTask
{
    class ClientConnectivityChangeTask : AsyncProcessor.IAsyncTask
    {
        private string _cacheId;
        private Runtime.Caching.ClientInfo _clientInfo;
        private ConnectivityStatus _status;
        Broker _parent;
        UsageStats _stats;

        public ClientConnectivityChangeTask(Broker parent, string cacheId, ClientInfo clientInfo,
            ConnectivityStatus status)
        {
            cacheId = _cacheId;
            _clientInfo = clientInfo;
            _status = status;
            _parent = parent;
        }

        public void Process()
        {
            try
            {
                if (_parent != null)
                {
                    _stats = new UsageStats();
                    _stats.BeginSample();
                    _parent._cache.EventListener.OnClientConnectivityChange(_cacheId, _clientInfo, _status);

                    _stats.EndSample();
                    _parent._perfStatsColl2.IncrementAvgEventProcessingSample(_stats.Current);
                }
            }
            catch (Exception ex)
            {
                if (_parent.Logger.IsErrorLogsEnabled)
                    _parent.Logger.NCacheLog.Error("Client Connectivity Change Task.Process", ex.ToString());
            }
        }
    }
}