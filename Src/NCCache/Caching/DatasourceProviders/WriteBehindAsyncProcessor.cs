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
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Runtime.DatasourceProviders;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    /// <summary>
    /// Processor to perform asynchronous operation, which will only be executed
    /// when preemted.
    /// </summary>
    public class WriteBehindAsyncProcessor : IGRShutDown
    {
        /// <summary>
        /// Defines the state of tasks in queue
        /// </summary>
        public enum TaskState : byte
        {
            Waite,
            Execute,
            Remove
        }
        /// <summary>
        /// Defines the state of operation in queue
        /// </summary>
        public enum OperationState
        {
            New,
            Requeue
        }
        /// <summary>
        /// Defines the write behind mode
        /// </summary>
        public enum WriteBehindMode
        {
            Batch,
            NonBatch
        }
        /// <summary>
        /// Queue using ArrayList as internal structure.
        /// </summary>
        internal class WaitQueue : ICloneable
        {
            private ArrayList _queue;
            /// <summary>
            /// Initializes new instance of WriteBehindQueue
            /// </summary>
            public WaitQueue()
            {
                this._queue = new ArrayList();
            }

            /// <summary>
            /// Initializes new instance of WriteBehindQueue
            /// </summary>
            /// <param name="capacity">capacity</param>
            public WaitQueue(int capacity)
            {
                this._queue = new ArrayList(capacity);
            }



            /// <summary>
            /// Queue a write behind task
            /// </summary>
            /// <param name="task">write behind task</param>
            public void Enqueue(DSWriteBehindOperation writeOperations)
            {
                lock (this._queue)
                {
                    this._queue.Add(writeOperations);
                }
            }

            /// <summary>
            /// Dequeue a write behind task
            /// </summary>
            /// <returns>write behind task</returns>
            public DSWriteOperation Dequeue()
            {
                lock (this._queue)
                {
                    if (this._queue.Count == 0) return null;

                    DSWriteOperation value = this._queue[0] as DSWriteOperation;
                    this._queue.RemoveAt(0);
                    return value;
                }
            }

            /// <summary>
            /// Get the write behind task at top of queue
            /// </summary>
            /// <returns>write behind task</returns>
            public DSWriteOperation Peek()
            {
                lock (this._queue)
                {
                    if (this._queue.Count == 0) return null;
                    return this._queue[0] as DSWriteOperation;
                }
            }

            public DSWriteBehindOperation this[int index]
            {
                get
                {
                    lock (this._queue)
                    {
                        if (index >= _queue.Count || index < 0) throw new IndexOutOfRangeException();
                        return _queue[index] as DSWriteBehindOperation;
                    }
                }
                set
                {
                }
            }

            public void RemoveAt(int index)
            {
                lock (this._queue)
                {
                    if (index >= _queue.Count || index < 0) throw new IndexOutOfRangeException();
                    _queue.RemoveAt(index);
                }
            }
            /// <summary>
            /// Removes write behind, by searching the task with same taskId in queue
            /// </summary>
            ///<param name="taskId">taskId</param>
            public void Remove(string taskId)
            {
                lock (this._queue)
                {
                    for (int i = this._queue.Count - 1; i >= 0; i--)
                    {
                        DSWriteBehindOperation qTask = this._queue[i] as DSWriteBehindOperation;
                        if (qTask.TaskId.Contains(taskId))
                        {
                            this._queue.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            /// <summary>
            /// Clears the write behind queue
            /// </summary>
            public void Clear()
            {
                lock (this._queue)
                {
                    this._queue.Clear();
                }
            }
            /// <summary>
            /// 
            /// </summary>
            public int Count
            {
                get { lock (this._queue) { return _queue.Count; } }
            }

            #region IEnumerable Members

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public IEnumerator GetEnumerator()
            {
                lock (this._queue)
                {
                    return _queue.GetEnumerator();
                }
            }

            #endregion

            #region ICloneable Members

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                lock (this._queue)
                {
                    return _queue.Clone();
                }
            }

            #endregion
        }

        /// <summary>
        /// Queue using Hashtable as internal structure.Removes previous operations in case of new one against same key
        /// </summary>
        [Serializable]
        internal class WriteBehindQueue : ICloneable
        {
            private Hashtable _queue = new Hashtable(1000);
            private Dictionary<string, int> _keyToIndexMap = new Dictionary<string, int>(1000);
            private Dictionary<int, string> _indexToKeyMap = new Dictionary<int, string>(1000);
            private Dictionary<string, int> _taskIDMap = new Dictionary<string, int>(1000);
            private WaitQueue _waitQueue = new WaitQueue();

            private int _tail = -1;
            private int _head = -1;
            private bool _tailMaxReached = false;
            private object _sync_mutex = new object();

            private int _requeueLimit = 0;
            private int _evictionRatio = 0;//requeue operations evictions ratio
            private float _ratio = 0.25F;
           
            private ArrayList _requeuedOps = new ArrayList();
            private CacheRuntimeContext _context;
         
            private Latch _shutdownStatusLatch = new Latch(ShutDownStatus.NONE);

            internal WaitQueue WaitQueue
            {
                get { return _waitQueue; }
            }
            
            public WriteBehindQueue(CacheRuntimeContext context)
            {
                _context = context;
                _waitQueue = new WaitQueue();
                _requeuedOps = new ArrayList();
            }

            public void WindUpTask()
            {
                _context.NCacheLog.CriticalInfo("WriteBehindQueue", "WindUp Task Started.");
                if (_queue != null)
                    _context.NCacheLog.CriticalInfo("WriteBehindQueue", "Write Behind Queue Count: " + _queue.Count);

                _shutdownStatusLatch.SetStatusBit(ShutDownStatus.SHUTDOWN_INPROGRESS, ShutDownStatus.NONE);

                lock (_sync_mutex)
                {
                    Monitor.PulseAll(_sync_mutex);
                }

                _context.NCacheLog.CriticalInfo("WriteBehindQueue", "WindUp Task Ended.");
            }

            public void WaitForShutDown(long interval)
            {
                _context.NCacheLog.CriticalInfo("WriteBehindQueue", "Waiting for shutdown task completion.");


                if (_queue.Count > 0)
                    _shutdownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED, interval * 1000);

                if (_queue != null && _queue.Count > 0)
                    _context.NCacheLog.CriticalInfo("WriteBehindQueue", "Remaining write behind queue operations: " + _queue.Count);

                _context.NCacheLog.CriticalInfo("WriteBehindQueue", "Shutdown task completed.");
            }

            /// <summary>
            /// Enqueue opeartion, adds the opeartion at the end of the queue and removes 
            /// any previous operations on that key.
            /// </summary>
            /// <param name="operation"></param>
            public void Enqueue(object cacheKey, bool merge, DSWriteBehindOperation operation)
            {
                bool isNewItem = true;
                bool isRequeueDisable = (operation.OperationState == OperationState.Requeue && _requeueLimit == 0);//for hot apply
                try
                {
                    lock (_sync_mutex)
                    {
                        if (operation.State == TaskState.Waite)
                        {
                            _waitQueue.Enqueue(operation);
                            return;
                        }
                        if (_tail == int.MaxValue)
                        {
                            _tail = -1;
                            _tailMaxReached = true;
                        }
                        if (!merge)
                        {
                            if (_requeueLimit > 0 && operation.OperationState == OperationState.Requeue)
                            {
                                if (_requeuedOps.Count > _requeueLimit)
                                    EvictRequeuedOps();
                            }
                            if (_keyToIndexMap.ContainsKey((string)cacheKey))
                            {
                                if (isRequeueDisable) return;
                                int queueIndex = _keyToIndexMap[(string)cacheKey];
                                DSWriteBehindOperation oldOperation = _queue[queueIndex] as DSWriteBehindOperation;
                                if (!oldOperation.OperationState.Equals(operation.OperationState))
                                {
                                    // we will keep old operation in case incoming operation is requeued
                                    //if existing is requeued and incoming operation is new,than we will keep new
                                    if (operation.OperationState == OperationState.New)
                                    {
                                        operation.EnqueueTime = DateTime.Now;
                                        _taskIDMap.Remove(oldOperation.TaskId);
                                        _queue[queueIndex] = operation;//update operation
                                        _taskIDMap[operation.TaskId] = queueIndex;
                                    }
                                }
                                else
                                {
                                    if (operation.OperationState == OperationState.Requeue)
                                    {
                                        operation.EnqueueTime = DateTime.Now;//reset operation delay
                                    }
                                    else
                                        operation.EnqueueTime = oldOperation.EnqueueTime;   //maintaining previous operation delay

                                    _requeuedOps.Add(operation);
                                    _taskIDMap.Remove(oldOperation.TaskId);
                                    _queue[queueIndex] = operation;//update operation
                                    _taskIDMap[operation.TaskId] = queueIndex;
                                }
                                isNewItem = false;
                            }
                        }
                        if (isNewItem && !isRequeueDisable)
                        {
                            int index = ++_tail;
                            operation.EnqueueTime = DateTime.Now; //starting operation delay
                            _queue[index] = operation;
                            _keyToIndexMap[(string)cacheKey] = index;
                            _indexToKeyMap[index] = (string)cacheKey;
                            _taskIDMap[(string)operation.TaskId] = index;
                            Monitor.PulseAll(_sync_mutex);
                            if (operation.OperationState == OperationState.Requeue)
                                _requeuedOps.Add(operation);
                        }
                        //write behind queue counter
                        _context.PerfStatsColl.SetWBQueueCounter(this._queue.Count);
                        _context.PerfStatsColl.SetWBFailureRetryCounter(_requeuedOps.Count);
                    }
                }
                catch (Exception exp)
                {
                    throw exp;
                }
                finally
                {
                }
            }

            public DSWriteBehindOperation Dequeue(bool batchOperations, DateTime selectionTime)
            {
                DSWriteBehindOperation operation = null;
                try
                {
                    lock (_sync_mutex)
                    {
                        if (this._queue.Count < 1)
                        {
                            if (_shutdownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                            {
                                _shutdownStatusLatch.SetStatusBit(ShutDownStatus.SHUTDOWN_COMPLETED, ShutDownStatus.SHUTDOWN_INPROGRESS);
                                return null;
                            }
                            if (batchOperations) return null;
                            Monitor.Wait(_sync_mutex);
                            _reset = true;
                        }
                        int index = 0;
                        do
                        {
                            if (_head < _tail || _tailMaxReached)
                            {
                                if (_head == int.MaxValue)
                                {
                                    _head = -1;
                                    _tailMaxReached = false;
                                }


                                index = ++_head;
                                operation = _queue[index] as DSWriteBehindOperation;

                                if (operation != null)
                                {
                                    if (batchOperations)
                                    {
                                        if (!_shutdownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                                        {
                                            if (!operation.OperationDelayExpired)
                                            {
                                                --_head;
                                                return null;
                                            }
                                            else
                                            {
                                                if (operation.EnqueueTime > selectionTime)
                                                {
                                                    --_head;
                                                    return null;
                                                }
                                            }
                                        }
                                    }
                                    string cacheKey = _indexToKeyMap[index] as string;
                                    _taskIDMap.Remove(operation.TaskId);
                                    _keyToIndexMap.Remove(cacheKey);
                                    _indexToKeyMap.Remove(index);
                                    _queue.Remove(index);
                                    if (operation.OperationState == OperationState.Requeue)
                                        _requeuedOps.Remove(operation);
                                }
                            }
                            else
                                break;
                        } while (operation == null);
                        _context.PerfStatsColl.SetWBQueueCounter(this._queue.Count);
                        _context.PerfStatsColl.SetWBFailureRetryCounter(_requeuedOps.Count);
                    }
                }
                catch (Exception exp)
                {
                    throw exp;
                }
                return operation;
            }
            /// <summary>
            /// Get the write behind task at top of queue
            /// </summary>
            /// <returns>write behind task</returns>
            public DSWriteBehindOperation Peek()
            {
                lock (this._queue)
                {
                    if (this._queue.Count == 0) return null;
                    return this._queue[0] as DSWriteBehindOperation;
                }
            }

            public DSWriteBehindOperation this[int index]
            {
                get
                {
                    lock (this._queue)
                    {
                        if (index >= _queue.Count || index < 0) throw new IndexOutOfRangeException();
                        return _queue[index] as DSWriteBehindOperation;
                    }
                }
                set
                {
                }
            }
            /// <summary>
            /// Evict Write behind requeued operations.
            /// </summary>
            public void EvictRequeuedOps()
            {
                lock (_sync_mutex)
                {
                    if (_requeuedOps.Count > 0)
                    {
                        int opsCountTobeRemoved = (int)Math.Ceiling((float)_requeuedOps.Count * _ratio);
                        ArrayList removableIndexes = new ArrayList();
                        _requeuedOps.Sort();
                        for (int i = opsCountTobeRemoved; i > 0; i--)
                        {
                            DSWriteBehindOperation operation = _requeuedOps[i] as DSWriteBehindOperation;
                            string cacheKey = operation.Key as string;
                            int index = _keyToIndexMap[cacheKey];
                            _keyToIndexMap.Remove(cacheKey);
                            _indexToKeyMap.Remove(index);
                            _taskIDMap.Remove(operation.TaskId);
                            _queue.Remove(index);
                            removableIndexes.Add(i);
                            _context.PerfStatsColl.IncrementWBEvictionRate();
                        }
                        for (int i = removableIndexes.Count; i > 0; i--)
                        {
                            _requeuedOps.RemoveAt(i);
                        }
                    }
                    _context.PerfStatsColl.SetWBQueueCounter(this._queue.Count);
                    _context.PerfStatsColl.SetWBFailureRetryCounter(_requeuedOps.Count);
                }
            }
            /// <summary>
            /// Updates write behind task state, by searching the task with same taskId in queue
            /// </summary>
            /// <param name="taskId">taskId</param>
            /// <param name="state">new state</param>
            public void UpdateState(string taskId, TaskState state)
            {
                lock (this._sync_mutex)
                {
                    bool found = false;
                    int index = -1;
                    for (int i = this._waitQueue.Count - 1; i >= 0; i--)
                    {
                        DSWriteBehindOperation operation = this._waitQueue[i] as DSWriteBehindOperation;
                        if (operation.TaskId.Contains(taskId))
                        {
                            if (state == TaskState.Execute)
                            {
                                operation.State = state;            //move to write behind queue only if state is execute
                                this.Enqueue(operation.Key, false, operation);
                            }
                            found = true;
                            index = i;
                            break;
                        }
                    }
                    if (index >= 0)
                        _waitQueue.RemoveAt(index);
                    if (!found)//for remove operation in main queue
                    {
                        if (_taskIDMap.ContainsKey(taskId))
                        {
                            int queueIndex = _taskIDMap[taskId];
                            string cachekey = _indexToKeyMap[queueIndex];
                            DSWriteBehindOperation operation = _queue[queueIndex] as DSWriteBehindOperation;
                            _queue.Remove(queueIndex);
                            _keyToIndexMap.Remove(cachekey);
                            _indexToKeyMap.Remove(queueIndex);
                            _taskIDMap.Remove(taskId);
                            if (operation.OperationState == OperationState.Requeue)
                                _requeuedOps.Remove(operation);
                        }
                    }
                    _context.PerfStatsColl.SetWBQueueCounter(this._queue.Count);
                    _context.PerfStatsColl.SetWBFailureRetryCounter(_requeuedOps.Count);
                }
            }

            /// <summary>
            /// Updates write behind task state, by searching the task with same taskId in queue
            /// </summary>
            /// <param name="taskId">taskId</param>
            /// <param name="state">new state</param>
            /// <param name="newBulkTable">table that contains keys and value that succeded bulk operation</param>
            public void UpdateState(string taskId, TaskState state, Hashtable newBulkTable)
            {
                lock (this._sync_mutex)
                {
                    bool removeOps = false;
                    int count = this._waitQueue.Count;
                    ArrayList removableIndexes = new ArrayList();
                    //for waite status
                    for (int i = 0, j = 0; i < count; i++)
                    {
                        DSWriteBehindOperation queueOp = this._waitQueue[i] as DSWriteBehindOperation;
                        if (newBulkTable.ContainsKey(queueOp.Key) && queueOp.TaskId.Contains(taskId))
                        {
                            if (state == TaskState.Execute)
                            {
                                queueOp.State = state;
                                this.Enqueue(queueOp.Key, false, queueOp);
                            }
                            removableIndexes.Add(i);
                            if (newBulkTable.Count == ++j)
                            {
                                removeOps = true;
                                break;
                            }
                        }
                    }
                    for (int i = removableIndexes.Count - 1; i >= 0; i--)
                    {
                        _waitQueue.RemoveAt((int)removableIndexes[i]);
                    }
                    //for remove status
                    if (state == TaskState.Remove && !removeOps)
                    {
                        IDictionaryEnumerator bulkTable = newBulkTable.GetEnumerator();
                        while (bulkTable.MoveNext())
                        {
                            string key = (string)bulkTable.Key;
                            if (_keyToIndexMap.ContainsKey(key))
                            {
                                int queueIndex = _keyToIndexMap[key];
                                string cachekey = _indexToKeyMap[queueIndex];
                                DSWriteBehindOperation operation = _queue[queueIndex] as DSWriteBehindOperation;
                                _queue.Remove(queueIndex);
                                _keyToIndexMap.Remove(cachekey);
                                _indexToKeyMap.Remove(queueIndex);
                                _taskIDMap.Remove(operation.TaskId);
                                if (operation.OperationState == OperationState.Requeue)
                                    _requeuedOps.Remove(operation);
                            }
                        }
                    }
                    _context.PerfStatsColl.SetWBQueueCounter(this._queue.Count);
                    _context.PerfStatsColl.SetWBFailureRetryCounter(_requeuedOps.Count);
                }
            }


            /// <summary>
            /// Search write behind tasks states, by searching the task with the same taskId in the table.
            /// </summary>
            /// <param name="states">key value pair of taskIds and there states</param>
            public void UpdateState(Hashtable states)
            {
                lock (this._sync_mutex)
                {
                    bool removeOps = false;
                    //for waite status
                    for (int i = 0, j = 0; i < this._waitQueue.Count; i++)
                    {
                        DSWriteBehindOperation queueOp = this._waitQueue[i] as DSWriteBehindOperation;
                        if (states.ContainsKey(queueOp.TaskId))
                        {
                            TaskState state = (TaskState)states[queueOp.TaskId];
                            if (state == TaskState.Execute)
                            {
                                queueOp.State = state;
                                this.Enqueue(queueOp.Key, false, queueOp);
                            }
                            _waitQueue.Remove(queueOp.TaskId);
                            if (states.Count == ++j)
                            {
                                removeOps = true;
                                break;
                            }
                        }
                    }
                    //for remove status
                    if (!removeOps)
                    {
                        IDictionaryEnumerator taskStaes = states.GetEnumerator();
                        while (taskStaes.MoveNext())
                        {
                            string taskId = (string)taskStaes.Key;
                            if (_taskIDMap.ContainsKey(taskId))
                            {
                                int queueIndex = _taskIDMap[taskId];
                                string cachekey = _indexToKeyMap[queueIndex];
                                DSWriteBehindOperation operation = _queue[queueIndex] as DSWriteBehindOperation;
                                _queue.Remove(queueIndex);
                                _keyToIndexMap.Remove(cachekey);
                                _indexToKeyMap.Remove(queueIndex);
                                if (operation.OperationState == OperationState.Requeue)
                                    _requeuedOps.Remove(operation);
                            }
                        }
                    }
                    _context.PerfStatsColl.SetWBQueueCounter(this._queue.Count);
                    _context.PerfStatsColl.SetWBFailureRetryCounter(_requeuedOps.Count);
                }
            }

            /// <summary>
            /// Search for the write behind tasks initiated from source address, and move these operations from wait queue to ready queue
            /// </summary>
            /// <param name="source">address of source node</param>
            public void UpdateState(string source)
            {
                lock (this._sync_mutex)
                {
                    int index = -1;
                    for (int i = 0; i < this._waitQueue.Count; i++)
                    {
                        DSWriteBehindOperation queueOp = this._waitQueue[i] as DSWriteBehindOperation;
                        if (queueOp.Source == source)
                        {
                            queueOp.State = TaskState.Execute;
                            this.Enqueue(queueOp.Key, false, queueOp);
                            index = i;
                            break;
                        }
                    }
                    if (index >= 0)
                        _waitQueue.RemoveAt(index);
                }
            }
            /// <summary>
            /// Search for the write behind task in wait queue
            /// </summary>
            /// <param name="source">address of source node</param>
            public bool SearchWaitQueue(string taskId)
            {
                lock (this._sync_mutex)
                {
                    bool found = false;
                    int index = -1;
                    for (int i = 0; i < this._waitQueue.Count; i++)
                    {
                        DSWriteBehindOperation queueOp = this._waitQueue[i] as DSWriteBehindOperation;
                        if (queueOp.TaskId.Contains(taskId))
                        {
                            found = true;
                            index = i;
                            break;
                        }
                    }
                    if (index >= 0)
                        _waitQueue.RemoveAt(index);
                    return found;
                }
            }
            /// <summary>
            /// Clears the write behind queue
            /// </summary>
            public void Clear()
            {
                lock (this._sync_mutex)
                {
                    if (_queue != null)
                        this._queue.Clear();
                    if (_requeuedOps != null)
                        this._requeuedOps.Clear();
                    if (_taskIDMap != null)
                        this._taskIDMap.Clear();
                    if (_taskIDMap != null)
                        this._taskIDMap.Clear();
                    if (_keyToIndexMap != null)
                        this._keyToIndexMap.Clear();
                }

                if (_context != null)
                {
                    _context.PerfStatsColl.SetWBQueueCounter(this._queue.Count);
                    _context.PerfStatsColl.SetWBFailureRetryCounter(_requeuedOps.Count);
                }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                lock (this._sync_mutex)
                {
                    WriteBehindQueue queue = new WriteBehindQueue(this._context);
                    queue._queue = this._queue;
                    queue._keyToIndexMap = this._keyToIndexMap;
                    queue._indexToKeyMap = this._indexToKeyMap;
                    queue._taskIDMap = this._taskIDMap;
                    queue._waitQueue = this._waitQueue;
                    queue._tail = this._tail;
                    queue._head = this._head;
                    queue._tailMaxReached = this._tailMaxReached;

                    queue._requeueLimit = this._requeueLimit;
                    queue._evictionRatio = this._evictionRatio;//requeue operations evictions ratio
                    queue._ratio = this._ratio;
                    queue._requeuedOps = this._requeuedOps;
                    return queue;
                }
            }
            public int Count
            {
                get { lock (this._queue) { return _queue.Count; } }
            }


            #region IEnumerable Members

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public IEnumerator GetEnumerator()
            {
                lock (this._queue)
                {
                    return _queue.GetEnumerator();
                }
            }

            #endregion
            internal void MergeQueue(WriteBehindQueue chunkOfQueue)
            {
                foreach (DictionaryEntry item in chunkOfQueue)
                {
                    if (item.Value is DSWriteBehindOperation)
                    {
                        DSWriteBehindOperation operation = (DSWriteBehindOperation)item.Value;
                        Enqueue(operation.Key, true, operation);
                    }
                }
            }

            internal void SetConfigDefaults(int requeueLimit, int requeueEvcRatio)
            {
                //only increased requeue limit is acceptable for hot apply
                if (requeueLimit >= this._requeueLimit)
                    this._requeueLimit = requeueLimit;
                if (this._requeueLimit > 0)
                    this._evictionRatio = requeueEvcRatio;
                _ratio = this._evictionRatio / 100f;
            }

            internal void ExecuteWaitQueue()
            {
                lock (this._sync_mutex)
                {
                    for (int i = 0; i < this._waitQueue.Count; i++)
                    {
                        DSWriteBehindOperation queueOp = this._waitQueue[i] as DSWriteBehindOperation;
                        queueOp.State = TaskState.Execute;
                        this.Enqueue(queueOp.Key, false, queueOp);
                        _waitQueue.Remove(queueOp.TaskId);
                    }
                }
            }

            /// <summary>
            /// Check if queue contains specified key.
            /// </summary>
            /// <param name="key">cache operation key.</param>
            /// <returns>True if queue contains the key, else false.</returns>
            public virtual bool Contains(string key)
            {
                return _keyToIndexMap.ContainsKey(key);
            }
        }


        /// <summary> The worker thread. </summary>
        private Thread _worker;

        private WriteBehindQueue _queue;
        

        /// <summary>Operation time out</summary>
        private int _timeout;

        /// <summary></summary>
        private object _statusMutex, _processMutex;

        /// <summary></summary>
        private bool _isDisposing;

        private DateTime? _startTime;
        private int _operationCount = 0; int test = 0;
        private static bool _reset = false;
        ILogger _ncacheLog;
        private string _mode;
        private int _throttleRate = 0;
        private int _batchInterval = 0;
        private int _operationDelay = 0;
        private int _requeueLimit = 0;
        private int _requeueEvcRatio = 0;
        private bool _isSliding = false;
        private DatasourceMgr _dsManager;
        private CacheBase _cacheImpl;
        private CacheRuntimeContext _context;
        private object _threadMutex = new object();

        private bool _isShutDown = false;

        private ArrayList _currentSelectedBatchOperations = new ArrayList();
        private DSWriteBehindOperation _currentSelectedOperation = null;

        /// <summary> The external datasource writer </summary>
        private Dictionary<string, WriteThruProviderMgr> _writerProivder = new Dictionary<string, WriteThruProviderMgr>();
        private ILogger NCacheLog
        {
            get { return _context.NCacheLog; }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        internal WriteBehindAsyncProcessor(DatasourceMgr dsManager, int rate, string mode, long batchInterval, long operationDelay, int requeueLimit, int evictionRatio, long taskWaiteTimeout, Dictionary<string, WriteThruProviderMgr> writerProvider, CacheBase cacheImpl, CacheRuntimeContext context)
        {
            this._dsManager = dsManager;
            this._context = context;
            this._worker = null;
            this._timeout = (int)taskWaiteTimeout;
            this._statusMutex = new object();
            this._processMutex = new object();
            this._isDisposing = false;
            this._writerProivder = writerProvider;
            this._mode = mode;
            SetConfigDefaults(mode, rate, batchInterval, operationDelay, requeueLimit, evictionRatio);
            this._cacheImpl = cacheImpl;
            this._queue = new WriteBehindQueue(_context);
            this._queue.SetConfigDefaults(_requeueLimit, _requeueEvcRatio);
        }

        public void WindUpTask()
        {
            _context.NCacheLog.CriticalInfo("WriteBehindAsyncProcessor", "WindUp Task Started.");

            _isShutDown = true;
            _batchInterval = 0;
            _operationDelay = 0;

            ExecuteWaitQueue();

            if (_queue != null)
            {

                _queue.WindUpTask();
            }

            _context.NCacheLog.CriticalInfo("WriteBehindAsyncProcessor", "WindUp Task Ended.");
        }

        public void WaitForShutDown(long interval)
        {
            _context.NCacheLog.CriticalInfo("WriteBehindAsyncProcessor", "Waiting for  Write Behind queue shutdown task completion.");

            DateTime startShutDown = DateTime.Now;

            if (_queue != null)
                _queue.WaitForShutDown(interval);
            
            _context.NCacheLog.CriticalInfo("WriteBehindAsyncProcessor", "Shutdown task completed.");
        }

        internal void SetConfigDefaults(string mode, int rate, long batchInterval, long operationDelay, int requeueLimit, int requeueEvcRatio)
        {

            if (rate < 0)
                this._throttleRate = 500;
            else
                this._throttleRate = rate;

            if (requeueLimit < 0)
                this._requeueLimit = 5000;
            else
                this._requeueLimit = (int)requeueLimit;

            if (this._requeueLimit > 0)
            {
                if (requeueEvcRatio < 0)
                    this._requeueEvcRatio = 5;
                else
                    this._requeueEvcRatio = (int)requeueEvcRatio;
            }

            switch ((string)mode)
            {
                case "batch":
                    if (batchInterval < 0)
                        this._batchInterval = (5 * 1000);//in sec
                    else
                    {
                        this._batchInterval = (int)batchInterval;// Now we receive value in miliseconds
                    }
                    if (operationDelay < 0)
                        this._operationDelay = 0;
                    else
                        this._operationDelay = (int)operationDelay;
                    foreach (KeyValuePair<string, WriteThruProviderMgr> kv in _writerProivder)
                    {
                        kv.Value.HotApplyConfig(_operationDelay);
                    }
                    this._mode = mode;
                    break;
                case "non-batch":
                    this._mode = "non-batch";
                    break;
            }
            //Setting config value in WB Queue
            if (_queue != null)
                _queue.SetConfigDefaults(this._requeueLimit, this._requeueEvcRatio);           
        }

        /// <summary>
        /// Get a value indicating if the processor is running
        /// </summary>
        internal bool IsRunning
        {
            get { return _worker != null && _worker.IsAlive; }
        }

        internal CacheBase CacheImpl
        {
            get
            {
                return _cacheImpl;
            }
            set
            {
                _cacheImpl = value;
            }
        }
        /// <summary>
        /// Start processing
        /// </summary>
        internal void Start()
        {
            lock (_threadMutex)
            {
                if (this._worker == null)
                {
                    this._worker = new Thread(new ThreadStart(this.Run));
                    this._worker.IsBackground = true;
                    this._worker.Name = "WriteBehindAsyncProcessor";
                    this._worker.Start();
                }
            }
        }

        /// <summary>
        /// Stop processing.
        /// </summary>
        internal void Stop()
        {
            lock (this._processMutex)
            {
                lock (this)
                {
                    Monitor.PulseAll(this);
                    this._isDisposing = true;

                    if (this._worker != null && this._worker.IsAlive)
                    {
#if !NETCORE
                        this._worker.Abort();
#else
                        this._worker.Interrupt();
#endif
                        this._worker = null;
                    }
                }
            }
        }

        /// <summary>
        /// Thread function, keeps running.
        /// </summary>
        internal void Run()
        {

            int remainingInetrval = _batchInterval;
            while (this._worker != null && !this._isDisposing)
            {
                try
                {
                    if (_mode.ToLower() == "batch")
                    {
                        if (!_isShutDown && remainingInetrval > 0)
                            Thread.Sleep(remainingInetrval); //for long value
                        DateTime start = DateTime.Now;
                        ProcessQueue(WriteBehindMode.Batch);
                        TimeSpan interval = DateTime.Now - start;
                        int processTime = (int)interval.TotalMilliseconds;
                        if (_isSliding && (_batchInterval - processTime > 0))
                            remainingInetrval = _batchInterval - processTime;
                        else
                            remainingInetrval = _batchInterval;//for hot apply

                    }
                    else
                        ProcessQueue(WriteBehindMode.NonBatch);
                }
                catch (ThreadAbortException e)
                {
                }
                catch (ThreadInterruptedException e)
                {

                }
                catch (Exception e)
                {
                }
            }

        }

        public void StartExecutionOfTasksForSource(string source, bool execute)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteAllTaskForSource), new object[] { source, execute });
        }


        private void ProcessQueue(WriteBehindMode mode)
        {
            DSWriteBehindOperation operation = null;
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            switch (mode)
            {
                case WriteBehindMode.NonBatch:
                    operation = this._queue.Dequeue(false, DateTime.Now);
                    if (operation != null)
                    {
                        _currentSelectedOperation = operation;
                        _context.PerfStatsColl.SetWBCurrentBatchOpsCounter(1);
                        lock (_processMutex)
                        {
                            if (!_isDisposing) ExecuteWriteOperation(operation, operationContext);
                            if (!_isShutDown) ThrottleOperations(_throttleRate, false);

                        }
                        _currentSelectedOperation = null;
                    }
                    break;
                case WriteBehindMode.Batch:
                    ArrayList selectedOperations = new ArrayList();
                    DateTime selectionTime = DateTime.Now;//we will select all expired operations uptill this limit
                    _currentSelectedBatchOperations = new ArrayList();

                    while (this._worker != null && !this._isDisposing)
                    {
                        try
                        {
                            operation = this._queue.Dequeue(true, selectionTime);
                            if (operation != null)
                                selectedOperations.Add(operation);
                            if (operation == null || this._queue.Count == 0)
                                break;

                        }
                        catch (ThreadAbortException e)
                        {
                            return;
                        }
                        catch (ThreadInterruptedException e)
                        {
                            return;
                        }
                        catch (Exception e)
                        {
                        }
                    }

                    _context.PerfStatsColl.SetWBCurrentBatchOpsCounter(selectedOperations.Count);
                    int rate = _throttleRate;
                    //apply to data source
                    if (selectedOperations.Count > 0)
                    {
                        lock (_currentSelectedBatchOperations)
                        {
                            _currentSelectedBatchOperations = selectedOperations;
                            _startTime = DateTime.Now;
                            Dictionary<string, WriteThruProviderMgr>.Enumerator providers = _writerProivder.GetEnumerator();
                            while (providers.MoveNext())
                            {
                                string provider = providers.Current.Key;
                                DSWriteBehindOperation[] operations = SortProviders(selectedOperations, provider);
                                if (operations != null && operations.Length > 0)
                                {
                                    int index = 0;
                                    bool getNext = true;
                                    while (getNext)
                                    {
                                        DSWriteBehindOperation[] opsBatch = CreateBatch(operations, rate, index, out getNext);
                                        if (opsBatch != null && opsBatch.Length > 0)
                                        {
                                            ExecuteWriteOperation(opsBatch, provider, operationContext);
                                            if (!_isShutDown) ThrottleOperations(opsBatch.Length, true);
                                        }
                                        if (getNext) index += rate;
                                    }
                                }
                            }
                            // In case it didn't, make it
                            _currentSelectedBatchOperations = null;
                        }
                    }
                    break;
                default:
                    return;
            }
        }
        private void ExecuteWriteOperation(DSWriteBehindOperation operation, OperationContext context)
        {
            OperationResult result = null;
            Hashtable opResult = new Hashtable(1);
            bool notify = false;
            if (operation != null)
            {
                try
                {
                    result = _dsManager.WriteThru(operation, context);
                    if (result != null)
                    {
                        if (result.DSOperationStatus == OperationResult.Status.FailureRetry)
                        {
                            _cacheImpl.Context.NCacheLog.Info("Retrying Write Operation: " + operation.OperationCode + " for key:" + operation.Key);
                            operation.OperationState = OperationState.Requeue;
                            operation.OperationDelay = _operationDelay;//for hot apply
                            operation.RetryCount++;
                            Enqueue(operation);
                            return;
                        }
                        _cacheImpl.DoWrite("Executing WriteBehindTask", "taskId=" + operation.TaskId + "operation result status=" + result.DSOperationStatus, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                                                
                        opResult.Add(operation.Key, result.DSOperationStatus);
                    }
                    else
                    {
                        notify = true;
                        opResult.Add(operation.Key, OperationResult.Status.Success);
                    }
                }
                catch (Exception excep)
                {
                    notify = true;
                    if (_cacheImpl.Context.NCacheLog.IsErrorEnabled) _cacheImpl.Context.NCacheLog.Error("Executing WriteBehindTask", excep.Message);
                    opResult.Add(operation.Key, excep);
                }
                finally
                {
                    if ((notify) || (result != null && result.DSOperationStatus != OperationResult.Status.FailureRetry))
                    {
                        NotifyWriteBehindCompletion(operation, opResult);
                    }
                }
            }
        }

        private void ExecuteWriteOperation(DSWriteBehindOperation[] operations, string provider, OperationContext context)
        {
            Hashtable opResult = new Hashtable();
            Hashtable returnSet = new Hashtable();
            CallbackEntry cbEntry = null;
            Exception exc = null;
            ArrayList retryOps = new ArrayList();
            ArrayList taskList = new ArrayList();
            string[] taskIds = null;//task ids of operations to be dequeued from other nodes
            if (operations != null && operations.Length > 0 && _dsManager != null)
            {
                try
                {
                    OperationResult[] returnOps = _dsManager.WriteThru(operations, provider, returnSet, context);
                    if (returnOps != null && returnOps.Length > 0)
                    {
                        for (int i = 0; i < operations.Length; i++)//iterate on passed operation array coz we dont have complete info to generate ds operation here.
                        {
                            if (returnSet.ContainsKey(operations[i].Key) && !(returnSet[operations[i].Key] is Exception))//for retry operations
                            {
                                OperationResult.Status status = (OperationResult.Status)returnSet[operations[i].Key];
                                if (status == OperationResult.Status.FailureRetry)
                                {
                                    retryOps.Add(operations[i].Key);
                                    _cacheImpl.Context.NCacheLog.Info("Retrying Write Behind " + operations[i].OperationCode + " operation for key:" + operations[i].Key);
                                    operations[i].OperationState = OperationState.Requeue;
                                    operations[i].OperationDelay = _operationDelay;//for hot apply
                                    operations[i].RetryCount++;
                                    Enqueue(operations[i]);
                                }
                            }

                        }
                    }
                }
                catch (Exception excep)
                {
                    _cacheImpl.Context.NCacheLog.Error("Excecuting Write Behind batch operations ", exc.Message);
                    exc = excep;
                }
                finally
                {
                    for (int i = 0; i < operations.Length; i++)
                    {
                        //populating operations with callbacks entries
                        if (operations[i] != null && operations[i].Entry != null && operations[i].Entry.Value is CallbackEntry)
                            cbEntry = operations[i].Entry.Value as CallbackEntry;
                        if (cbEntry != null)
                        {
                            if (exc != null)//in case of exception
                            {
                                operations[i].Exception = exc;
                                opResult[operations[i].Key] = operations[i];
                                continue;
                            }
                            if (returnSet.ContainsKey(operations[i].Key))
                            {
                                if (returnSet[operations[i].Key] is Exception)
                                {
                                    operations[i].Exception = returnSet[operations[i].Key] as Exception;
                                    opResult[operations[i].Key] = operations[i];
                                }
                                else
                                {
                                    OperationResult.Status status = (OperationResult.Status)returnSet[operations[i].Key];
                                    if (status != OperationResult.Status.FailureRetry)
                                    {
                                        operations[i].DSOpState = status;
                                        opResult[operations[i].Key] = operations[i];
                                    }
                                }
                            }
                            else
                            {
                                operations[i].DSOpState = OperationResult.Status.Success;
                                opResult[operations[i].Key] = operations[i];
                            }
                        }
                        //populating operations with taskids other than retry
                        if (!retryOps.Contains(operations[i].Key))
                            taskList.Add(operations[i].TaskId);
                    }
                    try
                    {
                        if (taskList.Count > 0)
                        {
                            taskIds = new string[taskList.Count];
                            Array.Copy(taskList.ToArray(), 0, taskIds, 0, taskList.Count);
                        }
                        _cacheImpl.NotifyWriteBehindTaskStatus(opResult, taskIds, provider, context);//taskids:to dequeue all operations on other nodes.
                    }
                    catch { }
                }
            }
        }

        private Dictionary<string, WriteOperation> CompileResult(OperationResult[] returnOps)
        {
            Dictionary<string, WriteOperation> result = new Dictionary<string, WriteOperation>();
            if (returnOps == null) return result;
            for (int i = 0; i < returnOps.Length; i++)
            {
                if (returnOps[i] != null)
                    result.Add(returnOps[i].Operation.Key, returnOps[i].Operation);
            }
            return result;
        }
        private void NotifyWriteBehindCompletion(DSWriteBehindOperation operation, Hashtable result)
        {
            CallbackEntry cbEntry = null;
            if (operation.Entry != null && operation.Entry.Value is CallbackEntry)
                cbEntry = operation.Entry.Value as CallbackEntry;
            try
            {
                _cacheImpl.NotifyWriteBehindTaskStatus(operation.OperationCode, result, cbEntry, operation.TaskId, operation.ProviderName, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
            }
            catch { }
        }

        DSWriteBehindOperation[] SortProviders(ArrayList operations, string provider)
        {
            ArrayList selectedOps = new ArrayList();
            for (int i = 0; i < operations.Count; i++)
            {
                DSWriteBehindOperation operation = operations[i] as DSWriteBehindOperation;
                if (operation.ProviderName.ToLower() == provider.ToLower()) selectedOps.Add(operation);
            }
            if (selectedOps.Count > 0)
            {
                DSWriteBehindOperation[] dsOps = new DSWriteBehindOperation[selectedOps.Count];
                Array.Copy(selectedOps.ToArray(), 0, dsOps, 0, selectedOps.Count);
                return dsOps;
            }
            return null;
        }
        DSWriteBehindOperation[] CreateBatch(DSWriteBehindOperation[] operations, int batchCount, int index, out bool getNext)
        {

            int operationCount = operations.Length;
            DSWriteBehindOperation[] result = null;
            if ((operationCount - index) <= 0)
            {
                getNext = false;
                return null;
            }
            if ((operationCount - index) <= batchCount)
            {
                result = new DSWriteBehindOperation[operationCount - index];
                Array.Copy(operations, index, result, 0, operationCount - index);
                getNext = false;
                return result;
            }
            result = new DSWriteBehindOperation[batchCount];
            Array.Copy(operations, index, result, 0, batchCount);
            getNext = true;
            return result;
        }
        /// <summary>
        /// Thread function, keeps running.
        /// </summary>
        protected void ExecuteAllTaskForSource(object args)
        {
            DSWriteBehindOperation operation = null;
            object[] objs = args as object[];
            string source = objs[0] as string;
            bool execute = (bool)objs[1];

            ArrayList removableIndexes = new ArrayList();
            WaitQueue waitQueue = _queue.WaitQueue;
            for (int i = 0; i < waitQueue.Count; i++)
            {
                try
                {
                    operation = waitQueue[i];

                    if (operation == null)
                    {
                        continue;
                    }
                    if (operation.Source != source)
                    {
                        continue;
                    }
                    removableIndexes.Add(i);
                    if (!execute)
                    {
                        continue;
                    }
                    operation.State = TaskState.Execute;//move this operation to ready queue
                    this.Enqueue(operation);
                }
                catch (ThreadAbortException e)
                {
                    break;
                }
                catch (ThreadInterruptedException e)
                {
                    break;
                }
                catch (Exception e)
                {
                }
            }

            for (int i = removableIndexes.Count - 1; i >= 0; i--)
            {
                _queue.WaitQueue.RemoveAt((int)removableIndexes[i]);
            }
        }
        /// <summary> 
        /// Add task to the queue
        /// </summary>
        /// <param name="task">task</param>
        internal void Enqueue(DSWriteBehindOperation operation)
        {
            lock (this)
            {
                this._queue.Enqueue(operation.Key, false, operation);
                if (!_startTime.HasValue) _startTime = DateTime.Now;
                Monitor.PulseAll(this);
            }
        }

        /// <summary>
        /// Dequeue write behind task from queue with matching taskId
        /// </summary>
        /// <param name="taskId">taskId</param>
        internal void Dequeue(string[] taskId)
        {
            lock (this)
            {
                if (taskId == null) return;
                for (int j = 0; j < taskId.Length; j++)
                {
                    //for operations in por replica
                    if (this._queue.SearchWaitQueue(taskId[j]))
                        return;
                    DSWriteBehindOperation operation = this._queue.Peek();
                    if (operation != null && operation.TaskId.Contains(taskId[j]))
                    {
                        this._queue.Dequeue(false, DateTime.Now);
                    }
                    else//ensure that no such task exists in queue, and all task before it are removed
                    {
                        WriteBehindQueue tempQ = (WriteBehindQueue)this._queue.Clone();
                        for (int i = 0; i < this._queue.Count; i++)
                        {
                            operation = tempQ.Dequeue(false, DateTime.Now);
                            if (operation != null && operation.TaskId.Contains(taskId[j]))
                            {
                                this._queue = tempQ;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the states of all the task which are added from this source
        /// </summary>
        /// <param name="address">address of the node which left the cluster</param>
        internal void NodeLeft(string address)
        {
            lock (this)
            {
                this._queue.UpdateState(address);
            }
        }

        /// <summary>
        /// Update the state of write behing task
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="state"></param>
        internal void SetState(string taskId, TaskState state)
        {
            lock (this)
            {
                this._queue.UpdateState(taskId, state);
            }
        }

        /// <summary>
        /// Update the state of write behing task
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="state"></param>
        internal void SetState(string taskId, TaskState state, Hashtable newTable)
        {
            lock (this)
            {
                this._queue.UpdateState(taskId, state, newTable);
            }
        }

        /// <summary>
        /// Get a clone of current queue
        /// </summary>
        /// <returns>write behind queue</returns>
        internal WriteBehindQueue CloneQueue()
        {
            lock (this)
            {                
                return (WriteBehindQueue)this._queue.Clone();
            }
        }

        internal void ExecuteWaitQueue()
        {
            lock (this)
            {
                this._queue.ExecuteWaitQueue();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="queue"></param>
        internal void MergeQueue(CacheRuntimeContext context, WriteBehindAsyncProcessor.WriteBehindQueue queue)
        {
            lock (this)
            {
                if (queue != null)
                {
                    this._queue.MergeQueue(queue);
                }
            }
        }
        /// <summary>
        /// to maintain throttling rate
        /// </summary>
        /// <param name="num of operations per second"></param>
        /// 
        private void ThrottleOperations(int operationExecuted, bool isBatch)
        {
            TimeSpan interval = DateTime.Now - _startTime.Value;
            int processTime = (int)interval.TotalMilliseconds;
            if (processTime > 1000 || _reset)//reset start time
            {
                _startTime = DateTime.Now;
                _operationCount = 0;
            }
            if (!isBatch)
                _operationCount++;
            else
                _operationCount += operationExecuted;
            //wait for remaining interval
            if (_operationCount > (operationExecuted - 1))
            {
                if (processTime < 1000)
                {
                    Thread.Sleep(1000 - processTime);
                    _startTime = DateTime.Now;
                }
                _reset = true;
                return;
            }
            _reset = false;
        }

        /// <summary>
        /// Bulk check keys, If WB queue contains any of the key.
        /// </summary>
        /// <param name="keys">Array of keys</param>
        /// <returns>True if any key exists, else False.</returns>
        internal bool CheckQueue(string[] keys)
        {
            foreach (string key in keys)
            {
                if (CheckQueue(key))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if WriteBehind queue contains the key.
        /// </summary>
        /// <param name="key">key to check the queue for existance.</param>
        /// <returns>True if queue has the key, else False.</returns>
        internal bool CheckQueue(object key)
        {
            if (this._queue.Contains((string)key))
                return true;

            // check current non-batch operation.
            if (_currentSelectedOperation != null)
                return _currentSelectedOperation.Key == key;

            // Check current selected batch of operations
            if (_currentSelectedBatchOperations != null && _currentSelectedBatchOperations.Count > 0)
            {
                lock (_currentSelectedBatchOperations)
                {
                    foreach (DSWriteBehindOperation op in _currentSelectedBatchOperations)
                    {
                        if (op.Key == key)
                            return true;
                    }
                }
            }
            return false;
        }
    }
}