//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License


using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;
using System;
using System.Collections;
using System.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// State Txfr corresponder for Mirror and Replicated topology.
    /// Gets the data from cache and sends the data to the joining node in the form of ReplicaStateTxfrInfo.
    /// </summary>
    class ReplicaStateTxfrCorresponder : IDisposable
    {
        /// <summary> 
        /// 200K is the threshold data size. 
        /// Above this threshold value, data will be
        /// transfered in chunks. 
        /// </summary>
        protected long _threshold = 20 * 1024;//200 * 1000;
        protected int _keyCount = 0;
        protected int _collKeyCount = 0;
        internal ClusterCacheBase _parent;
   
        protected object[] _keyList = null;
        protected object[] _collectionsList = null;
        protected bool _collectionStateTransferStarted = false;
        private Stream stream = null;
        private Address _clientNode;

        // <summary> The default operation timeout, to be specified in the configuration. </summary>
        private int _defOpTimeout = 60000;

        public int Timeout
        {
            get { return _defOpTimeout; }
            set { _defOpTimeout = value; }
        }


        internal ReplicaStateTxfrCorresponder(ClusterCacheBase parent, Address requestingNode)
        {
            _parent = parent;
            _clientNode = requestingNode;
            stream = new MemoryStream((int)_threshold);
            _keyCount = 0;
            _collKeyCount = 0;
        }

        internal ReplicaStateTxfrInfo GetData()
        {
            if (_keyList == null)
                _keyList = MiscUtil.GetKeys(_parent.InternalCache, _defOpTimeout);

            if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "state txfr request for cache entry");

            if (_keyList != null && _keyList.Length > 0)
            {
                return  GetCacheItems();
            }
            return new ReplicaStateTxfrInfo(true);
        }

        private ReplicaStateTxfrInfo GetCacheItems()
        {
            long sizeToSend = 0;
            CacheEntry entry = null;
            try
            {
                ReplicaStateTxfrInfo info = null;
                if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "bucket size :" + _keyList.Length);

                if (_keyList.Length == _keyCount && _collectionsList != null && _collectionsList.Length > 0)
                {
                    _keyList = _collectionsList;
                    _collectionStateTransferStarted = true;
                    _collectionsList = null;
                    _keyCount = 0;
                }
                else
                {
                    while (_keyCount < _keyList.Length)
                    {
                        string key = _keyList[_keyCount] as string;

                        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                        operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                        operationContext.UseObjectPool = false;
                        entry = _parent.InternalCache.InternalCache.Get(key, false, operationContext);
                        _keyCount++;
                        if (entry != null)
                        {
                            
                                info = new ReplicaStateTxfrInfo(key, entry, false, sizeToSend, this.stream, DataType.CacheItems);
                                break;
                           
                        }
                    }
                }

                if (_parent.Context.NCacheLog.IsInfoEnabled) _parent.Context.NCacheLog.Info("StateTxfrCorresponder.GetData(2)", "items sent :" + _keyCount);

                _parent.Context.PerfStatsColl.ResetStateTransferPerfmonCounter();
                _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStatsBy(_keyCount);

                if (_collectionStateTransferStarted || (info == null && _collKeyCount > 0))
                    return new ReplicaStateTxfrInfo(false);

                return info != null ? info : new ReplicaStateTxfrInfo(true);
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Global);
            }
        }

   

       

        private void TransferMessages()
        {
            OrderedDictionary topicWiseMessagees = _parent.GetMessageList(0,true);

            if (_parent.NCacheLog.IsInfoEnabled)
                _parent.NCacheLog.Info("StateTransferTask.TransferMessages", " message transfer started");

            if (topicWiseMessagees != null)
            {
                foreach (DictionaryEntry topicWiseMessage in topicWiseMessagees)
                {
                    ClusteredArrayList messageList = topicWiseMessage.Value as ClusteredArrayList;
                    if (_parent.NCacheLog.IsInfoEnabled)
                        _parent.NCacheLog.Info("StateTransferTask.TransferMessages", " topic : " + topicWiseMessage.Key + " messaeg count : " + messageList.Count);

                    foreach (string messageId in messageList)
                    {
                        try
                        {
                            TransferrableMessage message = _parent.GetTransferrableMessage(topicWiseMessage.Key as string, messageId);

                            if (message != null)
                            {
                                _parent.InternalCache.StoreTransferrableMessage(topicWiseMessage.Key as string, message);
                            }
                        }
                        catch (Exception e)
                        {
                            _parent.NCacheLog.Error("StateTransferTask.TransferMessages", e.ToString());
                        }
                    }
                }
            }

            if (_parent.NCacheLog.IsInfoEnabled)
                _parent.NCacheLog.Info("StateTransferTask.TransferMessages", " message transfer ended");
        }

        public void Dispose()
        {
            _collKeyCount = 0;
            _parent = null;
           
            _keyList = null;
            _collectionsList = null;
            _collectionStateTransferStarted = false;
            stream = null;
        }
    }
}
