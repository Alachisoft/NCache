
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
using Alachisoft.NCache.MapReduce.Notifications;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.MapReduce
{
    public abstract class TaskBase : Task, IDisposable
    {
        private string _taskId = "";
        private TaskType type = TaskType.MapReduce;
        private ClusteredArrayList callbackListener = new ClusteredArrayList(2);

        public abstract void Dispose();

        public string TaskId
        {
            get { return _taskId; }
            set { _taskId = value; }
        }

        public TaskType TaskType
        {
            get { return type; }
            set { type = value; }
        }
        public ClusteredArrayList CallbackListeners
        {
            get { return callbackListener; }
            set { callbackListener = value; }
        }

        public void AddTaskCallbackInfo(TaskCallbackInfo taskCallbackInfo)
        {
            if (CallbackListeners != null && !CallbackListeners.Contains(taskCallbackInfo))
                CallbackListeners.Add(taskCallbackInfo);
        }

        public void RemoveTaskCallbackInfo(TaskCallbackInfo taskCallbackInfo)
        {
            if (CallbackListeners != null && CallbackListeners.Contains(taskCallbackInfo))
                CallbackListeners.Remove(taskCallbackInfo);
        }

        
    }
}
