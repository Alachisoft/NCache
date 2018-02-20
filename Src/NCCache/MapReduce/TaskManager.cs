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
using System.Collections;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.MapReduce.OutputProviders;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.MapReduce
{
    internal class TaskManager : IDisposable
    {
        private TaskTracker _taskTracker = null;
        private CacheRuntimeContext _context = null;
        private TaskOutputStore taskOutputStore = null;

        public TaskManager(IDictionary properties, CacheRuntimeContext context)
        {
            this._context = context;
            taskOutputStore = new TaskOutputStore();
            this._taskTracker = new TaskTracker(properties, context, taskOutputStore);
        }

        /// <summary>
        /// Gets the TaskTracker instance.
        /// </summary>
        public TaskTracker TaskTracker
        {
            get { return _taskTracker; }
        }

        public void Dispose()
        {
            if (_taskTracker != null)
            {
                this._taskTracker.Dispose();
            }
            if (taskOutputStore != null)
            {
                this.taskOutputStore.Dispose();
            }
        }

        public object TaskOperationReceived(MapReduceOperation operation)
        {
            try
            {
                if (_taskTracker == null)
                    throw new GeneralFailureException("No instance is available to process the task Requests.");

                return this._taskTracker.TaskOperationRecieved(operation);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }
        }

        public void DeadClients(ArrayList clients)
        {
            if (_taskTracker != null)
                this._taskTracker.RemoveDeadClientsTasks(clients);
            if(taskOutputStore != null)
                this.taskOutputStore.RemoveDeadClientsIterators(clients);
        }

        public void DeadClient(string client)
        {
            ArrayList clients = new ArrayList();
            clients.Add(client);
            if (_taskTracker != null)
                this._taskTracker.RemoveDeadClientsTasks(clients);
            if (taskOutputStore != null)
                this.taskOutputStore.RemoveDeadClientsIterators(clients);
        }
    }
}
