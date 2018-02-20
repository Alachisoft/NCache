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
using System.Collections;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.DataStructures.Clustered;
namespace Alachisoft.NCache.Caching.Queries
{
    internal class ContinuousQueryManager : IDisposable
    {
        private ClusteredList<ContinuousQuery> registeredQueries;

        /// <summary>
        /// 1. server generated query id. this is unique between multiple clients so the same query
        /// is not executed again and again.
        /// 2. client id 
        /// 3. client side generated ids for each instance of ContinuousQuery. This helps
        /// determine if registeration is required or not.
        /// </summary>
        private HashVector clientRefs;


        /// <summary>
        /// Will keep datafilters in the following order
        /// QueryID -> clientID -> dataFilters
        /// 
        /// Keeps the max datafilter to be raised
        /// </summary>
        private HashVector maxAddDFAgainstCID;
        private HashVector maxUpdateDFAgainstCID;
        private HashVector maxRemoveDFAgainstCID;

        /// <summary>
        /// Will keep updated clientUID to be updated at unregistration
        /// ClientID -> ClientQueryUniqueID -> DataFilter
        /// </summary>
        private HashVector addDFAgainstCUniqueID;
        private HashVector updateDFAgainstCUniqueID;
        private HashVector removeDFAgainstCUniqueID;

        private HashVector addNotifications;

        private HashVector updateNotifications;

        private HashVector removeNotifications;

        public ContinuousQueryManager()
        {
            registeredQueries = new ClusteredList<ContinuousQuery>();
            clientRefs = new HashVector();
            addNotifications = new HashVector(); 
            updateNotifications = new HashVector();
            removeNotifications = new HashVector();

            maxAddDFAgainstCID = new HashVector();
            addDFAgainstCUniqueID = new HashVector();

            maxUpdateDFAgainstCID = new HashVector();
            updateDFAgainstCUniqueID = new HashVector();

            maxRemoveDFAgainstCID = new HashVector();
            removeDFAgainstCUniqueID = new HashVector();
        }

        internal ContinuousQueryManagerState GetState()
        {
            ContinuousQueryManagerState state = new ContinuousQueryManagerState();

            state.RegisteredQueries = registeredQueries;
            state.ClientRefs = clientRefs;
            state.AddNotifications = addNotifications;
            state.UpdateNotifications = updateNotifications;
            state.RemoveNotifications = removeNotifications;

            state.MaxAddDFAgainstCID = maxAddDFAgainstCID;
            state.MaxUpdateDFAgainstCID = maxUpdateDFAgainstCID;
            state.MaxRemoveDFAgainstCID = maxRemoveDFAgainstCID;
            state.AddDFAgainstCUID = addDFAgainstCUniqueID;
            state.UpdateDFAgainstCUID = updateDFAgainstCUniqueID;
            state.RemoveDFAgainstCUID = removeDFAgainstCUniqueID;

            return state;
        }

        internal void SetState(ContinuousQueryManagerState state)
        {
            registeredQueries = state.RegisteredQueries;
            clientRefs = state.ClientRefs;
            addNotifications = state.AddNotifications;
            updateNotifications = state.UpdateNotifications;
            removeNotifications = state.RemoveNotifications;

            maxAddDFAgainstCID = state.MaxAddDFAgainstCID;
            maxUpdateDFAgainstCID = state.MaxUpdateDFAgainstCID;
            maxRemoveDFAgainstCID = state.MaxRemoveDFAgainstCID;
            addDFAgainstCUniqueID = state.AddDFAgainstCUID;
            updateDFAgainstCUniqueID = state.UpdateDFAgainstCUID;
            removeDFAgainstCUniqueID = state.RemoveDFAgainstCUID;
        }

        public void Register(ContinuousQuery query, string clientId, string clientUniqueId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, QueryDataFilters datafilters)
        {
            lock (this)
            {
                if (!Exists(query))
                {
                    registeredQueries.Add(query);
                    
                    HashVector refs = new HashVector();
                    ClusteredList<string> clientUniqueIds = new ClusteredList<string>();
                    clientUniqueIds.Add(clientUniqueId);
                    refs[clientId] = clientUniqueIds;

                    clientRefs[query.UniqueId] = refs;

                    RegisterNotifications(notifyAdd, clientId, query.UniqueId, clientUniqueId, addNotifications, datafilters.AddDataFilter, maxAddDFAgainstCID, addDFAgainstCUniqueID);

                    RegisterNotifications(notifyUpdate, clientId, query.UniqueId, clientUniqueId, updateNotifications, datafilters.UpdateDataFilter, maxUpdateDFAgainstCID, updateDFAgainstCUniqueID);

                    RegisterNotifications(notifyRemove, clientId, query.UniqueId, clientUniqueId, removeNotifications, datafilters.RemoveDataFilter, maxRemoveDFAgainstCID, removeDFAgainstCUniqueID);
                }
                else
                {
                    Update(query, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);
                }

            }
        }

        public void Update(ContinuousQuery query, string clientId, string clientUniqueId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, QueryDataFilters datafilters)
        {
            lock (this)
            {
                if (clientRefs.ContainsKey(query.UniqueId))
                {
                    HashVector cRefs = (HashVector)clientRefs[query.UniqueId];
                    if (cRefs.ContainsKey(clientId))
                    {
                        ClusteredList<string> refs = (ClusteredList<string>)cRefs[clientId];
                        if (!refs.Contains(clientUniqueId))
                        {
                            refs.Add(clientUniqueId);
                        }
                    }
                    else
                    {
                        ClusteredList<string> clientUniqueIds = new ClusteredList<string>();
                        clientUniqueIds.Add(clientUniqueId);
                        cRefs[clientId] = clientUniqueIds;
                    }

                    UpdateNotifications(notifyAdd, clientId, query.UniqueId, clientUniqueId, addNotifications, datafilters.AddDataFilter, maxAddDFAgainstCID, addDFAgainstCUniqueID);

                    UpdateNotifications(notifyUpdate, clientId, query.UniqueId, clientUniqueId, updateNotifications, datafilters.UpdateDataFilter, maxUpdateDFAgainstCID, updateDFAgainstCUniqueID);

                    UpdateNotifications(notifyRemove, clientId, query.UniqueId, clientUniqueId, removeNotifications, datafilters.RemoveDataFilter, maxRemoveDFAgainstCID, removeDFAgainstCUniqueID);
                }
            }
        }

        private void UpdateNotifications(bool notify, string clientId, string serverUniqueId, string clientUniqueId, HashVector notifications
            , EventDataFilter datafilters, HashVector CID, HashVector CUID)
        {
            if (notify)
            {
                if (notifications.ContainsKey(serverUniqueId))
                {
                    HashVector clients = notifications[serverUniqueId] as HashVector;
                    if (clients.ContainsKey(clientId)) //New Query from same client
                    {
                        ClusteredList<string> clientQueries = clients[clientId] as ClusteredList<string>;
                        if (!clientQueries.Contains(clientUniqueId))
                        {
                            clientQueries.Add(clientUniqueId);
                        }

                    }
                    else //new client
                    {
                        ClusteredList<string> clientUniqueIds = new ClusteredList<string>();
                        clientUniqueIds.Add(clientUniqueId);
                        clients[clientId] = clientUniqueIds;
                    }
                }
                else //New Query altogether
                {
                    HashVector clients = new HashVector();
                    ClusteredList<string> clientUniqueIds = new ClusteredList<string>();
                    clientUniqueIds.Add(clientUniqueId);
                    clients[clientId] = clientUniqueIds;

                    notifications[serverUniqueId] = clients;
                }

                #region DataFilters registration
                //updating clientUIDs
                HashVector clientUIDs = null;
                
                if (CUID.ContainsKey(clientId))
                {
                    clientUIDs = CUID[clientId] as HashVector;
                }
                else
                    CUID[clientId] = clientUIDs = new HashVector();

                clientUIDs[clientUniqueId] = datafilters;

                //updating Max values
                HashVector clientDF = null;
                if (CID.ContainsKey(serverUniqueId))
                {
                    clientDF = CID[serverUniqueId] as HashVector;
                }
                else
                    CID[serverUniqueId] = clientDF = new HashVector();
                    

                EventDataFilter max = EventDataFilter.None;
                IDictionaryEnumerator ide = clientUIDs.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (((EventDataFilter)ide.Value) > max)
                        max = (EventDataFilter)ide.Value;
                }
                if (!clientDF.ContainsKey(clientId) || ((EventDataFilter)clientDF[clientId]) < max)
                    clientDF[clientId] = max; 
                #endregion
            }
            else //Not to be notified then remove it
            {
                if (notifications.ContainsKey(serverUniqueId))
                {
                    HashVector clients = notifications[serverUniqueId] as HashVector;
                    if (clients.ContainsKey(clientId))
                    {
                        ClusteredList<string> clientQueries = clients[clientId] as ClusteredList<string>;
                        if (clientQueries.Contains(clientUniqueId))
                        {
                            clientQueries.Remove(clientUniqueId);

                            if (clientQueries.Count == 0)
                            {
                                clients.Remove(clientId);
                            }
                        }
                    }
                }

                UnregisterDataFilter(serverUniqueId, clientId, clientUniqueId, CID, CUID);
            }

        }

        private void RegisterNotifications(bool notify, string clientId, string serverUniqueId, string clientUniqueId, HashVector notifications
            , EventDataFilter datafilter, HashVector CID, HashVector CUID)
        {
            if (notify)
            {
                HashVector clients = new HashVector();
                ClusteredList<string> clientUniqueIds = new ClusteredList<string>();
                clientUniqueIds.Add(clientUniqueId);
                clients[clientId] = clientUniqueIds;

                notifications[serverUniqueId] = clients;

                //Adding the max datafilter requirement
                CID[serverUniqueId] = new HashVector();
                ((IDictionary)CID[serverUniqueId]).Add(clientId, datafilter);

                //Adding the cid in the datafilter requirements
                CUID[clientId] = new HashVector();
                ((IDictionary)CUID[clientId]).Add(clientUniqueId, datafilter);
            }
        }

        public ContinuousQuery GetCQ(string commandText, IDictionary values)
        {
            lock (this)
            {
                ContinuousQuery query = new ContinuousQuery(commandText, values);
                if (registeredQueries.Contains(query))
                {
                    int qIndex = registeredQueries.IndexOf(query);
                    return registeredQueries[qIndex];
                }
                else
                {
                    query.UniqueId = GenerateUniqueId();
                }
                return query;
            }
        }

        public bool Exists(ContinuousQuery query)
        {
            lock (this)
            {
                if (registeredQueries.Contains(query))
                {
                    return true;
                }
                return false;
            }
        }

        private string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString();
        }

        public IList GetClients(string queryId)
        {
            lock (this)
            {
                ClusteredArrayList clients = null;
                if (clientRefs.ContainsKey(queryId))
                {
                    HashVector cRefs = (HashVector)clientRefs[queryId];
                    if (cRefs != null && cRefs.Count > 0)
                    {
                        clients = new ClusteredArrayList(cRefs.Keys);
                    }
                    else
                    {
                        clients = new ClusteredArrayList();
                    }
                }
                return clients;
            }
        }

        public bool AllowNotification(string queryId, string clientId, QueryChangeType changeType)
        {
            lock (this)
            {
                if (changeType == QueryChangeType.Add)
                {
                    if (addNotifications.ContainsKey(queryId))
                    {
                        HashVector clients = (HashVector)addNotifications[queryId];
                        if (clients!= null && clients.ContainsKey(clientId))
                        {
                            return true;
                        }
                    }
                }
                else if (changeType == QueryChangeType.Update)
                {
                    if (updateNotifications.ContainsKey(queryId))
                    {
                        HashVector clients = updateNotifications[queryId] as HashVector;
                        if (clients != null && clients.ContainsKey(clientId))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (removeNotifications.ContainsKey(queryId))
                    {
                        HashVector clients = removeNotifications[queryId] as HashVector;
                        if (clients != null && clients.ContainsKey(clientId))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public EventDataFilter GetDataFilter(string serverUID, string clientID, QueryChangeType type)
        {
            HashVector clientDF = null;
            EventDataFilter datafilter = EventDataFilter.None;

            if (type == QueryChangeType.Add)
            { 
                if (maxAddDFAgainstCID.ContainsKey(serverUID))
                {
                    clientDF = maxAddDFAgainstCID[serverUID] as HashVector;
                    if (clientDF.ContainsKey(clientID))
                        datafilter = (EventDataFilter)clientDF[clientID];
                    return datafilter;
                }
                else
                    return EventDataFilter.None;
            }
            else if (type == QueryChangeType.Update)
            {
                if (maxUpdateDFAgainstCID.ContainsKey(serverUID))
                {
                    clientDF = maxUpdateDFAgainstCID[serverUID] as HashVector;

                    if (clientDF.ContainsKey(clientID))
                        datafilter = (EventDataFilter)clientDF[clientID];
                    return datafilter;
                }
                else
                    return EventDataFilter.None;
            }
            else
            {
                if (maxRemoveDFAgainstCID.ContainsKey(serverUID))
                {
                    clientDF = maxRemoveDFAgainstCID[serverUID] as HashVector;

                    if (clientDF.ContainsKey(clientID))
                        datafilter = (EventDataFilter)clientDF[clientID];
                    return datafilter;
                }
                else
                    return EventDataFilter.None;
            }
                
        }

        public bool UnRegister(string serverUniqueId, string clientUniqueId, string clientId)
        {
            lock (this)
            {
                if (clientRefs.ContainsKey(serverUniqueId))
                {
                    HashVector cRefs = (HashVector)clientRefs[serverUniqueId];
                    if (cRefs.ContainsKey(clientId))
                    {
                        ClusteredList<string> refs = (ClusteredList<string>)cRefs[clientId];
                        if (refs.Count > 0)
                        {
                            refs.Remove(clientUniqueId);

                            UnRegisterNotifications(serverUniqueId, clientUniqueId, clientId, addNotifications, maxAddDFAgainstCID, addDFAgainstCUniqueID);
                            UnRegisterNotifications(serverUniqueId, clientUniqueId, clientId, updateNotifications, maxUpdateDFAgainstCID, updateDFAgainstCUniqueID);
                            UnRegisterNotifications(serverUniqueId, clientUniqueId, clientId, removeNotifications, maxRemoveDFAgainstCID, removeDFAgainstCUniqueID);

                            if (refs.Count == 0)
                            {
                                cRefs.Remove(clientId);

                            }
                        }
                        if (cRefs.Count == 0)
                        {
                            int qIndex = -1;
                            foreach (ContinuousQuery query in registeredQueries)
                            {
                                if (query.UniqueId.Equals(serverUniqueId))
                                {
                                    qIndex = registeredQueries.IndexOf(query);
                                    break;
                                }
                            }
                            if (qIndex != -1)
                            {
                                registeredQueries.RemoveAt(qIndex);
                            }
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private void UnRegisterNotifications(string serverUniqueId, string clientUniqueId, string clientId, HashVector notifications
            , HashVector CID, HashVector CUID)
        {
            if (notifications.ContainsKey(serverUniqueId))
            {
                HashVector clients = notifications[serverUniqueId] as HashVector;
                if (clients.ContainsKey(clientId))
                {
                    ClusteredList<string> clientQueries = clients[clientId] as ClusteredList<string>;
                    if (clientQueries.Contains(clientUniqueId))
                    {
                        clientQueries.Remove(clientUniqueId);

                        if (clientQueries.Count == 0)
                        {
                            clients.Remove(clientId); 
                        }
                    }
                }
            }


            UnregisterDataFilter(serverUniqueId, clientId, clientUniqueId, CID, CUID);
                
        }

        private void UnregisterDataFilter(string serverUID, string clientID, string clientUID, HashVector CID, HashVector CUID)
        {
            //updating clientUIDs
            HashVector clientUIDs = null;
            if (CUID.ContainsKey(clientID))
            {
                clientUIDs = CUID[clientID] as HashVector;
                clientUIDs.Remove(clientUID);
                if (clientUIDs.Count == 0)
                {
                    CUID[clientID] = null;
                    clientUIDs = CUID[clientID] as HashVector;
                    CUID.Remove(clientID);
                }
            }

            //updating Max values
            HashVector clientDF = null;
            if (CID.ContainsKey(serverUID))
            {
                clientDF = CID[serverUID] as HashVector;
                EventDataFilter max = EventDataFilter.None;
                if (clientUIDs != null)
                {
                    IDictionaryEnumerator ide = clientUIDs.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (((EventDataFilter)ide.Value) > max)
                            max = (EventDataFilter)ide.Value;
                    }
                    if (!clientDF.ContainsKey(clientID) || ((EventDataFilter)clientDF[clientID]) < max)
                        clientDF[clientID] = max;
                }
                else
                {
                    ((IDictionary)CID[serverUID]).Remove(clientID);
                    if (((IDictionary)CID[serverUID]).Count == 0)
                        CID.Remove(serverUID);
                }
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            Clear();

            if (registeredQueries != null)
            {
                registeredQueries = null;
            }

            if (clientRefs != null)
            {
                clientRefs = null;
            }

            if (addNotifications != null)
            {
                addNotifications = null;
            }

            if (updateNotifications != null)
            {
                updateNotifications = null;
            }

            if (removeNotifications != null)
            {
                removeNotifications = null;
            }

            maxRemoveDFAgainstCID = null;
            removeDFAgainstCUniqueID = null;

            maxAddDFAgainstCID = null;
            addDFAgainstCUniqueID = null;

            maxUpdateDFAgainstCID = null;
            updateDFAgainstCUniqueID = null;

        }

        #endregion

        public void Clear()
        {
        }
    }
}
