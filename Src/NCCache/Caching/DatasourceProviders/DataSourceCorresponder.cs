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

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    internal class DataSourceCorresponder: IDisposable
    {
        ClusterCacheBase _parent;
        DatasourceMgr _dsManager;
        string _nextChunkId;
        string _prevChunkId;
        WriteBehindAsyncProcessor.WriteBehindQueue _queue;
        const long  CHUNK_SIZE = 1024 *100;
        ILogger _ncacheLog;

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        public DataSourceCorresponder(DatasourceMgr dsManager, ILogger NCacheLog)
        {
            _dsManager = dsManager;
            _ncacheLog = NCacheLog;
        }

        public WriteBehindQueueResponse GetWriteBehindQueue(WriteBehindQueueRequest req)
        {
            WriteBehindQueueResponse rsp = null;

            if (_dsManager != null) return rsp;
            WriteBehindAsyncProcessor.WriteBehindQueue queueChunk = new WriteBehindAsyncProcessor.WriteBehindQueue(_parent.Context);
            int indexOfNextTask = 0;
            long currentChunkSize = 0;
            string nextChunkId = null;

            if (req != null)
            {
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DSReplicationCorr.GetWriteBehindQueue",  "received chunk request; nextchunkId :" + req.NextChunkId);

                DSWriteBehindOperation operation = null;
                if (_queue == null)
                {
                    WriteBehindAsyncProcessor.WriteBehindQueue queue = null; 
                    if (_dsManager._writeBehindAsyncProcess != null)
                    {
                        queue = _dsManager._writeBehindAsyncProcess.CloneQueue();
                    }
                    if (queue != null)
                    {
                        _queue = new WriteBehindAsyncProcessor.WriteBehindQueue(_parent.Context);
                        _queue.MergeQueue(queue);
                    }
                    else 
                        return null;
                }

                if (req.NextChunkId != null)
                {
                    for (int i = 0; i < _queue.Count; i++)
                    {

                        operation = _queue[i] as DSWriteBehindOperation;
                        if (operation != null)
                        {
                            if (operation.TaskId == req.NextChunkId)
                            {
                                indexOfNextTask = i;
                                break;
                            }
                        }
                    }
                }

                for (int i = indexOfNextTask; i < _queue.Count; i++)
                {
                    operation = _queue[i] as DSWriteBehindOperation;
                    if (operation != null)
                    {
                        if (currentChunkSize >= CHUNK_SIZE)
                        {
                            nextChunkId = operation.TaskId;
                            break;
                        }
                        currentChunkSize += operation.Size;
                        queueChunk.Enqueue(operation.Key,true,operation);
                    }
                }

                if (nextChunkId == null)
                {
                    _queue.Clear();
                    _queue = null;
                }
                if(queueChunk.Count >0)
                    rsp = new WriteBehindQueueResponse(queueChunk, nextChunkId, null);
            }

            return rsp;
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