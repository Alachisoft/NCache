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
using System.Diagnostics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.RequestLogging
{
    internal class ClientRequestAccount: ISizable
    {
        private HashVector  requestBook;
        private long lastAcknowledgedId = -1;
        private int size = 2*MemoryUtil.NetLongSize;
        
        public ClientRequestAccount()
        {
            requestBook = new HashVector();
            size += MemoryUtil.NetHashtableOverHead;
        }
    
        public int Count
        {
            get { return requestBook.Count; }
        }

        public void RegisterRequest(long requestId, long commandId, long lastAcknowledged)
        {
            if (requestId > -1)
            {
                CommandLog commands;
                lock (requestBook)
                {
                    if (!requestBook.ContainsKey(requestId))
                    {
                        commands = new CommandLog();
                        requestBook.Add(requestId, commands);
                    }
                    else
                    {
                        commands = (CommandLog) requestBook[requestId];
                        size -= commands.InMemorySize;
                    }
                }

                commands.MakeEntry(commandId);
                size += commands.InMemorySize;
            }
            lastAcknowledgedId = lastAcknowledged;
        }

        public void UpdateRequest(long requestId, long commandId, int newStatus,
            IList serializedResponse)
        {
            if (requestId > -1)
            {
                if (requestBook.ContainsKey(requestId))
                {
                    CommandLog log;
                    lock (requestBook)
                    {
                        log = (CommandLog) requestBook[requestId];
                    }
                    size -= log.InMemorySize;
                    log.UpdateEntry(commandId, newStatus, serializedResponse);
                    size += log.InMemorySize;
                }
            }
        }

        public RequestStatus GetRequestStatus(long requestId, long commandId)
        {
            if (requestId > -1)
            {
                lock (requestBook)
                {
                    if (requestBook.ContainsKey(requestId))
                        return ((CommandLog) requestBook[requestId]).GetEntry(commandId);
                }
            }
            return new RequestStatus(Common.Enum.RequestStatus.NOT_RECEIVED);
        }

        public int Clean(int cleanupInterval)
        {
            int removeCount = 0;
            DateTime currentTime = DateTime.Now.Subtract(new TimeSpan(0, 0, cleanupInterval));
            CommandLog reference;
            lock (requestBook)
            {
                long[] requests = new long[requestBook.Count];
                requestBook.Keys.CopyTo(requests, 0);
                foreach (long request in requests)
                {
                    reference = (CommandLog) requestBook[request];
                    if (request < lastAcknowledgedId || reference.LastUpdateTime < currentTime)
                    {
                        size -= reference.InMemorySize;
                        requestBook.Remove(request);
                        removeCount++;
                    }
                }
            }
            return removeCount;
        }

        private void cleanLastAcknowledged()
        {
            long[] requests = new long[requestBook.Count];
            requestBook.Keys.CopyTo(requests, 0);
            lock (requestBook)
            {
                foreach (long request in requests)
                    if (request < lastAcknowledgedId)
                    {
                        size -= ((CommandLog) requestBook[request]).InMemorySize;
                        requestBook.Remove(request);
                    }
            }

        }

        public int Size
        {
            get { return size; }
        }

        public int InMemorySize
        {
            get { return size + requestBook.BucketCount; }
        }
    }
}