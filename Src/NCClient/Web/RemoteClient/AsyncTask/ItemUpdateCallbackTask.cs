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
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Client.Caching;

using System;

namespace Alachisoft.NCache.Client
{
    class ItemUpdateCallbackTask : AsyncProcessor.IAsyncTask
    {
        private string _key;
        private bool _notifyAsync;
        private short _callBackId;
        private Broker _parent;
        private UsageStats _stats;
        private EventCacheItem _item;
        private EventCacheItem _oldItem;
        private BitSet _flag;
        private EventDataFilter _dataFilter = EventDataFilter.None;

        public ItemUpdateCallbackTask(Broker parent, string key, short callBackId, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem, BitSet flag, EventDataFilter dataFilter)
        {
            this._parent = parent;
            this._key = key;
            this._callBackId = callBackId;
            this._notifyAsync = notifyAsync;
            this._item = item;
            this._oldItem = oldItem;
            this._flag = flag;
            this._dataFilter = dataFilter;
        }

        public void Process()
        {
            try
            {
                if (_parent != null && _parent._cache.AsyncEventHandler != null)
                {
                    _stats = new UsageStats();
                    _stats.BeginSample();
                    _parent._cache.EventListener.OnCustomUpdateCallback(_callBackId, _key, _notifyAsync, _item, _oldItem, _flag, _dataFilter);

                    _stats.EndSample();
                    _parent._perfStatsColl.IncrementAvgEventProcessingSample(_stats.Current);
                }
            }
            catch (Exception ex)
            {
                if (_parent.Logger.IsErrorLogsEnabled) _parent.Logger.NCacheLog.Error("Item Updated Callback Task.Process", ex.ToString());
            }
        }
    }
}
