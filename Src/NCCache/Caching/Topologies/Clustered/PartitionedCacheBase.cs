// Copyright (c) 2015 Alachisoft
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

using Alachisoft;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using System.Net;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Runtime = Alachisoft.NCache.Runtime;


namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// A class to serve as the base for partitioned clustered cache implementations.
    /// </summary>
    internal class PartitionedCacheBase : ClusterCacheBase
    {
        /// <summary>
        /// An info object that is passed as identity of the members, i.e., additional data with the
        /// Address object. This will help the partition determine legitimate members as well as
        /// gather useful information about member configuration. Load balancer might be a good
        /// consumer of this information.
        /// </summary>
        internal class Identity : NodeIdentity, ICompactSerializable
        {
            public Identity(bool hasStorage, int renderPort, IPAddress renderAddress) : base(hasStorage, renderPort, renderAddress) { }


            #region ICompactSerializable Members

            void ICompactSerializable.Deserialize(CompactReader reader)
            {
                base.Deserialize(reader);
            }

            void ICompactSerializable.Serialize(CompactWriter writer)
            {
                base.Serialize(writer);
            }

            #endregion
        }

        /// <summary> string suffix used to differentiate group name. </summary>
        protected const string MCAST_DOMAIN = ".p20";

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public PartitionedCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
        }

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public PartitionedCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, IClusterEventsListener clusterListener)
            : base(properties, listener, context, clusterListener)
        {
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion

        /// <summary>
        /// Authenticate the client and see if it is allowed to join the list of valid members.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="identity"></param>
        /// <returns>true if the node is valid and belongs to the scheme's cluster</returns>
        public override bool AuthenticateNode(Address address, NodeIdentity identity)
        {
            try
            {
                if (identity == null || !(identity is Identity))
                {
                    Context.NCacheLog.Warn("PartitionedCacheBase.AuthenticateNode()", "A non-recognized node attempted to join cluster -> " + address);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        protected Hashtable Clustered_LockBuckets(ArrayList bucketIds, Address owner, Address targetNode)
        {
            object result = null;
            try
            {
                Function function = new Function((int)OpCodes.LockBuckets, new object[] { bucketIds, owner }, false);
                result = Cluster.SendMessage(targetNode, function, GroupRequest.GET_FIRST, false);

                if (result == null)
                    return null;

                return result as Hashtable;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected void Clustered_AckStateTxfrCompleted(Address targetNode, ArrayList bucketIds)
        {
            try
            {
                Function func = new Function((int)OpCodes.AckStateTxfr, bucketIds, true);
                Cluster.SendMessage(targetNode, func, GroupRequest.GET_NONE);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }



       

        #region	/                 --- Partitioned ICache.Insert ---           /

        /// <summary>
        /// Removes all entries from the cluster.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="handleClear"/> on every server node in the cluster.
        /// </remarks>
        protected void Clustered_Clear(CallbackEntry cbEntry, bool excludeSelf, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.Clear, new object[] { cbEntry, operationContext }, excludeSelf);
                Cluster.BroadcastToServers(func, GroupRequest.GET_ALL, false);
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        #endregion

        #region	/                 --- Partitioned ICache.Add ---           /

        /// <summary>
        /// Add the object to specfied node in the cluster. 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster.
        /// </remarks>
        protected CacheAddResult Clustered_Add(Address dest, object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.Add_1", "");
            CacheAddResult retVal = CacheAddResult.Success;
            try
            {
                Function func = new Function((int)OpCodes.Add, new object[] { key, cacheEntry.CloneWithoutValue(), operationContext });
                Array userPayLoad = null;
                if (cacheEntry.Value is CallbackEntry)
                {
                    CallbackEntry cbEntry = ((CallbackEntry)cacheEntry.Value);
                    userPayLoad = cbEntry.UserData;
                }
                else
                {
                    userPayLoad = cacheEntry.UserData;
                }

                func.UserPayload = userPayLoad;
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                if (result is CacheAddResult)
                    retVal = (CacheAddResult)result;
                else if (result is System.Exception)
                    throw (Exception)result;
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        /// <summary>
        /// Add the ExpirationHint to a specfied node in the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster.
        /// </remarks>
        protected bool Clustered_Add(Address dest, object key, ExpirationHint eh, OperationContext operationContext)
        {
            bool retVal = false;
            try
            {
                Function func = new Function((int)OpCodes.AddHint, new object[] { key, eh, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (bool)result; 
            }
            catch (StateTransferException e)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        /// <summary>
        /// Add the object to specfied node in the cluster. 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="keys"></param>
        /// <param name="cacheEntries"></param>
        /// <param name="operationContext"></param>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster.
        /// </remarks>
        protected Hashtable Clustered_Add(Address dest, object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.AddBlk", "");

            Hashtable retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.Add, new object[] { keys, cacheEntries, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (Hashtable)result; 
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }

            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        #endregion


        protected QueryResultSet Clustered_Search(ArrayList dests, string queryText, IDictionary values, bool excludeSelf, OperationContext operationContext)
        {
            QueryResultSet resultSet = new QueryResultSet();

            try
            {
                Function func = new Function((int)OpCodes.Search, new object[] { queryText, values, operationContext }, excludeSelf);
                RspList results = Cluster.BroadcastToMultiple(dests, func, GroupRequest.GET_ALL, false);

                if (results == null)
                    return null;

                ClusterHelper.ValidateResponses(results, typeof(QueryResultSet), Name);
                ArrayList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(QueryResultSet));

                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        QueryResultSet cRestultSet = (QueryResultSet)rsp.Value;
                        resultSet.Compile(cRestultSet);
                    }
                }

                return resultSet;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        protected QueryResultSet Clustered_SearchEntries(ArrayList dests, string queryText, IDictionary values, bool excludeSelf, OperationContext operationContext)
        {
            QueryResultSet resultSet = new QueryResultSet();

            try
            {
                Function func = new Function((int)OpCodes.SearchEntries, new object[] { queryText, values, operationContext }, excludeSelf);
                RspList results = Cluster.BroadcastToMultiple(dests, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }

                ClusterHelper.ValidateResponses(results, typeof(QueryResultSet), Name);
                ArrayList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(QueryResultSet));

                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        QueryResultSet cResultSet = (QueryResultSet)rsp.Value;
                        resultSet.Compile(cResultSet);

                    }
                }

                return resultSet;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }
        #region	/                 --- Partitioned ICache.Insert ---           /

        /// <summary>
        /// Updates or Adds the object to the cluster. 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry"></param>
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on the specified node.
        /// </remarks>
        protected CacheInsResultWithEntry Clustered_Insert(Address dest, object key, CacheEntry cacheEntry, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.Insert", "");

            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();
            try
            {
                Function func = new Function((int)OpCodes.Insert, new object[] { key, cacheEntry.CloneWithoutValue(), lockId, accessType, operationContext });
                Array userPayLoad = null;
                if (cacheEntry.Value is CallbackEntry)
                {
                    CallbackEntry cbEntry = ((CallbackEntry)cacheEntry.Value);
                    userPayLoad = cbEntry.UserData;
                }
                else
                {
                    userPayLoad = cacheEntry.UserData;
                }

                func.UserPayload = userPayLoad;
                func.ResponseExpected = true;
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST, false);
                if (result == null)
                {
                    return retVal;
                }

                retVal = (CacheInsResultWithEntry)((OperationResponse)result).SerializablePayload;
                if (retVal.Entry != null)
                    retVal.Entry.Value = ((OperationResponse)result).UserPayload;
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        /// <summary>
        /// Updates or Adds the objects to the cluster. 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">cache entries.</param>
        /// <param name="operationContext"></param>
        /// <returns>failed keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on the specified node.
        /// </remarks>
        protected Hashtable Clustered_Insert(Address dest, object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.InsertBlk", "");

            Hashtable inserted = null;
            try
            {
                Function func = new Function((int)OpCodes.Insert, new object[] { keys, cacheEntries, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST, false);
                if (result == null)
                {
                    return new Hashtable();
                }
                inserted = (Hashtable)result;
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return inserted;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Remove ---           /

        /// <summary>
        /// Remove the object from the cluster. 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster.
        /// </remarks>
        protected CacheEntry Clustered_Remove(Address dest, object key, ItemRemoveReason ir, CallbackEntry cbEntry, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.Remove", "");

            CacheEntry retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.Remove, new object[] { key, ir, notify, cbEntry, lockId, accessType, operationContext }, false);
                func.ResponseExpected = true;
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST, false);
                if (result != null)
                {
                    retVal = ((OperationResponse)result).SerializablePayload as CacheEntry;
                    if (retVal != null)
                        retVal.Value = ((OperationResponse)result).UserPayload;
                }
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        /// <summary>
        /// Remove the objects from the cluster. 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="ir"></param>
        /// <param name="cbEntry"></param>
        /// <param name="notify"></param>
        /// <param name="operationContext"></param>
        /// <returns>list of failed keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster.
        /// </remarks>
        protected Hashtable Clustered_Remove(Address dest, object[] keys, ItemRemoveReason ir, CallbackEntry cbEntry, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.RemoveBlk", "");

            Hashtable removedEntries = new Hashtable();
            ArrayList dests = new ArrayList();
            dests.Add(dest);
            try
            {
                Function func = new Function((int)OpCodes.Remove, new object[] { keys, ir, notify, cbEntry, operationContext }, false);
                RspList results = Cluster.Multicast(dests, func, GetFirstResponse, false);

                if (results == null)
                {
                    return removedEntries;
                }

                //muds:
                if (results.SuspectedMembers.Count == dests.Count)
                {
                    //All the members of this group has gone down. 
                    //we must try this operation on some other group.
                    throw new Runtime.Exceptions.SuspectedException("operation failed because the group member was suspected");
                }

                ClusterHelper.ValidateResponses(results, typeof(Hashtable), Name);
                ArrayList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(Hashtable));

                if (rspList.Count <= 0)
                {
                    return removedEntries;
                }

                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp)ia.Current;
                    Hashtable removed = (Hashtable)rsp.Value;

                    IDictionaryEnumerator ide = removed.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        removedEntries.Add(ide.Key, ide.Value);
                    }
                }
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return removedEntries;
        }


       
        #endregion

        #region	/                 --- Partitioned ICache.GetEnumerator ---           /

        #region	/                 --- Clustered Enumerators ---           /


        /// <summary>
        /// provides Enumerator over cache partitions 
        /// </summary>
        internal class LazyPartitionedKeysetEnumerator : LazyKeysetEnumerator
        {
            /// <summary> Holder for current dictionary entry. </summary>
            private Address _address;
            private bool _isLocalEnumerator;

            /// <summary>
            /// Constructor 
            /// </summary>
            /// <param name="cache"></param>
            /// <param name="keyList"></param>
            public LazyPartitionedKeysetEnumerator(PartitionedCacheBase cache,
                object[] keyList,
                Address address,
                bool isLocalEnumerator)
                : base(cache, keyList, false)
            {
                _address = address;
                _isLocalEnumerator = isLocalEnumerator;
            }

            /// <summary>
            /// Does the lazy loading of object. This method is virtual so containers can customize object 
            /// fetching logic.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            protected override object FetchObject(object key, OperationContext operationContext)
            {
                PartitionedServerCache ps = _cache as PartitionedServerCache;
                
                if (_isLocalEnumerator)
                    return ps.Local_Get(key, operationContext);
                
                return ps.Clustered_Get(_address, key, operationContext);
            }
        }

        /// <summary>
        /// provides Enumerator over replicated client cache
        /// </summary>
        internal class ClusteredEnumerator : LazyKeysetEnumerator
        {
            private Address _targetNode;

            /// <summary>
            /// Constructor 
            /// </summary>
            /// <param name="cache"></param>
            /// <param name="keyList"></param>
            public ClusteredEnumerator(PartitionedCacheBase cache, Address address, object[] keyList)
                : base(cache, keyList, true)
            {
                _targetNode = address;
            }

            /// <summary>
            /// Does the lazy loading of object. This method is virtual so containers can 
            /// customize object fetching logic.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            protected override object FetchObject(object key, OperationContext operationContext)
            {
                PartitionedCacheBase cache = _cache as PartitionedCacheBase;
                return cache.Clustered_Get(_targetNode, key, operationContext);
            }
        }


        #endregion

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        protected IDictionaryEnumerator Clustered_GetEnumerator(Address targetNode)
        {
            try
            {
                Function func = new Function((int)OpCodes.KeyList, null);
                object result = Cluster.SendMessage((Address)targetNode.Clone(),
                    func,
                    GroupRequest.GET_FIRST,
                    Cluster.Timeout * 10);
                if ((result == null) || !(result is object[]))
                {
                    return null;
                }
                return new ClusteredEnumerator(this, (Address)targetNode.Clone(), result as object[]);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        protected IDictionaryEnumerator Clustered_GetEnumerator(ArrayList dests, IDictionaryEnumerator local)
        {
            IDictionaryEnumerator retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.KeyList, null);
                RspList results = Cluster.BroadcastToMultiple(dests,
                    func,
                    GroupRequest.GET_ALL,
                    Cluster.Timeout * 10,
                    false);
                if (results == null)
                {
                    return retVal;
                }

                ClusterHelper.ValidateResponses(results, typeof(object[]), Name);

                Rsp rsp = null;
                ArrayList validRsps = new ArrayList();
                for (int i = 0; i < results.size(); i++)
                {
                    rsp = (Rsp)results.elementAt(i);

                    if (rsp.Value != null)
                    {
                        validRsps.Add(rsp);
                    }
                }

                int index = (local == null ? 0 : 1);
                int totalEnums = validRsps.Count + index;
                IDictionaryEnumerator[] enums = new IDictionaryEnumerator[totalEnums];
                if (local != null)
                {
                    enums[0] = local;
                }
                for (int i = 0; i < validRsps.Count; i++)
                {
                    rsp = (Rsp)validRsps[i];
                    enums[index++] = new LazyPartitionedKeysetEnumerator(this,
                        rsp.Value as object[],
                        rsp.Sender as Address, 
                        false);
                }
                retVal = new AggregateEnumerator(enums);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        #endregion
    }

}


