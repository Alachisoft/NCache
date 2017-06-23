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
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Common.Util;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{

    #region /                 --- BucketLockResult ---           /

    #endregion

    #region	/                 --- StateTxfrCorresponder ---           /


    class StateTxfrCorresponder : IDisposable
	{
        /// <summary> 
        /// 200K is the threshold data size. 
        /// Above this threshold value, data will be
        /// transfered in chunks. 
        /// </summary>
        protected long _threshold = 20 * 1024;//200 * 1000;
        protected int _currentBucket = -1;
        protected int _keyCount;
        private int _lastTxfrId = 0;

        internal ClusterCacheBase _parent;
        private DistributionManager _distMgr;

        protected bool _sendLogData;
        private bool _isBalanceDataLoad;
        
        protected Hashtable _keyUpdateLogTbl = new Hashtable();
        private HashVector _result = new HashVector();

        protected ClusteredArrayList _keyList;
        private ArrayList _logableBuckets = new ArrayList();
        private Stream stream = null;
        
        private Address _clientNode;
        private byte _transferType;


        /// <summary>
        /// Gets or sets a value indicating whether this StateTransfer Corresponder is in Data balancing mode or not.
        /// </summary>
        public bool IsBalanceDataLoad
        {
            get { return _isBalanceDataLoad; }
            set { _isBalanceDataLoad = value; }
        }

        internal StateTxfrCorresponder(ClusterCacheBase parent, DistributionManager distMgr, Address requestingNode, byte transferType)
		 {
			 _parent = parent;
			 _distMgr = distMgr;
			 _clientNode = requestingNode;
             _transferType = transferType;
             stream = new MemoryStream((int)_threshold);
		 }


		 public StateTxfrInfo TransferBucket(ArrayList bucketIds, bool sparsedBuckets, int expectedTxfrId)
		 {
             stream.Seek(0, SeekOrigin.Begin);
			 if (bucketIds != null)
			 {
				 for (int i = bucketIds.Count - 1; i >= 0; i--)
				 {
					 int bkId = (int)bucketIds[i];
					 if (_transferType == StateTransferType.MOVE_DATA && !_distMgr.VerifyTemporaryOwnership(bkId, _clientNode))
					 {
						 if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.TransferBucket", bkId + " ownership changed");

					 }
				 }
			 }

			 if (sparsedBuckets)
			 {
                 return new StateTxfrInfo(true);
			 }
			 else
			 {
				 if (bucketIds != null && bucketIds.Count > 0)
				 {
                     foreach (int bucketId in bucketIds)
                     {
                         lock (_parent._bucketStateTxfrStatus.SyncRoot)
                         {
                             _parent._bucketStateTxfrStatus[bucketId] = true;
                         }
                     }

					 if (_currentBucket != (int)bucketIds[0])
					 {
                         if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.TxfrBucket", "bucketid : " + bucketIds[0] + " exptxfrId : " + expectedTxfrId);

						 _lastTxfrId = expectedTxfrId;
						 //request for a new bucket.

						 _currentBucket = (int)bucketIds[0];
                         bool enableLogs = _transferType == StateTransferType.MOVE_DATA ? true : false;
                         _parent.InternalCache.GetKeyList(_currentBucket, enableLogs, out _keyList);
						 _logableBuckets.Add(_currentBucket);

						 //reset the _lastLogTblCount
						 _sendLogData = false;
					 }
					 else
					 {
                         if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.TxfrBucket", "bucketid : " + bucketIds[0] + " exptxfrId : " + expectedTxfrId);
						 //remove all the last sent keys from keylist that has not been
						 //modified during this time.
						 if (_keyList != null && expectedTxfrId > _lastTxfrId)
						 {
							 lock (_keyList.SyncRoot)
							 {
								 _keyList.RemoveRange(0, _keyCount);
								 _keyCount = 0;
							 }
							 _lastTxfrId = expectedTxfrId;
						 }
					 }
				 }
				 else
				 {
                     return new StateTxfrInfo(new HashVector(), false, 0, null);
				 }

				 //take care that we need to send data in chunks if 
				 //bucket is too large.
				 return GetData(_currentBucket);
			 }
		 }

		 protected StateTxfrInfo GetData(int bucketId)
		 {
             if (_result.Count > 0)
             {
                 _result.Clear();
             }

			 long sizeToSend = 0;

             lock (_parent._bucketStateTxfrStatus.SyncRoot)
             {
                 _parent._bucketStateTxfrStatus[bucketId] = true;
             }

             if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "state txfr request for :" + bucketId + " txfrid :" + _lastTxfrId);

			 if (_keyList != null && _keyList.Count > 0)
			 {
                 if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "bucket size :" + _keyList.Count);

				 for (_keyCount = 0; _keyCount < _keyList.Count; _keyCount++)
				 {
					 string key = _keyList[_keyCount] as string;

                     OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                      operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                     CacheEntry entry = _parent.InternalCache.InternalCache.Get(key, false,operationContext);
					 if (entry != null)
					 {
                         long size = (entry.InMemorySize + Common.MemoryUtil.GetStringSize(key));//.DataSize;                         
                         if (sizeToSend > _threshold) break;

                         _result[key] = entry;
						 sizeToSend += size;
					 }
				 }
                 if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "items sent :" + _keyCount);
                 
                 if (_parent.Context.NCacheLog.IsInfoEnabled)
                     _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "BalanceDataLoad = " + _isBalanceDataLoad.ToString()); 

                 if (_isBalanceDataLoad)
                     _parent.Context.PerfStatsColl.IncrementDataBalPerSecStatsBy(_result.Count);
                 else
                     _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStatsBy(_result.Count);

				 return new StateTxfrInfo(_result,false,sizeToSend, this.stream);
			 }
             else if (_transferType == StateTransferType.MOVE_DATA)
             {
                 //We need to transfer the logs.
                 if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "sending log data for bucket: " + bucketId);

                 ArrayList list = new ArrayList(1);
                 list.Add(bucketId);
                 return GetLoggedData(list);
             }
             else
             {
                 //As transfer mode is not MOVE_DATA, therefore no logs are maintained
                 //and hence are not transferred.
                 return new StateTxfrInfo(null, true, 0, null);
             }
		 }

	
		 protected virtual StateTxfrInfo GetLoggedData(ArrayList bucketIds)
		 {
			 ArrayList updatedKeys = null;
			 ArrayList removedKeys = null;
			 Hashtable logTbl = null;
			 StateTxfrInfo info = null;

			 bool isLoggingStopped = false;

			 try
			 {

				 logTbl = _parent.InternalCache.GetLogTable(bucketIds, ref isLoggingStopped);

				 if (logTbl != null)
				 {

					 updatedKeys = logTbl["updated"] as ArrayList;
					 removedKeys = logTbl["removed"] as ArrayList;

					 if (updatedKeys != null && updatedKeys.Count > 0)
					 {
						 for (int i = 0; i < updatedKeys.Count; i++)
						 {
							 string key = updatedKeys[i] as string;
                             OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                             operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                             CacheEntry entry = _parent.InternalCache.Get(key, false, operationContext);
                             _result[key] = entry;
						 }
					 }
					 if (removedKeys != null && removedKeys.Count > 0)
					 {
						 for (int i = 0; i < removedKeys.Count; i++)
						 {
							 string key = removedKeys[i] as string;
							 _result[key] = null;
						 }
					 }

                     if (!isLoggingStopped)
                         info = new StateTxfrInfo(_result, false, 0, this.stream);
                     else
                         info = new StateTxfrInfo(_result, true, 0, this.stream);


                     _parent.Context.NCacheLog.Debug("StateTxfrCorresponder.GetLoggedData()", info == null ? "returning null state-txfr-info" : "returning " + info.data.Count.ToString() + " items in state-txfr-info");
					 return info;
				 }
				 else
                     if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresoponder.GetLoggedData", "no logged data found");
			 }
			 catch (Exception e)
			 {
                 _parent.Context.NCacheLog.Error("StateTxfrCorresoponder.GetLoggedData", e.ToString());
				 throw;
			 }
			 finally
			 {
                
			 }
			 //no operation has been logged during state transfer.
			 //so announce completion of state transfer for this bucket.
             return new StateTxfrInfo(_result, true, 0, this.stream);
		 }

		 #region IDisposable Members

		 /// <summary>
		 /// Disposes the state txfr corresponder. On dispose corresponder should
		 /// stop logger in the hashed cache if it has turned on any one.
		 /// </summary>
		 public void Dispose()
		 {
             if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.Dispose", _clientNode.ToString() + " corresponder disposed");
			 if(_keyList != null) _keyList.Clear();
			 if(_keyUpdateLogTbl != null) _keyUpdateLogTbl.Clear();
             if (_result != null) _result.Clear();

             if (_transferType == StateTransferType.MOVE_DATA)
             {
                 if (_parent != null && _logableBuckets != null)
                 {
                     for (int i = 0; i < _logableBuckets.Count; i++)
                     {
                         if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.Dispose", " removing logs for bucketid " + _logableBuckets[i]);
                         _parent.RemoveFromLogTbl((int)_logableBuckets[i]);
                     }
                 }
             }
		 }

		 #endregion
	 }

    #endregion
}
