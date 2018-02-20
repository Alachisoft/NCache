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
using Alachisoft.NCache.Runtime.MapReduce;
using System.Threading;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Web.MapReduce
{
    /// <summary>
    /// Tracks the execution of MapReduce Task.
    /// <remarks><b>Note:</b>MapReduceCallback() and GetResult() cannot be executed together because GetResult is a blocking call and this combination will throw an exception.</remarks>
    /// </summary>
    public class MapReduceTaskResult : ITrackableTask
    {
        private ITaskManagement _cacheMgmt = null;
        private string taskId = null;

        private short _uniqueId = 0;

        private object mutex = new object();
        private bool asyncGet = false;
        private bool syncGet = false;
        private bool callbackReceived = false;
        private ITaskResult _taskResult = null;
        private TaskResult _response;
        private TaskCompletionStatus taskStatus = TaskCompletionStatus.Success;

        /// <summary>
        /// Initialize a new instance of class.
        /// </summary>
        /// <param name="taskMgmt">instance of ITaskManagement.</param>
        /// <param name="taskId">task id of the Task.</param>
        public MapReduceTaskResult(ITaskManagement taskMgmt, string taskId)
        {
            this._cacheMgmt = taskMgmt;
            this.taskId = taskId;
        }

        internal short UniqueId
        {
            get { return _uniqueId; }
            set { _uniqueId = value; }
        }

        /// <summary>
        /// Unique GUID identification of MapReduce task. 
        /// </summary>
        public string TaskId
        {
            get { return this.taskId; }
        }

        /// <summary>
        /// Registered Async callback on completion, failure or cancellation of task. 
        /// </summary>
        public event MapReduceCallback OnMapReduceComplete = null;

        /// <summary>
        /// Cancel the already running task.
        /// </summary>
        public void CancelTask()
        {
            try
            {
                this._cacheMgmt.CancelTask(this.taskId);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }
        }

        /// <summary>
        /// Returns the status of task. 
        /// </summary>
        public TaskStatus TaskStatus
        {
            get
            {
                if (callbackReceived)
                {
                    if (taskStatus == TaskCompletionStatus.Cancelled)
                        return new TaskStatus(Runtime.MapReduce.TaskStatus.Status.Cancelled);
                    else if (taskStatus == TaskCompletionStatus.Failure)
                        return new TaskStatus(Runtime.MapReduce.TaskStatus.Status.Failed);
                    else if (taskStatus == TaskCompletionStatus.Success)
                        return new TaskStatus(Runtime.MapReduce.TaskStatus.Status.Completed);
                }
                else
                {
                    try
                    {
                        return this._cacheMgmt.GetTaskProgress(this.taskId);
                    }
                    catch (Exception ex)
                    {
                        throw new OperationFailedException(ex.Message);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Blocking call that waits until server returns the result in form of completion, failure or cancellation of a task.
        /// </summary>
        /// <returns>Returns an ITaskResult instance</returns>
        public ITaskResult GetResult()
        {
            return GetResult(int.MaxValue);
        }

        /// <summary>
        /// If no result is obtained within the provided timeout, Operation failed exception will be thrown. 
        /// </summary>
        /// <param name="timeout">Time in millisecond in which if result is not returned, thread will be terminated and exception or null will be given</param>
        /// <returns>Returns an ITaskResult instance</returns>
        public ITaskResult GetResult(int timeout)
        {
            if (timeout < 0)
                throw new ArgumentException("Invalid timeout value");

            if (this.OnMapReduceComplete != null)
                this.asyncGet = true;

            if (_taskResult != null && !this.asyncGet)
                return _taskResult;

            if (!asyncGet)
            {
                syncGet = true;
                lock (mutex)
                {
                    try
                    {
                        Monitor.Wait(mutex, (int) timeout);
                    }
                    catch (Exception exx)
                    {
                    }

                    if (!callbackReceived)
                        throw new OperationFailedException("GetResult request timeout.");

                    if (_response != null)
                    {
                        if (_response.TaskStatus == TaskCompletionStatus.Cancelled)
                        {
                            throw new OperationFailedException(
                                "Task was cancelled. Reason: '" + _response.TaskFailureReason + "'");
                        }
                        else if (_response.TaskStatus == TaskCompletionStatus.Failure)
                        {
                            throw new OperationFailedException(
                                "Task was failed. Reason: '" + _response.TaskFailureReason +
                                "'. Please refer to logs for detailed information.");
                        }
                    }
                }

                return _taskResult;
            }
            else
            {
                throw new OperationFailedException(
                    "You have already registered a callback (async method for Get Result)");
            }
        }

        internal void OnTaskResult(TaskResult response)
        {
            response.TaskManagement = _cacheMgmt;
            _response = response;

            if (response.TaskStatus == TaskCompletionStatus.Success)
                response.GetEnumerator();

            _taskResult = _response;
            lock (mutex)
            {
                try
                {
                    taskStatus = response.TaskStatus;

                    callbackReceived = true;
                    Monitor.Pulse(mutex);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.StackTrace);
                }
            }

            if (OnMapReduceComplete != null)
            {
                OnMapReduceComplete.Invoke(response);
            }
        }
    }
}