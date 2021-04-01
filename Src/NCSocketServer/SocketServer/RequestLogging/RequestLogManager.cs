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
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.Common.Util;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.RequestLogging
{
    class RequestLogManager
    {
        Dictionary<string, Dictionary<long, RequestStatus>> _clientRequestLog;
        //Dictionary<string, long> _lastRemovedRequestId;
        Thread expireOldEntries;

        const int NO_REQUEST_REMOVED = -1;
        int _logCleanInterval = 120;
        private StatisticsCounter _statsCollector;

        public RequestLogManager(StatisticsCounter statsCollector)
        {
            _statsCollector = statsCollector;
        }

        /// <summary>
        /// Gets the flag which indicates whether logging is eanbled on this server or not.
        /// </summary>
        public bool LoggingEnabled { get { return false; } }

        public int ExpirationInterval
        {
            get { return _logCleanInterval; }
        }

        public void RegisterRequest(string clientId, long requestId, long acknowledgedRequestId)
        {
            if (LoggingEnabled)
            {
                if (!string.IsNullOrEmpty(clientId) && requestId > 0)
                {
                    RequestStatus status = new RequestStatus();
                    status.Status = Alachisoft.NCache.Common.Enum.RequestStatus.RECEIVED_AND_INEXECUTION;
                    //status.RequestLogTime = DateTime.Now;

                    Dictionary<long, RequestStatus> clientRequests;
                    bool found = false;
                    lock (_clientRequestLog)
                    {
                        found = _clientRequestLog.TryGetValue(clientId, out clientRequests);
                        if (!found)
                        {
                            clientRequests = new Dictionary<long, RequestStatus>();
                            _clientRequestLog.Add(clientId, clientRequests);
                        }

                        lock (clientRequests)
                        {
                            if (!clientRequests.ContainsKey(requestId))
                            {
                                clientRequests.Add(requestId, status);
                                _statsCollector.IncrementRequestLogCount(1);
                            }
                            else
                            {
                                clientRequests[requestId] = status;
                            }
                        }
                    }



                    if (acknowledgedRequestId >= 0)
                    {
                        if (found)
                        {
                            RemoveLogRequests(clientRequests, acknowledgedRequestId);
                        }

                    }
                }
            }
        }

        public void RemovePreviousLogRequests(string clientId, long requestId, long acknowledgedRequestId)
        {
            if (LoggingEnabled && !string.IsNullOrEmpty(clientId) && requestId > 0 && acknowledgedRequestId >= 0)
            {
                Dictionary<long, RequestStatus> clientRequests;
                bool found = false;
                lock (_clientRequestLog)
                {
                    found = _clientRequestLog.TryGetValue(clientId, out clientRequests);
                }
                if (found)
                    RemoveLogRequests(clientRequests, acknowledgedRequestId);
            }
        }

        public void UpdateRequestStatus(string clientId, long requestId, int requestStatus, IList SerializedResponsePackets)
        {
            if (LoggingEnabled)
            {
                Dictionary<long, RequestStatus> clientRequests;
                RequestStatus status;
                bool found = false;
                long size = 0;

                if (!string.IsNullOrEmpty(clientId) && requestId > 0)
                {
                    lock (_clientRequestLog)
                    {
                        found = _clientRequestLog.TryGetValue(clientId, out clientRequests);
                    }
                    if (found)
                    {
                        bool requestFound;
                        lock (clientRequests)
                        {
                            requestFound = clientRequests.TryGetValue(requestId, out status);
                            if (requestFound)
                            {
                                status.Status = requestStatus;
                                status.RequestResult = SerializedResponsePackets;
                            }
                        }
                    }
                }
            }
        }

        public RequestStatus GetRequestStatus(string clientId, long requestId)
        {
            Dictionary<long, RequestStatus> clientRequests;
            RequestStatus requestStatus = null;
            bool found = false;

            if (LoggingEnabled)
            {
                if (!string.IsNullOrEmpty(clientId) && requestId > 0)
                {
                    lock (_clientRequestLog)
                    {
                        found = _clientRequestLog.TryGetValue(clientId, out clientRequests);
                    }
                    if (found)
                    {
                        lock (clientRequests)
                        {
                            found = clientRequests.TryGetValue(requestId, out requestStatus);
                            if (found)
                                return requestStatus;
                        }
                    }

                }
            }
            return requestStatus ?? new RequestStatus(Alachisoft.NCache.Common.Enum.RequestStatus.NOT_RECEIVED);
        }

        /// <summary>
        /// cleans up old enteries
        /// </summary>
        private void ExpireOldEntries()
        {
            while (true)
            {
                try
                {
                    List<string> allClientKeys = null;

                    lock (_clientRequestLog)
                    {
                        allClientKeys = new List<string>(_clientRequestLog.Keys);
                    }

                    if (allClientKeys != null)
                    {
                        int count = allClientKeys.Count;

                        while (count > 0)
                        {
                            string clientId = allClientKeys[count - 1];
                            Dictionary<long, RequestStatus> clientRequests;
                            bool found = _clientRequestLog.TryGetValue(clientId, out clientRequests);
                            if (found)
                            {
                                List<long> allRequestKeys = new List<long>(clientRequests.Keys); ;
                                int reqCount = allRequestKeys.Count;
                                while (reqCount > 0)
                                {
                                    RequestStatus status;
                                    long reqId = allRequestKeys[reqCount - 1];
                                    bool requestFound = clientRequests.TryGetValue(reqId, out status);
                                    if (requestFound)
                                    {

                                    }

                                    reqCount--;
                                }

                                lock (_clientRequestLog)
                                {
                                    lock (clientRequests)
                                    {
                                        if (clientRequests.Count == 0)
                                        {
                                            _clientRequestLog.Remove(clientId);
                                        }
                                    }
                                }
                            }

                            count--;
                        }
                    }
                    Thread.Sleep(10000);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (Exception ex)
                { }
            }
        }

        void RemoveRequests(Dictionary<long, RequestStatus> clientRequests, string clientId, long from, long to, ref long lastRemovedId)
        {
            while (from <= to)
            {
                lock (clientRequests)
                {
                    bool exist = clientRequests.Remove(from);
                    if (exist)
                    {
                        lastRemovedId = from;
                        _statsCollector.DecrementRequestLogCount();
                    }

                    from = from + 1;
                    if (from < 0)
                        break;
                }
            }
        }

        void RemoveLogRequests(Dictionary<long, RequestStatus> clientRequests, long acknowledgedRequestId)
        {
            RequestStatus reqStatus;
            long requestId, size = 0;
            List<long> allRequestKeys;
            int iterator;

            lock (clientRequests)
            {
                allRequestKeys = new List<long>(clientRequests.Keys);
            }

            iterator = allRequestKeys.Count;
            while (iterator > 0)
            {
                requestId = allRequestKeys[iterator - 1];
                if (requestId <= acknowledgedRequestId)
                {
                    bool success = clientRequests.TryGetValue(requestId, out reqStatus);
                    if (success)
                    {
                        lock (clientRequests)
                        {
                            success = clientRequests.Remove(requestId);
                        }

                        if (success)
                            _statsCollector.DecrementRequestLogCount();
                    }
                }

                iterator--;
            }
        }
    }
}
