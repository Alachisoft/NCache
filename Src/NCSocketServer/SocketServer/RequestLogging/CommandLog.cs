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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.RequestLogging
{
    internal class CommandLog: ISizable
    {
        private Dictionary<long, RequestStatus> commandBook;
        private DateTime lastUpdateTime;
        private int size = MemoryUtil.NetDateTimeSize + MemoryUtil.NetIntSize;

        public CommandLog()
        {
            commandBook = new Dictionary<long, RequestStatus>();
            size = MemoryUtil.NetHashtableOverHead;
            lastUpdateTime = DateTime.Now;
        }

        public DateTime LastUpdateTime
        {
            get { return lastUpdateTime; }
        }

        public int Count
        {
            get { return commandBook.Count; }
        }

        public void MakeEntry(long commandId)
        {
            RequestStatus status = new RequestStatus(Common.Enum.RequestStatus.RECEIVED_AND_INEXECUTION);
            lock (commandBook)
            {
                if (!commandBook.ContainsKey(commandId))
                {
                    commandBook.Add(commandId, status);
                    size += status.InMemorySize;
                }
                else
                    commandBook[commandId] = status;
            }
            
            lastUpdateTime = DateTime.Now;
        }

        public void UpdateEntry(long commandId, int newStatus, IList serializedResponse)
        {
            RequestStatus status;
            lock (commandBook)
            {
                if (commandBook.TryGetValue(commandId, out status))
                {
                    size -= status.InMemorySize;
                    status.Status = newStatus;
                    status.RequestResult = serializedResponse;
                    lastUpdateTime = DateTime.Now;
                    size += status.InMemorySize;
                }
            }
        }

        public RequestStatus GetEntry(long commandId)
        {
            RequestStatus status;
            lock (commandBook)
            {
                if (commandBook.TryGetValue(commandId, out status))
                    return status;
            }
            return new RequestStatus(Common.Enum.RequestStatus.NOT_RECEIVED);
        }



        public int Size
        {
            get { return size + commandBook.Count + MemoryUtil.NetDateTimeSize; }
        }

        public int InMemorySize
        {
            get { return Size; }
        }
    }
}