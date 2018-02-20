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
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.MapReduce
{
    internal class CombinerTask : Task, Alachisoft.NCache.Common.Threading.IThreadRunnable
    {
        private Thread combinerThread;
        private volatile bool isAlive = true;
        private volatile bool isMapperAlive = true;
        private MapReduceTask parent;
        private ICombinerFactory combinerFactory;
        private HashVector combiners;

        private Queue combinerInputQueue = null;
        
        private int totalKeysCombined = 0;

        private object mutex = new object();

        public CombinerTask(ICombinerFactory combiner, MapReduceTask p)
        {
            this.combinerFactory = combiner;
            this.parent = p;
            combiners = new HashVector();

            // initializing and synchronizing the queue
            this.combinerInputQueue = new Queue();
            Queue synchQueue = Queue.Synchronized(combinerInputQueue);
            this.combinerInputQueue = synchQueue;

        }

        public Queue CombinerInputQueue
        {
            get { return combinerInputQueue; }
            set { combinerInputQueue = value; }
        }


        private void FinalizeCombiners()
        {
            HashVector combinerOutput = new HashVector();
            lock (mutex)
            {
                IEnumerator ite = combiners.GetEnumerator();
                while (ite.MoveNext())
                {
                    combinerOutput.Add((((DictionaryEntry)ite.Current).Key),
                        ((ICombiner)((DictionaryEntry)ite.Current).Value).FinishChunk());
                }
            }
            lock (mutex)
            {
                IEnumerator final = combinerOutput.GetEnumerator();
                while (final.MoveNext())
                {
                    parent.ReducerDataQueue.Enqueue(final.Current);
                }
            }
        }

        public bool IsMapperAlive
        {
            get { return isMapperAlive; }
            
            set 
            {
                lock (CombinerInputQueue)
                {
                    isMapperAlive = value;
                    Monitor.PulseAll(CombinerInputQueue);
                }
            }
        }

        public void Run()
        {
            try
            {
                if (parent.Context != null)
                {
                    if (parent.Context.NCacheLog.IsInfoEnabled)
                        parent.Context.NCacheLog.Info("CombinerTask(" + parent.TaskId + ").Start", "Combiner task is started");
                }

                bool completedSuccessfully = true;
                while (isAlive)
                {
                    try
                    {
                        object currObj = null;
                        lock (CombinerInputQueue)
                        {
                            if (CombinerInputQueue.Count == 0 && isMapperAlive)
                            {
                                Monitor.Wait(CombinerInputQueue);
                            }
                            if(CombinerInputQueue.Count > 0)
                                currObj = CombinerInputQueue.Dequeue();
                        }

                        if (currObj != null)
                        {
                            if (currObj.GetType().Equals(typeof(bool)))
                            {
                                bool finalizeChunk = (bool)currObj;
                                if (finalizeChunk)
                                {
                                    FinalizeCombiners();
                                    parent.SendReducerData();

                                    lock (mutex)
                                    {
                                        foreach (ICombiner cc in combiners.Values)
                                            cc.Dispose();
                                        combiners.Clear();
                                    }
                                }
                            }
                            else
                            {
                                DictionaryEntry entry = (DictionaryEntry)currObj;
                                object key = entry.Key;
                                ICombiner c = null;
                                lock (mutex)
                                {
                                    if (!combiners.ContainsKey(key))
                                    {
                                        c = combinerFactory.Create(key);
                                        c.BeginCombine();
                                        combiners.Add(key, c);
                                    }
                                    else
                                    {
                                        c = (ICombiner)combiners[key];
                                    }
                                }

                                c.Combine(entry.Value);
                                totalKeysCombined++;
                                if (parent.Context.PerfStatsColl != null)
                                    parent.Context.PerfStatsColl.IncrementCombinedPerSecRate();
                            }
                        }
                        else
                        {
                            if (!IsMapperAlive)
                            {
                                FinalizeCombiners();
                                parent.SendReducerData();
                                lock (mutex)
                                {
                                    foreach (ICombiner cc in combiners.Values)
                                        cc.Dispose();
                                    combiners.Clear();
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
                                parent.Context.NCacheLog.Error("CombinerTask (" + parent.TaskId + ").Run", " Exception: " + ex.Message);
                            parent.ExceptionCount = parent.ExceptionCount + 1;
                        }
                        else
                        {
                            completedSuccessfully = false;
                            parent.LocalCombinerFailed();
                        }
                    }
                }

                if (completedSuccessfully && isAlive)
                {
                    if (parent.Context.NCacheLog != null)
                    {
                        if (parent.Context.NCacheLog.IsInfoEnabled)
                            parent.Context.NCacheLog.Info("CombinerTask (" + parent.TaskId + ").Run", "Total Keys Combined : " + totalKeysCombined);
                    }
                    parent.LocalCombinerCompleted();
                }
            }
            catch (Exception e)
            {
                try
                {
                    parent.LocalCombinerFailed();
                }
                catch (Exception)
                {
                }
            }
        }

        public override void StartTask()
        {
            try
            {
                combinerThread = new Thread(new ThreadStart(Run));
                combinerThread.Start();
            }
            catch (Exception ex)
            { }
        }

        public override void StopTask()
        {
            try {
                if (combinerThread != null) {
                    isAlive = false;
                    lock (CombinerInputQueue) {
                        Monitor.PulseAll(CombinerInputQueue);
                    }
#if !NETCORE
                    combinerThread.Abort();
#else
                    combinerThread.Interrupt();
#endif

                    combinerThread = null;
                }
            }
            catch (Exception ex)
            { }
        }
    }
}
