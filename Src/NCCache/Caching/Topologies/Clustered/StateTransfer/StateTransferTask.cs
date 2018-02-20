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
using System.Text;
using System.Collections;

using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Queries.Continuous.StateTransfer;
using Alachisoft.NCache.Caching.Exceptions;
#if DEBUGSTATETRANSFER
using Alachisoft.NCache.Caching.Topologies.History;
#endif
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Caching.Messaging;
namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    #region	/                 --- StateTransferTask ---           /

    /// <summary>
    /// Asynchronous state tranfer job.
    /// </summary>
    internal class StateTransferTask
    {
        //enum StateTransferTaskType
        //{
        //    /// <summary>
        //    /// This state transfer removes the buckets from source node after it is completly transfered.
        //    /// This state transfer is done by Group Coordinators to get the redistributed buckets after 
        //    /// AutoBalance or Manual Load Balance.
        //    /// </summary>
        //    Partitioned,

        //    /// <summary>
        //    /// This state transfer does not remove the buckets from the source after it is completly transfered.
        //    /// This stateTransfer is used by Replica nodes to replciate the data from the Group Coordinator.
        //    /// </summary>
        //    PartitionedReplica
        //}

        /// <summary> The partition base class </summary>
        internal ClusterCacheBase _parent = null;

        /// <summary> A promise object to wait on. </summary>
        protected Promise _promise = null;

        /// <summary> 10K is the threshold data size. Above this threshold value, data will be state
        /// transfered in chunks. </summary>
       
        //in future we may need it back.
        protected long _threshold = 0; //10 * 1000; 

        /// <summary>
        /// All the buckets that has less than threshold data size are sparsed.
        /// This is the list of sparsed bucket ids.
        /// </summary>
        protected ArrayList _sparsedBuckets = new ArrayList();

        /// <summary>
        /// All the buckets that has more than threshold data size are filled.
        /// This is the list of the filled buckted ids.
        /// </summary>
        protected ArrayList _filledBuckets = new ArrayList();

        protected bool _isInStateTxfr = false;

        protected System.Threading.Thread _worker;

        protected bool _isRunning;
        protected object _stateTxfrMutex = new object();
		protected Address _localAddress;
		protected int _bktTxfrRetryCount = 3;
		protected ArrayList _correspondingNodes = new ArrayList();
        /// <summary>Flag which determines that if sparsed buckets are to be transferred in bulk or not.</summary>
        protected bool _allowBulkInSparsedBuckets = true;
        protected byte _trasferType = StateTransferType.MOVE_DATA;
        protected bool _logStateTransferEvent;
        protected bool _stateTransferEventLogged;

        /// <summary>
        /// Gets or sets a value indicating whether this task is for Balancing Data Load or State Transfer.
        /// </summary>
        protected bool _isBalanceDataLoad = false;

        /// <summary>
        /// Keep List of Failed keys on main node during state txfr
        /// </summary>
        /// 
        ArrayList failedKeysList = null;
        
        /// <summary>
        /// State Transfer Size is used to control the rate of data transfer during State Tranfer i.e per second tranfer rate in MB
        /// </summary>
        private static long MB = 1024 * 1024;
        protected long stateTxfrDataSizePerSecond = 5 * MB;

        TimeSpan _interval = new TimeSpan(0, 0, 1);

        string _cacheserver="NCache";

        private object _updateIdMutex = new object();
        private ThrottlingManager _throttlingManager;
        private bool _enableGc;
        private long _gcThreshhold = 1024 * MB * 2;//default is 2 Gb
        private long _dataTransferred;
        int updateCount;
      

        /// <summary>
        /// Constructor
        /// </summary>
        protected StateTransferTask()
        {
            _promise = new Promise();
        }

        protected virtual string Name
        {
            get { return "StateTransferTask"; }
        }

        protected virtual bool IsSyncReplica
        {
            get { return false; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent"></param>
		public StateTransferTask(ClusterCacheBase parent,Address localAdd)
		{
			_parent = parent;
			_promise = new Promise();
			_localAddress = localAdd;
                    
            if (ServiceConfiguration.StateTransferDataSizePerSecond > 0)
                stateTxfrDataSizePerSecond = (long)(ServiceConfiguration.StateTransferDataSizePerSecond * MB);

                    
            if (ServiceConfiguration.EnableGCDuringStateTransfer)
                _enableGc = ServiceConfiguration.EnableGCDuringStateTransfer;
            
            _gcThreshhold = ServiceConfiguration.GCThreshold * MB;

            if (_parent != null && _parent.NCacheLog.IsDebugEnabled)
                _parent.NCacheLog.Debug(Name, " explicit-gc-enabled =" + _enableGc + " threshold = " + _gcThreshhold);
		}

        public void Start()
        {
            string instanceName = this.ToString();
            _throttlingManager = new ThrottlingManager(stateTxfrDataSizePerSecond);
            _throttlingManager.Start();
            _worker = new System.Threading.Thread(new System.Threading.ThreadStart(Process));
            _worker.IsBackground = true;
            _worker.Start();
        }

        public void Stop()
        {
            if (_worker != null)
            {
                _parent.Context.NCacheLog.Flush();
                _worker.Abort();
                _worker = null;
            }

            _sparsedBuckets.Clear();
            _filledBuckets.Clear();
        }

		public void DoStateTransfer(ArrayList buckets,bool transferQueue)
		{
            int updateId = 0;

            lock (_updateIdMutex)
            {
                updateId = ++updateCount;
            }
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(UpdateAsync), new object[] { buckets, transferQueue, updateId });
		}

		public void UpdateAsync(object state)
        {
            try
            {
                object[] obj = state as object[];
                ArrayList buckets = obj[0] as ArrayList;
                bool transferQueue = (bool)obj[1];
                int updateId = (int)obj[2];
               

                _parent.DetermineClusterStatus();
                if (!UpdateStateTransfer(buckets, updateId))
                {
                    return;
                }


                if (_parent.HasDisposed)
                    return;

                if (!_isRunning)
                {
                    _logStateTransferEvent = _stateTransferEventLogged = false;

                    Start();
                }
                else
                {
                    _parent.Cluster.ViewInstallationLatch.SetStatusBit(ViewStatus.COMPLETE, ViewStatus.INPROGRESS | ViewStatus.NONE);
                }
                
               

            }
            catch (Exception e)
            {
                _parent.Context.NCacheLog.Error(Name + ".UpdateAsync", e.ToString());
            }

		}

        /// <summary>
        /// Gets or sets a value indicating whether this StateTransfer task is initiated for Data balancing purposes or not.
        /// </summary>
        public bool IsBalanceDataLoad
        {
            get { return _isBalanceDataLoad; }
            set { _isBalanceDataLoad = value; }
        }

        public bool IsRunning
        {
            //get { lock (_stateTxfrMutex) { return _isRunning; } }
            get {return _isRunning; }
            set { lock (_stateTxfrMutex) { _isRunning = value; } }
        }
		/// <summary>
		/// Do the state transfer now.
		/// </summary>
		protected virtual void Process()
		{
			 
			//fetch the latest stats from every node.
			_isRunning = true;

            _parent.Cluster.ViewInstallationLatch.SetStatusBit(ViewStatus.COMPLETE, ViewStatus.INPROGRESS | ViewStatus.NONE);
            object result = null;

			try
			{
                _parent.Cluster.MarkClusterInStateTransfer();
                _parent.DetermineClusterStatus();
                _parent.AnnouncePresence(true);

                try
                {
                    _parent.TransferTopicState();
                    if (_parent.IsCQStateTransfer)
                    {
                        _parent.Context.NCacheLog.CriticalInfo(Name + ".Process", "CQState transfer has started.");
                        ContinuousQueryStateTransferManager cqStateTxfrMgr = new ContinuousQueryStateTransferManager(_parent, _parent.QueryAnalyzer);
                        cqStateTxfrMgr.TransferState(_parent.Cluster.Coordinator);
                        _parent.IsCQStateTransfer = false;
                        _parent.Context.NCacheLog.CriticalInfo(Name + ".Process", "CQState transfer has ended.");
                    }
                }
                catch (Exception ex)
                {
                    _parent.Context.NCacheLog.Error(Name + ".Process", " Transfering Continuous Query State: " + ex.ToString());
                }
                _parent.Context.NCacheLog.CriticalInfo(Name + ".Process", "State Transfer has started.");
                
				
                BucketTxfrInfo info ;
				while (true)
				{

					lock (_stateTxfrMutex)
					{
						info = GetBucketsForTxfr();

						  
						//if no more data to transfer then stop.
						if (info.end)
						{
							_isRunning = false;
							break;
						}
					}

					ArrayList bucketIds = info.bucketIds;
					Address owner = info.owner;
					bool isSparsed = info.isSparsed;

					if (bucketIds != null && bucketIds.Count > 0)
					{
						
                        if (!_correspondingNodes.Contains(owner))
							_correspondingNodes.Add(owner);

						TransferData(bucketIds, owner, isSparsed);
					}
				}
				result = _parent.Local_Count();
			}
			catch (Exception e)
			{
                _parent.Context.NCacheLog.Error(Name + ".Process", e.ToString());
				result = e;
			}
			finally
			{

                //Mark state transfer completed.
                _parent.Cluster.MarkClusterStateTransferCompleted();
                try
                {
                    if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".Process", " Ending state transfer with result : " + result.ToString());

                    _parent.EndStateTransfer(result);

                    if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".Process", " Total Corresponding Nodes: " + _correspondingNodes.Count);
                    foreach (Address corNode in _correspondingNodes)
                    {

                        if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".Process", " Corresponding Node: " + corNode.ToString());
                        _parent.SignalEndOfStateTxfr(corNode);
                    }
                    _isInStateTxfr = false;

                    if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".Process", " Finalizing state transfer");

                    FinalizeStateTransfer();

                    _parent.Context.NCacheLog.CriticalInfo(Name + ".Process", "State transfer has ended");
                    if (_logStateTransferEvent)
                    {
                        AppUtil.LogEvent(_cacheserver, "\"" + _parent.Context.SerializationContext + "(" + _parent.Cluster.LocalAddress.ToString() + ")\"" + " has ended state transfer.", System.Diagnostics.EventLogEntryType.Information, EventCategories.Information, EventID.StateTransferStop);
                    }

                    if (_parent.Context.NCacheLog.IsInfoEnabled)
                    {
                        
                    }
                }
                catch (Exception ex)
                {
                    _parent.Context.NCacheLog.Error(Name + ".Process", ex.ToString());
                }
			}
		}

        protected virtual void FinalizeStateTransfer() { }
		private void TransferData(int bucketId, Address owner,bool sparsedBucket)
		{
			ArrayList tmp = new ArrayList(1);
			tmp.Add(bucketId);
			TransferData(tmp, owner,sparsedBucket);
		}

		protected virtual void TransferData(ArrayList bucketIds, Address owner,bool sparsedBuckets)
		{
			ArrayList ownershipChanged = null;
			ArrayList lockAcquired = null;
			

			 
			//ask coordinator node to lock this/these bucket(s) during the state transfer.
			Hashtable lockResults = AcquireLockOnBuckets(bucketIds);

			if (lockResults != null)
			{
				ownershipChanged = (ArrayList)lockResults[BucketLockResult.OwnerChanged];
				if (ownershipChanged != null && ownershipChanged.Count > 0)
				{
					 
					//remove from local buckets. remove from sparsedBuckets. remove from filledBuckets.
					//these are no more my property.
					IEnumerator ie = ownershipChanged.GetEnumerator();
					while (ie.MoveNext())
					{
                        if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".TransferData", " " + ie.Current.ToString() + " ownership changed");
#if DEBUGSTATETRANSFER
                        _parent.Cluster._history.AddActivity(new Activity("StateTransferTask.TransferData Ownership changed of bucket " + ie.Current.ToString() + ". Should be removed from local buckets."));
#endif
						if (_parent.InternalCache.Statistics.LocalBuckets.Contains(ie.Current))
						{
							lock (_parent.InternalCache.Statistics.LocalBuckets.SyncRoot)
							{
								_parent.InternalCache.Statistics.LocalBuckets.Remove(ie.Current);
							}
						}
					}
				}

				lockAcquired = (ArrayList)lockResults[BucketLockResult.LockAcquired];
				if (lockAcquired != null && lockAcquired.Count > 0)
				{
#if DEBUGSTATETRANSFER
                    _parent.Cluster._history.AddActivity(new Activity("StateTransferTask.TransferData Announcing state transfer for bucket " + lockAcquired[0].ToString() + "."));
#endif
                    failedKeysList = new ArrayList();
                    AnnounceStateTransfer(lockAcquired);
                   
                    StartBucketFilteration(lockAcquired);

					bool bktsTxfrd = TransferBuckets(lockAcquired,ref owner,sparsedBuckets);

                    StopBucketFilteration(lockAcquired);

                    ReleaseBuckets(lockAcquired);

                    RemoveFailedKeysOnReplica();
				}
			}
			else
                if (_parent.Context.NCacheLog.IsErrorEnabled) _parent.Context.NCacheLog.Error(Name + ".TransferData", " Lock acquisition failure");
		}

        protected virtual void StopBucketFilteration(ArrayList lockAcquired)
        {
            if (this._parent.InternalCache != null)
            {                
                var buckets=new System.Collections.Generic.List<int>(new int[]{(int)lockAcquired[0]});
                _parent.InternalCache.StopBucketFilteration(buckets, Common.Queries.FilterType.BucketFilter);
            }
        }

        protected virtual void StartBucketFilteration(ArrayList lockAcquired)
        {
            if (this._parent.InternalCache != null)
                _parent.InternalCache.StartBucketFilteration((int)lockAcquired[0], Common.Queries.FilterType.BucketFilter);
        }

        protected virtual void PrepareBucketsForStateTxfr(ArrayList buckets) { }
        protected virtual void EndBucketsStateTxfr(ArrayList buckets) { }

        protected virtual void AnnounceStateTransfer(ArrayList buckets)
        {
            _parent.AnnounceStateTransfer(buckets);
        }

        protected virtual void ReleaseBuckets(ArrayList lockedBuckets)
        {
            if (_parent != null) _parent.ReleaseBuckets(lockedBuckets);
        }
		/// <summary>
		/// Transfers the buckets from a its owner. We may receive data in chunks.
		/// It is a pull model, a node wanting state transfer a bucket makes request
		/// to its owner.
		/// </summary>
		/// <param name="buckets"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		private bool TransferBuckets(ArrayList buckets,ref Address owner,bool sparsedBuckets)
		{
			bool transferEnd;
			bool successfullyTxfrd = false;
			int expectedTxfrId = 1;
            bool resync = false;
            try
            {
                if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".TransferBuckets", " Starting transfer. Owner : " + owner.ToString() + " , Bucket : " + ((int)buckets[0]).ToString());
                        

                PrepareBucketsForStateTxfr(buckets);
                long dataRecieved = 0;
                long currentIternationData = 0;

                while (true)
                {

                    if (_enableGc && _dataTransferred >= _gcThreshhold)
                    {
                        _dataTransferred = 0;
                        DateTime start = DateTime.Now;
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                        DateTime end = DateTime.Now;
                        TimeSpan diff = end - start;
                        if (_parent.NCacheLog.IsErrorEnabled) _parent.NCacheLog.CriticalInfo(this.Name + ".TransferBucket", "explicit GC called. time taken(ms) :" + diff.TotalMilliseconds + " gcThreshold :" + _gcThreshhold);
                    }
                    else
                        _dataTransferred += currentIternationData;

                    Boolean sleep = false;
                    resync = false;
                    transferEnd = true;
                    StateTxfrInfo info = null;
                    try
                    {
#if DEBUGSTATETRANSFER
                        _parent.Cluster._history.AddActivity(new StateTxferActivity((int)buckets[0], owner, expectedTxfrId));
#endif

                        currentIternationData = 0;
                        info = SafeTransferBucket(buckets, owner, sparsedBuckets, expectedTxfrId);

                        if (info != null)
                        {
                            currentIternationData = info.DataSize;
                            dataRecieved += info.DataSize;
                        }
                    }
                    catch (Runtime.Exceptions.SuspectedException)
                    {
                        resync = true;
                    }
                    catch (Runtime.Exceptions.TimeoutException)
                    {
                       
                    }
                    finally 
                    {
                        
                    }

                    if (resync)
                    {
                        if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".TransferBuckets", owner + " is suspected");
                        Address changedOwner = GetChangedOwner((int)buckets[0], owner);

                        if (changedOwner != null)
                        {
                            if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".TransferBuckets", changedOwner + " is new owner");

#if DEBUGSTATETRANSFER
                            _parent.Cluster._history.AddActivity(new Activity("Owner changed. Bucket : " + (int)buckets[0] + ", Owner : " + changedOwner.ToString()));
#endif
                            if (changedOwner.Equals(owner))
                            {
                                continue;
                            }
                            else
                            {
                                owner = changedOwner;
                                expectedTxfrId = 1;
                                continue;
                            }

                        }
                        else
                        {
                            _parent.Context.NCacheLog.Error(Name + ".TransferBuckets", " Could not get new owner");
                            info = new StateTxfrInfo(true);
                        }
                    }

                    if (info != null)
                    {
                        successfullyTxfrd = true;
                        transferEnd = info.transferCompleted;
                        //next transfer 
                        expectedTxfrId++;
                        //add data to local cache.
                        if (!info.HasLoggedOperations)
                        {
                            if (!info.IsMessageData)
                                AddDataToCache(info);
                            else
                                AddMessagesToCache(info);
                        }
                        else
                        {
                            ArrayList loggedOperations = null;
                            if (info.data != null)
                            {
                                loggedOperations = info.data["$__messageLogs__$"] as ArrayList;
                                info.data.Remove("$__messageLogs__$");

                            }
                            AddDataToCache(info);
                            ApplyLoggedMesssageOperations(loggedOperations);
                        }

                    }
                    else
                        successfullyTxfrd = false;

                    if (transferEnd)
                    {
                        BucketsTransfered(owner, buckets);
                        EndBucketsStateTxfr(buckets);
                         
                        //send ack for the state transfer over.
                        //Ask every node to release lock on this/these bucket(s)

                        if(_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".TransferBuckets", "Acknowledging transfer. Owner : " + owner.ToString() + " , Bucket : "+ ((int)buckets[0]).ToString());

#if DEBUGSTATETRANSFER
                        _parent.Cluster._history.AddActivity(new Activity("Acknowledging transfer. Owner : " + owner.ToString() + " , Bucket : " + ((int)buckets[0]).ToString()));
#endif
                        AcknowledgeStateTransferCompleted(owner, buckets);
                        break;
                    }

                    if (info != null)
                        _throttlingManager.Throttle(info.DataSize);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                EndBucketsStateTxfr(buckets);
                throw;
            }
			return successfullyTxfrd;

		}

        private void AddMessagesToCache(StateTxfrInfo info)
        {
            if (info != null)
            {
                HashVector tbl = info.data;
                CacheEntry entry = null;

                //next transfer 
                 
                //add data to local cache.

                if (tbl != null && tbl.Count > 0)
                {
                    int totalMessages = 0;
                    IDictionaryEnumerator ide = tbl.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (!_stateTransferEventLogged)
                        {
                            AppUtil.LogEvent(_cacheserver, "\"" + _parent.Context.SerializationContext + "(" + _parent.Cluster.LocalAddress.ToString() + ")\"" + " has started state transfer.", System.Diagnostics.EventLogEntryType.Information, EventCategories.Information, EventID.StateTransferStart);
                            _stateTransferEventLogged = _logStateTransferEvent = true;
                        }

                        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                        if (ide.Value != null)
                        {
                            ClusteredArrayList messages = ide.Value as ClusteredArrayList;

                            foreach (TransferrableMessage message in messages)
                            {
                                try
                                {
                                    totalMessages++;
                                    _parent.InternalCache.StoreTransferrableMessage(ide.Key as string, message);
                                }
                                catch (Exception e)
                                {
                                    _parent.Context.NCacheLog.Error(Name + ".AddMessagesToCache", " Can not store message to topic " + ide.Key + " ; message Id: " + message.Message.MessageId + " error : " + e);
                                }
                            }

                        }
                    }

                    if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".AddMessagesToCache", " BalanceDataLoad = " + _isBalanceDataLoad.ToString());
                    
                      _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStatsBy(totalMessages);

                }
            }
        }

        private void ApplyLoggedMesssageOperations(ArrayList loggedOperations)
        {
            if (loggedOperations != null)
            {
                foreach (object operation in loggedOperations)
                {
                    try
                    {
                        _parent.ApplyMessageOperation(operation);
                    }
                    catch (Exception e)
                    {
                        _parent.Context.NCacheLog.Error(Name + ".ApplyLoggedMesssageOperations", " failed to apply logged operation " + operation.GetType().Name + " error : " + e.Message);
                    }
                }
            }
        }
        private void AddDataToCache(StateTxfrInfo info)
        {
            if (info != null)
            {
                HashVector tbl = info.data;
                CacheEntry entry = null;

                //next transfer 
                 
                //add data to local cache.

                if (tbl != null && tbl.Count > 0)
                {
                    IDictionaryEnumerator ide = tbl.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (info.HasLoggedOperations && ide.Key == "$__messageLogs__$")
                            continue;

                        if (!_stateTransferEventLogged)
                        {
                            AppUtil.LogEvent(_cacheserver, "\"" + _parent.Context.SerializationContext + "(" + _parent.Cluster.LocalAddress.ToString() + ")\"" + " has started state transfer.", System.Diagnostics.EventLogEntryType.Information, EventCategories.Information, EventID.StateTransferStart);
                           _stateTransferEventLogged = _logStateTransferEvent = true;
                        }
                        try
                        {
                            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                            if (ide.Value != null)
                            {
                                entry = ide.Value as CacheEntry;


                                CacheInsResultWithEntry result = _parent.InternalCache.Insert(ide.Key, entry, false, false, null, entry.Version, LockAccessType.PRESERVE_VERSION, operationContext);

                                if (result != null && result.Result == CacheInsResult.NeedsEviction)
                                {
                                    failedKeysList.Add(ide.Key);                                    
                                }

                            }
                            else
                            {
                                _parent.InternalCache.Remove(ide.Key, ItemRemoveReason.Removed, false, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                            }
                        }
                        catch (StateTransferException se)
                        {
                            _parent.Context.NCacheLog.Error(Name + ".AddDataToCache", " Can not add/remove key = " + ide.Key + " : value is " + ((ide.Value == null) ? "null" : " not null") + " : " + se.Message);
                        }
                        catch (Exception e)
                        {
                            _parent.Context.NCacheLog.Error(Name + ".AddDataToCache", " Can not add/remove key = " + ide.Key + " : value is " + ((ide.Value == null) ? "null" : " not null") + " : " + e.Message);
                        }
                    }

                    if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".AddDataToCache", " BalanceDataLoad = " + _isBalanceDataLoad.ToString());
                    _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStatsBy(tbl.Count);

                }
            }
        }

        private void RemoveFailedKeysOnReplica()
        {
            try
            {
                if (IsSyncReplica && failedKeysList != null && failedKeysList.Count > 0)
                {                  
                    OperationContext operationContext = new OperationContext(OperationContextFieldName.RemoveOnReplica, true);
                    operationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                    IEnumerator ieFailedKeys = failedKeysList.GetEnumerator();
                    while (ieFailedKeys.MoveNext())
                    {
                        string key = (string)ieFailedKeys.Current;

                        try
                        {
                            _parent.Context.CacheImpl.Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            finally
            {
                failedKeysList = null;
            }
        }


        public virtual void AcknowledgeStateTransferCompleted(Address owner, ArrayList buckets)
        {
            if (owner != null)
            {
                _parent.AckStateTxfrCompleted(owner, buckets);
            }
        }
		/// <summary>
		/// Safely transfer a buckets from its owner. In case timeout occurs we
		/// retry once again.
		/// </summary>
		/// <param name="buckets"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		private StateTxfrInfo SafeTransferBucket(ArrayList buckets, Address owner,bool sparsedBuckets,int expectedTxfrId)
		{
			StateTxfrInfo info = null;
			int retryCount = _bktTxfrRetryCount;

			while (retryCount > 0)
			{
				try
				{
					info = _parent.TransferBucket(buckets, owner,_trasferType, sparsedBuckets,expectedTxfrId, _isBalanceDataLoad);
					return info;
				}
				catch (Runtime.Exceptions.SuspectedException)
				{
					//Member with which we were doing state txfer has left.
                    _parent.Context.NCacheLog.Error(Name + ".SafeTransterBucket", " " + owner + " is suspected during state transfer");
					foreach (int bucket in buckets)
					{
						try
						{
							_parent.EmptyBucket(bucket);
						}
						catch (Exception e)
						{
                            _parent.Context.NCacheLog.Error(Name + ".SafeTransterBucket", e.ToString());
						}
					}
                    throw;	
				}
				catch (Runtime.Exceptions.TimeoutException tout_e)
				{
                    _parent.Context.NCacheLog.Error(Name + ".SafeTransterBucket", " State transfer request timed out from " + owner);
					retryCount--;
                    if (retryCount <= 0)
                        throw;
				}
				catch (Exception e)
				{
                    _parent.Context.NCacheLog.Error(Name + ".SafeTransterBucket", " An error occurred during state transfer " + e.ToString());
					break;
				}
			}
			return info;
		}
		/// <summary>
		/// Acquire locks on the buckets.
		/// </summary>
		/// <param name="buckets"></param>
		/// <returns></returns>
		protected virtual Hashtable AcquireLockOnBuckets(ArrayList buckets)
		{
                int maxTries = 3;
                while (maxTries > 0)
                {
                    try
                    {
                        Hashtable lockResults = _parent.LockBuckets(buckets);
                        return lockResults;
                    }
                    catch (Exception e)
                    {
                        _parent.Context.NCacheLog.Error(Name + ".AcquireLockOnBuckets", "could not acquire lock on buckets. error: " + e.ToString());
                        maxTries--;
                    }
                }
                return null;         
		}

		public virtual BucketTxfrInfo GetBucketsForTxfr()
		{
			ArrayList bucketIds = null;
			Address owner = null;
			int bucketId;
            ArrayList filledBucketIds = null;

			lock (_stateTxfrMutex)
			{
				if (_sparsedBuckets != null && _sparsedBuckets.Count > 0)
				{
					lock (_sparsedBuckets.SyncRoot)
					{
						BucketsPack bPack = _sparsedBuckets[0] as BucketsPack;
						owner = bPack.Owner;
						bucketIds = bPack.BucketIds;
                        if (_allowBulkInSparsedBuckets)
                        {
                            return new BucketTxfrInfo(bucketIds, true, owner);
                        }
                        else
                        {
                            ArrayList list = new ArrayList();
                            list.Add(bucketIds[0]);
                            //Although it is from the sparsed bucket but we intentionally set flag as non-sparsed.
                            return new BucketTxfrInfo(list, false, owner);
                        }
					}
				}
				else if (_filledBuckets != null && _filledBuckets.Count > 0)
				{
					lock (_filledBuckets.SyncRoot)
					{
						BucketsPack bPack = _filledBuckets[0] as BucketsPack;
						owner = bPack.Owner;
						filledBucketIds = bPack.BucketIds;
						if (filledBucketIds != null && filledBucketIds.Count > 0)
						{
							bucketId = (int)filledBucketIds[0];                           
							bucketIds = new ArrayList(1);
							bucketIds.Add(bucketId);
						}
					}
					return new BucketTxfrInfo(bucketIds, false, owner);
				}
				else
					return new BucketTxfrInfo(true);
			}
		}

        /// <summary>
        /// Removes the buckets from the list of transferable buckets after we have
        /// transferred them.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="buckets"></param>
        /// <param name="sparsed"></param>
        protected void BucketsTransfered(Address owner,ArrayList buckets)
        {
            BucketsPack bPack = null;
            lock (_stateTxfrMutex)
            {
                if (_sparsedBuckets != null)
                {
                    BucketsPack dummy = new BucketsPack(null, owner);
                    int index = _sparsedBuckets.IndexOf(dummy);
                    if (index != -1)
                    {
                        bPack = _sparsedBuckets[index] as BucketsPack;
                        foreach (int bucket in buckets)
                        {
                            bPack.BucketIds.Remove(bucket);
                        }
                        if (bPack.BucketIds.Count == 0)
                            _sparsedBuckets.RemoveAt(index);
                    }
                }
                if (_filledBuckets != null)
                {
                    BucketsPack dummy = new BucketsPack(null, owner);
                    int index = _filledBuckets.IndexOf(dummy);
                    if (index != -1)
                    {
                        bPack = _filledBuckets[index] as BucketsPack;
                        foreach (int bucket in buckets)
                        {
                            bPack.BucketIds.Remove(bucket);
                        }
                        if (bPack.BucketIds.Count == 0)
                            _filledBuckets.RemoveAt(index);
                    }
                }
            }
        }

        private BucketStatistics GetBucketStats(int bucketId, Address owner)
        {
            ArrayList nodeInfos = _parent._stats.Nodes.Clone() as ArrayList;
            if (nodeInfos != null)
            {
                IEnumerator ie = nodeInfos.GetEnumerator();
                if (ie != null)
                {
                    while (ie.MoveNext())
                    {
                        NodeInfo tmp = ie.Current as NodeInfo;
                        if (tmp.Address.CompareTo(owner) == 0 && tmp.Statistics != null)
                        {
                            if (tmp.Statistics.LocalBuckets != null)
                            {
                                object obj = tmp.Statistics.LocalBuckets[bucketId];
                                if (obj != null)
                                    return obj as BucketStatistics;
                            }
                            return new BucketStatistics();
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// This method removes all the buckets that were being transferred 
        /// from leaving member and are still incomplete.
        /// This method is required in POR where we can get these incomplete
        /// buckets from other members in the group....
        /// The purpose of removing 
        /// </summary>
        /// <param name="leavingMbr"></param>
        public void ResetStateTransfer()
        {
         
		}

		/// <summary>
		/// Updates the state transfer task in synchronus way. It adds/remove buckets
		/// to be transferred by the state transfer task.
		/// </summary>
		/// <param name="myBuckets"></param>
        public bool UpdateStateTransfer(ArrayList myBuckets, int updateId)
		{
            if (_parent.HasDisposed)
                return false;

            StringBuilder sb = new StringBuilder();
            lock (_updateIdMutex)
            {
                if (updateId != updateCount)
                {
                    if (_parent.NCacheLog.IsErrorEnabled) _parent.Context.NCacheLog.CriticalInfo(this.Name + "UpdateStateTxfr", " Do not need to update the task as update id does not match; provided id :" + updateId + " currentId :" + updateCount);
                    return false;
                }
            }

            lock (_stateTxfrMutex)
            {
                try
                {
                    if (myBuckets != null)
                    {
                        if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".UpdateStateTxfr", " my buckets " + myBuckets.Count);
                        //we work on the copy of the map.
                        ArrayList buckets = myBuckets.Clone() as ArrayList;
                        ArrayList leavingNodes = new ArrayList();

                        if (_sparsedBuckets != null && _sparsedBuckets.Count > 0)
                        {
                            IEnumerator e = _sparsedBuckets.GetEnumerator();

                            lock (_sparsedBuckets.SyncRoot)
                            {
                                while (e.MoveNext())
                                {
                                    BucketsPack bPack = (BucketsPack)e.Current;
                                    ArrayList bucketIds = bPack.BucketIds.Clone() as ArrayList;
                                    foreach (int bucketId in bucketIds)
                                    {
                                        HashMapBucket current = new HashMapBucket(null, bucketId);

                                        if (!buckets.Contains(current))
                                        {
                                            ((BucketsPack)e.Current).BucketIds.Remove(bucketId);
                                        }
                                        else
                                        {
                                            HashMapBucket bucket = buckets[buckets.IndexOf(current)] as HashMapBucket;
                                            if (!bPack.Owner.Equals(bucket.PermanentAddress))
                                            {
                                                //either i have become owner of the bucket or 
                                                //some one else for e.g a replica node 
                                                if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".UpdateStateTxfer", bucket.BucketId + "bucket owner changed old :" + bPack.Owner + " new :" + bucket.PermanentAddress);
                                                bPack.BucketIds.Remove(bucketId);
                                            }
                                        }
                                    }
                                    if (bPack.BucketIds.Count == 0)
                                    {
                                        //This owner has left.
                                        leavingNodes.Add(bPack.Owner);
                                    }

                                }
                                foreach (Address leavigNode in leavingNodes)
                                {
                                    BucketsPack bPack = new BucketsPack(null, leavigNode);
                                    _sparsedBuckets.Remove(bPack);
                                }
                                leavingNodes.Clear();
                            }
                        }

                        if (_filledBuckets != null && _filledBuckets.Count > 0)
                        {
                            IEnumerator e = _filledBuckets.GetEnumerator();
                            lock (_filledBuckets.SyncRoot)
                            {
                                while (e.MoveNext())
                                {
                                    BucketsPack bPack = (BucketsPack)e.Current;
                                    ArrayList bucketIds = bPack.BucketIds.Clone() as ArrayList;
                                    foreach (int bucketId in bucketIds)
                                    {
                                        HashMapBucket current = new HashMapBucket(null, bucketId);
                                        if (!buckets.Contains(current))
                                        {
                                            ((BucketsPack)e.Current).BucketIds.Remove(bucketId);
                                        }
                                        else
                                        {
                                            HashMapBucket bucket = buckets[buckets.IndexOf(current)] as HashMapBucket;
                                            if (!bPack.Owner.Equals(bucket.PermanentAddress))
                                            {
                                                //either i have become owner of the bucket or 
                                                //some one else for e.g a replica node 
                                                bPack.BucketIds.Remove(bucketId);
                                            }
                                        }
                                    }

                                    if (bPack.BucketIds.Count == 0)
                                    {
                                        //This owner has left.
                                        leavingNodes.Add(bPack.Owner);
                                    }

                                }
                                foreach (Address leavigNode in leavingNodes)
                                {
                                    BucketsPack bPack = new BucketsPack(null, leavigNode);
                                    _filledBuckets.Remove(bPack);
                                }
                                leavingNodes.Clear();
                            }
                        }

                        //Now we add those buckets which we have to be state transferred
                        //and are not currently in our list
                        IEnumerator ie = buckets.GetEnumerator();

                        while (ie.MoveNext())
                        {
                            HashMapBucket bucket = ie.Current as HashMapBucket;
                            if (_localAddress.Equals(bucket.TempAddress) && !_localAddress.Equals(bucket.PermanentAddress))
                            {
                                BucketsPack bPack = new BucketsPack(null, bucket.PermanentAddress);

                                if (IsSparsedBucket(bucket.BucketId, bucket.PermanentAddress))
                                {
                                    int index = _sparsedBuckets.IndexOf(bPack);
                                    if (index != -1)
                                    {
                                        bPack = _sparsedBuckets[index] as BucketsPack;
                                    }
                                    else
                                        _sparsedBuckets.Add(bPack);

                                    if (!bPack.BucketIds.Contains(bucket.BucketId))
                                    {
                                        bPack.BucketIds.Add(bucket.BucketId);
                                    }

                                }
                                else
                                {
                                    int index = _filledBuckets.IndexOf(bPack);
                                    if (index != -1)
                                    {
                                        bPack = _filledBuckets[index] as BucketsPack;
                                    }
                                    else
                                        _filledBuckets.Add(bPack);


                                    if (!bPack.BucketIds.Contains(bucket.BucketId))
                                    {
                                        bPack.BucketIds.Add(bucket.BucketId);
                                    }
                                }
                            }
                        }

#if DEBUGSTATETRANSFER
                        ArrayList filledBuckets = new ArrayList();
                        foreach(BucketsPack pack in _filledBuckets) 
                        {
                            filledBuckets.Add(pack.Clone());
                        }
                        ArrayList sparsedBuckets = new ArrayList();
                        foreach(BucketsPack pack in _sparsedBuckets) 
                        {
                            sparsedBuckets.Add(pack.Clone());
                        }
                        _parent.Cluster._history.AddActivity(new StateTxferUpdateActivity(filledBuckets, sparsedBuckets));
#endif
                    }
                }
                catch (NullReferenceException ex)
                {
                    _parent.Context.NCacheLog.Error(Name + ".UpdateStateTxfr", ex.ToString());
                }
                catch (Exception e)
                {
                    _parent.Context.NCacheLog.Error(Name + ".UpdateStateTxfr", e.ToString());
                }
                finally
                {
                    if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".UpdateStateTxfr", " Pulsing waiting thread");
                    System.Threading.Monitor.PulseAll(_stateTxfrMutex);
                }
            }

            return true;
		}
        protected Address GetChangedOwner(int bucket, Address currentOwner)
        {
            Address newOwner = null;
            lock (_stateTxfrMutex)
            {
                while(true)
                {
                    newOwner = GetOwnerOfBucket(bucket);
                    if (newOwner == null) return null;

                    if (newOwner.Equals(currentOwner))
                    {
                        System.Threading.Monitor.Wait(_stateTxfrMutex);
                    }
                    else
                        return newOwner;
                } 
            }
        }

        protected Address GetOwnerOfBucket(int bucket)
        {
            lock (_stateTxfrMutex)
            {
                if (_sparsedBuckets != null)
                {
                    foreach (BucketsPack bPack in _sparsedBuckets)
                    {
                        if (bPack.BucketIds.Contains(bucket))
                            return bPack.Owner;
                    }
                }
                if (_filledBuckets != null)
                {
                    foreach (BucketsPack bPack in _filledBuckets)
                    {
                        if (bPack.BucketIds.Contains(bucket))
                            return bPack.Owner;
                    }
                }
                
            }
            return null;
        }
		/// <summary>
		/// Determines whether a given bucket is sparsed one or not. A bucket is
		/// considered sparsed if its size is less than the threshhold value.
		/// </summary>
		/// <param name="bucketId"></param>
		/// <param name="owner"></param>
		/// <returns>True, if bucket is sparsed.</returns>
		public bool IsSparsedBucket(int bucketId, Address owner)
		{
			bool isSparsed = false;
			BucketStatistics stats = GetBucketStats((int)bucketId, owner);
			isSparsed = stats != null ? stats.DataSize < _threshold : false;
			return isSparsed;

		}

        public void UpdateBuckets()
        {
            lock (_parent._internalCache.Statistics.LocalBuckets.SyncRoot)
            {
                IDictionaryEnumerator ide = _parent._internalCache.Statistics.LocalBuckets.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (_isInStateTxfr)
                    {
                        if (_sparsedBuckets != null && _sparsedBuckets.Count > 0)
                        {
                            ArrayList tmp = _sparsedBuckets.Clone() as ArrayList;
                            IEnumerator e = tmp.GetEnumerator();
                            lock (_sparsedBuckets.SyncRoot)
                            {
                                while (e.MoveNext())
                                {
                                    ArrayList bucketIds = ((BucketsPack)e.Current).BucketIds.Clone() as ArrayList;
                                    foreach (int bucketId in bucketIds)
                                    {
                                        if (!_parent._internalCache.Statistics.LocalBuckets.Contains(bucketId))
                                        {
                                            if (((HashMapBucket)_parent.HashMap[bucketId]).Status != BucketStatus.UnderStateTxfr)
                                                ((BucketsPack)e.Current).BucketIds.Remove(bucketId);
                                        }
                                    }
                                }
                            }
                        }
                        if (_filledBuckets != null && _filledBuckets.Count > 0)
                        {
                            ArrayList tmp = _filledBuckets.Clone() as ArrayList;
                            IEnumerator e = tmp.GetEnumerator();
                            lock (_filledBuckets.SyncRoot)
                            {
                                while (e.MoveNext())
                                {
                                    ArrayList bucketIds = ((BucketsPack)e.Current).BucketIds.Clone() as ArrayList;
                                    foreach (int bucketId in bucketIds)
                                    {
                                        if (!_parent._internalCache.Statistics.LocalBuckets.Contains(bucketId))
                                        {
                                            if (((HashMapBucket)_parent.HashMap[bucketId]).Status != BucketStatus.UnderStateTxfr)
                                                ((BucketsPack)e.Current).BucketIds.Remove(bucketId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Address owner = ((HashMapBucket)_parent.HashMap[(int)ide.Key]).PermanentAddress;
                        BucketsPack bPack = new BucketsPack(null, owner);

                        BucketStatistics bucketStats = GetBucketStats((int)ide.Key, owner);

                        if (bucketStats.Count > 0)
                        {
                            if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info(Name + ".UpdateBuckets()", " Bucket : " + ide.Key + " has " + bucketStats.Count + " items");
                        }

                        if (bucketStats.DataSize < _threshold)
                        {
                            int index = _sparsedBuckets.IndexOf(bPack);
                            if (index != -1)
                            {
                                bPack = _sparsedBuckets[index] as BucketsPack;
                            }

                            bPack.BucketIds.Add(ide.Key);

                            if (!_sparsedBuckets.Contains(bPack))
                            {
                                _sparsedBuckets.Add(bPack);
                            }
                        }
                        else
                        {
                            int index = _filledBuckets.IndexOf(bPack);
                            if (index != -1)
                            {
                                bPack = _filledBuckets[index] as BucketsPack;
                            }

                            bPack.BucketIds.Add(ide.Key);

                            if (!_filledBuckets.Contains(owner))
                            {
                                _filledBuckets.Add(bPack);
                            }
                        }
                    }
                }
            }
        }
    }

#endregion
}
