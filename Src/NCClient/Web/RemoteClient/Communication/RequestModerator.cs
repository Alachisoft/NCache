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

using System.Collections.Generic;
using System.Threading;

namespace Alachisoft.NCache.Client
{
    internal class RequestModerator
    {
        private Dictionary<string, NodeRequestStatus> statusBook;

        private Timer cleanupTask;

        public RequestModerator()
        {
            statusBook = new Dictionary<string, NodeRequestStatus>();
            cleanupTask = new Timer(Clean, this, 0, 15000);
        }

        public long RegisterRequest(string address, long currentId)
        {
            long returnValue = -1;
            if (address != null)
            {
                lock (statusBook)
                {
                    NodeRequestStatus status;
                    if (!statusBook.TryGetValue(address, out status))
                    {
                        status = new NodeRequestStatus();
                        statusBook.Add(address, status);
                    }
                    lock (status.SyncRoot)
                    {
                        status.RegisterRequest(currentId);
                        returnValue = status.LastAcknowledged;
                    }
                }
            }
            return returnValue;
        }

        public void UnRegisterRequest(long Id)
        {
            lock (statusBook)
            {
                foreach (NodeRequestStatus status in statusBook.Values)
                    status.Acknowledge(Id);
            }
        }

        private void Clean(object obj)
        {
            lock (statusBook)
                foreach (NodeRequestStatus status in statusBook.Values)
                {
                    status.Clean();
                }
        }
    }
}
