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

using System.Collections;
using Alachisoft.NCache.Runtime.MapReduce;

namespace Alachisoft.NCache.Web.MapReduce
{
    /// <summary>
    /// Returns the blocking call of Map Reduce Task result form server.
    /// </summary>
    public class TaskResult : ITaskResult
    {
        private ITaskManagement _cachemgmt = null;

        private TaskCompletionStatus tStatus = TaskCompletionStatus.Success;
        private string taskId;
        private short uniqueId;
        IDictionaryEnumerator taskEnumResult = null;
        private string failureReason = null;

        public TaskResult(TaskCompletionStatus status, string taskId, short uniqueId, string reason)
        {
            this.tStatus = status;
            this.taskId = taskId;
            this.uniqueId = uniqueId;
            this.failureReason = reason;
        }

        /// <summary>
        /// Obtain the result in form of dictionary.
        /// </summary>
        /// <returns>dictionary containing the MapReduce result</returns>
        public IDictionaryEnumerator GetEnumerator()
        {
            if (taskEnumResult != null)
                return taskEnumResult;
            else
            {
                taskEnumResult = _cachemgmt.GetTaskEnumerator(this.taskId, this.uniqueId);
                return taskEnumResult;
            }
        }

        /// <summary>
        ///  Return status of Task which can be failure, success or cancelled.
        /// </summary>
        public TaskCompletionStatus TaskStatus
        {
            get { return taskEnumResult != null ? TaskCompletionStatus.Success : tStatus; }
        }
        
        public ITaskManagement TaskManagement
        {
            set { this._cachemgmt = value; }
        }

        /// <summary>
        /// Returns reason behind the failure of task.
        /// </summary>
        public string TaskFailureReason
        {
            get { return this.failureReason; }
        }
    }
}