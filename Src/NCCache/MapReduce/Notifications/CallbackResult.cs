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

namespace Alachisoft.NCache.MapReduce.Notifications
{
    public class CallbackResult
    {
        private TaskBase task = null;
        private TaskStatus status = TaskStatus.Success;
        private string failureReason = null;


        public CallbackResult(TaskBase t, TaskStatus status, string failureReason)
        {
            this.task = t;
            this.status = status;
            this.failureReason = failureReason;
        }

        public string FailureReason
        {
            get { return this.failureReason; }
            set { this.failureReason = value; }
        }
        public TaskBase Task
        {
            get { return this.task; }
            set { this.task = value; }
        }
        public TaskStatus Status
        {
            get { return this.status; }
            set { this.status = value; }
        }
    }
}
