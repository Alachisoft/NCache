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
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Caching
{
    internal class ContinuousQueryManager
    {
        static object sync = new object();

        static private List<ContinuousQuery> queries = new List<ContinuousQuery>();

        public static void Register(ContinuousQuery query)
        {
            lock (sync)
            {
                if (!queries.Contains(query))
                {
                    queries.Add(query);
                }
                else
                {
                    queries.Remove(query);
                    queries.Add(query);
                }
            }
        }

        public static void UnRegister(ContinuousQuery query)
        {
            lock (sync)
            {
                queries.Remove(query);
            }
        }

        public static void Notify(string queryId, QueryChangeType changeType, string key, bool notifyAsync,
            EventCacheItem item, EventCacheItem oldItem, BitSet flag, string cacheName, EventDataFilter datafilter)
        {
            ContinuousQuery[] registeredQueries = null;
            lock (sync)
            {
                if (queries.Count == 0) return;
                registeredQueries = new ContinuousQuery[queries.Count];
                queries.CopyTo(registeredQueries);
            }

            foreach (ContinuousQuery query in registeredQueries)
            {
                try
                {
                    if (query.ServerUniqueID.Equals(queryId))
                    {
                        if (changeType == QueryChangeType.Add)
                        {
                            query.FireCQEvents(key, Runtime.Events.EventType.ItemAdded, item, oldItem, notifyAsync,
                                cacheName, flag, datafilter);
                        }
                        else if (changeType == QueryChangeType.Remove)
                        {
                            query.FireCQEvents(key, Runtime.Events.EventType.ItemRemoved, item, oldItem, notifyAsync,
                                cacheName, flag, datafilter);
                        }
                        else
                        {
                            query.FireCQEvents(key, Runtime.Events.EventType.ItemUpdated, item, oldItem, notifyAsync,
                                cacheName, flag, datafilter);
                        }
                    }
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}