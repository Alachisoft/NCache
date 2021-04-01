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
using Alachisoft.NCache.Client.Caching;

using System;

namespace Alachisoft.NCache.Client
{
    class ItemUpdatedTask : AsyncProcessor.IAsyncTask
    {
        private string _key;
        private bool _notifyAsync;
        private Broker _parent;
        private UsageStats _stats;
        private EventCacheItem _item;
        private EventCacheItem _oldItem;
        private BitSet _flag;

        public ItemUpdatedTask(Broker parent, string key, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem, BitSet flag)
        {
            this._parent = parent;
            this._key = key;
            this._notifyAsync = notifyAsync;
            this._item = item;
            this._oldItem = oldItem;
            this._flag = flag;
        }

        public void Process()
        {
            try
            {
                if (_parent != null)
                {
                    _stats = new UsageStats();
                    _stats.BeginSample();
                    _parent._cache.EventListener.OnItemUpdated(_key, _notifyAsync, _item, _oldItem, _flag);

                    _stats.EndSample();
                    _parent._perfStatsColl.IncrementAvgEventProcessingSample(_stats.Current);
                }
            }
            catch (Exception ex)
            {
                if (_parent.Logger.IsErrorLogsEnabled) _parent.Logger.NCacheLog.Error("Item Updated Task.Process", ex.ToString());
            }
        }
    }
}
