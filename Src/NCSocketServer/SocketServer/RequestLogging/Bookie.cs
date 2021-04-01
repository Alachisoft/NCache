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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Util;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.RequestLogging
{
    internal class Bookie : IDisposable, ISizable
    {
        private HashVector ledger;
        private bool requestLoggingEnabled = false;
        private Statistics.StatisticsCounter _perfStatsCollector;
        private int cleanInterval = 15;
        private Thread cleanupTask;
        private bool isRunning;
        private int size = 2*MemoryUtil.NetIntSize + MemoryUtil.NetByteSize;

        public Bookie(Statistics.StatisticsCounter perfStatsCollector)
        {
            RequestLoggingEnabled = false;

            ledger = new HashVector();
            size += MemoryUtil.NetHashtableOverHead;
            _perfStatsCollector = perfStatsCollector;
            if (RequestLoggingEnabled)
            {
                cleanInterval = 15;
                cleanupTask = new Thread(new ThreadStart(Clean));
                isRunning = true;
                cleanupTask.Start();
                size += 2*MemoryUtil.NetOverHead;
            }
            _perfStatsCollector.RequestLogSize = InMemorySize;
        }

        public bool RequestLoggingEnabled
        {
            get { return requestLoggingEnabled; }
            set { requestLoggingEnabled = value; }
        }

        public int CleanInterval
        {
            get { return cleanInterval; }
            set { cleanInterval = value; }
        }

        public void RegisterRequest(string clientId, long requestId, long commandId, long lastAcknowledged)
        {
            if (RequestLoggingEnabled)
                if (!String.IsNullOrEmpty(clientId))
                {
                    ClientRequestAccount account = new ClientRequestAccount();
                    lock (ledger)
                    {
                        if (!ledger.ContainsKey(clientId))
                        {
                            ledger.Add(clientId, account);
                        }
                        else
                        {
                            account = (ClientRequestAccount) ledger[clientId];
                            
                        }
                    }
                    size -= account.InMemorySize;
                    account.RegisterRequest(requestId, commandId, lastAcknowledged);
                    size += account.InMemorySize;
                    _perfStatsCollector.IncrementRequestLogPerSec();
                    _perfStatsCollector.IncrementRequestLogCount(1);
                    _perfStatsCollector.RequestLogSize = InMemorySize;
                }
        }

        public void UpdateRequest(string clientId, long requestId, long commandId, int newStatus,
            IList serializedResponse)
        {
            if (RequestLoggingEnabled)
                if (!String.IsNullOrEmpty(clientId))
                {
                    if (ledger.ContainsKey(clientId))
                    {
                        ClientRequestAccount clientAccount;
                        lock (ledger)
                        {
                            clientAccount = ((ClientRequestAccount) ledger[clientId]);
                        }
                        size -= clientAccount.InMemorySize;
                        clientAccount.UpdateRequest(requestId, commandId, newStatus, serializedResponse);
                        size += clientAccount.InMemorySize;
                        _perfStatsCollector.RequestLogSize = InMemorySize;
                    }
                }
        }

        public RequestStatus GetRequestStatus(string clientId, long requestId, long commandId)
        {
            if (requestLoggingEnabled)
                if (!String.IsNullOrEmpty(clientId))
                {
                    lock (ledger)
                    {
                        if (ledger.ContainsKey(clientId))
                            return ((ClientRequestAccount) ledger[clientId]).GetRequestStatus(requestId, commandId);
                    }
                }
            return new RequestStatus(Common.Enum.RequestStatus.NOT_RECEIVED);
        }

        public void RemoveClientAccount(string clientId)
        {
            if (RequestLoggingEnabled)
                if (!string.IsNullOrEmpty(clientId))
                    if (ledger.ContainsKey(clientId))
                    {
                        ClientRequestAccount account;
                        lock (ledger)
                        {
                            account = (ClientRequestAccount) ledger[clientId];
                        }
                        size -= account.InMemorySize;
                        ledger.Remove(clientId);
                        size -= clientId.Length * MemoryUtil.NetStringCharSize;
                        _perfStatsCollector.DecrementRequestLogCount(clientId.Length);
                        _perfStatsCollector.RequestLogSize = InMemorySize;
                    }
        }

        public void Clean()
        {
            while (isRunning)
            {
                try
                {
                    if (RequestLoggingEnabled)
                    {
                        string[] keys = new string[ledger.Count];
                        ledger.Keys.CopyTo(keys, 0);
                        for (int i = 0; i < keys.Length; i++)
                        {
                            ClientRequestAccount account = (ClientRequestAccount)ledger[keys[i]];
                            size -= account.InMemorySize;
                            int removed = account.Clean(cleanInterval);
                            _perfStatsCollector.DecrementRequestLogCount(removed);
                            if (account.Count == 0)
                                lock (ledger)
                                {
                                    ledger.Remove(keys[i]);
                                }
                            else
                                size += account.InMemorySize;
                            if (size < 0) size = 0;
                            _perfStatsCollector.RequestLogSize = InMemorySize;
                        }
                        if (ledger.Count == 0)
                        {
                            _perfStatsCollector.RequestLogCount = 0;
                        }
                    }
                    Thread.Sleep(cleanInterval*1000);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            isRunning = false;
            cleanupTask = null;
        }
        

        public int Size
        {
            get { return size; }
        }

        public int InMemorySize
        {
            get { return size + ledger.BucketCount; }
        }
    }
}