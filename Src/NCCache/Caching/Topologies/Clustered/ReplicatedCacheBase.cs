using System;
using System.Collections;
using Alachisoft;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.AutoExpiration;
using System.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// 
    /// </summary>
    internal class ReplicatedCacheBase : ClusterCacheBase
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
        protected const string MCAST_DOMAIN = ".r20";

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public ReplicatedCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
        }
        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public ReplicatedCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, IClusterEventsListener clusterListener)
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
                    Context.NCacheLog.Warn("ReplicatedCacheBase.AuthenticateNode()", "A non-recognized node attempted to join cluster -> " + address);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

     
        #region	/                 --- ReplicatedCacheBase ICache.Clear ---           /

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
                Cluster.BroadcastToServers(func, GroupRequest.GET_ALL);
            }
            catch (Exception e)
            {
               throw new GeneralFailureException(e.Message, e);
            }
        }

        #endregion

        #region	/                 --- ReplicatedCacheBase ICache.Get ---           /

        /// <summary>
        /// Retrieve the object from the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>cache entry.</returns>
        protected CacheEntry Clustered_Get(Address address, object key, ref object lockId, ref DateTime lockDate, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Get", "enter");
                Function func = new Function((int)OpCodes.Get, new object[] { key, operationContext });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST, false);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (CacheEntry)((OperationResponse)result).SerializablePayload;
                if (retVal != null) retVal.Value = ((OperationResponse)result).UserPayload;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Get", "exit");
            }
            return retVal;
        }

        #endregion

 

        #region /       --- Session Lock ---        /
        protected void Clustered_UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Unlock", "enter");
                Function func = new Function((int)OpCodes.UnLockKey, new object[] { key, lockId, isPreemptive, operationContext }, false);
                Cluster.BroadcastToMultiple(Cluster.Servers,
                        func,
                        GroupRequest.GET_ALL);
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Unlock", "exit");
            }
        }

        protected bool Clustered_Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Lock", "enter");
                Function func = new Function((int)OpCodes.LockKey, new object[] { key, lockId, lockDate, lockExpiration, operationContext }, false);

                RspList results = Cluster.BroadcastToMultiple(Cluster.Servers,
                        func,
                        GroupRequest.GET_ALL);

                try
                {
                    ClusterHelper.ValidateResponses(results, typeof(LockOptions), Name);
                }
                catch (LockingException le)
                {
                    //release the lock preemptively...
                    Clustered_UnLock(key, null, true, operationContext);
                    return false;
                }

                return ClusterHelper.FindAtomicLockStatusReplicated(results, ref lockId, ref lockDate);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Lock", "exit");
            }
        }

        protected LockOptions Clustered_IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.IsLocked, new object[] { key, lockId, lockDate, operationContext }, false);

                RspList results = Cluster.BroadcastToMultiple(Cluster.Servers,
                        func,
                        GroupRequest.GET_ALL);

                try
                {
                    ClusterHelper.ValidateResponses(results, typeof(LockOptions), Name);
                }
                catch (LockingException le)
                {
                    //release the lock preemptively...
                    Clustered_UnLock(key, null, true, operationContext);
                    return null;
                }

                return ClusterHelper.FindAtomicIsLockedStatusReplicated(results, ref lockId, ref lockDate);
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
        #endregion

        #region	/                 --- ReplicatedCacheBase ICache.Add ---           /

        /// <summary>
        /// Add the object to the cluster. Does load balancing as well.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on one of the server nodes in the cluster, 
        /// or invokes <see cref="Local_Add"/> locally.
        /// </remarks>
        protected CacheAddResult Clustered_Add(ArrayList dests, object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            CacheAddResult result = CacheAddResult.Failure;
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Add", "enter");

                /// Ask every server to add the object, except myself.
                Function func = new Function((int)OpCodes.Add, new object[] { key, cacheEntry, operationContext }, false, key);
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
                RspList results = Cluster.BroadcastToMultiple(dests,
                    func,
                    GroupRequest.GET_ALL, _asyncOperation);

                ClusterHelper.ValidateResponses(results, typeof(CacheAddResult), Name);

                /// Check if the operation failed on any node.
                result = ClusterHelper.FindAtomicAddStatusReplicated(results);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Add", "exit");
            }
            return result;
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
        protected bool Clustered_Add(ArrayList dests, object key, ExpirationHint eh, OperationContext operationContext)
        {
            bool result = false;
            try
            {
                /// Ask every server to add the object, except myself.
                Function func = new Function((int)OpCodes.AddHint, new object[] { key, eh, operationContext }, false, key);
                RspList results = Cluster.BroadcastToMultiple(dests,
                    func,
                    GroupRequest.GET_ALL, _asyncOperation);

                ClusterHelper.ValidateResponses(results, typeof(bool), Name);

                /// Check if the operation failed on any node.
                result = ClusterHelper.FindAtomicAddHintReplicated(results);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return result;
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
        protected Hashtable Clustered_Add(ArrayList dests, object[] keys, CacheEntry[] cacheEntries,OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.AddBlk", "enter");
                /// Ask every server to add the object, except myself.
                Function func = new Function((int)OpCodes.Add, new object[] { keys, cacheEntries,  operationContext }, false);
                RspList results = Cluster.BroadcastToMultiple(dests,
                    func,
                    GroupRequest.GET_ALL);

                ClusterHelper.ValidateResponses(results, typeof(Hashtable), Name);

                /// Check if the operation failed on any node.
                return ClusterHelper.FindAtomicBulkInsertStatusReplicated(results);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.AddBlk", "exit");
            }
        }

        #endregion

        #region	/                 --- ReplicatedCacheBase ICache.Insert ---           /

        /// <summary>
        /// Updates or Adds the object to the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on the specified node.
        /// </remarks>
        protected CacheInsResult Clustered_Insert(Address dest, object key, CacheEntry cacheEntry,  OperationContext operationContext)
        {
            CacheInsResult retVal = CacheInsResult.Failure;
            try
            {
                Function func = new Function((int)OpCodes.Insert, new object[] { key, cacheEntry, operationContext }, false, key);
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

                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST, _asyncOperation);
                if (result == null)
                {
                    return retVal;
                }

                retVal = (CacheInsResult)result;
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
        public Hashtable Clustered_Insert(object[] keys, CacheEntry[] cacheEntries,  bool notify, OperationContext operationContext)
        {
            /// Wait until the object enters any running status

            Hashtable pEntries = null;

            pEntries = Get(keys, operationContext); //dont remove

            Hashtable existingItems;
            Hashtable jointTable = new Hashtable();
            Hashtable failedTable = new Hashtable();
            Hashtable insertable = new Hashtable();
            Hashtable insertResults = null;
            ArrayList inserted = new ArrayList();
            ArrayList added = new ArrayList();
            object[] validKeys;
            CacheEntry[] validEnteries;
            int index = 0;
            object key;


            for (int i = 0; i < keys.Length; i++)
            {
                jointTable.Add(keys[i], cacheEntries[i]);
            }

            Hashtable keyValTable = jointTable.Clone() as Hashtable;

            //if (jointTable.Count > 0)
            //{
            //    index = 0;
            //    validKeys = new object[jointTable.Count];
            //    validEnteries = new CacheEntry[jointTable.Count];

            //    IDictionaryEnumerator ide = jointTable.GetEnumerator();
            //    while (ide.MoveNext())
            //    {
            //        key = ide.Key;
            //        validKeys[index] = key;
            //        index += 1;
            //    }
            //}

            if (jointTable.Count > 0)
            {
                index = 0;
                validKeys = new object[jointTable.Count];
                validEnteries = new CacheEntry[jointTable.Count];
                IDictionaryEnumerator ide = jointTable.GetEnumerator();
                while (ide.MoveNext())
                {
                    key = ide.Key;
                    validKeys[index] = key;
                    validEnteries[index] = (CacheEntry)ide.Value;
                    added.Add(key);
                    index += 1;
                }
                //for (int i = 0; i < validKeys.Length; i++)
                //{
                //    key = validKeys[i];
                //    if (jointTable.Contains(key))
                //        jointTable.Remove(key);
                //}
                try
                {
                    insertResults = null;
                    if (validKeys.Length>0)
                      insertResults = Clustered_Insert(Cluster.Servers, validKeys, validEnteries,  operationContext);
                }
                catch (Exception e)
                {
                    Context.NCacheLog.Error("ReplicatedServerCacheBase.Insert(Keys)", e.ToString());
                    for (int i = 0; i < validKeys.Length; i++)
                    {
                        failedTable.Add(validKeys[i], e);
                        added.Remove(validKeys[i]);
                    }

                    Clustered_Remove(validKeys, ItemRemoveReason.Removed, null, false, operationContext);
                }


            }
            if (insertResults != null)
            {
                Hashtable failed = CacheHelper.CompileInsertResult(insertResults);
                IDictionaryEnumerator Ie = failed.GetEnumerator();
                while (Ie.MoveNext())
                    failedTable[Ie.Key] = Ie.Value;
             }
            return failedTable;
        }



        /// <summary>
        /// Updates or Adds the objects to the cluster. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">cache entries.</param>
        /// <returns>failed keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on the specified node.
        /// </remarks>
        protected Hashtable Clustered_Insert(Address dest, object[] keys, CacheEntry[] cacheEntries,  OperationContext operationContext)
        {
            Hashtable inserted = null;
            try
            {
                Function func = new Function((int)OpCodes.Insert, new object[] { keys, cacheEntries, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return new Hashtable();
                }
                inserted = (Hashtable)result;
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

        protected CacheInsResultWithEntry Clustered_Insert(ArrayList dests, object key, CacheEntry cacheEntry,  object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Insert", "enter");
             

                /// Ask every server to update the object, except myself.
                Function func = new Function((int)OpCodes.Insert, new object[] { key, cacheEntry, _statusLatch.IsAnyBitsSet(NodeStatus.Initializing), lockId, accessType, operationContext }, false, key);
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
                RspList results = Cluster.BroadcastToMultiple(dests,
                    func,
                    GroupRequest.GET_ALL, _asyncOperation);

                ClusterHelper.ValidateResponses(results, typeof(OperationResponse), Name);

                //Bug Fixed, during state transfer (one node up with the exisiting one) of replicated cache, 
                //while client doing insert operaion continously, which incrementing the add/sec counter while the client only performing insert
                //means no need to incrment add/sec counter, need only updat/sec to be incremented
                //so after discussing with QA, we modify the code here.
                CacheInsResultWithEntry retVal = ClusterHelper.FindAtomicInsertStatusReplicated(results);
                if (retVal != null && retVal.Result == CacheInsResult.Success && results != null)
                {
                    for (int i = 0; i < results.Results.Count; i++)
                    {
                        if (((CacheInsResultWithEntry)((OperationResponse)results.Results[i]).SerializablePayload).Result == CacheInsResult.SuccessOverwrite)
                        {
                            retVal.Result = CacheInsResult.SuccessOverwrite;
                            break;
                        }
                    }
                }
                return retVal;

            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Insert", "exit");
            }
        }

        protected Hashtable Clustered_Insert(ArrayList dests, object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.InsertBlk", "enter");
                /// Ask every server to update the object, except myself.
                Function func = new Function((int)OpCodes.Insert, new object[] { keys, cacheEntries,  operationContext }, false);
                RspList results = Cluster.BroadcastToMultiple(dests,
                    func,
                    GroupRequest.GET_ALL);

                ClusterHelper.ValidateResponses(results, typeof(Hashtable), Name);

                /// Check if the operation failed on any node.
                return ClusterHelper.FindAtomicBulkInsertStatusReplicated(results);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.InsertBlk", "exit");
            }
        }

        #endregion

        #region	/                 --- ReplicatedCacheBase ICache.Remove ---           /

        /// <summary>
        /// Remove the object from the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster.
        /// </remarks>
        protected CacheEntry Clustered_Remove(object key, ItemRemoveReason ir, CallbackEntry cbEntry,   bool notify, object lockId,  LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Remove", "enter");
                Function func = new Function((int)OpCodes.Remove, new object[] { key, ir, notify, cbEntry,  lockId, accessType,  operationContext }, false, key);
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL, _asyncOperation);
                if (results == null)
                {
                    return retVal;
                }

                ClusterHelper.ValidateResponses(results, typeof(OperationResponse), Name);

                Rsp rsp = ClusterHelper.FindAtomicRemoveStatusReplicated(results);
                if (rsp == null)
                {
                    return retVal;
                }

                OperationResponse opRes = rsp.Value as OperationResponse;
                if (opRes != null)
                {
                    CacheEntry entry = opRes.SerializablePayload as CacheEntry;
                    if (entry != null)
                        entry.Value = opRes.UserPayload;
                    return entry;
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
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Remove", "exit");
            }
            return retVal;
        }

        /// <summary>
        /// Remove the objects from the cluster. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of failed keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster.
        /// </remarks>
        protected Hashtable Clustered_Remove(IList keys, ItemRemoveReason ir, CallbackEntry cbEntry,  bool notify, OperationContext operationContext)
        {
            Hashtable removedEntries = new Hashtable();
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.RemoveBlk", "enter");
                Function func = new Function((int)OpCodes.Remove, new object[] { keys, ir, notify, cbEntry, operationContext }, false);
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL);

                if (results == null)
                {
                    return removedEntries;
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
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.RemoveBlk", "exit");
            }
            return removedEntries;
        }

        /// <summary>
        /// Remove the objects from the cluster. For efficiency multiple objects are sent as one.
        /// </summary>
        /// <param name="keys">list of keys to remove.</param>
        /// <returns>true if succeded, false otherwise.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemoveRange"/> on every server node in the cluster.
        /// </remarks>
        protected bool Clustered_Remove(IList keys, ItemRemoveReason reason, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.RemoveRange, new object[] { keys, reason, operationContext }, false);
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL, true);

                if (results != null)
                {
                    for (int i = 0; i < results.size(); i++)
                    {
                        Rsp rsp1 = (Rsp)results.elementAt(i);
                        
                        if (!rsp1.wasReceived())
                        {
                            Context.NCacheLog.Error("ReplicatedBase.Remove[]", "timeout_failure :" + rsp1.Sender + " Keys :" + keys.Count);
                            continue;
                        }
                    }
                }
                Rsp rsp = ClusterHelper.FindAtomicRemoveStatusReplicated(results, Context.NCacheLog);

                return true;
            }
            catch (Runtime.Exceptions.TimeoutException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        #endregion

        #region	/                 --- ReplicatedCacheBase ICache.GetEnumerator ---           /

        #region	/                 --- Clustered Enumerators ---           /

        /// <summary>
        /// provides Enumerator over replicated client cache
        /// </summary>
        internal class ClusteredEnumerator : LazyKeysetEnumerator
        {
            private Address _targetNode;
            private bool _isUserOperation = true;
            /// <summary>
            /// Constructor 
            /// </summary>
            /// <param name="cache"></param>
            /// <param name="keyList"></param>
            public ClusteredEnumerator(ReplicatedCacheBase cache, Address address, object[] keyList, bool isUserOperation)
                : base(cache, keyList, true)
            {
                _targetNode = address; 
                _isUserOperation = isUserOperation;
            }

            /// <summary>
            /// Does the lazy loading of object. This method is virtual so containers can 
            /// customize object fetching logic.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            protected override object FetchObject(object key, OperationContext operationContext)
            {
                ReplicatedCacheBase cache = _cache as ReplicatedCacheBase;

                object obj = null;
                bool doAgain = false;


                do
                {
                    doAgain = false;
                    Address targetNode = cache.Cluster.Coordinator;
                    if (targetNode == null) return null;

                    if (cache.Cluster.IsCoordinator)
                    {
                        //coordinator has left and i am the new coordinator so need not to do
                        //state transfer.
                        _bvalid = false;
                        return obj;
                    }
                    try
                    {
                        operationContext.Add(OperationContextFieldName.GenerateQueryInfo , true);
                        obj = cache.Clustered_Get(targetNode, key, operationContext,_isUserOperation);

                    }
                    catch (Runtime.Exceptions.SuspectedException se)
                    {
                        //coordinator has left; so need to synchronize with the new coordinator.
                        doAgain = true;
                    }
                }
                while (doAgain);

                return obj;
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
            bool isUserOperation = false;
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

                return new ClusteredEnumerator(this, (Address)targetNode.Clone(), result as object[],isUserOperation);
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

        #endregion


        protected QueryResultSet Clustered_Search(Address dest, string queryText, IDictionary values, bool excludeSelf, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.Search, new object[] { queryText, values, operationContext }, excludeSelf);
                Object result = Cluster.SendMessage(dest, func, GroupRequest.GET_ALL, false);

                if (result == null)
                    return null;

                return (QueryResultSet)result;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new OperationFailedException("Clustered_Search failed, Error: " + e.Message, e);
            }
        }

        protected QueryResultSet Clustered_SearchEntries(Address dest, string queryText, IDictionary values, bool excludeSelf, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.SearchEntries, new object[] { queryText, values, operationContext }, excludeSelf);
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST, false);

                if (result == null)
                    return null;

                return (QueryResultSet)result;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new OperationFailedException("Clustered_SearchEntries failed, Error: " + e.Message, e);
            }
        }


    }
}


