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
using Alachisoft.NCache.Config;
#if JAVA
using Alachisoft.TayzGrid.Runtime.Exceptions;
#else
using Alachisoft.NCache.Runtime.Exceptions;
#endif
using Alachisoft.NCache.Util;

using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Cluster.Util;
using Alachisoft.NCache.Cluster.Blocks;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Locking;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// This class provides the partitioned cluster cache primitives, while staying a client node
    /// in the cluster, i.e., a node that is not storage-enabled. 
    /// </summary>
    internal class PartitionedClientCache : PartitionedCacheBase
    {
        /// <summary> The physical storage for this cache </summary>
        private CacheBase _internalCache = null;

        /// <summary> The load balancer to be used for load balancing. </summary>
        private IActivityDistributor _loadBalancer = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        public PartitionedClientCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
            _stats.ClassName = "partitioned-client";
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
                    // else throw new ConfigurationException("invalid or non-local cache scheme specified in Replicated cache");

                    //CacheBase does not contain any instance of CacheName, instead CacheRuntimContext has it
                    //nTrace = _internalCache.Context.CacheTrace;
                }
                catch (Exception)
                {
                    // Dispose();
                }

                _loadBalancer = new ObjectCountBalancer(_internalCache.Context);
                _stats.Nodes = new ArrayList(2);
                _initialJoiningStatusLatch = new Latch();
                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(false, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)));
                _stats.GroupName = Cluster.ClusterName;
                _initialJoiningStatusLatch.WaitForAny(NodeStatus.Running);
                if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.Initialize", "determinining cluster status");
                DetermineClusterStatus();
                if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedClientCache.Initialize", "updating status");
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
            _initialJoiningStatusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);

            //            _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedClientCache.OnAfterMembershipChange", "changing status to running");
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
            //_statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);

            if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedClientCache.OnMemberJoined()", "Found p-server: " + address);
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

            if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedClientCache.OnMemberLeft()", "p-server lost: " + address);
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
                Context.NCacheLog.Error("ParitionedClientCache.DetermineClusterStatus()", e.ToString()); 
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
            if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedClientCache.handlePresenceAnnouncement", "sender :" + sender);
            
            NodeInfo info;
            lock (Servers.SyncRoot)
            {
                NodeInfo other = obj as NodeInfo;
                info = _stats.GetNode(sender as Address);
                if (other != null && info != null)
                {
                    info.Statistics = other.Statistics;
                    info.Status = other.Status;
                    info.ConnectedClients = other.ConnectedClients;
                    info.LocalConnectedClientsInfo = other.LocalConnectedClientsInfo;
                    info.DataAffinity = other.DataAffinity;

                    if (other.DataAffinity != null && other.DataAffinity.Groups != null)
                    {
                        IEnumerator ie = other.DataAffinity.Groups.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (!_stats.ClusterDataAffinity.Contains(ie.Current))
                            {
                                _stats.ClusterDataAffinity.Add(ie.Current);
                            }
                        }

                        if (other.DataAffinity.AllBindedGroups != null)
                        {
                            ie = other.DataAffinity.AllBindedGroups.GetEnumerator();
                            while (ie.MoveNext())
                            {
                                if (!_stats.ClusterDataAffinity.Contains(ie.Current))
                                {
                                    _stats.ClusterDataAffinity.Add(ie.Current);
                                }
                            }
                        }
                    }

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
                CacheStatistics c = ClusterHelper.CombinePartitionStatistics(_stats);
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

        /// <summary>
        /// Return the next node in load balacing order.
        /// </summary>
        /// <returns></returns>
        private Address GetNextNode(string group)
        {
            NodeInfo node = _loadBalancer.SelectNode(_stats, group);
            return node == null ? null : node.Address;
        }

        #region	/                 --- ICache ---           /

        #region	/                 --- Partitioned ICache.Count ---           /

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count
        {
            get
            {
                return 0;
                //CheckServerAvailability();
                //return Clustered_Count();
            }
        }

        /// <summary>
        ///// Returns the count of clustered cache items.
        ///// </summary>
        ///// <param name="key">The key to locate in the cache.</param>
        ///// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        ///// <returns>address of the node that contains the specified key; otherwise, null.</returns>
        //private long Clustered_Count()
        //{
        //    return Clustered_Count(Cluster.Servers, true);
        //}

        #endregion

        #region	/                 --- Partitioned ICache.Contains ---           /

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
            return Clustered_Contains(key) != null;
        }


        /// <summary>
        /// Determines whether the cache contains the given keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of available keys from the given key list</returns>
        public override Hashtable Contains(object[] keys, OperationContext operationContext)
        {

            ClusteredArrayList list = new ClusteredArrayList();
            try
            {
                list = Local_Contains(keys);
                if (list.Count < keys.Length)
                {
                    CheckServerAvailability();

                    object[] rKeys = MiscUtil.GetNotAvailableKeys(keys, list);

                    // check the cluster minus ourselves.
                    HashVector clusterTable = Clustered_Contains(rKeys);

                    IDictionaryEnumerator ide = clusterTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        ClusteredArrayList keyArr = (ClusteredArrayList)ide.Value;
                        list.AddRange(keyArr);
                    }
                }

                //return list;
                return null;
            }
            finally
            {
                //				long stop = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
                //				if(Trace.isInfoEnabled) Trace.info("PartitionedCache.Contains()", "time taken: " + (stop - start));
            }
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
        private ClusteredArrayList Local_Contains(object[] keys)
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
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>address of the node that contains the specified key; otherwise, null.</returns>
        private Address Clustered_Contains(object key)
        {
            //return Clustered_Contains(Cluster.Servers, key, true);
            return null;
        }

        /// <summary>
        /// Determines whether the cluster contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>addresses of the nodes that contains the specified keys;</returns>
        private HashVector Clustered_Contains(object[] keys)
        {
            //return Clustered_Contains(Cluster.Servers, keys, true);
            return null;
        }

        #endregion


        #region /                 --- Partitioned Search ---                /

        private ICollection Clustered_Search(string queryText, IDictionary values)
        {
            //return Clustered_Search(Cluster.Servers, queryText, values, true);
            return null;
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

        private IDictionary Clustered_SearchEntries(string queryText, IDictionary values)
        {
            //return Clustered_SearchEntries(Cluster.Servers, queryText, values, true);
            return null;
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
        #region	/                 --- Partitioned ICache.Clear ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        /// <remarks>
        /// This method invokes handleClear() on every server node in the partition, which then 
        /// trigger a cluster-wide notification to the clients (handled in <see cref="handleNotifyCacheCleared"/>).
        /// </remarks>
        public override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedClient.Clear", "clear all contents");
            Local_Clear(operationContext);
            CheckServerAvailability();
            Clustered_Clear(null, null, true, operationContext);
        }

        /// <summary>
        /// Removes all entries from the near cache.
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
            //NotifyCacheCleared();
        }

        #endregion

        #region	/                 --- Partitioned ICache.Get ---           /

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
                e = Clustered_Get(key);
            }
            ////			CacheEntry e = null;
            ////			if(_internalCache != null) 
            ////			{
            ////				e = _internalCache.Get( key );
            ////			}
            ////			if(e == null)
            ////			{
            ////				CheckServerAvailability();
            ////				e = Clustered_Get( key );
            ////			}
            if (e == null)
            {
                _stats.BumpMissCount();
            }
            else
            {
                _stats.BumpHitCount();
                Local_Insert(key, e);
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

                HashVector cTable = Clustered_Get(rKeys);
                IDictionaryEnumerator ide = cTable.GetEnumerator();
                while (ide.MoveNext())
                {
                    table.Add(ide.Key, ide.Value);
                }
            }

            IDictionaryEnumerator idr = table.GetEnumerator();
            while (idr.MoveNext())
            {
                CacheEntry e = (CacheEntry)idr.Value;
                if (e == null)
                {
                    _stats.BumpMissCount();
                }
                else
                {
                    _stats.BumpHitCount();
                    Local_Insert(idr.Key, e);
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
        private CacheEntry Clustered_Get(object key)
        {
            //return Clustered_Get(Cluster.Servers, key, true);
            return null;
        }

        /// <summary>
        /// Retrieve the objects from the cluster. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>cache entry.</returns>
        private HashVector Clustered_Get(object[] keys)
        {
            //return Clustered_Get(Cluster.Servers, keys, true);
            return null;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache for the given group or sub group.
        /// </summary>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            ///return Clustered_GetKeys(group, subGroup);
            return null;
        }

        public override Common.Events.PollingResult Poll(OperationContext operationContext)
        {
            return null;
        }

        public override void RegisterPollingNotification(short callbackId, OperationContext operationContext)
        {

        }

        /// <summary>
        /// Retrieve the list of key and value pairs from the cache for the given group or sub group.
        /// </summary>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            return Clustered_GetData(group, subGroup, operationContext);
        }

        #endregion

        #region	/                 --- Partitioned ICache.Add ---           /

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on any one server-node in the cluster. The
        /// choice of the server node is made by the <see cref="LoadBalancer"/>. The notifications are
        /// initiated by the server-node, which is handled in <see cref="handleNotifyAdd"/>.
        /// </remarks>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            CheckServerAvailability();
            CacheAddResult result = CacheAddResult.Failure;
            if (Contains(key, operationContext))
            {
                return CacheAddResult.KeyExists;
            }
            else
            {
                cacheEntry.FlattenObject(_context.SerializationContext);
                result = Clustered_Add(key, cacheEntry, operationContext);
            }
            if (result == CacheAddResult.Success)
            {
                Local_Insert(key, cacheEntry);
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            CheckServerAvailability();
            try
            {
                if (eh != null && eh.IsRoutable)
                {
                    // find the node that contains the entry, so as to do a direct update
                    // otherwise we do an Add operation.
                    Address node = Clustered_Contains(key);
                    if (node != null)
                    {
                        return Clustered_Add(node, key, eh, operationContext);
                    }
                }
                else
                {
                    throw new OperationFailedException("Specified dependency is non routable");
                }
            }
            finally
            {
                //				long stop = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
                //				if(Trace.isInfoEnabled) Trace.info("PartitionedCache.Add()", "time taken: " + (stop - start));
            }

            return false;
        }

        /// <summary>
        /// Adds key and value pairs to the cache. Throws an exception or returns the
        /// list of keys for which add operation failed.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>returns the array of added keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide item-add 
        /// notificaion.
        /// </remarks>
        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            //CheckServerAvailability();

            //ArrayList list = new ArrayList();
            //Hashtable added = new Hashtable();
            //Hashtable tmp = new Hashtable();

            //list = Contains(keys);
            //int failCount = list.Count;

            //if (failCount > 0)
            //{
            //    IEnumerator ie = list.GetEnumerator();
            //    while (ie.MoveNext())
            //    {
            //        added[ie.Current] = new OperationFailedException("");
            //    }
            //}

            //for (int i=0; i<cacheEntries.Length; i++)
            //{
            //    cacheEntries[i].FlattenObject(_context.SerializationContext);
            //}

            //tmp = Clustered_Add(keys, cacheEntries);

            //if (tmp.Count > 0)
            //{
            //    if (tmp.Count < keys.Length)
            //    {
            //        object[] iArr = new object[tmp.Count];
            //        CacheEntry[] iEntry = new CacheEntry[tmp.Count];

            //        MiscUtil.FillArrays(keys, cacheEntries, iArr, iEntry, tmp);
            //        Local_Insert(iArr, iEntry);
            //    }
            //    else
            //    {
            //        Local_Insert(keys, cacheEntries);
            //    }
            //}

            //IDictionaryEnumerator ide = tmp.GetEnumerator();
            //while (ide.MoveNext())
            //{
            //    added[ide.Key] = ide.Value;
            //}

            //return added;
            return null;
        }


        /// <summary>
        /// Add the object to the near cache. 
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
        /// Add the object to the cluster. Does load balancing as well.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on any one server-node in the cluster. The
        /// choice of the server node is made by the <see cref="LoadBalancer"/>.
        /// </remarks>
        private CacheAddResult Clustered_Add(object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            CacheAddResult result = CacheAddResult.Failure;
            string group = null;
            if (cacheEntry.GroupInfo != null)
                group = cacheEntry.GroupInfo.Group;
            Address targetNode = GetNextNode(group);

            if (targetNode == null)
            {
                throw new Exception("No target node available to accommodate the data.");
            }
            result = Clustered_Add(targetNode, key, cacheEntry.RoutableClone(null), null, operationContext);
            return result;
        }


        /// <summary>
        /// Add the objects to the cluster. Does load balancing as well.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on any one server-node in the cluster. The
        /// choice of the server node is made by the <see cref="LoadBalancer"/>.
        /// </remarks>
        private Hashtable Clustered_Add(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            Hashtable result = null;

            CacheEntry entry = cacheEntries[0];
            string group = null;
            if (entry.GroupInfo != null)
                group = cacheEntries[0].GroupInfo.Group;
            Address targetNode = GetNextNode(group);

            if (targetNode == null)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    result[keys[i]] = new OperationFailedException("No target node available to accommodate the data.");
                }
            }

            for (int i = 0; i < cacheEntries.Length; i++)
            {
                CacheEntry cacheEntry = cacheEntries[i].RoutableClone(null);
                cacheEntries[i] = cacheEntry;
            }

            try
            {
                result = Clustered_Add(targetNode, keys, cacheEntries, null, operationContext);
            }
            catch (Exception inner)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    result[keys[i]] = new OperationFailedException(inner.Message, inner);
                }
            }

            return result;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Insert ---           /

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleInsert"/> on any cluster node. <see cref="handleInsert"/>
        /// on the handling node triggers either <see cref="OnItemAdded"/> or <see cref="OnItemUpdated"/>, 
        /// which in turn trigger either an item-added or item-updated cluster-wide notification.
        /// </remarks>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            //CheckServerAvailability();
            ////if(Trace.isInfoEnabled) Trace.info("PartitionedClient.Insert", "KEY->" + key);

            //CacheInsResult result = CacheInsResult.Failure;
            //cacheEntry.FlattenObject(_context.SerializationContext);
            //Address node = null;
            //DataGrouping.GroupInfo info = null;
            //ClusteredOperationResult res = Clustered_GetGroupInfo(key);
            //if (res != null)
            //{
            //    info = res.Result as DataGrouping.GroupInfo;
            //    node = res.Sender;
            //}

            //if (info != null)
            //{
            //    DataGrouping.GroupInfo newInfo = cacheEntry.GroupInfo;
            //    if (CacheHelper.CheckDataGroupsCompatibility(newInfo, info))
            //    {
            //        if (node != null)
            //        {
            //            if (cacheEntry.ExpirationHint != null && !cacheEntry.ExpirationHint.IsRoutable)
            //                throw new Exception("Specified dependency is non routable");

            //            result = Clustered_Insert(node, key, cacheEntry.RoutableClone(LocalAddress));
            //        }

            //    }
            //    else
            //        throw new Exception("Data group of the inserted item does not match the existing item's data group");
            //}
            //else
            //    result = Clustered_Insert(key, cacheEntry);

            //if ((result == CacheInsResult.Success || result == CacheInsResult.SuccessOverwrite))
            //{
            //    Local_Insert(key, cacheEntry);
            //}
            //return result;
            return null;
        }




        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entry.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>list of keys that failed to be added</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleInsert"/> on any cluster node or invokes 
        /// <see cref="Local_Insert"/> locally. The choice of the server node is determined by the 
        /// <see cref="LoadBalancer"/>.
        /// <see cref="Local_Insert"/> triggers either <see cref="OnItemAdded"/> or <see cref="OnItemUpdated"/>, which
        /// in turn trigger either an item-added or item-updated cluster-wide notification.
        /// </remarks>
        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            //Hashtable failed = null;
            //CheckServerAvailability();
            //failed = Clustered_Insert(keys, cacheEntries, notify);

            //object key;
            //for (int i = 0; i < keys.Length; i++)
            //{
            //    key = keys[i];
            //    if (failed == null || !failed.Contains(key))
            //        Local_Insert(key , cacheEntries[i]);
            //}

            //return failed;
            return null;
        }





        /// <summary>
        /// Add the object to the near cache. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheInsResult Local_Insert(object key, CacheEntry cacheEntry)
        {
            //CacheInsResult retVal = CacheInsResult.Success;
            //try
            //{
            //    if(_internalCache != null) 
            //        retVal = _internalCache.Insert(key, cacheEntry, false);
            //}
            //catch(Exception)
            //{
            //}
            //return retVal;
            return CacheInsResult.Failure;
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
        /// Updates or Adds the object to the cluster. 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cacheEntry"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method either invokes <see cref="handleInsert"/> on any cluster node or invokes 
        /// <see cref="Local_Insert"/> locally. The choice of the server node is determined by the 
        /// <see cref="LoadBalancer"/>.
        /// </remarks>
        protected override CacheInsResultWithEntry Clustered_Insert(object key, CacheEntry cacheEntry)
        {
            //Address targetNode = null;

            //string group = string.Empty;
            //if (cacheEntry.GroupInfo != null)
            //    group = cacheEntry.GroupInfo.Group;
            //targetNode = GetNextNode(group);

            //if (targetNode == null)
            //{
            //    throw new Exception("No target node available to accommodate the data.");
            //}

            //CacheInsResult result = CacheInsResult.Failure;

            //result = Clustered_Insert(targetNode, key, cacheEntry.RoutableClone(LocalAddress));

            //if (result == CacheInsResult.Success)
            //    Local_Insert(key, cacheEntry);
            //// Remove the key dependency from the cluster
            //if (result == CacheInsResult.SuccessOverwrite)
            //{
            //    if (Servers.Count > 1)
            //    {
            //        Clustered_RemoveKeyDep(key);
            //    }
            //}

            //return result;
            return null;
        }



        /// <summary>
        /// Updates or Adds the objects to the cluster. 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="cacheEntries"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method either invokes <see cref="handleInsert"/> on any cluster node or invokes 
        /// <see cref="Local_Insert"/> locally. The choice of the server node is determined by the 
        /// <see cref="LoadBalancer"/>.
        /// </remarks>
        protected override Hashtable Clustered_Insert(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            CacheEntry entry = cacheEntries[0];
            string group = null;
            if (entry.GroupInfo != null)
                group = cacheEntries[0].GroupInfo.Group;
            Address targetNode = GetNextNode(group);
            //			ArrayList nodes = _loadBalancer.GetPreferredNodeOrder(_stats.Nodes);
            //			targetNode = (nodes.Count > 0) ? (NodeInfo)nodes[0]: null;

            Hashtable inserted = null;

            // check for self addition
            //if((targetNode == null) )
            //{
            //    return new Hashtable();
            //}

            if (targetNode == null)
            {
                throw new Exception("No target node available to accommodate the data.");
            }
            for (int i = 0; i < cacheEntries.Length; i++)
            {
                cacheEntries[i] = cacheEntries[i].RoutableClone(null);
            }

            inserted = Clustered_Insert(targetNode, keys, cacheEntries, null, operationContext);


            return inserted;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Remove ---           /

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="PartitionedServerCache.handleRemove"/> on every server 
        /// node in the cluster. In a partition only one node can remove an item (due to partitioning
        /// of data). Therefore the <see cref="PartitionedServerCache.OnItemsRemoved"/> handler of 
        /// the node actually removing the item is responsible for triggering a cluster-wide 
        /// Item removed notification. 
        /// <para>
        /// <b>Note:</b> Evictions and Expirations are also handled through the 
        /// <see cref="PartitionedServerCache.OnItemsRemoved"/> handler.
        /// </para>
        /// </remarks>
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            //Local_Remove(key);
            //CheckServerAvailability();
            //return Clustered_Remove(key, ir, notify);
            return null;
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
            //Local_Remove(keys);
            //CheckServerAvailability();
            //return Clustered_Remove(keys, ir, notify);
            return null;
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            CheckServerAvailability();
            return Clustered_RemoveGroup(group, subGroup, notify, operationContext);
        }

        /// <summary>
        /// Remove the object from the near cache. 
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
        /// <returns>keys that actualy removed from the cache</returns>
        private Hashtable Local_Remove(object[] keys, OperationContext operationContext)
        {
            Hashtable retVal = null;
            try
            {
                if (_internalCache != null)
                {
                    retVal = _internalCache.Remove(keys, ItemRemoveReason.Removed, false, operationContext);
                }
            }
            catch (Exception)
            {
            }
            return retVal;
        }

        #endregion

        #region	/                 --- Partitioned ICache.GetEnumerator ---           /

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            CheckServerAvailability();
            return Clustered_GetEnumerator(Cluster.Servers, null);
        }

        #endregion

        #endregion

        #region	/                 --- Events Notifications ---           /

        #region	/                 --- OnCacheCleared ---           /

        /// <summary>
        /// Handler for clustered cache clear notification.
        /// </summary>
        /// <returns>null</returns>
        private object handleNotifyCacheCleared()
        {
            NotifyCacheCleared(true, null, null);
            return null;
        }

        #endregion

        #region	/                 --- OnItemAdded ---           /

        /// <summary>
        /// Hanlder for clustered item added notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyAdd(object info)
        {
            object[] objs = info as object[];
            if (objs != null)
            {
                OperationContext opContext = null;
                EventContext evContext = null;
                if (objs.Length > 1)
                    opContext = objs[1] as OperationContext;
                if (objs.Length > 2)
                    evContext = objs[2] as EventContext;

                NotifyItemAdded(objs[0], true, opContext, evContext); 
            }
            else
                NotifyItemAdded(info, true, null, null); 

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

        /// <summary>
        /// Hanlder for clustered item updated notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyUpdate(object info)
        {
            object[] args = info as object[];
            if (args != null)
            {
                OperationContext opContext = null;
                EventContext evContext = null;
                if (args.Length > 1)
                    opContext = args[1] as OperationContext;
                if (args.Length > 2)
                    evContext = args[2] as EventContext;


                NotifyItemUpdated(args[0], true, opContext, evContext);
            }
            else
                NotifyItemUpdated(info, true, null, null);

            return null;
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
        public override void NotifyItemRemoved(object key, object val, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext eventContext)
        {
            Local_Remove(key,operationContext);
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
                Local_Remove(key[i],operationContext);
            base.NotifyItemsRemoved(key, val, reason, async,operationContext,null);
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
                NotifyItemsRemoved((object[])objs[0], (object[])objs[1], (ItemRemoveReason)objs[2], true,operationContext, eventContexts);
            }
            else
            {
                NotifyItemRemoved(objs[0], objs[1], (ItemRemoveReason)objs[2], true,operationContext,evContext);
            }
            return null;
        }


        #endregion

        #endregion
    }
}


