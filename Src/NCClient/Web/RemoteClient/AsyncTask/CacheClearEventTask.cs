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

using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Threading;

using System;

namespace Alachisoft.NCache.Client
{
    class CacheClearEventTask : AsyncProcessor.IAsyncTask
    {
        private bool _notifyAsync;
        private Broker _parent;
        private UsageStats _stats;

        public CacheClearEventTask(Broker parent, bool notifyAsync)
        {
            this._parent = parent;
            this._notifyAsync = notifyAsync;
        }

        public void Process()
        {
            try
            {
                if (_parent != null)
                {
                    _stats = new UsageStats();
                    _stats.BeginSample();
                   

                    _stats.EndSample();
                    _parent._perfStatsColl.IncrementAvgEventProcessingSample(_stats.Current);
                }
            }
            catch (Exception ex)
            {
                if (_parent.Logger.IsErrorLogsEnabled) _parent.Logger.NCacheLog.Error("Cache Cleared Task.Process", ex.ToString());
            }
        }
    }
}
