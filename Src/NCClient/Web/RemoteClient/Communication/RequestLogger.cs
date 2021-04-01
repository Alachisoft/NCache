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

using Alachisoft.NCache.Common.DataStructures;
using System;

namespace Alachisoft.NCache.Client
{
    internal class RequestLogger
    {
        XrefDictionary _requestTable;
        long _lastAcknowledgedReferenceId = -1;
        long _lastAcknowledgedRequestId = -1;

        object _syncLock = new object();

        public RequestLogger()
        {
            _requestTable = new XrefDictionary();
        }

        /// <summary>
        /// Adds a new request to the table and returns the last removed request id.
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns>Last remove request Id</returns>
        public long LogNewRequest(long requestId)
        {
            lock (_syncLock)
            {
                XReference sourceReferenceId = new XReference(requestId);
                _requestTable.AddSource(sourceReferenceId);

                long smallestRequestId;
                long smallestReferenceId = _requestTable.GetFirstReferenceId(_lastAcknowledgedReferenceId, out smallestRequestId);

                _lastAcknowledgedRequestId = smallestRequestId - 1;
                _lastAcknowledgedReferenceId = smallestReferenceId - 1;
            }

            return _lastAcknowledgedRequestId;
        }

        /// <summary>
        /// Removes the request with specified Id.
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public bool RemoveRequest(long requestId)
        {
            lock (_syncLock)
            {
                XReference sourceReferenceId = new XReference(requestId);
                long referenceId = _requestTable.RemoveSource(sourceReferenceId);
                if (referenceId != -1)
                {
                    if (_lastAcknowledgedReferenceId + 1 == referenceId)
                        _lastAcknowledgedReferenceId = referenceId;
                }
            }

            return true;
        }

        /// <summary>
        /// Expires the old requests
        /// </summary>
        /// <param name="expirationInterval">expiration interval in seconds</param>
        public void Expire(long expirationInterval)
        {
            XReference[] loggedRequests = new XReference[_requestTable.SourceIds.Count];

            lock (_syncLock)
            {
                _requestTable.SourceIds.CopyTo(loggedRequests, 0);
            }

            foreach (XReference refernce in loggedRequests)
            {
                if (refernce != null && refernce.CreationTime.AddSeconds(expirationInterval) < DateTime.Now)
                    RemoveRequest(refernce.SourceId);
            }
        }
    }
}
