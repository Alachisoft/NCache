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
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.AsyncTask
{
    class CQCallBackTask : AsyncProcessor.IAsyncTask
    {
        private string _key;
        private Broker _parent;
        private bool _notifyAsync;
        private string _queryId;
        private QueryChangeType _changeType;
        private UsageStats _stats;
        private EventCacheItem _oldItem;
        private EventCacheItem _item;
        private BitSet _flag;
        private EventDataFilter _datafilter;

        public CQCallBackTask(Broker parent, string key, string queryId, QueryChangeType changeType, bool notifyAsync,
            EventCacheItem item, EventCacheItem oldItem, BitSet flag, EventDataFilter datafilter)
        {
            this._key = key;
            this._parent = parent;
            this._notifyAsync = notifyAsync;
            this._queryId = queryId;
            this._changeType = changeType;
            this._item = item;
            this._oldItem = oldItem;
            this._flag = flag;
            this._datafilter = datafilter;
        }

        public void Process()
        {
            try
            {
                if (_parent != null)
                {
                    _stats = new UsageStats();
                    _stats.BeginSample();

                    _parent._cache.EventListener.OnActiveQueryChanged(_queryId, _changeType, _key, _notifyAsync, _item,
                        _oldItem, _flag, _datafilter);

                    _stats.EndSample();
                    _parent._perfStatsColl2.IncrementAvgEventProcessingSample(_stats.Current);
                }
            }
            catch (Exception ex)
            {
                if (_parent.Logger.IsErrorLogsEnabled)
                    _parent.Logger.NCacheLog.Error("CQ Callback Task.Process", ex.ToString());
            }
        }
    }
}