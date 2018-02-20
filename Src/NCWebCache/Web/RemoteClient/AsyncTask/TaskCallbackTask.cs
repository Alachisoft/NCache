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
using Alachisoft.NCache.Web.Communication;

namespace Alachisoft.NCache.Web.AsyncTask
{
    internal class TaskCallbackTask : AsyncProcessor.IAsyncTask
    {
        private Broker broker;
        private string taskId;
        private short taskStatus;
        private short callbackId;
        private string taskFailureReason;

        public TaskCallbackTask(Broker parent, string taskid, short taskstatus, string taskFailureReason,
            short callbackid)
        {
            this.broker = parent;
            this.taskId = taskid;
            this.taskStatus = taskstatus;
            this.callbackId = callbackid;
            this.taskFailureReason = taskFailureReason;
        }

        public void Process()
        {
            try
            {
                if (broker != null)
                {
                    broker._cache.EventListener.OnTaskCompletedCallback(taskId, taskStatus, taskFailureReason,
                        callbackId);
                }
            }
            catch (Exception ex)
            {
                if (broker.Logger.IsErrorLogsEnabled)
                    broker.Logger.NCacheLog.Error("CQ Callback Task.Process", ex.ToString());
            }
        }
    }
}