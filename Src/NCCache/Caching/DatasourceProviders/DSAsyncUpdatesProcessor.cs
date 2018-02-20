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
using System.Threading;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Runtime.DatasourceProviders;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    /// <summary>
    /// Manager class for Async updates after DS write operations
    /// </summary>
    public class DSAsyncUpdatesProcessor
    {
        
        internal class UpdateQueue : ICloneable
        {
            private ArrayList _queue;
            /// <summary>
            /// Initializes new instance of UpdateQueue
            /// </summary>
            public UpdateQueue()
            {
                this._queue = new ArrayList();
            }

            /// <summary>
            /// Initializes new instance of UpdateQueue
            /// </summary>
            /// <param name="capacity">capacity</param>
            public UpdateQueue(int capacity)
            {
                this._queue = new ArrayList(capacity);
            }
            /// <summary>
            /// Queue a write operation
            /// </summary>
            /// <param name="task">write operation</param>
            public void Enqueue(WriteOperation operationResult)
            {
                lock (this._queue)
                {
                    this._queue.Add(operationResult);
                }
            }

            /// <summary>
            /// Dequeue a write operation
            /// </summary>
            /// <returns>write operation</returns>
            public WriteOperation Dequeue()
            {
                lock (this._queue)
                {
                    if (this._queue.Count == 0) return null;

                    WriteOperation value = this._queue[0] as WriteOperation;
                    this._queue.RemoveAt(0);
                    return value;
                }
            }

            /// <summary>
            /// Get the write operation at top of queue
            /// </summary>
            /// <returns>write operation</returns>
            public WriteOperation Peek()
            {
                lock (this._queue)
                {
                    if (this._queue.Count == 0) return null;
                    return this._queue[0] as WriteOperation;
                    // return operation.Key;
                }
            }

            public WriteOperation this[int index]
            {
                get
                {
                    lock (this._queue)
                    {
                        if (index >= _queue.Count || index < 0) throw new IndexOutOfRangeException();
                        return _queue[index] as WriteOperation;
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
           
            /// <summary>
            /// 
            /// </summary>
            public int Count
            {
                get { lock (this._queue) { return _queue.Count; } }
            }

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

        /// <summary> The worker thread. </summary>
        private Thread _worker;
        private UpdateQueue _updateQueue;
        /// <summary></summary>
        private object _processMutex;
        /// <summary></summary>
        private bool _isDisposing;
        private DatasourceMgr _dsMgr;
        ILogger _ncacheLog;

        private Latch _shutdownStatusLatch = new Latch(ShutDownStatus.NONE);

        private ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }
        /// <summary>
        /// Constructor
        /// </summary>
        internal DSAsyncUpdatesProcessor(DatasourceMgr dsMgr, ILogger NCacheLog)
        {
            this._ncacheLog = NCacheLog;
            this._worker = null;
            this._processMutex = new object();
            this._isDisposing = false;
            this._dsMgr = dsMgr;
            this._updateQueue = new UpdateQueue();
        }

        /// <summary>
        /// Get a value indicating if the processor is running
        /// </summary>
        internal bool IsRunning
        {
            get { return _worker != null && _worker.IsAlive; }
        }

        /// <summary>
        /// Start processing
        /// </summary>
        internal void Start()
        {
            lock (this)
            {
                if (this._worker == null)
                {
                    this._worker = new Thread(new ThreadStart(this.Run));
                    this._worker.IsBackground = true;
                    this._worker.Name = "Data Source AsyncUpdatesProcessor";
                    this._worker.Start();
                }
            }
        }

        /// <summary>
        /// Stop processing.
        /// </summary>
        internal void Stop()
        {
            lock (this)
            {
                Monitor.PulseAll(this);
                lock (this._processMutex)
                {
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
        protected void Run()
        {
            while (this._worker != null && !this._isDisposing)
            {
                WriteOperation operation = null;
                try
                {
                    lock (this)
                    {
                        if (this._updateQueue.Count< 1)
                        {
                            if(_shutdownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                            {
                                _shutdownStatusLatch.SetStatusBit(ShutDownStatus.SHUTDOWN_COMPLETED, ShutDownStatus.SHUTDOWN_INPROGRESS);
                                return;
                            }

                            Monitor.Wait(this);
                        }

                        if (this._updateQueue.Count > 0)
                        {
                            operation = this._updateQueue.Dequeue();
                        }
                        else if (_updateQueue.Count == 0 && _shutdownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                            break;
                    }

                    if (operation == null) continue;
                    lock (_processMutex)
                    {
                        if (!_isDisposing) 
                        {
                            if (_dsMgr != null)
                                _dsMgr.DSAsyncUpdateInCache(operation);
                        }
                    }
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

        }
        internal void Enqueue(WriteOperation operation)
        {
            lock (this)
            {
                this._updateQueue.Enqueue(operation);
                Monitor.PulseAll(this);
            }
        }

        public void WindUpTask()
        {
            _ncacheLog.CriticalInfo("DSAsyncUpdatesProcessor", "WindUp Task Started.");
            if (_updateQueue != null)
                _ncacheLog.CriticalInfo("DSAsyncUpdatesProcessor", "Update processor Queue Count: " + _updateQueue.Count);
            _shutdownStatusLatch.SetStatusBit(ShutDownStatus.SHUTDOWN_INPROGRESS, ShutDownStatus.NONE);
            lock (this)
            {
                Monitor.PulseAll(this);
            }
            _ncacheLog.CriticalInfo("DSAsyncUpdatesProcessor", "WindUp Task Ended.");
        }

        public void WaitForShutDown(long interval)
        {
            _ncacheLog.CriticalInfo("DSAsyncUpdatesProcessor", "Waiting for shutdown task completion.");

            if(_updateQueue.Count > 0)
                _shutdownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED, interval * 1000);

            if (_updateQueue != null && _updateQueue.Count > 0)
                _ncacheLog.CriticalInfo("DSAsyncUpdatesProcessor", "Remaining update processor queue operations: " + _updateQueue.Count);
                
            _ncacheLog.CriticalInfo("DSAsyncUpdatesProcessor", "Shutdown task completed.");
        }
    }
}