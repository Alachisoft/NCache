// Copyright (c) 2017 Alachisoft
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
using System.Text;
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{

    #region Queue replicator

    public class AsyncItemReplicator : IDisposable
    {
        CacheRuntimeContext _context = null;
        TimeSpan _interval = new TimeSpan(0, 0, 2);
        Thread runner = null;
        OptimizedQueue _queue = new OptimizedQueue();
        private Hashtable _updateIndexKeys = Hashtable.Synchronized(new Hashtable());
        private long _uniqueKeyNumber;
        private int _updateIndexMoveThreshhold = 200;
        private int _moveCount;

        private bool stopped = true;
        private int _bulkKeysToReplicate = 300;


        internal AsyncItemReplicator(CacheRuntimeContext context, TimeSpan interval)
        {
            _bulkKeysToReplicate = ServiceConfiguration.BulkItemsToReplicate;
            this._context = context;
            this._interval = interval;
        }

        /// <summary>
        /// Creates a new Thread and Starts it.
        /// </summary>
        public void Start()
        {
            if (stopped)
            {
                stopped = false;
                runner = new Thread(new ThreadStart(Run));
                runner.IsBackground = true;
                runner.Name = "AsyncItemReplicationThread";
                runner.Start();
            }
        }

        /// <summary>
        /// An operation to update an index on the replica node is queued to be
        /// replicate. These operations are send in bulk to the replica node.
        /// </summary>
        /// <param name="key"></param>
        public void AddUpdateIndexKey(object key)
        {
            lock (_updateIndexKeys.SyncRoot)
            {
                _updateIndexKeys[key] = null;
                _context.PerfStatsColl.IncrementSlidingIndexQueueSizeStats(_updateIndexKeys.Count);
            }
        }

        public void RemoveUpdateIndexKey(object key)
        {
            lock (_updateIndexKeys.SyncRoot)
            {
                _updateIndexKeys.Remove(key);
                _context.PerfStatsColl.IncrementSlidingIndexQueueSizeStats(_updateIndexKeys.Count);
            }
        }

        /// <summary>
        /// Add the key and entry in teh Hashtable for Invalidation by preodic thread.
        /// </summary>
        /// <param name="key">The key of the item to invalidate.</param>
        /// <param name="entry">CacheEntry to Invalidate.</param>
        internal void EnqueueOperation(object key, ReplicationOperation operation)
        {
            try
            {
                if (key == null)
                    key = System.Guid.NewGuid().ToString() + Interlocked.Increment(ref _uniqueKeyNumber);

                _queue.Enqueue(key, operation);

                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("AsyncReplicator.Enque", "queue_size :" + _queue.Count);
                _context.PerfStatsColl.IncrementMirrorQueueSizeStats(_queue.Count);

            }
            catch (Exception e)
            {
                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("AsyncItemReplicator", string.Format("Exception: {0}", e.ToString()));
            }
        }

        /// <summary>
        /// Clears the Queue of any keys for replication.
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
            _context.PerfStatsColl.IncrementMirrorQueueSizeStats(_queue.Count);
        }

        /// <summary>
        /// Clears the Queue of any keys for replication.
        /// </summary>
        internal void EnqueueClear(ReplicationOperation operation)
        {
            _queue.Clear();
            this.EnqueueOperation("NcAcHe$Cl@Ea%R", operation);
        }

        private object[] GetIndexOperations()
        {
            object[] keys = null;

            lock (_updateIndexKeys.SyncRoot)
            {
                _moveCount++;
                if (_updateIndexKeys.Count >= _updateIndexMoveThreshhold || _moveCount > 2)
                {
                    if (_updateIndexKeys.Count > 0)
                    {
                        keys = new object[_updateIndexKeys.Count];
                        IDictionaryEnumerator ide = _updateIndexKeys.GetEnumerator();

                        int index = 0;
                        while (ide.MoveNext())
                        {
                            keys[index] = ide.Key;
                            index++;
                        }
                    }
                    _moveCount = 0;
                    _updateIndexKeys.Clear();
                }
            }
            return keys;
        }
        /// <summary>
        /// replication thread function.
        /// note: While replicating operations, a dummy '0' sequence id is passed.
        /// this sequence id is totally ignored by asynchronous POR, but we are keeping it
        /// to maintain the symmetry in API.
        /// </summary>
        public void Run()
        {
            //reload threashold value from service config, consider the probability that values would have been changed by user
            _bulkKeysToReplicate = ServiceConfiguration.BulkItemsToReplicate;

            ArrayList opCodesToBeReplicated = new ArrayList(_bulkKeysToReplicate);
            ArrayList infoToBeReplicated = new ArrayList(_bulkKeysToReplicate);
            ArrayList compilationInfo = new ArrayList(_bulkKeysToReplicate);
            ArrayList userPayLoad = new ArrayList();            

            try
            {
                while (!stopped || _queue.Count > 0)
                {
                    DateTime startedAt = DateTime.Now;
                    DateTime finishedAt = DateTime.Now;

                    try
                    {
                        for (int i = 0; _queue.Count > 0 && i < _bulkKeysToReplicate; i++)
                        {
                            IOptimizedQueueOperation operation = null;
                            operation = _queue.Dequeue();
                            
                            DictionaryEntry entry = (DictionaryEntry)operation.Data;
                            opCodesToBeReplicated.Add(entry.Key);
                            infoToBeReplicated.Add(entry.Value);

                            if (operation.UserPayLoad != null)
                            {
                                for (int j = 0; j < operation.UserPayLoad.Length; j++)
                                {
                                    userPayLoad.Add(operation.UserPayLoad.GetValue(j));
                                }
                            }

                            compilationInfo.Add(operation.PayLoadSize);                            
                        }
                        object[] updateIndexKeys = GetIndexOperations();

                        if (!stopped)
                        {
                            if (opCodesToBeReplicated.Count > 0 || updateIndexKeys != null)
                            {
                                if (updateIndexKeys != null)
                                {
                                    opCodesToBeReplicated.Add((int)ClusterCacheBase.OpCodes.UpdateIndice);
                                    infoToBeReplicated.Add(updateIndexKeys);
                                }

                                _context.CacheImpl.ReplicateOperations(opCodesToBeReplicated.ToArray(), infoToBeReplicated.ToArray(), userPayLoad.ToArray(), compilationInfo, _context.CacheImpl.OperationSequenceId, _context.CacheImpl.CurrentViewId);
                            }
                        }

                        if (!stopped && _context.PerfStatsColl != null) _context.PerfStatsColl.IncrementMirrorQueueSizeStats(_queue.Count);
                    }
                    catch (Exception e)
                    {
                        if (e.Message.IndexOf("operation timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _context.NCacheLog.CriticalInfo("AsyncReplicator.Run", "Bulk operation timedout. Retrying the operation.");
                            try
                            {
                                if (!stopped)
                                {
                                    _context.CacheImpl.ReplicateOperations(opCodesToBeReplicated.ToArray(), infoToBeReplicated.ToArray(), userPayLoad.ToArray(), compilationInfo, 0, 0);
                                    _context.NCacheLog.CriticalInfo("AsyncReplicator.Run", "RETRY is successfull.");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error( "AsyncReplicator.RUN", "Error occurred while retrying operation. " + ex.ToString());
                            }
                        }
                        else
                            if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error( "AsyncReplicator.RUN", e.ToString());
                    }
                    finally
                    {
                        opCodesToBeReplicated.Clear();
                        infoToBeReplicated.Clear();
                        compilationInfo.Clear();
                        userPayLoad.Clear();
                        finishedAt = DateTime.Now;
                    }

                    if (_queue.Count > 0)
                        continue;

                    if ((finishedAt.Ticks - startedAt.Ticks) < _interval.Ticks)
                        Thread.Sleep(_interval.Subtract(finishedAt.Subtract(startedAt)));
                    else
                        Thread.Sleep(_interval);
                }
            }
            catch (ThreadAbortException ta)
            {
            }
            catch (ThreadInterruptedException ti)
            {
            }
            catch (NullReferenceException)
            {

            }
            catch (Exception e)
            {
                if (!stopped)
                    _context.NCacheLog.Error("AsyncReplicator.RUN", "Async replicator stopped. " + e.ToString());
            }
        }

        /// <summary>
        /// Stops and disposes the Repliaction thread. The thread can be started using Start method.
        /// <param name="gracefulStop">If true then operations pending in the queue are performed
        /// on the passive node, otherwise stopped instantly </param>
        /// </summary>
        public void Stop(bool gracefulStop)
        {
            stopped = true;
            if (runner != null && runner.IsAlive)
            {
                if (gracefulStop)
                    runner.Join();
                else
                {
                    try
                    {
                        if (runner.IsAlive)
                        {
                            _context.NCacheLog.Flush();
                            runner.Abort();
                        }
                    }
                    catch (Exception) { }

                }
                try
                {
                    Clear();
                }
                catch { }
            }
        }

        /// <summary>
        /// Returns the number of operations in the queue.
        /// </summary>
        public long QueueCount
        {
            get { return _queue.Count; }
        }

        #region IDisposable Members
        /// <summary>
        /// Terminates the replciation thread and Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            Stop(false);
            runner = null;
        }

        #endregion
    }
    #endregion
  
}
