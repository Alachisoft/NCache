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
using System.Threading;
using Alachisoft.NCache.Runtime.MapReduce;
using System.Collections;

using Alachisoft.NCache.Common.MapReduce;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.MapReduce
{
    internal class ReducerTask : Task, Alachisoft.NCache.Common.Threading.IThreadRunnable
    {
        private Thread reducerThread;
        private IReducerFactory reducerFactory;
        private volatile bool isAlive = true;
        private volatile bool isMappercompleted = false;
        private HashVector reducers;

        private Queue reducerInputQueue = null;
        
        private  MapReduceTask parent;
        private long reducedCount = 0;

        private object mutex = new object();

        public ReducerTask(IReducerFactory reducer, MapReduceTask p)
        {
            this.reducerFactory = reducer;
            this.reducers = new HashVector();
            this.parent = p;

            //initializing and synchronizing the queue
            this.reducerInputQueue = new Queue();
            Queue synchronizedQueue = Queue.Synchronized(reducerInputQueue);
            this.reducerInputQueue = synchronizedQueue;
        }

        private ClusteredArrayList FinalizeReducers()
        {
            ClusteredArrayList reducersOutput = new ClusteredArrayList();
            lock (mutex)
            {
                IEnumerator it = reducers.Values.GetEnumerator();
                while (it.MoveNext())
                {
                    Alachisoft.NCache.Runtime.MapReduce.KeyValuePair result = ((IReducer)it.Current).FinishReduce();
                    reducersOutput.Add(new TaskOutputPair(result.Key, result.Value));
                }
            }
            return reducersOutput;
        }

        public long ReducedCount
        {
            get { return reducedCount; }
            set { reducedCount = value; }
        }

        public Queue ReducerInputQueue
        {
            get { return reducerInputQueue; }
        }

        internal void SetMappersCompleted(bool mCompleted)
        {
            lock (ReducerInputQueue) {
                isMappercompleted = mCompleted;
                Monitor.PulseAll(ReducerInputQueue);
            }
        }

        public void Run()
        {
            try
            {
                if (parent.Context != null)
                {
                    if (parent.Context.NCacheLog.IsInfoEnabled)
                        parent.Context.NCacheLog.Info("ReducerTask(" + parent.TaskId + ").Start", "Reducer task is started");
                }

                bool isCompeletedSuccessfully = true;

                while (isAlive)
                {
                    try
                    {
                        object currObj = null;
                        lock (ReducerInputQueue)
                        {
                            if (ReducerInputQueue.Count == 0 && !isMappercompleted)
                            {
                                Monitor.Wait(ReducerInputQueue);
                            }
                            if(ReducerInputQueue.Count > 0)
                                currObj = ReducerInputQueue.Dequeue();
                        }
                        if (currObj != null)
                        {
                            Alachisoft.NCache.Runtime.MapReduce.KeyValuePair entry = (Alachisoft.NCache.Runtime.MapReduce.KeyValuePair)currObj;
                            Object key = entry.Key;
                            IReducer r = null;
                            lock (mutex)
                            {
                                if (!reducers.ContainsKey(key))
                                {
                                    r = reducerFactory.Create(key);
                                    r.BeginReduce();
                                    reducers.Add(key, r);
                                }
                                else
                                {
                                    r = (IReducer)reducers[key];
                                }
                            }
                            r.Reduce(entry.Value);
                            ReducedCount = ReducedCount + 1; // increment the reducedCount
                            if (parent.Context.PerfStatsColl != null)
                                parent.Context.PerfStatsColl.IncrementReducedPerSecRate();
                        }
                        else
                        {
                            if (isMappercompleted)
                            {
                                parent.PersistOutput(FinalizeReducers());
                                if (parent.Context.NCacheLog.IsInfoEnabled)
                                {
                                    parent.Context.NCacheLog.Info("ReducerTask(" + parent.TaskId + ").Run ", "Reducer Completed, output persisted.");
                                }

                                break;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        if (parent.ExceptionCount < parent.MaxExceptions)
                        {
                            if (parent.Context.NCacheLog != null)
                                parent.Context.NCacheLog.Error("ReducerTask(" + parent.TaskId + ").Run", " Exception:" + ex.Message);
                            parent.ExceptionCount = parent.ExceptionCount + 1;
                        }
                        else
                        {
                            isCompeletedSuccessfully = false;
                            parent.LocalReducerFailed();

                            lock (mutex)
                            {
                                //Dispose Reducers
                                foreach (IReducer rr in reducers.Values)
                                    rr.Dispose();
                                reducers.Clear();
                            }

                            break;
                        }
                    }
                }

                if (isCompeletedSuccessfully && isAlive)
                {
                    if (parent.Context.NCacheLog.IsInfoEnabled)
                        parent.Context.NCacheLog.Info("ReducerTask (" + parent.TaskId + ").Run ", "Reduced Total Keys : " + this.ReducedCount);
                    parent.LocalReducerCompleted();
                    lock (mutex)
                    {
                        foreach (IReducer rr in reducers.Values)
                            rr.Dispose();
                        reducers.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    parent.LocalReducerFailed();
                }
                catch (Exception)
                {}
            }
        }

        public override void StartTask()
        {
            try
            {
                reducerThread = new Thread(new ThreadStart(Run));
                reducerThread.Start();
            }
            catch (Exception ex)
            { }
        }

        public override void StopTask()
        {
            try {
                if (reducerThread != null) {
                    isAlive = false;
                    lock (ReducerInputQueue) {
                        Monitor.PulseAll(ReducerInputQueue);
                    }
                    reducerThread = null;
                }
            }
            catch (Exception ex)
            { }
        }

    }
}
