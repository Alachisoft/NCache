// Copyright (c) 2015 Alachisoft
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
        protected long _threshold = 50 * 1024;//200 * 1000;

        internal ClusterCacheBase _parent;

        protected int _currentBucket = -1;

        protected ArrayList _keyList;

        protected Hashtable _keyUpdateLogTbl = new Hashtable();

        protected int _keyCount;

        protected bool _sendLogData = false;
        
		 int _lastTxfrId = 0;
		 private DistributionManager _distMgr;
		 private Address _clientNode;
		 private ArrayList _logableBuckets = new ArrayList();
         private byte _transferType;

        private bool _isBalanceDataLoad = false;

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
		 }


		 public StateTxfrInfo TransferBucket(ArrayList bucketIds, bool sparsedBuckets, int expectedTxfrId)
		 {    
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
				 return GetData(bucketIds);
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
						 //get its key list from parent.
						 _currentBucket = (int)bucketIds[0];
                         bool enableLogs = _transferType == StateTransferType.MOVE_DATA ? true : false;
                         ArrayList keyList = _parent.InternalCache.GetKeyList(_currentBucket, enableLogs);
						 _logableBuckets.Add(_currentBucket);

						 if (keyList != null)
							 _keyList = keyList.Clone() as ArrayList;

						 //muds:
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
					 return new StateTxfrInfo(new Hashtable(),null,null, true);
				 }

				
				 //take care that we need to send data in chunks if 
				 //bucket is too large.
				 return GetData(_currentBucket);
			 }
		 }

         protected StateTxfrInfo GetData(ArrayList bucketIds)
		 {
			 try
			 {
				 object[] keys = null;
				 Hashtable data = null;
				 Hashtable result = new Hashtable();
                 ArrayList payLoad = new ArrayList();
                 ArrayList payLoadCompilationInfo = new ArrayList();

				 if (!_sendLogData)
				 {
					 IEnumerator ie = bucketIds.GetEnumerator();
					 while (ie.MoveNext())
					 {
						 int bucketId = (int)ie.Current;
                         if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(1)", "transfering data for bucket : " + bucketId);
                         bool enableLogs = _transferType == StateTransferType.MOVE_DATA ? true : false;
                         ArrayList keyList = _parent.InternalCache.GetKeyList(bucketId, enableLogs);
						 _logableBuckets.Add(bucketId);

						 data = null;
						 if (keyList != null)
						 {
                             if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(1)", "bucket : " + bucketId + " [" + keyList.Count + " ]");

							 keys = keyList.ToArray();

                             OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                             operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
							 data = _parent.InternalCache.Get(keys,operationContext);
						 }
                        
						 if (data != null && data.Count > 0)
						 {
							 if (result.Count == 0)
							 {
								 result = data.Clone() as Hashtable;
							 }
							 else
							 {
								 IDictionaryEnumerator ide = data.GetEnumerator();
								 while (ide.MoveNext())
								 {
                                     CacheEntry entry = ide.Value as CacheEntry;
                                     UserBinaryObject ubObject = null;
                                     if (entry.Value is CallbackEntry)
                                     {
                                         ubObject = ((CallbackEntry)entry.Value).Value as UserBinaryObject;
                                     }
                                     else
                                         ubObject = entry.Value as UserBinaryObject;

                                     payLoad.AddRange(ubObject.Data);
                                     long size = entry.DataSize;
                                     int index = payLoadCompilationInfo.Add(size);
                                     PayloadInfo payLoadInfo = new PayloadInfo(entry.CloneWithoutValue(), index);
                                     result[ide.Key] = payLoadInfo;
								 }

							 }
						 }
					 }
					 _sendLogData = true;
                     if (_parent.Context.NCacheLog.IsInfoEnabled)
                         _parent.Context.NCacheLog.Info("State Transfer Corresponder", "BalanceDataLoad = " + _isBalanceDataLoad.ToString());
                     if (_isBalanceDataLoad)
                     {
                         _parent.Context.PerfStatsColl.IncrementDataBalPerSecStatsBy(result.Count);
                     }
                     else
                     {
                         _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStatsBy(result.Count);
                     }

					 return new StateTxfrInfo(result,payLoad,payLoadCompilationInfo, false);
				 }
				 else
					 return GetLoggedData(bucketIds);
			 }
			 catch (Exception ex)
			 {
                 _parent.Context.NCacheLog.Error("StateTxfrCorresponder.GetData(1)", ex.ToString());
				 return null;
			 }
		 }

		 protected StateTxfrInfo GetData(int bucketId)
		 {
			 Hashtable result = new Hashtable();
             ArrayList payLoad = new ArrayList();
             ArrayList payLoadCompilationInfo = new ArrayList();

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

                         UserBinaryObject ubObject = null;
                         if (entry.Value is CallbackEntry)
                         {
                             ubObject = ((CallbackEntry)entry.Value).Value as UserBinaryObject;
                         }
                         else
                             ubObject = entry.Value as UserBinaryObject;

                         payLoad.AddRange(ubObject.Data);
                         long entrySize = entry.DataSize;
                         int index = payLoadCompilationInfo.Add(entrySize);
                         PayloadInfo payLoadInfo = new PayloadInfo(entry.CloneWithoutValue(), index);

                         result[key] = payLoadInfo;
						 sizeToSend += size;
					 }
				 }
                 if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "items sent :" + _keyCount);
                 
                 if (_parent.Context.NCacheLog.IsInfoEnabled)
                     _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "BalanceDataLoad = " + _isBalanceDataLoad.ToString()); 

                 if (_isBalanceDataLoad)
                     _parent.Context.PerfStatsColl.IncrementDataBalPerSecStatsBy(result.Count);
                 else
                     _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStatsBy(result.Count);

				 return new StateTxfrInfo(result,payLoad,payLoadCompilationInfo, false,sizeToSend);
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
                 return new StateTxfrInfo(null,null,null, true);
             }
		 }

	
		 protected virtual StateTxfrInfo GetLoggedData(ArrayList bucketIds)
		 {
			 ArrayList updatedKeys = null;
			 ArrayList removedKeys = null;
			 Hashtable logTbl = null;
			 StateTxfrInfo info = null;
			 Hashtable result = new Hashtable();
             ArrayList payLoad = new ArrayList();
             ArrayList payLoadCompilationInfo = new ArrayList();


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
                             UserBinaryObject ubObject = null;
                             if (entry.Value is CallbackEntry)
                             {
                                 ubObject = ((CallbackEntry)entry.Value).Value as UserBinaryObject;
                             }
                             else
                                 ubObject = entry.Value as UserBinaryObject;

                             payLoad.AddRange(ubObject.Data);
                             long size = entry.DataSize;
                             int index = payLoadCompilationInfo.Add(size);
                             PayloadInfo payLoadInfo = new PayloadInfo(entry.CloneWithoutValue(), index);

							 result[key] = payLoadInfo;
						 }
					 }
					 if (removedKeys != null && removedKeys.Count > 0)
					 {
						 for (int i = 0; i < removedKeys.Count; i++)
						 {
							 string key = removedKeys[i] as string;
							 result[key] = null;
						 }
					 }

					 if (!isLoggingStopped)
						 info = new StateTxfrInfo(result,payLoad,payLoadCompilationInfo, false);
					 else
                         info = new StateTxfrInfo(result, payLoad, payLoadCompilationInfo, true);


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
             return new StateTxfrInfo(result, payLoad, payLoadCompilationInfo, true);
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

    #region /                 --- BucketTxfrInfo ---           /

    #endregion

    #region /                 --- BucketTxfrInfo ---           /

    #endregion

    #region /                 --- StateTxfrInfo ---           /

    #endregion
}
