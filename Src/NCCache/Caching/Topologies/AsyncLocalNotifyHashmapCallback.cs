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

using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Threading;
using System.Collections;

namespace Alachisoft.NCache.Caching.Topologies
{
#if !CLIENT

    /// <summary>
    /// Asynchronous hashmap notification dispatcher
    /// </summary>
    internal class AsyncLocalNotifyHashmapCallback : AsyncProcessor.IAsyncTask
    {
        private ICacheEventsListener _listener;
        private NewHashmap _hashMap;
        private bool _updateClientMap;


        public AsyncLocalNotifyHashmapCallback(ICacheEventsListener listener, long lastViewid, Hashtable newmap, ArrayList members, bool updateClientMap)
        {
            this._listener = listener;
            this._hashMap = new NewHashmap(lastViewid, newmap, members);
            this._updateClientMap = updateClientMap;

        }

        #region IAsyncTask Members

        void AsyncProcessor.IAsyncTask.Process()
        {
            _listener.OnHashmapChanged(this._hashMap, this._updateClientMap);
        }

        #endregion
    }

#endif
}