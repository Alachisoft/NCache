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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Runtime.MapReduce
{
    /// <summary>
    ///  Returns the status of running task. 
    /// </summary>
    [Serializable]
    public class TaskStatus
    {
        /// <summary>
        /// Types of running task status.
        /// </summary>
        [Serializable]
        public enum Status
        {
            /// <summary>
            /// Task is in waiting queue.
            /// </summary>
            Waiting = 1,
            /// <summary>
            /// Task is being executed.
            /// </summary>
            InProgress = 2,
            /// <summary>
            /// Task has been completed.
            /// </summary>
            Completed = 3,
            /// <summary>
            /// Task has been cancelled.
            /// </summary>
            Cancelled = 4,
            /// <summary>
            /// Exception has been thrown or time out has been reached. 
            /// </summary>
            Failed = 5
        }

        private Status _status = Status.Completed;

        private string _reason = null;
        /// <summary>
        ///Sets/Gets failure reason of the task. 
        /// </summary>
        public string FailureReason
        {
            get { return _reason; }
            set { _reason = value; }
        }
        /// <summary>
        /// Return task progress in form of status. 
        /// </summary>
        public Status Progress
        {
            get { return _status; }
            set { _status = value; }
        }
        /// <summary>
        /// Return task current status.
        /// </summary>
        /// <param name="status">status of the task</param>
        public TaskStatus(Status status)
        {
            this._status = status;
        }


    }
}
