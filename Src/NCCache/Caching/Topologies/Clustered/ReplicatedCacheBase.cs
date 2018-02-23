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

using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Exceptions;

using Alachisoft.NCache.Caching.Statistics;

using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.AutoExpiration;
using System.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.DatasourceProviders;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NGroups.Util;

using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
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

        #region	/                 --- ReplicatedCacheBase ICache.Count ---           /


        /// <summary>
        /// Returns the count of clustered cache items, from a functional node.
        /// </summary>
        protected long Clustered_SessionCount(Address targetNode)
        {
            try
            {
                Function func = new Function((int)OpCodes.GetSessionCount, null);
                object result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return 0;
                }

                return Convert.ToInt64(result);
            }
            catch (CacheException e)
            {
                NCacheLog.Error("ReplicatedCacheBase.Clustered_SessionCount()", e.ToString());
                throw;
            }
            catch (Exception e)
            {
                NCacheLog.Error("ReplicatedCacheBase.Clustered_SessionCount()", e.ToString());
                throw new GeneralFailureException("Clustered_SessionCount failed, Error: " + e.Message, e);
            }
        }

        #endregion

        #region	/                 --- ReplicatedCacheBase ICache.Clear ---           /

        /// <summary>
        /// Removes all entries from the cluster.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="handleClear"/> on every server node in the cluster.
        /// </remarks>
        protected void Clustered_Clear(CallbackEntry cbEntry, string taskId, bool excludeSelf, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.Clear, new object[] { cbEntry, taskId, operationContext }, excludeSelf);
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
                Priority priority = Priority.Normal;
                if (operationContext.Contains(OperationContextFieldName.IsClusteredOperation))
                {
                    priority = Priority.Critical;                   
                }

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Get", "enter");
                Function func = new Function((int)OpCodes.Get, new object[] { key, operationContext });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST, false,priority);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (CacheEntry)((OperationResponse)result).SerializablePayload;
                if (retVal != null && ((OperationResponse)result).UserPayload !=null) retVal.Value = ((OperationResponse)result).UserPayload;
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

        protected PollingResult Clustered_Poll(Address address, OperationContext context)
        {
            PollingResult retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.Poll, new object[] { context });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST, _asyncOperation);
                if (result == null)
                {
                    return null;
                }
                retVal = (PollingResult)result;
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
        /// Retrieve the objects from the cluster. 
        /// </summary>
        /// <param name="group">group for which keys are needed</param>
        /// <param name="subGroup">sub group of the group</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>list of keys</returns>
        protected ArrayList Clustered_GetKeys(Address address, string group, string subGroup,OperationContext operationContext)
        {
            ArrayList retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.GetKeys, new object[] { group, subGroup, operationContext });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST, _asyncOperation);
                if (result == null)
                {
                    return null;
                }
                retVal = (ArrayList)result;
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
        /// Retrieve the objects from the cluster. 
        /// </summary>
        /// <param name="group">group for which keys are needed</param>
        /// <param name="subGroup">sub group of the group</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>list of keys</returns>
        protected CacheEntry Clustered_GetGroup(Address address, object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, TimeSpan lockTimeout, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.GetGroup, new object[] { key, group, subGroup, operationContext });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return null;
                }
                retVal = (CacheEntry)result;
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

        #region /                       ---ReplictedCacheBase.Clustered_GetGroupInfo ---          /

        /// <summary>
        /// Gets the data group info of the item. Node containing the item will return the
        /// data group information.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Result of the operation</returns>
        /// <remarks>On the other ndoe handleGetGroupInfo is called</remarks>
        public ClusteredOperationResult Clustered_GetGroupInfo(object key, OperationContext operationContext)
        {
            return Clustered_GetGroupInfo(Cluster.Servers, key, true, operationContext);
        }

        /// <summary>
        /// Gets the data group info the items. Node containing items will return a table
        /// of Data grop information.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        /// /// <remarks>On the other ndoe handleGetGroupInfo is called</remarks>
        public ICollection Clustered_GetGroupInfoBulk(object[] keys, OperationContext operationContext)
        {
            return Clustered_GetGroupInfoBulk(Cluster.Servers, keys, true, operationContext);
        }

        /// <summary>
        /// Gets data group info the items
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>IDictionary of the data grup info the items</returns>
        public Hashtable Clustered_GetGroupInfoBulkResult(object[] keys, OperationContext operationContext)
        {

            ICollection result = Clustered_GetGroupInfoBulk(keys, operationContext);
            ClusteredOperationResult opRes;
            Hashtable infos;
            Hashtable max = null;
            Hashtable infoTable;
            if (result != null)
            {
                IEnumerator ie = result.GetEnumerator();
                while (ie.MoveNext())
                {
                    opRes = (ClusteredOperationResult)ie.Current;
                    if (opRes != null)
                    {
                        infos = (Hashtable)opRes.Result;
                        if (max == null)
                            max = infos;
                        else if (infos.Count > max.Count)
                            max = infos;

                    }
                }
            }
            infoTable = max;
            return infoTable;
        }
        #endregion

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
        public Hashtable Clustered_Insert(object[] keys, CacheEntry[] cacheEntries, string taskId, bool notify, OperationContext operationContext)
        {
            /// Wait until the object enters any running status

            HashVector pEntries = null;

            pEntries = (HashVector)Get(keys, operationContext); //dont remove

            Hashtable existingItems;
            Hashtable jointTable = new Hashtable();
            Hashtable failedTable = new Hashtable();
            Hashtable insertable = new Hashtable();
            Hashtable insertResults = null;
            ArrayList inserted = new ArrayList();
            ArrayList added = new ArrayList();
            object[] validKeys;
            CacheEntry[] validEnteries;
            object[] failedKeys;
            CacheEntry[] failedEnteries;
            int index = 0;
            object key;


            for (int i = 0; i < keys.Length; i++)
            {
                jointTable.Add(keys[i], cacheEntries[i]);
            }

            Hashtable keyValTable = jointTable.Clone() as Hashtable;

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
                    index += 1;
                }

                Hashtable groups = Clustered_GetGroupInfoBulkResult(validKeys, operationContext);
                if (groups != null)
                {
                    existingItems = groups;
                    if (existingItems != null && existingItems.Count > 0)
                    {
                        insertable = CacheHelper.GetInsertableItems(existingItems, jointTable);
                        if (insertable != null)
                        {
                            index = 0;
                            validKeys = new object[insertable.Count];
                            validEnteries = new CacheEntry[insertable.Count];

                            ide = insertable.GetEnumerator();
                            CacheEntry entry;
                            while (ide.MoveNext())
                            {
                                key = ide.Key;
                                entry = (CacheEntry)ide.Value;
                                validKeys[index] = key;
                                validEnteries[index] = entry;
                                inserted.Add(key);
                                index += 1;
                                jointTable.Remove(key);
                            }
                            try
                            {
                                insertResults = Clustered_Insert(Cluster.Servers, validKeys, validEnteries, taskId, operationContext);
                            }
                            catch (Exception e)
                            {
                                Context.NCacheLog.Error("ReplicatedServerCacheBase.Insert(Keys)", e.ToString());
                                for (int i = 0; i < validKeys.Length; i++)
                                {
                                    failedTable.Add(validKeys[i], e);
                                    inserted.Remove(validKeys[i]);
                                }
                                Clustered_Remove(validKeys, ItemRemoveReason.Removed, null, null, null, false, operationContext);
                            }

                            ide = existingItems.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                key = ide.Key;
                                if (jointTable.Contains(key))
                                {
                                    failedTable.Add(key, new OperationFailedException("Data group of the inserted item does not match the existing item's data group", false));
                                    jointTable.Remove(key);
                                }
                            }
                        }

                    }
                }
            }

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
                for (int i = 0; i < validKeys.Length; i++)
                {
                    key = validKeys[i];
                    if (jointTable.Contains(key))
                        jointTable.Remove(key);
                }
                try
                {
                    insertResults = null;
                    insertResults = Clustered_Insert(Cluster.Servers, validKeys, validEnteries, taskId, operationContext);
                }
                catch (Exception e)
                {
                    Context.NCacheLog.Error("ReplicatedServerCacheBase.Insert(Keys)", e.ToString());
                    for (int i = 0; i < validKeys.Length; i++)
                    {
                        failedTable.Add(validKeys[i], e);
                        added.Remove(validKeys[i]);
                    }

                    Clustered_Remove(validKeys, ItemRemoveReason.Removed, null, null, null, false, operationContext);
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


        protected override void DequeueWriteBehindTask(string[] taskId, string providerName, OperationContext operationContext)
        {
            if (taskId == null) return;

            Function func = new Function((int)OpCodes.WBTCompleted, new object[] { taskId, providerName, operationContext }, true);
            Cluster.BroadcastToServers(func, GroupRequest.GET_NONE, true);
        }
        //for atomic
        protected override void EnqueueWriteBehindOperation(DSWriteBehindOperation operation)
        {
            if (operation.TaskId == null) return;
            Function func = new Function((int)OpCodes.EnqueueWBOp, new object[] { operation }, true);
            Cluster.BroadcastToServers(func, GroupRequest.GET_NONE, true);
        }
        //for bulk
        protected override void EnqueueWriteBehindOperation(ArrayList operations)
        {
            if (operations == null) return;
            Function func = new Function((int)OpCodes.EnqueueWBOp, new object[] { operations }, true);
            Cluster.BroadcastToServers(func, GroupRequest.GET_NONE, true);
        }

        #region	/                 --- OnItemUpdated ---           /
        /// <summary>
        /// Hanlder for clustered item updated notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        protected object handleNotifyUpdate(object info)
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

        #region	/                 --- OnItemAdded ---           /

        /// <summary>
        /// Hanlder for clustered item added notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        protected object handleNotifyAdd(object info)
        {
            object[] objs = info as object[];
            OperationContext opContext = null;
            EventContext evContext = null;
            if (objs != null)
            {
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
        /// <summary>
        /// Retrieve the objects from the cluster. 
        /// </summary>
        /// <param name="group">group for which keys are needed</param>
        /// <param name="subGroup">sub group of the group</param>
        /// <returns>key and entry pairs</returns>
        protected HashVector Clustered_GetData(Address address, string group, string subGroup, OperationContext operationContext)
        {
            HashVector retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.GetData, new object[] { group, subGroup, operationContext });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST, _asyncOperation);
                if (result == null)
                {
                    return new HashVector();
                }
                retVal = (HashVector)result;
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
        protected CacheAddResult Clustered_Add(ArrayList dests, object key, CacheEntry cacheEntry, string taskId, OperationContext operationContext)
        {
            CacheAddResult result = CacheAddResult.Failure;
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Add", "enter");
                bool writeThruEnable = _context.DsMgr != null;
                CacheEntry entryToBeSent = cacheEntry;
                if (writeThruEnable && !_context.InMemoryDataFormat.Equals(DataFormat.Object))
                {
                    entryToBeSent = cacheEntry.CloneWithoutValue();
                } 
                /// Ask every server to add the object, except myself.
                Function func = new Function((int)OpCodes.Add, new object[] { key, entryToBeSent, taskId, operationContext }, false, key);
                Array userPayLoad = null;
                if (cacheEntry.Value is CallbackEntry)
                {
                    CallbackEntry cbEntry = ((CallbackEntry)cacheEntry.Value);
                    if (!_context.InMemoryDataFormat.Equals(DataFormat.Object))
                    {
                        userPayLoad = cbEntry.UserData;
                        if (!writeThruEnable) cbEntry.Value = null;
                    }
                    else
                         userPayLoad = null;
                    
                }
                else
                {
                    if (!_context.InMemoryDataFormat.Equals(DataFormat.Object))
                    {
                        userPayLoad = cacheEntry.UserData;
                        if (!writeThruEnable) cacheEntry.Value = null;
                    }
                    else
                        userPayLoad = null;
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
                result = ClusterHelper.FindAtomicResponseReplicated(results);
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
        /// Add the object to the cluster. Does load balancing as well.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on one of the server nodes in the cluster, 
        /// or invokes <see cref="Local_Add"/> locally.
        /// </remarks>
        protected bool Clustered_Add(ArrayList dests, object key, CacheSynchronization.CacheSyncDependency syncDependency, OperationContext operationContext)
        {
            bool result = false;
            try
            {
                /// Ask every server to add the object, except myself.
                Function func = new Function((int)OpCodes.AddSyncDependency, new object[] { key, syncDependency, operationContext }, false, key);
                RspList results = Cluster.BroadcastToMultiple(dests,
                    func,
                    GroupRequest.GET_ALL, _asyncOperation);

                ClusterHelper.ValidateResponses(results, typeof(bool), Name);

                /// Check if the operation failed on any node.
                result = ClusterHelper.FindAtomicResponseReplicated(results);
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
        protected Hashtable Clustered_Add(ArrayList dests, object[] keys, CacheEntry[] cacheEntries, string taskId, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.AddBlk", "enter");
                /// Ask every server to add the object, except myself.
                Function func = new Function((int)OpCodes.Add, new object[] { keys, cacheEntries, taskId, operationContext }, false);
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
        protected CacheInsResult Clustered_Insert(Address dest, object key, CacheEntry cacheEntry, string taskId, OperationContext operationContext)
        {
            CacheInsResult retVal = CacheInsResult.Failure;
            try
            {
                bool writeThruEnable = _context.DsMgr != null;
                CacheEntry entryToBeSent = cacheEntry;
                if (writeThruEnable && !_context.InMemoryDataFormat.Equals(DataFormat.Object)) 
                    entryToBeSent = cacheEntry.CloneWithoutValue();

                Function func = new Function((int)OpCodes.Insert, new object[] { key, entryToBeSent, operationContext }, false, key);
                Array userPayLoad = null;
                if (cacheEntry.Value is CallbackEntry)
                {
                    CallbackEntry cbEntry = ((CallbackEntry)cacheEntry.Value);
                    if (!_context.InMemoryDataFormat.Equals(DataFormat.Object))
                    {
                        userPayLoad = cacheEntry.UserData;
                        if (!writeThruEnable) cacheEntry.Value = null;
                    }
                    else
                        userPayLoad = null;
                }
                else
                {
                    if (!_context.InMemoryDataFormat.Equals(DataFormat.Object))
                    {
                        userPayLoad = cacheEntry.UserData;
                        if (!writeThruEnable) cacheEntry.Value = null;
                    }
                    else
                        userPayLoad = null;
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
        /// Updates or Adds the objects to the cluster. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">cache entries.</param>
        /// <returns>failed keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on the specified node.
        /// </remarks>
        protected Hashtable Clustered_Insert(Address dest, object[] keys, CacheEntry[] cacheEntries, string taskId, OperationContext operationContext)
        {
            Hashtable inserted = null;
            try
            {
                Function func = new Function((int)OpCodes.Insert, new object[] { keys, cacheEntries, taskId, operationContext });
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


        protected DeleteQueryResultSet Clustered_DeleteQuery(ArrayList dests, string query, IDictionary values, bool notify, bool isUserOperation, ItemRemoveReason ir, OperationContext operationContext)
        {
            DeleteQueryResultSet res = new DeleteQueryResultSet();
            try
            {
                Function func = new Function((int)OpCodes.DeleteQuery, new object[] { query, values, notify, isUserOperation, ir, operationContext }, false);
                RspList results = Cluster.Broadcast(func, GroupRequest.GET_ALL, true,Common.Enum.Priority.Normal);
                if (results == null)
                {
                    return res;
                }
                ClusterHelper.ValidateResponses(results, typeof(DeleteQueryResultSet), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(DeleteQueryResultSet));
                if (rspList.Count <= 0)
                {
                    return res;
                }
                else
                {
                    Rsp rsp = (Rsp)rspList[0];
                    DeleteQueryResultSet result = (DeleteQueryResultSet)rsp.Value;
                    return result;
                }

            }
            catch (Exception e)
            {
                throw;
            }

            return res;
        }


        protected CacheInsResultWithEntry Clustered_Insert(ArrayList dests, object key, CacheEntry cacheEntry, string taskId, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Insert", "enter");
                bool writeThruEnable = _context.DsMgr != null;
                CacheEntry entryToBeSent = cacheEntry;
                if (writeThruEnable && !_context.InMemoryDataFormat.Equals(DataFormat.Object)) 
                        entryToBeSent = cacheEntry.CloneWithoutValue();

                /// Ask every server to update the object, except myself.
                Function func = new Function((int)OpCodes.Insert, new object[] { key, entryToBeSent, taskId, _statusLatch.IsAnyBitsSet(NodeStatus.Initializing), lockId, accessType, operationContext }, false, key);
                Array userPayLoad = null;
                if (cacheEntry.Value is CallbackEntry)
                {
                    CallbackEntry cbEntry = ((CallbackEntry)cacheEntry.Value);
                    if (!_context.InMemoryDataFormat.Equals(DataFormat.Object))
                    {
                        userPayLoad = cbEntry.UserData;
                        if (!writeThruEnable) cbEntry.Value = null;
                    }
                    else
                        userPayLoad = null;
                }
                else
                {
                    if (!_context.InMemoryDataFormat.Equals(DataFormat.Object))
                    {
                        userPayLoad = cacheEntry.UserData;
                        if (!writeThruEnable) cacheEntry.Value = null;
                    }
                    else
                        userPayLoad = null;
                }

                func.UserPayload = userPayLoad;
                RspList results = Cluster.BroadcastToMultiple(dests,
                    func,
                    GroupRequest.GET_ALL, _asyncOperation);

                ClusterHelper.ValidateResponses(results, typeof(OperationResponse), Name);

                // Check if the operation failed on any node.

                //Bug Fixed, during state transfer (one node up with the existing one) of replicated cache, 
                //while client doing insert operation continuously, which incrementing the add/sec counter while the client only performing insert
                //means no need to increment add/sec counter, need only update/sec to be incremented
            
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

        protected Hashtable Clustered_Insert(ArrayList dests, object[] keys, CacheEntry[] cacheEntries, string taskId, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.InsertBlk", "enter");
                /// Ask every server to update the object, except myself.
                Function func = new Function((int)OpCodes.Insert, new object[] { keys, cacheEntries, taskId, operationContext }, false);
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
        protected CacheEntry Clustered_Remove(object key, ItemRemoveReason ir, CallbackEntry cbEntry, string taskId, string providerName, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.Remove", "enter");
                Function func = new Function((int)OpCodes.Remove, new object[] { key, ir, notify, cbEntry, taskId, lockId, accessType, version, providerName, operationContext }, false, key);
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
                    if (entry != null && opRes.UserPayload!=null)
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
        protected Hashtable Clustered_Remove(ICollection keys, ItemRemoveReason ir, CallbackEntry cbEntry, string taskId, string providerName, bool notify, OperationContext operationContext)
        {
            Hashtable removedEntries = new Hashtable();
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCacheBase.RemoveBlk", "enter");
                Function func = new Function((int)OpCodes.Remove, new object[] { keys, ir, notify, cbEntry, taskId, providerName, operationContext }, false);
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL);

                if (results == null)
                {
                    return removedEntries;
                }

                ClusterHelper.ValidateResponses(results, typeof(Hashtable), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(Hashtable));

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
                        if(!removedEntries.ContainsKey(ide.Key))
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
        protected bool Clustered_Remove(ICollection keys, ItemRemoveReason reason, OperationContext operationContext)
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

        protected void Clustered_UnRegisterCQ(ArrayList dests, string serverUniqueId, string clientUniqueId, string clientId, bool excludeSelf)
        {
            try
            {
                Function func = new Function((int)OpCodes.UnRegisterCQ, new object[] { serverUniqueId, clientUniqueId, clientId }, excludeSelf);
                RspList results = Cluster.BroadcastToMultiple(dests, func, GroupRequest.GET_ALL, false);
                ClusterHelper.ValidateResponses(results, null, Name);
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

        protected void Clustered_RegisterCQ(ArrayList dests, ContinuousQuery query, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, bool excludeSelf, OperationContext operationContext, QueryDataFilters datafilters)
        {
            try
            {
                Function func = new Function((int)OpCodes.RegisterCQ, new object[] { query, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters }, excludeSelf);
                RspList results = Cluster.BroadcastToMultiple(dests, func, GroupRequest.GET_ALL, false);
                ClusterHelper.ValidateResponses(results, null, Name);
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
    }
}


