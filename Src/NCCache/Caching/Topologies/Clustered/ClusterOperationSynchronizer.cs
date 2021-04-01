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
using System;
using System.Collections;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    class ClusterOperationSynchronizer : IDisposable
    {
        Hashtable _lockTable = new Hashtable();
        Hashtable _pendingRequests = new Hashtable();

        object _sync = new object();
        ClusterService _cluster;

        public ClusterOperationSynchronizer(ClusterService cluster)
        {
            _cluster = cluster;

        }

        public void HandleRequest(AsyncRequst operation)
        {
            bool pooled = false;
            bool poolReq = false;
            bool allowOperation = false;
            ArrayList pendingOperations;

            if (operation != null)
            {
                if (operation.SyncKey != null)
                {
                    lock (_sync)
                    {
                        if (!_lockTable.Contains(operation.SyncKey))
                        {
                            _lockTable.Add(operation.SyncKey, operation.RequsetId);
                            allowOperation = true;
                        }
                        else
                        {
                            if (_pendingRequests.Contains(operation.SyncKey))
                            {
                                pendingOperations = (ArrayList)_pendingRequests[operation.SyncKey];
                            }
                            else
                            {
                                pendingOperations = new ArrayList();
                                _pendingRequests.Add(operation.SyncKey, pendingOperations);
                            }
                            pendingOperations.Add(operation);
                            _pendingRequests[operation.SyncKey] = pendingOperations;
                        }
                    }
                }
                else
                {
                    allowOperation = true;
                }
            }
            if (allowOperation)
                ProcessRequest(operation);
        }

        /// <summary>
        /// Process the request in a thread from the thread pool.
        /// It not only process the current request, but also any pending request
        /// for the same SyncKey(Key)
        /// </summary>
        /// <param name="request"></param>
        public void ProcessRequest(object request)
        {
            AsyncRequst synRequest = null;
            ArrayList pendingRequests = null;
            AsyncRequst pendingRequest = null;
            object result = null;

            try
            {
                if (request != null && request is AsyncRequst)
                {
                    synRequest = request as AsyncRequst;
                    result = _cluster.handleFunction(synRequest.Src, (Function)synRequest.Operation);
                }
            }
            catch (Exception e)
            {
                result = e;
            }
            finally
            {
                if (synRequest != null)
                {
                    if (synRequest.RequsetId >= 0)
                    {
                        _cluster.SendResponse(synRequest.Src, result, synRequest.RequsetId);
                    }


                    if (synRequest.SyncKey != null)
                    {
                        lock (_sync)
                        {

                            if (_pendingRequests.Contains(synRequest.SyncKey))
                            {
                                pendingRequests = (ArrayList)_pendingRequests[synRequest.SyncKey];
                                pendingRequest = (AsyncRequst)pendingRequests[0];
                                _lockTable.Add(synRequest.SyncKey, pendingRequest.RequsetId);
                                pendingRequests.RemoveAt(0);
                                if (pendingRequests.Count == 0)
                                    _pendingRequests.Remove(synRequest.SyncKey);

                            }
                            else
                                _lockTable.Remove(synRequest.SyncKey);
                        }

                        if (pendingRequest != null)
                            ProcessRequest(pendingRequest);
                    }
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_lockTable != null)
            {
                _lockTable.Clear();
                _lockTable = null;
            }

            if (_pendingRequests != null)
            {
                _pendingRequests.Clear();
                _pendingRequests = null;
            }

            _cluster = null;
        }

        #endregion
    }
}