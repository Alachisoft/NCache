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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Common.Locking;
#if JAVA
using Alachisoft.TayzGrid.Runtime.Exceptions;
#else
using Alachisoft.NCache.Runtime.Exceptions;
#endif
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Runtime.Caching;

using Alachisoft.NCache.Cluster.Blocks;
using Alachisoft.NCache.Cluster.Util;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
	/// <summary>
	/// This class provides the partitioned cluster cache primitives. 
	/// </summary>
	internal class PartitionOfReplicasClientCache :
		PartitionOfReplicasCacheBase 
		
	{
		/// <summary> The physical storage for this cache </summary>
		private CacheBase					_internalCache;

		/// <summary> The periodic update task. </summary>
		private PeriodicPresenceAnnouncer	_taskUpdate;

		/// <summary> The sub-cluster this node belongs to. </summary>
		private SubCluster					_currGroup;

		 
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
		/// <param name="properties">properties collection for this cache.</param>
		/// <param name="listener">cache events listener</param>
		public PartitionOfReplicasClientCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
			: base(properties, listener, context)
		{
			_stats.ClassName = @"partitioned-replicas-client";
			Initialize(cacheClasses, properties);
		}

		#region	/                 --- IDisposable ---           /

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			if(_taskUpdate != null)
			{
				_taskUpdate.Cancel();
				_taskUpdate = null;
			}
			if(_internalCache != null)
			{
				_internalCache.Dispose();
				_internalCache = null;
			}
			base.Dispose();
		}

		#endregion

		/// <summary> 
		/// Returns the cache local to the node, i.e., internal cache.
		/// </summary>
		protected override internal CacheBase InternalCache
		{ 
			get { return _internalCache; } 
		}

		#region	/                 --- Initialization ---           /

		/// <summary>
		/// Method that allows the object to initialize itself. Passes the property map down 
		/// the object hierarchy so that other objects may configure themselves as well..
		/// </summary>
		/// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
		/// <param name="properties">properties collection for this cache.</param>
		protected override void Initialize(IDictionary cacheClasses, IDictionary properties)
		{ 
			if(properties == null)
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
                        _internalCache = CacheBase.Synchronized(new LocalCache(cacheClasses,this, frontCacheProps, null, _context, null));
                    }
                    else if (cacheType.CompareTo("overflow-cache") == 0)
                    {
                        _internalCache = CacheBase.Synchronized(new OverflowCache(cacheClasses,this, frontCacheProps, null, _context, null));
                    }
                    // else throw new ConfigurationException("invalid or non-local cache scheme specified in Replicated cache");
                }
                catch (Exception)
                {
                    // Dispose();
                }

                _loadBalancer = new CoordinatorBiasedObjectCountBalancer(_internalCache.Context);
                _stats.Nodes = new ArrayList(2);
                _initialJoiningStatusLatch = new Latch();

                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(false, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)));
                _stats.GroupName = Cluster.ClusterName;

                _initialJoiningStatusLatch.WaitForAny(NodeStatus.Running);
                 DetermineClusterStatus();
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

		#region	/                 --- Overrides for ClusteredCache ---           /

		/// <summary>
		/// Called after the membership has been changed. Lets the members do some
		/// member oriented tasks.
		/// </summary>
		public override void OnAfterMembershipChange()
		{
			base.OnAfterMembershipChange();
			_currGroup = Cluster.CurrentSubCluster;
            //Set joining completion status.
            _initialJoiningStatusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            //DetermineClusterStatus();
            //UpdateCacheStatistics();
            //_statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionOfReplicasClientCache.OnAfterMembershipChange", "changing status to running");
		}

		/// <summary>
		/// Called when a new member joins the group.
		/// </summary>
		/// <param name="address">address of the joining member</param>
		/// <param name="identity">additional identity information</param>
		/// <returns>true if the node joined successfuly</returns>
		public override bool OnMemberJoined(Address address, NodeIdentity identity)
		{
			if(!base.OnMemberJoined(address, identity) || !((Identity)identity).HasStorage)
				return false;

			NodeInfo info = new NodeInfo(address as Address);
			info.SubgroupName = identity.SubGroupName;
		    lock (_stats.Nodes.SyncRoot)
		    {
		        _stats.Nodes.Add(info);
		    }
		    //_statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);

			if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServer.OnMemberJoined()", "Partition extended: " + address);
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


			if(Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServer.OnMemberLeft()", "Partition shrunk: " + address);
			return true;
		}

		/// <summary>
		/// Handles the function requests.
		/// </summary>
		/// <param name="func"></param>
		/// <returns></returns>
        public override object HandleClusterMessage(Address src, Function func)
		{
			switch(func.Opcode)
			{
				case (int)OpCodes.PeriodicUpdate:
					return handlePresenceAnnouncement(src, func.Operand);

				case (int)OpCodes.NotifyAdd:
					return handleNotifyAdd(func.Operand);

				case (int)OpCodes.NotifyUpdate:
					return handleNotifyUpdate(func.Operand);

				case (int)OpCodes.NotifyRemoval:
					return handleNotifyRemoval(func.Operand);
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

				for(int i=0; i<results.size(); i++)
				{
					Rsp rsp = (Rsp) results.elementAt(i);
					NodeInfo nodeInfo = rsp.Value as NodeInfo;
					if(nodeInfo != null)
					{
						handlePresenceAnnouncement(rsp.Sender as Address, nodeInfo);
					}
				}
            }
			catch(Exception e)
			{
				if (Context != null)
                {
                    Context.NCacheLog.Error("PoRServer.DetermineClusterStatus()", e.ToString());
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
				if(other != null && info != null)
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
				CacheStatistics c = ClusterHelper.CombinePartitionReplicasStatistics(_stats);
				_stats.UpdateCount( c.Count );
				_stats.HitCount = c.HitCount;
				_stats.MissCount = c.MissCount;
				_stats.MaxCount = c.MaxCount;
			}
			catch(Exception)
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
                //return Clustered_Count(true); 
			}
		}

		/// <summary>
		/// Returns the count of local cache items only.
		/// </summary>
		/// <returns>count of items.</returns>
		private long Local_Count()
		{
			if(_internalCache != null)
				return _internalCache.Count;
			return 0;
		}
		
        ///// <summary>
        ///// Returns the count of clustered cache items.
        ///// </summary>
        ///// <returns>Count of nodes in cluster.</returns>
        //private long Clustered_Count(bool excludeSelf)
        //{
        //    return Clustered_Count(Cluster.SubCoordinators, excludeSelf);
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
			if(Local_Contains( key,operationContext ))
			{
				return true;
			}

			CheckServerAvailability();
			return Clustered_Contains( key, true ) != null;
		}


		/// <summary>
		/// Determines whether the cache contains the given keys.
		/// </summary>
		/// <param name="keys">The keys to locate in the cache.</param>
		/// <returns>list of available keys from the given key list</returns>
        public override Hashtable Contains(object[] keys, OperationContext operationContext)
		{
            //ArrayList list = Local_Contains(keys);
            //if (list.Count < keys.Length)
            //{
            //    CheckServerAvailability();

            //    object[] rKeys = MiscUtil.GetNotAvailableKeys(keys, list);

            //    // check the cluster minus ourselves.
            //    Hashtable clusterTable = Clustered_Contains(rKeys, true);

            //    IDictionaryEnumerator ide = clusterTable.GetEnumerator();
            //    while (ide.MoveNext())
            //    {
            //        ArrayList keyArr = (ArrayList)ide.Value;
            //        list.AddRange(keyArr);
            //    }
            //}

            //return list;
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
			if(_internalCache != null)
				retVal = _internalCache.Contains( key, operationContext );
			return retVal;
		}
		
		/// <summary>
		/// Determines whether the local cache contains the given keys.
		/// </summary>
		/// <param name="key">The key to locate in the cache.</param>
		/// <returns>List of the available keys</returns>
		private ArrayList Local_Contains(object[] keys)
		{
            //ArrayList list = new ArrayList();
            //if(_internalCache != null)
            //{
            //    list = _internalCache.Contains( keys );
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
		private Address Clustered_Contains(object key, bool excludeSelf)
		{
			//return Clustered_Contains(Cluster.SubCoordinators, key, excludeSelf);
            return null;
		}

		/// <summary>
		/// Determines whether the cluster contains the specified keys.
		/// </summary>
		/// <param name="keys">The keys to locate in the cache.</param>
		/// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
		/// <returns>addresses of the nodes that contains the specified keys;</returns>
		private Hashtable Clustered_Contains(object[] keys, bool excludeSelf)
		{
			//return Clustered_Contains(Cluster.SubCoordinators, keys, excludeSelf);
            return null;
		}

		#endregion


        #region /                 --- Partitioned Search ---               /

        private ICollection Clustered_Search(string queryText, IDictionary values)
        {
            //return Clustered_Search(Cluster.SubCoordinators, queryText, values, true);
            return null;
        }

        private IDictionary Clustered_SearchEntries(string queryText, IDictionary values)
        {
            //return Clustered_SearchEntries(Cluster.SubCoordinators, queryText, values, true);
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
		/// This method invokes <see cref="handleClear"/> on every node in the partition, 
		/// which then fires OnCacheCleared locally. The <see cref="handleClear"/> method on the 
		/// coordinator will also trigger a cluster-wide notification to the clients.
		/// </remarks>
        public override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
		{
			Local_Clear(operationContext);
			CheckServerAvailability();
            Clustered_Clear(null, null, false,operationContext);
		}

		/// <summary>
		/// Removes all entries from the local cache only.
		/// </summary>
		private void Local_Clear(OperationContext operationContext)
		{
			if(_internalCache != null)
				_internalCache.Clear(null, DataSourceUpdateOptions.None,operationContext);
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
			CacheEntry e = Local_Get( key,operationContext );
			if (e == null && Servers.Count > 0)
			{
				e = Clustered_Get(key, true,operationContext);
			}
			if(e == null)
			{
				_stats.BumpMissCount();
			}
			else
			{
				_stats.BumpHitCount();
				Local_Insert(key, e, false);
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
                HashVector cTable = Clustered_Get(rKeys, true, operationContext);
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
					Local_Insert(idr.Key, e, false);
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
			if(_internalCache != null) 
				retVal = _internalCache.Get( key,operationContext );
			return retVal;
		}
		
		/// <summary>
		/// Retrieve the objects from the local cache only. 
		/// </summary>
		/// <param name="keys">keys of the entry.</param>
		/// <returns>cache entries.</returns>
        private IDictionary Local_Get(object[] keys, OperationContext operationContext)
		{
			if(_internalCache != null) 
				return _internalCache.Get( keys,operationContext );

			return null;
		}

		/// <summary>
		/// Retrieve the object from the cluster. 
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
		/// <returns>cache entry.</returns>
        private CacheEntry Clustered_Get(object key, bool excludeSelf, OperationContext operationContext)
		{
			return Clustered_Get(Cluster.SubCoordinators, key, excludeSelf,operationContext);
		}

		/// <summary>
		/// Retrieve the objects from the cluster. 
		/// </summary>
		/// <param name="keys">keys of the entries.</param>
		/// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
		/// <returns>cache entry.</returns>
        private HashVector Clustered_Get(object[] keys, bool excludeSelf, OperationContext operationContext)
		{
			return Clustered_Get(Cluster.SubCoordinators, keys, excludeSelf,operationContext);
		}
		

		/// <summary>
		/// Retrieve the list of keys fron the cache for the given group or sub group.
		/// </summary>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
		{
			/// Wait until the object enters the running status
			_statusLatch.WaitForAny(NodeStatus.Running);
			if(_internalCache == null) throw new InvalidOperationException();
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
		/// Retrieve the list of key and value pairs from the cache for the given group or sub group.
		/// </summary>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
		{
			/// Wait until the object enters the running status
			_statusLatch.WaitForAny(NodeStatus.Running);
			if(_internalCache == null) throw new InvalidOperationException();
			return Clustered_GetData(group, subGroup,operationContext);
		}


		/// <summary>
		/// Retrieve the keys of the given group from the cluster. 
		/// </summary>
		/// <param name="group">group for which data is required.</param>
		/// <param name="subGroup">sub group of the group.</param>
		/// <returns>list of keys</returns>
		private ArrayList Clustered_GetKeys(string group, string subGroup,OperationContext operationContext)
		{
            return Clustered_GetGroupKeys(Cluster.SubCoordinators, group, subGroup, operationContext);
		}


		/// <summary>
		/// Retrieve the key and value pairs of the given group from the cluster. 
		/// </summary>
		/// <param name="group">group for which data is required.</param>
		/// <param name="subGroup">sub group of the group.</param>
		/// <returns>list of key and value pairs</returns>
        private HashVector Clustered_GetData(string group, string subGroup, OperationContext operationContext)
		{
			return Clustered_GetGroupData(Cluster.SubCoordinators, group, subGroup,operationContext);
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
		/// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster, 
		/// or invokes <see cref="Local_Add"/> locally. <see cref="Local_Add"/> can only be called on 
		/// one node in the cluster, therefore it triggers a cluster-wide item added notification.
		/// </remarks>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
		{
            //CheckServerAvailability();
            //if(Trace.isInfoEnabled) Trace.info("Replicated.Add()", "Key = " + key);

            CacheAddResult result = CacheAddResult.Success;
            //Exception thrown = null;
            //try
            //{
            //    Address address = Clustered_Contains(key, true);
            //    if (address != null)
            //        return CacheAddResult.KeyExists;

            //    CacheEntry remote = cacheEntry.FlattenedClone(_context.SerializationContext);
            //    // Try to add to the local node and the cluster.
            //    result = Clustered_Add(key, remote.RoutableClone(null));
            //    if (result == CacheAddResult.KeyExists)
            //    {
            //        Trace.error("Replicated.Clustered_Add()", "KeyExists = " + key + ", result = " + result);
            //        return result;
            //    }
            //}
            //catch (Exception e)
            //{
            //    Trace.error("Replicated.Clustered_Add()", e.ToString());
            //    thrown = e;
            //}

            //if (result != CacheAddResult.Success || thrown != null)
            //{
            //    Clustered_Remove(key, ItemRemoveReason.Removed, false);
            //    if (thrown != null) throw thrown;
            //}
            //else if (notify)
            //{
            //    // If everything went ok!, initiate local and cluster-wide notifications.
            //    RaiseItemAddNotifier(key);
            //    handleNotifyAdd(key);
            //}
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
            try
            {
                if (eh != null && eh.IsRoutable)
                {
                    Address address = Clustered_Contains(key, true);
                    if (address != null)
                    {
                        SubCluster cluster = Cluster.GetSubCluster(address);
                        return Clustered_Add(cluster.Coordinator, key, eh,operationContext);
                    }
                }
                else
                {
                    throw new OperationFailedException("Specified dependency is non routable");
                }
            }
            catch (Exception e)
            {
				if (Context != null)
                {
                    Context.NCacheLog.Error("PartitionOfReplicas.Add()", e.ToString());
                }
                throw e;
            }

            return false;
        }
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
        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
		{
            //CheckServerAvailability();

            //Hashtable addResult = new Hashtable();
            //Hashtable tmp = new Hashtable();

            //ArrayList list = Contains(keys);
            //int failCount = list.Count;
            //if (failCount > 0)
            //{
            //    IEnumerator ie = list.GetEnumerator();
            //    while (ie.MoveNext())
            //    {
            //        addResult[ie.Current] = new OperationFailedException("The specified key already exists.");
            //    }
            //    if (failCount == keys.Length) return addResult;
            //}

            //Exception thrown = null;
            //CacheEntry entry;
            //for (int i = 0; i < cacheEntries.Length; i++)
            //{
            //    entry = cacheEntries[i].FlattenedClone(_context.SerializationContext);
            //    cacheEntries[i] = entry.RoutableClone(null);
            //}
            //try
            //{
            //    // Try to add to the local node and the cluster.
            //    tmp = Clustered_Add(keys, cacheEntries);

            //    if (tmp.Count > 0)
            //    {
            //        if (tmp.Count < keys.Length)
            //        {
            //            object[] iArr = new object[tmp.Count];
            //            CacheEntry[] iEntry = new CacheEntry[tmp.Count];

            //            MiscUtil.FillArrays(keys, cacheEntries, iArr, iEntry, tmp);
            //            Local_Insert(iArr, iEntry, false);
            //        }
            //        else
            //        {
            //            Local_Insert(keys, cacheEntries, false);
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    Trace.error("PartitionOfReplicas.Clustered_Add()", e.ToString());
            //    for (int i = 0; i < keys.Length; i++)
            //    {
            //        tmp[keys[i]] = new OperationFailedException(e.Message, e);
            //    }
            //    thrown = e;
            //}
            //if (thrown != null)
            //{
            //    Clustered_Remove(keys, ItemRemoveReason.Removed, false);
            //    //if (thrown != null) throw thrown;
            //}
            //else
            //{
            //    failCount = 0;
            //    ArrayList failKeys = new ArrayList();
            //    IDictionaryEnumerator ide = tmp.GetEnumerator();
            //    while (ide.MoveNext())
            //    {
            //        if (ide.Value is CacheAddResult)
            //        {
            //            CacheAddResult res = (CacheAddResult)ide.Value;
            //            switch (res)
            //            {
            //                case CacheAddResult.Failure:
            //                case CacheAddResult.KeyExists:
            //                case CacheAddResult.NeedsEviction:
            //                    failCount++;
            //                    failKeys.Add(ide.Key);
            //                    addResult[ide.Key] = ide.Value;
            //                    break;

            //                case CacheAddResult.Success:
            //                    addResult[ide.Key] = ide.Value;
            //                    break;
            //            }
            //        }
            //        else //it means value is exception
            //        {
            //            failCount++;
            //            failKeys.Add(ide.Key);
            //            addResult[ide.Key] = ide.Value;
            //        }
            //    }

            //    if (failCount > 0)
            //    {
            //        object[] keysToRemove = new object[failCount];
            //        failKeys.CopyTo(keysToRemove, 0);

            //        Clustered_Remove(keysToRemove, ItemRemoveReason.Removed, false);
            //    }
            //    if (notify)
            //    {
            //        IDictionaryEnumerator ie = addResult.GetEnumerator();
            //        while (ie.MoveNext())
            //        {
            //            object key = ie.Key;
            //            // If everything went ok!, initiate local and cluster-wide notifications.
            //            RaiseItemAddNotifier(key);
            //            handleNotifyAdd(key);
            //        }
            //    }
            //}
            //return addResult;
            return null;
		}
		
		/// <summary>
		/// Add the object to the local cache. 
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <returns>cache entry.</returns>
		/// <remarks>
		/// This method can only be called on one node in the cluster. It triggers <see cref="OnItemAdded"/>,
		/// which initiates a cluster-wide item added notification.
		/// </remarks>
        private CacheAddResult Local_Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
		{
			CacheAddResult retVal = CacheAddResult.Failure;
			if(_internalCache != null) 
				retVal = _internalCache.Add(key, cacheEntry, notify,operationContext);
			return retVal;
		}

		/// <summary>
		/// Add the object to the local cache. 
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <returns>cache entry.</returns>
		/// <remarks>
		/// This method can only be called on one node in the cluster. It triggers <see cref="OnItemAdded"/>,
		/// which initiates a cluster-wide item added notification.
		/// </remarks>
		private Hashtable Local_Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
		{
			Hashtable table = new Hashtable();
			if(_internalCache != null) 
			{
				table = _internalCache.Add(keys, cacheEntries, notify,operationContext);
			}
			return table;
		}

		/// <summary>
		/// Add the object to the cluster. Does load balancing as well.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <returns>cache entry.</returns>
		/// <remarks>
		/// This method either invokes <see cref="handleAdd"/> on one of the server nodes in the cluster, 
		/// or invokes <see cref="Local_Add"/> locally.
		/// </remarks>
        private CacheAddResult Clustered_Add(object key, CacheEntry cacheEntry)
        {
            SubCluster targetGroup = null;
            string group = null;
            if (cacheEntry.GroupInfo != null)
                group = cacheEntry.GroupInfo.Group;
            targetGroup = GetNextSubCluster(group);
            if (targetGroup == null)
            {
                throw new Exception("No target node available to accommodate the data.");
            }

            return CacheAddResult.Success;//Clustered_Add(targetGroup.Servers, key, cacheEntry, null);
        }

		/// <summary>
		/// Add the object to the cluster. 
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <returns>cache entry.</returns>
		/// <remarks>
		/// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
		/// fails on any one node the whole operation is considered to have failed and is rolled-back.
		/// </remarks>
        private Hashtable Clustered_Add(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            Hashtable addResult = new Hashtable();
            SubCluster targetGroup = null;
            string group = null;

            if (cacheEntries[0].GroupInfo != null)
                group = cacheEntries[0].GroupInfo.Group;
           
            targetGroup = GetNextSubCluster(group);
            
            if (targetGroup == null)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    addResult[keys[i]] = new OperationFailedException("No target node available to accommodate the data.");
                }
            }

            try
            {
                addResult = Clustered_Add(Cluster.LocalAddress, keys, cacheEntries, null,operationContext);
            }
            catch (Exception inner)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    addResult[keys[i]] = new OperationFailedException(inner.Message, inner);
                }
            }
            return addResult;
        }

        #endregion

        /// <summary>
        /// Determines weather a node can handle a particular group data.
        /// </summary>
        /// <param name="targetNode">Target node</param>
        /// <param name="entry">actual data</param>
        /// <returns>true, if node is compatible otherwise false.</returns>
        protected bool IsCompatibleDataGroupNode(Address targetNode, CacheEntry entry)
        {
            bool strict = false;
            bool compatible = false;
            bool nodeAffinity = false;
            if (entry != null)
            {
                DataGrouping.GroupInfo info = entry.GroupInfo;
                if (info != null)
                {
                    string group = info.Group;

                    if (_stats.ClusterDataAffinity != null && _stats.ClusterDataAffinity.Contains(group))
                        strict = true;

                    NodeInfo node = _stats.GetNode(targetNode);
                    if (node != null)
                    {
                        if (node.DataAffinity != null && node.DataAffinity.Groups.Contains(group))
                            compatible = true;
                        nodeAffinity = node.DataAffinity.Strict;
                    }
                }
            }
            compatible = strict ? compatible : (nodeAffinity ? false : true);
            return compatible;
        }

        #region	/                 --- Partitioned ICache.Insert ---           /

		/// <summary>
		/// Adds a pair of key and value to the cache. If the specified key already exists 
		/// in the cache; it is updated, otherwise a new item is added to the cache.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <param name="cacheEntry">the cache entry.</param>
		/// <returns>returns the result of operation.</returns>
		/// <remarks>
		/// This method either invokes <see cref="handleInsert"/> on any cluster node or invokes 
		/// <see cref="Local_Insert"/> locally. The choice of the server node is determined by the 
		/// <see cref="LoadBalancer"/>.
		/// <see cref="Local_Insert"/> triggers either <see cref="OnItemAdded"/> or <see cref="OnItemUpdated"/>, which
		/// in turn trigger either an item-added or item-updated cluster-wide notification.
		/// </remarks>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accesType, OperationContext operationContext)
		{
            //CheckServerAvailability();

            //if (_internalCache == null) throw new InvalidOperationException();

            //if(Trace.isInfoEnabled) Trace.info("Replicated.Insert()", "Key = " + key);

            //CacheInsResult result = CacheInsResult.Success;
            //CacheEntry pEntry = null;
            //Exception thrown = null;
            //Address address = null;
            //DataGrouping.GroupInfo oldInfo, newInfo;

            //try
            //{
            //    pEntry = Get(key);// dont remove this line.
            //    CacheEntry remote = cacheEntry.FlattenedClone(_context.SerializationContext);
       
            //    address = Clustered_Contains(key, true);

            //    if (address != null)
            //    {
            //        oldInfo = pEntry.GroupInfo;
            //        newInfo = remote.GroupInfo;
            //        if (!address.Equals(Cluster.LocalAddress))
            //        {
            //            if (CacheHelper.CheckDataGroupsCompatibility(newInfo, oldInfo))
            //            {
            //                //if (cacheEntry.ExpirationHint != null && !cacheEntry.ExpirationHint.IsRoutable)
            //                //    throw new Exception("Specified dependency is non routable");
            //            }
            //            else
            //                throw new Exception("Data group of the inserted item does not match the existing item's data group");
            //        }

            //        SubCluster cluster = Cluster.GetSubCluster(address);
            //        result = Clustered_Insert(cluster.Servers, key, remote);
            //    }
            //    else
            //        result = result = Clustered_Insert(key, remote);

            //}
            //catch (Exception e)
            //{
            //    thrown = e;
            //}

            ////			if(result == CacheInsResult.Success || result == CacheInsResult.SuccessOverwrite)
            ////			{
            ////			}
            ////
            //// Try to insert to the local node and the cluster.
            
            //// Try to insert to the local node and the cluster.
            //if ((result == CacheInsResult.NeedsEviction || result == CacheInsResult.Failure) || thrown != null)
            //{
            //    Trace.warn("Replicated.Insert()", "rolling back, since result was " + result);
            //    /// failed on the cluster, so remove locally as well.
            //    Clustered_Remove(key, ItemRemoveReason.Removed, false);
            //    if (thrown != null) throw thrown;
            //}

            //if (notify && result == CacheInsResult.Success)
            //{
            //    RaiseItemAddNotifier(key);
            //    handleNotifyUpdate(key);
            //    Local_Insert(key, cacheEntry, notify);
            //}
            //if (notify && result == CacheInsResult.SuccessOverwrite)
            //{
            //    if (pEntry != null)
            //    {
            //        object value = pEntry.Value;//pEntry.DeflattedValue(_context.SerializationContext);
            //        if (value is CallbackEntry)
            //        {
            //            RaiseCustomUpdateCalbackNotifier(key, (ArrayList)((CallbackEntry)value).ItemUpdateCallbackListener);
            //        }
            //    }

            //    RaiseItemUpdateNotifier(key);
            //    handleNotifyUpdate(key);
            //    Local_Insert(key, cacheEntry, notify);
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
            //Hashtable failed;
            //CheckServerAvailability();
            //failed = Clustered_Insert(keys, cacheEntries, notify);

            //object key;

            //for (int i = 0; i < keys.Length; i++)
            //{
            //    key = keys[i];
            //    if (failed == null || !failed.Contains(key))
            //        Local_Insert(key, cacheEntries[i],notify);
            //}
            //return failed;
            return null;
        }

        /// <summary>
        /// Insert the object to the local cache. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheInsResult Local_Insert(object key, CacheEntry cacheEntry, bool notify)
		{
            //CacheInsResult retVal = CacheInsResult.Failure;
            //if(_internalCache != null) 
            //    retVal = _internalCache.Insert(key, cacheEntry, notify);
            //return retVal;
            return CacheInsResult.Failure;
		}


		/// <summary>
		/// Insert the objects to the local cache. 
		/// </summary>
		/// <param name="keys">keys of the entries.</param>
		/// <returns>cache entries.</returns>
        private Hashtable Local_Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
		{
			Hashtable retVal = null;
			if(_internalCache != null) 
				retVal = _internalCache.Insert(keys, cacheEntries, notify,operationContext);
			return retVal;
		}
 
		 
		#endregion

		#region	/                 --- Partitioned ICache.Remove ---           /

		/// <summary>
		/// Removes the object and key pair from the cache. The key is specified as parameter.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <returns>cache entry.</returns>
		/// <remarks>
		/// This method invokes <see cref="handleRemove"/> on every server node in the cluster. In
		/// a partition only one node can remove an item (due to partitioning of data). Therefore the
		/// <see cref="OnItemsRemoved"/> handler of the node actually removing the item is responsible
		/// for triggering a cluster-wide Item removed notification. 
		/// <para>
		/// <b>Note:</b> Evictions and Expirations are also handled through the 
		/// <see cref="OnItemsRemoved"/> handler.
		/// </para>
		/// </remarks>
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
		{
            //Local_Remove(key, ItemRemoveReason.Removed, false);
            //CheckServerAvailability();
            //CacheEntry e = Clustered_Remove(key, ir, false);
            //if (notify && e != null)
            //{
            //    object value = e.Value;// e.DeflattedValue(_context.SerializationContext);
            //    if (value is CallbackEntry)
            //    {
            //        RaiseCustomRemoveCalbackNotifier(key, (CallbackEntry)value, ir);
            //    }

            //    object data = new object[] { key, e, ir };
            //    RaiseItemRemoveNotifier(data);
            //    handleNotifyRemoval(data);
            //}
            //return e;
            return null;
		}
		
		/// <summary>
		/// Removes the key and value pairs from the cache. The keys are specified as parameter.
		/// </summary>
		/// <param name="keys">keys of the entries.</param>
		/// <returns>list of removed keys</returns>
		/// <remarks>
		/// This method invokes <see cref="handleRemove"/> on every server node in the cluster. In
		/// a partition only one node can remove an item (due to partitioning of data). Therefore the
		/// <see cref="OnItemsRemoved"/> handler of the node actually removing the item is responsible
		/// for triggering a cluster-wide Item removed notification. 
		/// <para>
		/// <b>Note:</b> Evictions and Expirations are also handled through the 
		/// <see cref="OnItemsRemoved"/> handler.
		/// </para>
		/// </remarks>
        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
		{
            //Local_Remove(keys, ItemRemoveReason.Removed, false);
            //CheckServerAvailability();
            //Hashtable removed = Clustered_Remove(keys, ir, false);
            //if (notify)
            //{
            //    if (removed != null)
            //    {
            //        IDictionaryEnumerator ide = removed.GetEnumerator();
            //        while (ide.MoveNext())
            //        {
            //            object key = ide.Key;
            //            CacheEntry e = (CacheEntry)ide.Value;
            //            if (e != null)
            //            {
            //                object value = e.Value;// e.DeflattedValue(_context.SerializationContext);
            //                if (value is CallbackEntry)
            //                {
            //                    RaiseCustomRemoveCalbackNotifier(key, (CallbackEntry)value, ir);
            //                }

            //                object data = new object[] { key, e, ir };
            //                RaiseItemRemoveNotifier(data);
            //                handleNotifyRemoval(data);
            //            }
            //        }
            //    }
            //}

            //return removed;
            return null;
		}

		/// <summary>
		/// Remove the object from the local cache only. 
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <returns>cache entry.</returns>
        private CacheEntry Local_Remove(object key, ItemRemoveReason ir, bool notify, OperationContext operationContext)
		{
			CacheEntry retVal = null;
			if(_internalCache != null)
                retVal = _internalCache.Remove(key, ir, notify, null, 0, LockAccessType.IGNORE_LOCK,operationContext);
			return retVal;
		}
		
		/// <summary>
		/// Remove the objects from the local cache only. 
		/// </summary>
		/// <param name="keys">keys of the entries.</param>
		/// <returns>list of removed keys.</returns>
        private Hashtable Local_Remove(object[] keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
		{
			Hashtable removedKeys = null;
			if(_internalCache != null) 
				removedKeys = _internalCache.Remove( keys, ir, notify,operationContext );
			return removedKeys;
		}

		/// <summary>
		/// Remove the group from cache.
		/// </summary>
		/// <param name="group">group to be removed.</param>
		/// <param name="subGroup">subGroup to be removed.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
		{
			CheckServerAvailability();
            return null;//Clustered_RemoveGroup(group, subGroup, notify,operationContext);
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
			if(Servers.Count <= 1)
			{
				return _internalCache.GetEnumerator();
			}

			return Clustered_GetEnumerator(Cluster.SubCoordinators, null);
		}

		#endregion

		#endregion

//        #region	/                 --- ICacheEventsListener ---           /

//        #region	/                 --- OnCacheCleared ---           /

//        /// <summary> 
//        /// Fire when the cache is cleared. 
//        /// </summary>
//        void ICacheEventsListener.OnCacheCleared()
//        {
//            // do local notifications only, every node does that, so we get a replicated notification.
//            UpdateCacheStatistics();
//            handleNotifyCacheCleared();
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="notifId"></param>
//        /// <param name="data"></param>
//        void ICacheEventsListener.OnCustomEvent(object notifId, object data)
//        {
//        }

        /// <summary>
        /// Hanlder for clustered cache clear notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyCacheCleared()
        {
            NotifyCacheCleared(true, null, null);
            return null;
        }

//        #endregion

//        #region	/                 --- OnItemAdded ---           /

//        /// <summary> 
//        /// Fired when an item is added to the cache. 
//        /// </summary>
//        /// <remarks>
//        /// Triggers a cluster-wide item added notification.
//        /// </remarks>
//        void ICacheEventsListener.OnItemAdded(object key)
//        {
////			long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
//            // Handle all exceptions, do not let the effect seep thru
//            try
//            {
//                UpdateCacheStatistics();
//                // do not broad cast if there is only one node.
//                if(IsItemAddNotifier && _currGroup.IsCoordinator && ValidMembers.Count > 1)
//                {
//                    AggregateFunction func = new AggregateFunction
//                    (
//                        new Function((int)OpCodes.NotifyAdd, key),
//                        new Function((int)OpCodes.PeriodicUpdate, _stats.LocalNode.Clone())
//                    );
//                    Cluster.SendNoReplyMessageAsync(func);
//                }
//                //else
//                //{
//                //    Cluster.SendNoReplyMessageAsync(new Function((int)OpCodes.PeriodicUpdate, _stats.LocalNode.Clone()));
//                //}
//                handleNotifyAdd(key);
//                if(Trace.isInfoEnabled) Trace.info("PoRServer.OnItemAdded()", "key: " + key.ToString());
//            }
//            catch(Exception e)
//            {
//                Trace.error("PoRServer.OnItemAdded()", e.ToString());
//            }
////			finally
////			{
////				long stop = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
////				if(Trace.isInfoEnabled) Trace.info("PoRServer.OnItemAdded()", "key: " + key.ToString());
////			}
//        }

       

//        #endregion

//        #region	/                 --- OnItemUpdated ---           /

//        /// <summary> 
//        /// handler for item updated event.
//        /// </summary>
//        void ICacheEventsListener.OnItemUpdated(object key)
//        {
////			long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
//            // Handle all exceptions, do not let the effect seep thru
//            try
//            {
//                UpdateCacheStatistics();
//                // do not broad cast if there is only one node.
//                bool clusteredNotif = ValidMembers.Count > 1;
//                if (IsItemUpdateNotifier && _currGroup.IsCoordinator && ValidMembers.Count > 1)
//                {
//                    AggregateFunction func = new AggregateFunction
//                    (
//                        new Function((int)OpCodes.NotifyUpdate, key),
//                        new Function((int)OpCodes.PeriodicUpdate, _stats.LocalNode.Clone())
//                    );
//                    Cluster.SendNoReplyMessageAsync(func);
//                }
//                //else
//                //{
//                //    Cluster.SendNoReplyMessageAsync(new Function((int)OpCodes.PeriodicUpdate, _stats.LocalNode.Clone()));
//                //}
//                handleNotifyUpdate(key);
//                if(Trace.isInfoEnabled) Trace.info("PoRServer.OnItemUpdated()", "key: " + key.ToString());
//            }
//            catch(Exception e)
//            {
//                Trace.error("PoRServer.OnItemUpdated()", e.ToString());
//            }
////			finally
////			{
////				long stop = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
////			}
//        }

       


//        #endregion

//        #region	/                 --- OnItemRemoved ---           /

//        /// <summary> 
//        /// Fired when an item is removed from the cache.
//        /// </summary>
//        void ICacheEventsListener.OnItemRemoved(object key, object val, ItemRemoveReason reason)
//        {
//            ((ICacheEventsListener)this).OnItemsRemoved(new object[] {key}, new object[] {val}, reason);
//        }

//        /// <summary> 
//        /// Fired when multiple items are removed from the cache. 
//        /// </summary>
//        /// <remarks>
//        /// In a partition only one node can remove an item (due to partitioning of data). 
//        /// Therefore this handler triggers a cluster-wide Item removed notification.
//        /// </remarks>
//        void ICacheEventsListener.OnItemsRemoved(object[] keys, object[] values, ItemRemoveReason reason)
//        {
////			long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
//            // Handle all exceptions, do not let the effect seep thru
//            try
//            {
//                // do not notify if explicitly removed by Remove()
//                // if(reason == ItemRemoveReason.Removed) return;
//                UpdateCacheStatistics();
////				Cluster.SendNoReplyMessageAsync(new Function((int)OpCodes.PeriodicUpdate, _stats.LocalNode.Clone()));

//                CacheEntry entry;
//                for(int i=0; i<keys.Length; i++)
//                {
//                    if(values[i] == null) continue;
//                    object data = new object[] {keys[i], values[i], reason};

//                    entry = (CacheEntry)values[i];
//                    object value = entry.DeflattedValue(_context.SerializationContext);
//                    if (value is CallbackEntry)
//                    {
//                        RaiseCustomRemoveCalbackNotifier(keys[i], (CallbackEntry)value, reason);
//                    }

//                    if (IsItemRemoveNotifier && _currGroup.IsCoordinator && ValidMembers.Count > 1)
//                        Cluster.SendNoReplyMessageAsync(new Function((int)OpCodes.NotifyRemoval, data));
//                }
			
//                NotifyItemsRemoved(keys, values, reason, true);
//            }
//            catch(Exception e)
//            {
//                Trace.error("PoRServer.OnItemsRemoved()", e.ToString());
//            }
////			finally
////			{
////				long stop = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
////				if(Trace.isInfoEnabled) Trace.info("PoRServer.OnItemsRemoved()", "time taken: " + (stop - start));
////			}
//        }

        /// <summary>
        /// Hanlder for clustered item removal notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyRemoval(object info)
        {
            object[] objs = (object[])info;
            OperationContext operationContext = null;
            EventContext evContext = null;
            if (objs.Length > 2)
                operationContext = objs[2] as OperationContext;
            if (objs.Length > 3)
            {
                evContext = objs[3] as EventContext;
            }
            //			if(objs[0] is object[])
            //			{
            //				_context.ExpiryMgr.RemoveDependencyKeys((object[])objs[0]);
            //			}
            //			else
            //			{
            //				_context.ExpiryMgr.RemoveDependencyKeys(objs[0]);
            //			}
            NotifyItemRemoved(objs[0], null, (ItemRemoveReason)objs[1], true, operationContext, evContext);
            return null;
        }


//        #endregion

//        #endregion
	}
}



