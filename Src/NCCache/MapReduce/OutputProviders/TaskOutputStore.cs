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
using Alachisoft.NCache.Common.MapReduce;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.MapReduce.OutputProviders
{
    public class TaskOutputStore : IDisposable
    {
        private HashVector taskOutput = new HashVector();
        private object objectMutex = new object();

        public void PushInTaskOutput(string taskId, TaskOutput output)
        {
            lock (objectMutex)
            {
                if(!taskOutput.ContainsKey(taskId))
                    taskOutput.Add(output.TaskId, output);
            }
        }

        public TaskEnumeratorResult GetTaskEnumerator(TaskEnumeratorPointer pointer)
        {
            lock (objectMutex)
            {
                if (TaskOutputExists(pointer.TaskId))
                {
                    TaskOutput output = (TaskOutput)taskOutput[pointer.TaskId];
                    TaskEnumeratorResult result = output.GetEnumerator(pointer);

                    if (output.OutputDisposed)
                        taskOutput.Remove(pointer.TaskId);

                    return result;
                }
                else
                    throw new InvalidTaskEnumeratorException("Output with taskId '" + pointer.TaskId + "' does not exist. Task might be cancelled or failed.");
            }
        }

        public TaskEnumeratorResult NextRecord(TaskEnumeratorPointer pointer)
        { 
            lock (objectMutex) {
                if (TaskOutputExists(pointer.TaskId)) {
                    TaskOutput output = (TaskOutput) taskOutput[pointer.TaskId];
                    TaskEnumeratorResult result = output.NextRecord(pointer);
                
                    if(output.OutputDisposed)
                        taskOutput.Remove(pointer.TaskId);
                
                    return result;                
                } else {
                    throw new InvalidTaskEnumeratorException("Output with task id : " + pointer.TaskId + " got corrupted.");
                }
            }
        }

        public bool TaskOutputExists(string taskId)
        {
            return taskOutput.ContainsKey(taskId);
        }

        public TaskOutput TaskOutput(string taskId)
        {
            return (TaskOutput)taskOutput[taskId];
        }

        public void Dispose()
        {
            taskOutput.Clear();
        }

        public void RemoveDeadClientsIterators(ArrayList clients)
        {
            ICollection outputKeyList = taskOutput.Keys;
            foreach(String key in outputKeyList)
            {
                TaskOutput taskout = (TaskOutput)taskOutput[key];
                taskout.RemoveDeadClients(clients);
                if(taskout.OutputDisposed)
                    taskOutput.Remove(key);
            }
        }

    }
}
