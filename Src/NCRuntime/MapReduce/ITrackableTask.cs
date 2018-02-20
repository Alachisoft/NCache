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
using System.Collections;

namespace Alachisoft.NCache.Runtime.MapReduce
{
    /// <summary>
    /// Tracks the execution of MapReduce Task.
    /// <remarks><b> Note:</b>MapReduceCallback() and GetResult() cannot be executed together because GetResult is a blocking call and this combination will throw an exception.</remarks>
    /// </summary>
    public interface ITrackableTask
    {
        /// <summary>
        /// Unique GUID identification of MapReduce task. 
        /// </summary>
        string TaskId { get; }
        /// <summary>
        /// Registered Async callback on completion, failure or cancellation of task. 
        /// </summary>
        event MapReduceCallback OnMapReduceComplete;
        /// <summary>
        /// Cancel the already running task.
        /// </summary>
        void CancelTask();
        /// <summary>
        /// Returns the status of task. 
        /// </summary>
        TaskStatus TaskStatus { get; }
        /// <summary>
        /// Blocking call that waits untill server returns the result in form of completion, failure or cancellation of a task.
        /// </summary>
        /// <returns>Returns an ITaskResult instance</returns>
        ITaskResult GetResult();
        /// <summary>
        /// If no result is obtained within the provided timeout, OperationFailedException will be thrown. 
        /// </summary>
        /// <param name="timeout">Time in millisecond in which if result is not returned, thread will be terminated and exception or null will be given</param>
        /// <returns>Returns an ITaskResult instance</returns>
        ITaskResult GetResult(int timeout);
    }
}
