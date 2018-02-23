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
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    internal class DataSourceReplicationManager :IDisposable
    {
        ClusterCacheBase _parent;
        DatasourceMgr _dsManager;
        string _nextChunkId;
        string _prevChunkId;
        WriteBehindAsyncProcessor.WriteBehindQueue _queue;
     
        ILogger _ncacheLog;
        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }


        public DataSourceReplicationManager(ClusterCacheBase parent, DatasourceMgr dsMgr,ILogger NCacheLog)
        {
            _parent = parent;
            _dsManager = dsMgr;
            _ncacheLog = NCacheLog;
            _queue = new WriteBehindAsyncProcessor.WriteBehindQueue(_parent.Context);
        }

        public void ReplicateWriteBehindQueue()
        {
            if (_parent != null)
            {
                
                if (_dsManager != null) return;
              
                WriteBehindQueueRequest req = new WriteBehindQueueRequest(null, null);
                WriteBehindQueueResponse rsp = null;
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DSReplicationMgr.ReplicatedQueue", "---->started replicating queue");
                while (true)
                {
                    Address coordinator = _parent.Cluster.Coordinator;
                    if (_parent.Cluster.IsCoordinator ) break;
                    if (_parent.Cluster.CurrentSubCluster != null)
                    {
                        if (_parent.Cluster.CurrentSubCluster.IsCoordinator) break;                        
                    }

                    try
                    {
                        if (coordinator != null)
                        {
                            rsp = _parent.TransferQueue(coordinator, req);
                        }
                    }
                    catch (SuspectedException se)
                    {
                        System.Threading.Thread.Sleep(5);//wait until view is changed properly
                        continue;
                    }

                    catch (Runtime.Exceptions.TimeoutException te)
                    {
                        continue;
                    }

                    if (rsp != null)
                    {
                        //install the queue

                        WriteBehindAsyncProcessor.WriteBehindQueue chunkOfQueue = rsp.Queue;
                        if (chunkOfQueue != null)
                        {
                            _queue.MergeQueue(chunkOfQueue);
                        }

                        if (rsp.NextChunkId == null)
                            break;
                        else
                        {
                            req = new WriteBehindQueueRequest(rsp.NextChunkId, null);
                        }

                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DSReplicationMgr.ReplicatedQueue", "received chunk from " + coordinator + " nextchunkId :" + req.NextChunkId);

                    }
                    else
                        break;

                }

                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DSReplicationMgr.ReplicatedQueue", "queue has been transfered");
                if (_queue.Count > 0)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DSReplicationMgr.ReplicatedQueue", "queue count :" + _queue.Count);

                    if (_dsManager._writeBehindAsyncProcess != null)
                    {
                        _dsManager._writeBehindAsyncProcess.MergeQueue(_parent.Context, _queue);
                    }
                }
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DSReplicationMgr.ReplicatedQueue", "---->replication of queue completed");
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_queue != null)
            {
                _queue.Clear();
                _queue = null;
            }
        }

        #endregion
    }
}