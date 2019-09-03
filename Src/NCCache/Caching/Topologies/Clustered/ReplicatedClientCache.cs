//  Copyright (c) 2018 Alachisoft
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
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
#if JAVA
using Alachisoft.TayzGrid.Runtime.Exceptions;
#else
using Alachisoft.NCache.Runtime.Exceptions;
#endif
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Resources;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Cluster.Blocks;
using Alachisoft.NCache.Cluster.Util;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Locking;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// This class provides the replicated cluster cache primitives, while staying a client node
    /// in the cluster, i.e., a node that is not storage-enabled. 
    /// </summary>
    internal class ReplicatedClientCache : ReplicatedCacheBase
    {
        /// <summary> The physical storage for this cache </summary>
        private CacheBase _internalCache = null;

        /// <summary> Call balancing helper object. </summary>
        private IActivityDistributor _callBalancer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        public ReplicatedClientCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
            _stats.ClassName = "replicated-client";
            Initialize(cacheClasses, properties);
        }


        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_internalCache != null)
            {
                _internalCache.Dispose();
                _internalCache = null;
            }
            base.Dispose();
        }

        #endregion

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Method that allows the object to initialize itself. Passes the property map down 
        /// the object hierarchy so that other objects may configure themselves as well..
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        protected override void Initialize(IDictionary cacheClasses, IDictionary properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                base.Initialize(cacheClasses, properties);
                try
                {
                    IDictionary frontCacheProps = ConfigHelper.GetCacheScheme(cacheClasses, properties, "internal-cache");
                    string cacheType = Convert.ToString(frontCacheProps["type"]).ToLower();
                    if (cacheType.CompareTo("local-cache") == 0)
                    {
                        _internalCache = CacheBase.Synchronized(new LocalCache(cacheClasses, this, frontCacheProps, null, _context, null));
                    }
                    else if (cacheType.CompareTo("overflow-cache") == 0)
                    {
                        _internalCache = CacheBase.Synchronized(new OverflowCache(cacheClasses, this, frontCacheProps, null, _context, null));
                    }
                    // else throw new ConfigurationException("invalid or non-local cache class specified in Replicated cache");
                    //					if(_internalCache != null)
                    //						_internalCache.Notifiers = Notifications.None;
                }
                catch (Exception)
                {
                    //Dispose();
                }

                _stats.Nodes = new ArrayList(2);
                _callBalancer = new CallBalancer();
                _initialJoiningStatusLatch = new Latch();

                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(false, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)));
                _stats.GroupName = Cluster.ClusterName;
                _initialJoiningStatusLatch.WaitForAny(NodeStatus.Running);
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.Initialize", "determinining cluster status");
                DetermineClusterStatus();
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.Initialize", "updating status");
                UpdateCacheStatistics();
                _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);

            }
            catch (ConfigurationException)
            {
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.Message, e);
            }
        }

        #endregion

        /// <summary> 
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal override CacheBase InternalCache
        {
            get { return _internalCache; }
        }

        #region	/                 --- Overrides for ClusteredCache ---           /

        /// <summary>
        /// Called after the membership has been changed. Lets the members do some
        /// member oriented tasks.
        /// </summary>

        public override void OnAfterMembershipChange()
        {
            base.OnAfterMembershipChange();
            //DetermineClusterStatus();
            //UpdateCacheStatistics();
            //_statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            _initialJoiningStatusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.OnAfterMembershipChange", "changing status to running");
        }


        /// <summary>
        /// Called when a new member joins the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <param name="identity">additional identity information</param>
        /// <returns>true if the node joined successfuly</returns>
        public override bool OnMemberJoined(Address address, NodeIdentity identity)
        {

            if (!base.OnMemberJoined(address, identity) || !((Identity)identity).HasStorage)
                return false;

            NodeInfo info = new NodeInfo(address as Address);
            info.SubgroupName = identity.SubGroupName;
            lock (_stats.Nodes.SyncRoot)
            {
                _stats.Nodes.Add(info);
            }

            if (LocalAddress.CompareTo(address) == 0)
            {
                _stats.LocalNode = info;
            }

            //_statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.OnMemberJoined()", "Found r-server: " + address);
            return true;
        }

        /// <summary>
        /// Called when an existing member leaves the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <returns>true if the node left successfuly</returns>
        public override bool OnMemberLeft(Address address, NodeIdentity identity)
        {
            if (!base.OnMemberLeft(address, identity))
                return false;

            NodeInfo info = _stats.GetNode(address as Address);
            lock (_stats.Nodes.SyncRoot)
            {
                _stats.Nodes.Remove(info);
            }

            if (Servers.Count < 1)
                _statusLatch.SetStatusBit(NodeStatus.Initializing, NodeStatus.Running);

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.OnMemberLeft()", "r-server lost: " + address);
            return true;
        }

        /// <summary>
        /// Handles the function requests.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public override object HandleClusterMessage(Address src, Function func)
        {
            switch (func.Opcode)
            {
                case (int)OpCodes.PeriodicUpdate:
                    return handlePresenceAnnouncement(src, func.Operand);

                case (int)OpCodes.NotifyAdd:
                    return handleNotifyAdd(func.Operand);

                case (int)OpCodes.NotifyUpdate:
                    return handleNotifyUpdate(func.Operand);

                case (int)OpCodes.NotifyRemoval:
                    return handleNotifyRemoval(func.Operand);

                case (int)OpCodes.NotifyClear:
                    return handleNotifyCacheCleared();
            }
            return base.HandleClusterMessage(src, func);
        }

        #endregion

        #region	/                 --- Statistics Replication ---           /

        /// <summary>
        /// Periodic update (PULL model), i.e. on demand fetch of information from every node.
        /// </summary>
        private bool DetermineClusterStatus()
        {
            try
            {
                Function func = new Function((int)OpCodes.ReqStatus, null);
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL);

                for (int i = 0; i < results.size(); i++)
                {
                    Rsp rsp = (Rsp)results.elementAt(i);
                    NodeInfo nodeInfo = rsp.Value as NodeInfo;
                    if (nodeInfo != null)
                    {
                        handlePresenceAnnouncement(rsp.Sender as Address, nodeInfo);
                    }
                }
            }
            catch (Exception e)
            {
                if (Context != null)
                {
                    Context.NCacheLog.Error("ReplicatedClientCache.DetermineClusterStatus()", e.ToString());
                }
            }
            return false;
        }

        /// <summary>
        /// Handler for Periodic update (PUSH model).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private object handlePresenceAnnouncement(Address sender, object obj)
        {
            NodeInfo info = null;
            lock (Servers.SyncRoot)
            {
                NodeInfo other = obj as NodeInfo;
                info = _stats.GetNode(sender as Address);
                if (other != null && info != null)
                {
                    info.Statistics = other.Statistics;
                    info.Status = other.Status;
                    info.ConnectedClients = other.ConnectedClients;
                  
                }
            }
            UpdateCacheStatistics();
            return null;
        }

        /// <summary>
        /// Updates the statistics for the cache scheme.
        /// </summary>
        private void UpdateCacheStatistics()
        {
            try
            {
                _stats.LocalNode.Statistics = null;
                _stats.LocalNode.Status.Data = _statusLatch.Status.Data;
                _stats.SetServerCounts(Convert.ToInt32(Servers.Count),
                    Convert.ToInt32(ValidMembers.Count),
                    Convert.ToInt32(Members.Count - ValidMembers.Count));
                CacheStatistics c = ClusterHelper.CombineReplicatedStatistics(_stats);
                _stats.UpdateCount(c.Count);
                _stats.HitCount = c.HitCount;
                _stats.MissCount = c.MissCount;
                _stats.MaxCount = c.MaxCount;
            }
            catch (Exception)
            {
            }
        }

        #endregion

        /// <summary>
        /// Return the next node in call balacing order.
        /// </summary>
        /// <returns></returns>
        private Address GetNextNode()
        {
            CheckServerAvailability();
            NodeInfo node = _callBalancer.SelectNode(_stats, null);
            return node == null ? null : node.Address;
        }

        /// <summary>
        /// Checks if there are enough servers in the group to process the request
        /// throws exception if there aren't.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void CheckServerAvailability()
        {
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (Servers.Count < 1)
            {
                throw new InvalidOperationException("No servers available to process request");
            }
        }


        #region	/                 --- ICache ---           /

        #region	/                 --- Replicated ICache.Count ---           /

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count
        {
            get
            {
                return 0;
                //return Clustered_Count();
            }
        }

        /// <summary>
        /// Returns the count of clustered cache items.
        /// </summary>
        //private long Clustered_Count()
        //{
        //    Address targetNode = GetNextNode();
        //    return Clustered_Count(targetNode);
        //}

        #endregion

        #region	/                 --- Replicated ICache.Contains ---           /

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        public override bool Contains(object key, OperationContext operationContext)
        {
            // Do a local local lookup, trying to avoid cluster calls as much as possible.
            if (Local_Contains(key, operationContext))
            {
                return true;
            }

            CheckServerAvailability();
            // check the cluster minus ourselves. :)
            return Clustered_Contains(key, operationContext) != null;
        }


        /// <summary>
        /// Determines whether the cache contains the given keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of available keys from the given key list</returns>
        public override Hashtable Contains(object[] keys, OperationContext operationContext)
        {
            //ArrayList list = new ArrayList();
            //try 
            //{
            //    list = Local_Contains( keys );
            //    if(list.Count < keys.Length)
            //    {
            //        CheckServerAvailability();

            //        object[] rKeys = MiscUtil.GetNotAvailableKeys(keys, list);

            //        // check the cluster minus ourselves.
            //        ArrayList clusterList =  Clustered_Contains( rKeys );
            //        list.AddRange(clusterList);
            //    }
            //    return list;
            //}
            //finally
            //{
            //    //	long stop = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
            //    //	if(NCacheLog.IsInfoEnabled(Context.CacheName)) NCacheLog.Info(Context.CacheName, "PartitionedCache.Contains()", "time taken: " + (stop - start));
            //}
            return null;
        }




        /// <summary>
        /// Determines whether the local cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        private bool Local_Contains(object key, OperationContext operationContext)
        {
            bool retVal = false;
            try
            {
                if (_internalCache != null)
                    retVal = _internalCache.Contains(key, operationContext);
            }
            catch (Exception)
            {
            }
            return retVal;
        }


        /// <summary>
        /// Determines whether the local cache contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        private ArrayList Local_Contains(object[] keys)
        {
            //ArrayList list = new ArrayList();
            //if(_internalCache != null)
            //{
            //    list = _internalCache.Contains(keys);
            //}
            //return list;
            return null;
        }


        /// <summary>
        /// Determines whether the cluster contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>address of the node that contains the specified key; otherwise, null.</returns>
        private Address Clustered_Contains(object key, OperationContext operationContext)
        {
            Address targetNode = GetNextNode();
            return Clustered_Contains(targetNode, key, operationContext);
        }

        /// <summary>
        /// Determines whether the cluster contains the specified keys.
        /// </summary>
        /// <param name="key">The keys to locate in the cache.</param>
        /// <returns>list of available keys</returns>
        private ArrayList Clustered_Contains(object[] keys)
        {
            Address targetNode = GetNextNode();
            //return Clustered_Contains(targetNode, keys);
            return null;
        }

        #endregion

        #region /            --- Replicated Search ---                /

        private ICollection Clustered_Search(Address targetNode, string queryText, IDictionary values, OperationContext operationContext)
        {
            ArrayList keyList = null;
            try
            {
                Function func = new Function((int)OpCodes.Search, new object[] { queryText, values, operationContext }, true);
                func.Cancellable = true;
                object result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL);

                if (result == null)
                {
                    return null;
                }

                keyList = result as ArrayList;

                if (keyList != null && keyList.Count > 0)
                    return keyList;
                return null;
            }
            catch (CacheException e)
            {
                if (Context != null)
                {
                    Context.NCacheLog.Error("ReplicatedCacheBase.Clustered_Search()", e.ToString());
                }
                throw;
            }
            catch (Exception e)
            {
                if (Context != null)
                {
                    Context.NCacheLog.Error("ReplicatedCacheBase.Clustered_Search()", e.ToString());
                }
                throw new GeneralFailureException(e.Message, e);
            }
        }

        private IDictionary Clustered_SearchEntries(Address targetNode, string queryText, IDictionary values, OperationContext operationContext)
        {
            Hashtable keyValues = null;
            try
            {
                Function func = new Function((int)OpCodes.Search, new object[] { queryText, values, operationContext }, true);
                func.Cancellable = true;
                object result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL);

                if (result == null)
                {
                    return null;
                }

                keyValues = result as Hashtable;

                if (keyValues != null && keyValues.Count > 0)
                    return keyValues;
                return null;
            }
            catch (CacheException e)
            {
                if (Context != null)
                {
                    Context.NCacheLog.Error("ReplicatedCacheBase.Clustered_SearchEntries()", e.ToString());
                }
                throw;
            }
            catch (Exception e)
            {
                if (Context != null)
                {
                    Context.NCacheLog.Error("ReplicatedCacheBase.Clustered_SearchEntries()", e.ToString());
                }
                throw new GeneralFailureException(e.Message, e);
            }
        }

        private ICollection Clustered_Search(string query, IDictionary values, OperationContext operationContext)
        {
            Address targetNode = GetNextNode();
            return Clustered_Search(targetNode, query, values, operationContext);
        }

        public override QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            try
            {
                //CheckServerAvailability();
                //return Clustered_Search(query, values);
                return null;
            }
            finally
            {
            }
        }

        private IDictionary Clustered_SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            Address targetNode = GetNextNode();
            return Clustered_SearchEntries(targetNode, query, values, operationContext);
        }

        public override QueryResultSet SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            try
            {
                //CheckServerAvailability();
                //return Clustered_SearchEntries(query, values);
                return null;
            }
            finally
            {
            }
        }

        #endregion
        #region	/                 --- Replicated ICache.Clear ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        /// <remarks>
        /// This method invokes handleClear() on every server node in the cluster, which then 
        /// trigger a cluster-wide notification to the clients (handled in <see cref="handleNotifyCacheCleared"/>).
        /// </remarks>
        public override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            Local_Clear(operationContext);
            CheckServerAvailability();
            Clustered_Clear(null, null, true, operationContext);
        }

        /// <summary>
        /// Removes all entries from the local cache only.
        /// </summary>
        private void Local_Clear(OperationContext operationContext)
        {
            try
            {
                if (_internalCache != null)
                    _internalCache.Clear(null, DataSourceUpdateOptions.None, operationContext);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region	/                 --- Replicated ICache.Get ---           /

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry e = Local_Get(key, operationContext);
            if (e == null && Servers.Count > 0)
            {
                e = Clustered_Get(key, operationContext);
            }
            if (e == null)
            {
                _stats.BumpMissCount();
            }
            else
            {
                _stats.BumpHitCount();
                Local_Insert(key, e, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
            }
            return e;
        }


        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override IDictionary Get(object[] keys, OperationContext operationContext)
        {
            HashVector table = (HashVector)Local_Get(keys, operationContext);
            if (table.Count < keys.Length && Servers.Count > 0)
            {
                object[] rKeys = MiscUtil.GetNotAvailableKeys(keys, table);
                HashVector cTable = Clustered_Get(rKeys, operationContext);
                IDictionaryEnumerator ide = cTable.GetEnumerator();
                while (ide.MoveNext())
                {
                    table.Add(ide.Key, ide.Value);
                }
            }

            IDictionaryEnumerator idr = table.GetEnumerator();
            while (idr.MoveNext())
            {
                if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                CacheEntry e = (CacheEntry)idr.Value;
                if (e == null)
                {
                    _stats.BumpMissCount();
                }
                else
                {
                    _stats.BumpHitCount();
                    Local_Insert(idr.Key, e, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                }
            }

            return table;
        }


        /// <summary>
        /// Retrieve the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Get(object key, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            try
            {
                if (_internalCache != null)
                    retVal = _internalCache.Get(key, operationContext);
            }
            catch (Exception)
            {
            }
            return retVal;
        }


        /// <summary>
        /// Retrieve the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        private IDictionary Local_Get(object[] keys, OperationContext operationContext)
        {
            HashVector retVal = new HashVector();
            if (_internalCache != null)
                retVal = (HashVector)_internalCache.Get(keys, operationContext);
            return retVal;
        }

        /// <summary>
        /// Retrieve the object from the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Clustered_Get(object key, OperationContext operationContext)
        {
            Address targetNode = GetNextNode();
            return Clustered_Get(targetNode, key, operationContext);
        }


        /// <summary>
        /// Retrieve the objects from the cluster. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        private HashVector Clustered_Get(object[] keys, OperationContext operationContext)
        {
            Address targetNode = GetNextNode();
            return Clustered_Get(targetNode, keys, operationContext);
        }


        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of keys.</returns>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            return Clustered_GetKeys(group, subGroup, operationContext);
        }

        public override Common.Events.PollingResult Poll(OperationContext operationContext)
        {
            return Clustered_Poll(operationContext);
        }

        public override void RegisterPollingNotification(short callbackId, OperationContext operationContext)
        {
            Clustered_RegisterPollingNotification(callbackId, operationContext);
        }

        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>key and value pairs.</returns>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            return Clustered_GetData(group, subGroup, operationContext);
        }

        private PollingResult Clustered_Poll(OperationContext context)
        {
            Address targetNode = GetNextNode();
            if (targetNode != null)
            {
                return Clustered_Poll(targetNode, context);
            }
            return null;

        }

        /// <summary>
        /// Retrieve the objects from the cluster. Used during state trasfer, when the cache
        /// is loading state from other members of the cluster.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        private ArrayList Clustered_GetKeys(string group, string subGroup, OperationContext operationContext)
        {
            /// Fetch address of a fully functional node. There should always be one
            /// fully functional node in the cluster (coordinator is alway fully-functional).
            Address targetNode = GetNextNode();
            if (targetNode != null)
                return Clustered_GetKeys(targetNode, group, subGroup, operationContext);
            return null;
        }


        /// <summary>
        /// Retrieve the objects from the cluster. Used during state trasfer, when the cache
        /// is loading state from other members of the cluster.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        private HashVector Clustered_GetData(string group, string subGroup, OperationContext operationContext)
        {
            /// Fetch address of a fully functional node. There should always be one
            /// fully functional node in the cluster (coordinator is alway fully-functional).
            Address targetNode = GetNextNode();
            if (targetNode != null)
                return Clustered_GetData(targetNode, group, subGroup, operationContext);
            return null;
        }




        #endregion

        #region	/                 --- Replicated ICache.Add ---           /

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide item-add 
        /// notificaion.
        /// </remarks>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            CheckServerAvailability();

            CacheAddResult result = CacheAddResult.Success;
            try
            {
                CacheEntry remote = cacheEntry.FlattenedClone(_context.SerializationContext);
                result = Clustered_Add(key, remote, operationContext);

                // success on the cluster so update locally as well.
                if (result == CacheAddResult.Success)
                {
                    Local_Insert(key, cacheEntry,  new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                }
            }
            catch (Exception)
            {
                throw;
            }


            if (result == CacheAddResult.KeyExists)
            {
                return result;
            }

            if (result != CacheAddResult.Success)
            {
                Local_Remove(key, operationContext);
                if (Servers.Count > 1)
                {
                    Clustered_Remove(key, ItemRemoveReason.Removed, null, null, null, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                }
            }
            else if (notify)
            {
                // If everything went ok!, initiate local and cluster-wide notifications.
                RaiseItemAddNotifier(key, null, null, null);
                handleNotifyAdd(key);
            }
            return result;
        }


        /// <summary>
        /// Add ExpirationHint against the given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            CheckServerAvailability();

            bool result = false;
            Exception thrown = null;
            try
            {
                result = Clustered_Add(Cluster.Servers, key, eh, operationContext);
                if (result == false)
                {
                    Context.NCacheLog.Error("Replicated.Clustered_Add()", "Hint not added: Key = " + key);
                    return result;
                }
            }
            catch (Exception e)
            {
                if (Context != null)
                {
                    Context.NCacheLog.Error("Replicated.Clustered_Add()", e.ToString());
                }
                thrown = e;
            }

            return result;
        }

        /// <summary>
        /// Adds key and value pairs to the cache. Throws an exception or returns the
        /// list of keys for which add operation failed.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>returns the list of added keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide item-add 
        /// notificaion.
        /// </remarks>
        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            CheckServerAvailability();

            Hashtable keyVals = new Hashtable();
            for (int i = 0; i < keys.Length; i++)
            {
                keyVals[keys[i]] = cacheEntries[i];
            }

            Hashtable added = new Hashtable();
            Exception thrown = null;

            try
            {
                for (int i = 0; i < cacheEntries.Length; i++)
                {
                    CacheEntry remote = cacheEntries[i].FlattenedClone(_context.SerializationContext);
                    cacheEntries[i] = remote;
                }
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.Add:bulk", keys.Length.ToString());
                added = Clustered_Add(keys, cacheEntries, operationContext);
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.Add:bulk", "SUCCESSFULL");
            }
            catch (Exception inner)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    added[keys[i]] = new OperationFailedException(inner.Message, inner);
                }
                thrown = inner;
            }
            if (thrown == null)	// success on the cluster so update locally as well.
            {
                int failCount = 0;
                ArrayList failKeys = new ArrayList();
                Hashtable tbl = added.Clone() as Hashtable;
                IDictionaryEnumerator ide = tbl.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Value is CacheAddResult)
                    {
                        CacheAddResult res = (CacheAddResult)ide.Value;
                        switch (res)
                        {
                            case CacheAddResult.Failure:
                            case CacheAddResult.KeyExists:
                            case CacheAddResult.NeedsEviction:
                                failCount++;
                                failKeys.Add(ide.Key);
                                added[ide.Key] = ide.Value;
                                keyVals.Remove(ide.Key);
                                break;

                            case CacheAddResult.Success:
                                added[ide.Key] = ide.Value;
                                break;
                        }
                    }
                    else //it means value is exception
                    {
                        failCount++;
                        failKeys.Add(ide.Key);
                        added[ide.Key] = ide.Value;
                        keyVals.Remove(ide.Key);
                    }
                }

                object[] keysToAdd = new object[keyVals.Count];
                CacheEntry[] entriesToAdd = new CacheEntry[keyVals.Count];
                keyVals.Keys.CopyTo(keysToAdd, 0);
                keyVals.Values.CopyTo(entriesToAdd, 0);

                Local_Insert(keysToAdd, entriesToAdd, operationContext);

                if (failCount > 0)
                {
                    object[] keysToRemove = new object[failCount];
                    failKeys.CopyTo(keysToRemove, 0);
                    try
                    {
                        if (Servers.Count > 1)
                        {
                            Clustered_Remove(keysToRemove, ItemRemoveReason.Removed, null, null, null, false, operationContext);
                        }
                    }
                    catch (Exception) { }
                }
            }

            if (notify)
            {
                foreach (object key in added.Keys)
                {
                    // If everything went ok!, initiate local and cluster-wide notifications.
                    RaiseItemAddNotifier(key, null, null, null); 
                    handleNotifyAdd(key);
                }
            }
            return added;
        }

        /// <summary>
        /// Add the object to the local cache. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheAddResult Local_Add(object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            CacheAddResult retVal = CacheAddResult.Success;
            try
            {
                if (_internalCache != null)
                    retVal = _internalCache.Add(key, cacheEntry, false, operationContext);
            }
            catch (Exception)
            {
            }
            return retVal;
        }

        /// <summary>
        /// Add the objects to the local cache. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        private Hashtable Local_Add(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            Hashtable retVal = null;
            try
            {
                if (_internalCache != null)
                    retVal = _internalCache.Add(keys, cacheEntries, false, operationContext);
            }
            catch (Exception)
            {
            }
            return retVal;
        }

        /// <summary>
        /// Add the object to specfied node in the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// </remarks>
        private CacheAddResult Clustered_Add(object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            return Clustered_Add(Cluster.Servers, key, cacheEntry, null, operationContext);
        }

        /// <summary>
        /// Add objects to the cluster. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// </remarks>
        private Hashtable Clustered_Add(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            return Clustered_Add(Cluster.Servers, keys, cacheEntries, null, operationContext);
        }

        #endregion

        #region	/                 --- Replicated ICache.Insert ---           /

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide 
        /// item-update notificaion.
        /// </remarks>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accesType, OperationContext operationContext)
        {
            CheckServerAvailability();

            CacheEntry pEntry = null;
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();
            try
            {
                pEntry = Get(key, operationContext);// dont remove this line.
                retVal.Entry = pEntry;

                if (pEntry != null)
                {
                    DataGrouping.GroupInfo oldInfo = pEntry.GroupInfo;
                    DataGrouping.GroupInfo newInfo = cacheEntry.GroupInfo;

                    if (!CacheHelper.CheckDataGroupsCompatibility(newInfo, oldInfo))
                    {
                        throw new Exception("Data group of the inserted item does not match the existing item's data group");
                    }

                }
                CacheEntry remote = cacheEntry.FlattenedClone(_context.SerializationContext);
                retVal = Clustered_Insert(key, remote);
            }
            catch (Exception)
            {
                throw;
            }

            if (retVal.Result == CacheInsResult.Success || retVal.Result == CacheInsResult.SuccessOverwrite)
            {
                Local_Insert(key, cacheEntry, operationContext);
            }

            // Try to insert to the local node and the cluster.
            if ((retVal.Result == CacheInsResult.NeedsEviction || retVal.Result == CacheInsResult.Failure))
            {
                /// failed on the cluster, so remove locally as well.
                Local_Remove(key, operationContext);
                if (Servers.Count > 1)
                {
                    Clustered_Remove(key, ItemRemoveReason.Removed, null, null, null, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                }
            }

            if (notify && retVal.Result == CacheInsResult.Success)
            {
                RaiseItemAddNotifier(key, null, null, null);
                handleNotifyUpdate(new object[] { key, operationContext });
            }
            else if (notify && retVal.Result == CacheInsResult.SuccessOverwrite)
            {
                if (pEntry != null)
                {
                    //object value = pEntry.Value;//pEntry.DeflattedValue(_context.SerializationContext);
                    if (pEntry.Notifications != null)
                    {
                        RaiseCustomUpdateCalbackNotifier(key, pEntry.Notifications.ItemUpdateCallbackListener);
                    }
                }

                RaiseItemUpdateNotifier(key, operationContext, null);
                handleNotifyUpdate(new object[] { key, operationContext });
            }
            return retVal;
        }

        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>the list of keys that failed to add.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide 
        /// item-update notificaion.
        /// </remarks>
        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            CheckServerAvailability();
            return Clustered_Insert(keys, cacheEntries, null, notify, operationContext);
        }

        /// <summary>
        /// Insert the object to the local cache. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheInsResultWithEntry Local_Insert(object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();
            try
            {
                if (_internalCache != null)
                    retVal = _internalCache.Insert(key, cacheEntry, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
            }
            catch (Exception)
            {
            }
            return retVal;
        }

        /// <summary>
        /// Insert the objects to the local cache. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>array of keys that failed to be inserted</returns>
        private Hashtable Local_Insert(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();
            try
            {
                if (_internalCache != null)
                    retVal = _internalCache.Insert(keys, cacheEntries, false, operationContext);
            }
            catch (Exception)
            {
            }
            return retVal;
        }

        /// <summary>
        /// Add the object to the cluster. 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cacheEntry"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on every server-node in the cluster. If 
        /// the operation fails on any one node the whole operation is considered to have failed 
        /// and is rolled-back.
        /// </remarks>
        private CacheInsResultWithEntry Clustered_Insert(object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            return Clustered_Insert(Cluster.Servers, key, cacheEntry, null, null, LockAccessType.IGNORE_LOCK, operationContext);
        }

        /// <summary>
        /// Add the objects to the cluster. 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="cacheEntries"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on every server-node in the cluster. If 
        /// the operation fails on any one node the whole operation is considered to have failed 
        /// and is rolled-back.
        /// </remarks>
        protected override Hashtable Clustered_Insert(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            return Clustered_Insert(Cluster.Servers, keys, cacheEntries, null, operationContext);
        }


        #endregion

        #region	/                 --- Replicated ICache.Remove ---           /

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster. It then
        /// triggers the item remove notification.
        /// Notifications in case of expirations and evictions are only initiated by server nodes.
        /// </remarks>
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            Local_Remove(key, operationContext);
            CheckServerAvailability();
            CacheEntry e = Clustered_Remove(key, ir, null, null, null, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
            if (notify && e != null)
            {
                //object value = e.Value;// e.DeflattedValue(_context.SerializationContext);
                if (e.Notifications != null)
                {
                    RaiseCustomRemoveCalbackNotifier(key, e, ir);
                }

                EventContext eventContext = CreateEventContextForGeneralDataEvent(Persistence.EventType.ITEM_REMOVED_EVENT, operationContext, e, null);
                object data = new object[] { key, ir, operationContext , eventContext};
                RaiseItemRemoveNotifier(data);
                handleNotifyRemoval(data);
            }
            return e;
        }

        /// <summary>
        /// Removes the objects and key pairs from the cache. The keys are specified as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>keys that actually removed from the cache</returns>
        /// <remarks>
        /// Remove notifications in a repicated cluster are handled differently. If there is an 
        /// explicit request for Remove, the node initiating the request triggers the notifications.
        /// Expirations and Evictions are replicated and again the node initiating the replication
        /// triggers the cluster-wide notification.
        /// </remarks>
        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            Local_Remove(keys, operationContext);
            CheckServerAvailability();
            Hashtable removed = Clustered_Remove(keys, ir, null, null, null, false, operationContext);
            if (notify)
            {
                if (removed != null)
                {
                    IDictionaryEnumerator ide = removed.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);
                        
                        object key = ide.Key;
                        CacheEntry e = (CacheEntry)ide.Value;
                        if (e != null)
                        {
                            //object value = e.Value;// e.DeflattedValue(_context.SerializationContext);
                            if (e.Notifications != null)
                            {
                                RaiseCustomRemoveCalbackNotifier(key, e, ir);
                            }

                            EventContext eventContext = CreateEventContextForGeneralDataEvent(Persistence.EventType.ITEM_REMOVED_EVENT, operationContext, e, null);
                            object data = new object[] { key, ir, operationContext , eventContext};
                            RaiseItemRemoveNotifier(data);
                            handleNotifyRemoval(data);
                        }
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// Remove the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Remove(object key, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            try
            {
                if (_internalCache != null)
                    retVal = _internalCache.Remove(key, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
            }
            catch (Exception)
            {
            }
            return retVal;
        }

        /// <summary>
        /// Remove the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="ir"></param>
        /// <param name="notify"></param>
        /// <returns>keys that actualy removed from the cache</returns>
        private Hashtable Local_Remove(object[] keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();
            if (_internalCache != null)
            {
                retVal = _internalCache.Remove(keys, ir, notify, operationContext);
                UpdateCacheStatistics();
            }
            return retVal;
        }


        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            CheckServerAvailability();
            return null;//Clustered_Remove(group, subGroup, false, operationContext);
        }




        #endregion

        #region	/                 --- Replicated ICache.GetEnumerator ---           /

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            Address targetNode = GetNextNode();
            return Clustered_GetEnumerator(targetNode);
        }

        #endregion

        #endregion

        #region	/                 --- Events Notifications ---           /

        #region	/                 --- OnCacheCleared ---           /

        /// <summary>
        /// Hanlder for clustered cache clear notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyCacheCleared()
        {
            NotifyCacheCleared(true,null,null);
            return null;
        }

        #endregion



        #region	/                 --- OnItemUpdated ---           /

        /// <summary>
        /// Notify the listener that an item has been updated.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="val">item itself</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        protected override void NotifyItemUpdated(object key, bool async, OperationContext operationContext,EventContext eventContext)
        {
            Local_Remove(key, operationContext);
            base.NotifyItemUpdated(key, async, operationContext, eventContext);
        }




        #endregion

        #region	/                 --- OnItemRemoved ---           /

        /// <summary>
        /// Notify the listener that an item is removed from the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="val">item itself</param>
        /// <param name="reason">reason the item was removed</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        public override void NotifyItemRemoved(object key, object val, ItemRemoveReason reason, bool async, OperationContext operationContext,EventContext eventContext)
        {
            Local_Remove(key, operationContext);
            base.NotifyItemRemoved(key, val, reason, async, operationContext, eventContext);
        }

        /// <summary>
        /// Notify the listener that items are removed from the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="val">item itself</param>
        /// <param name="reason">reason the item was removed</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        public override void NotifyItemsRemoved(object[] key, object[] val, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext[] eventContext)
        {
            for (int i = 0; i < key.Length; i++)
                Local_Remove(key[i], operationContext);
            base.NotifyItemsRemoved(key, val, reason, async, operationContext, eventContext);
        }

        /// <summary>
        /// Hanlder for clustered item removal notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyRemoval(object info)
        {
            object[] objs = (object[])info;

            OperationContext operationContext = null;
            EventContext[] eventContexts = null;
            EventContext evContext = null;
            if (objs.Length > 2)
                operationContext = objs[2] as OperationContext;
            if (objs.Length > 3)
            {
                eventContexts = objs[3] as EventContext[];
                if (eventContexts == null)
                    evContext = objs[3] as EventContext;
            }

            if (objs[0] is object[])
            {
                NotifyItemsRemoved((object[])objs[0], null, (ItemRemoveReason)objs[1], true, operationContext, eventContexts);
            }
            else
            {
                NotifyItemRemoved(objs[0], null, (ItemRemoveReason)objs[1], true, operationContext,evContext);
            }
            return null;
        }

        #endregion

        #endregion

    }
}


