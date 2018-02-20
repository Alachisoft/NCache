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
using Alachisoft.NCache.Common.MapReduce;
using System.Collections;
using Alachisoft.NCache.MapReduce.Notifications;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.MapReduce.OutputProviders
{
    public class TaskOutput
    {
        private string taskId;
        private IList output;
        private IList listeners;
        private HashVector enumerators;
        private bool disposeOutput;
        private int resultIndex = 0;

        private object _mutex = new object();

        public TaskOutput(string taskId, ClusteredArrayList output, ClusteredArrayList listeners)
        {            
            this.taskId = taskId;
            this.output = output;
            this.listeners = listeners;
        }

        public TaskEnumeratorResult GetEnumerator(TaskEnumeratorPointer pointer)
        {
            if (this.enumerators == null)
                this.enumerators = new HashVector();

            if (this.enumerators.ContainsKey(pointer))
                throw new InvalidTaskEnumeratorException("Enumerator already exists with specified Pointer.");
            if(!IsValidPointer(pointer))
                throw new InvalidTaskEnumeratorException("Invalid Enumerator Pointer specified.");

            IEnumerator it = this.output.GetEnumerator();
            it.MoveNext();
            lock (_mutex)
            {
                this.enumerators.Add(pointer, it);
            }
            return NextRecord(pointer);
        }

        public TaskEnumeratorResult NextRecord(TaskEnumeratorPointer pointer)
        {
            if(!this.enumerators.ContainsKey(pointer))
                throw new InvalidTaskEnumeratorException("Enumerator does not exist with specified Pointer.");

            TaskEnumeratorResult result = new TaskEnumeratorResult();
            result.Pointer = pointer;

            IEnumerator it = null;
            lock(_mutex)
            {
                it = (IEnumerator)this.enumerators[pointer];
            }

            try
            {
                if (it.Current != null)
                {
                    TaskOutputPair pair = (TaskOutputPair)it.Current;
                    result.RecordSet = new DictionaryEntry(pair.Key, pair.Value);
                }
            }
            catch (Exception exx) { }

            if (!it.MoveNext())
            {
                result.IsLastResult = true;
                lock (_mutex)
                {
                    this.enumerators.Remove(pointer);
                }

                this.RemoveFromListeners(pointer);

                if (this.enumerators.Count == 0 && this.listeners.Count == 0)
                    disposeOutput = true;
            }
            return result;

        }

        public void AddToListeners(TaskCallbackInfo listener)
        {
            lock (_mutex)
            {
                if (this.listeners != null)
                    listeners.Add(listener);
            }
        }

        public void RemoveFromListeners(TaskCallbackInfo listener)
        {
            if (this.listeners != null)
            {
                lock (_mutex)
                {
                    if (listeners.Contains(listener))
                        listeners.Remove(listener);
                }
            }
        }

        private void RemoveFromListeners(TaskEnumeratorPointer pointer)
        {
            if (listeners != null)
            {
                lock (_mutex)
                {
                    IEnumerator itListeners = listeners.GetEnumerator();
                    // change to for loop.
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        TaskCallbackInfo callBackInfo = (TaskCallbackInfo)listeners[i];
                        if (callBackInfo.Client.Equals(pointer.ClientId) && ((short)callBackInfo.CallbackId).Equals(pointer.CallbackId))
                        {
                            listeners.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private bool IsValidPointer(TaskEnumeratorPointer pointer)
        {
            if (this.listeners != null)
            {
                lock (_mutex)
                {
                    IEnumerator it = this.listeners.GetEnumerator();
                    while (it.MoveNext())
                    {
                        TaskCallbackInfo callbackInfo = (TaskCallbackInfo)it.Current;
                        if ((callbackInfo.Client.Equals(pointer.ClientId) && (callbackInfo.CallbackId.Equals(pointer.CallbackId))))
                            return true;
                    }
                }
            }
            return false;
        }

        public void RemoveDeadClients(ArrayList clients)
        {
            lock (_mutex)
            {
                foreach (object client in clients)
                {
                    this.listeners.Remove(client);
                    ICollection keySet = this.enumerators.Keys;
                    foreach (TaskEnumeratorPointer pointer in keySet)
                    {
                        if (pointer.ClientAddress.IpAddress.ToString().Equals(client))
                            this.enumerators.Remove(pointer);
                    }
                }
            }
            if (this.enumerators.Count == 0)
                disposeOutput = true;
        }

        #region Public Properties

        public string TaskId
        {
            get { return taskId; }
            set { taskId = value; }
        }

        public IList Output
        {
            get { return output; }
            set { output = value; }
        }

        public bool OutputDisposed
        {
            get { return disposeOutput; }
            set { disposeOutput = value; }
        }

        #endregion

    }
}
