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
    class AsyncOperationCompletedEventTask : AsyncProcessor.IAsyncTask
    {
        private bool _notifyAsync;
        private CommandBase _command;
        private string _key;
        private object _asyncOpResult;
        private Broker _parent;
        private UsageStats _stats;

        public AsyncOperationCompletedEventTask(Broker parent, CommandBase command, string key, object asyncOpResult, bool notifyAsync)
        {
            this._parent = parent;
            this._key = key;
            this._command = command;
            this._asyncOpResult = asyncOpResult;
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

                    if (_command is AddCommand)
                    {
                        _parent._cache.AsyncEventHandler.OnAsyncAddCompleted(_key, ((AddCommand)_command).AsycItemAddedOpComplete, _asyncOpResult, _notifyAsync);
                    }
                    else if (_command is InsertCommand)
                    {
                        _parent._cache.AsyncEventHandler.OnAsyncInsertCompleted(_key, ((InsertCommand)_command).AsycItemUpdatedOpComplete, _asyncOpResult, _notifyAsync);
                    }
                    else if (_command is RemoveCommand)
                    {
                        _parent._cache.AsyncEventHandler.OnAsyncRemoveCompleted(_key, ((RemoveCommand)_command).AsyncItemRemovedOpComplete, _asyncOpResult, _notifyAsync);
                    }
                    else if (_command is ClearCommand)
                    {
                        _parent._cache.AsyncEventHandler.OnAsyncClearCompleted(((ClearCommand)_command).AsyncCacheClearedOpComplete, _asyncOpResult, _notifyAsync);
                    }
                }
                _stats.EndSample();
                _parent._perfStatsColl.IncrementAvgEventProcessingSample(_stats.Current);

            }
            catch (Exception ex)
            {
                if (_parent.Logger.IsErrorLogsEnabled) _parent.Logger.NCacheLog.Error("Async Operation completed Event Task.Process", ex.ToString());
            }
        }
    }
}
