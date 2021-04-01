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
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System;
using System.Collections.Generic;


namespace Alachisoft.NCache.Caching.Messaging
{
    internal class MessageExpirationIndex
    {
        private HashVector<string, DateTime> _expirationIndex;
        private object _mutex;

        internal int Count { get { lock (_mutex) { return _expirationIndex.Count; } } }

        internal MessageExpirationIndex()
        {
            _expirationIndex = new HashVector<string, DateTime>();
            _mutex = new object();
        }

        internal void Add(string messageId, DateTime time)
        {
            lock (_mutex)
            {
                _expirationIndex.Add(messageId, time);
            }
        }

        internal void Remove(string messageId)
        {
            lock (_mutex)
            {
                _expirationIndex.Remove(messageId);
            }
        }

        internal ClusteredList<string> GetExpiredKeys()
        {
            ClusteredList<string> selectedKeys = new ClusteredList<string>();

            IEnumerator<KeyValuePair<string, DateTime>> em = _expirationIndex.GetEnumerator();

            if (em != null)
            {
                while (em.MoveNext())
                {
                    DateTime absoluteExpiration = em.Current.Value;
                    if (absoluteExpiration <= DateTime.UtcNow)
                    {
                        selectedKeys.Add(em.Current.Key);
                    }
                }
            }

            return selectedKeys;
        }

        public void Clear()
        {
            lock (_mutex)
            {
                _expirationIndex.Clear();
            }
        }
    }
}


