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
using Alachisoft.NCache.Common.Threading;
using System.Collections;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.Web.AsyncTask
{
    class DSUpdateEventTask : AsyncProcessor.IAsyncTask
    {
        private bool _notifyAsync;
        private short _callBackId;
        private Hashtable _result;
        private OpCode _opCode;
        private Broker _parent;
        private UsageStats _stats;

        public DSUpdateEventTask(Broker parent, short callBackId, Hashtable result, OpCode opCode, bool notifyAsync)
        {
            this._parent = parent;
            this._callBackId = callBackId;
            this._result = result;
            this._opCode = opCode;
            this._notifyAsync = notifyAsync;
        }

        public void Process()
        {
            try
            {
                if (_parent != null && _parent._cache.AsyncEventHandler != null)
                {
                    _stats = new UsageStats();
                    _stats.BeginSample();

                    _parent._cache.AsyncEventHandler.OnDataSourceUpdated(_callBackId, _result, _opCode, _notifyAsync);

                    _stats.EndSample();
                    _parent._perfStatsColl2.IncrementAvgEventProcessingSample(_stats.Current);
                }
            }
            catch (Exception ex)
            {
                if (_parent.Logger.IsErrorLogsEnabled)
                    _parent.Logger.NCacheLog.Error("DS Update event Task.Process", ex.ToString());
            }
        }
    }
}