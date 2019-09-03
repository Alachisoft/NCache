//  Copyright (c) 2019 Alachisoft
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
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Util;
using Alachisoft.NGroups;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.Resources;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;


namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// A class to serve as the base for partitioned clustered cache implementations.
    /// </summary>
    internal class PartitionedCacheBase : PartitionedCommonBase
    {

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

        #region	/                 --- Partitioned ICache.GetKeys ---           /

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        protected ArrayList Clustered_GetKeys(ArrayList dests, string group, string subGroup)
        {
            ArrayList list = new ArrayList();
            try
            {
                Function func = new Function((int)OpCodes.GetKeys, new object[] { group, subGroup }, true);
                func.Cancellable = true;
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }

                ClusterHelper.ValidateResponses(results, typeof(ArrayList), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(ClusteredArrayList));

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
                        ArrayList cList = (ArrayList)rsp.Value;
                        if (cList != null) list.AddRange(cList);
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
            return list;
        }

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        protected HashVector Clustered_GetData(string group, string subGroup, OperationContext operationContext)
        {
            HashVector table = new HashVector();
            try
            {
                Function func = new Function((int)OpCodes.GetData, new object[] { group, subGroup, operationContext }, true);
                func.Cancellable = true;
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }

                ClusterHelper.ValidateResponses(results, typeof(HashVector), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(HashVector));

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
                        HashVector cTable = (HashVector)rsp.Value;
                        if (cTable != null)
                        {
                            IDictionaryEnumerator ide = cTable.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                table[ide.Key] = ide.Value;
                            }
                        }
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

            return table;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Get ---           /

        protected Hashtable Clustered_GetTag(ArrayList dests, string[] tags, TagComparisonType comparisonType, bool excludeSelf, OperationContext operationContext)
        {
            Hashtable keyValues = new Hashtable();

            try
            {
                Function func = new Function((int)OpCodes.GetTag, new object[] { tags, comparisonType, operationContext }, excludeSelf);
                func.Cancellable = true;
                RspList results = Cluster.BroadcastToMultiple(dests, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }

                ClusterHelper.ValidateResponses(results, typeof(HashVector), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(HashVector));

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
                        IDictionary entries = (IDictionary)rsp.Value;
                        if (entries != null)
                        {
                            IDictionaryEnumerator ide = entries.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                keyValues[ide.Key] = ide.Value;
                            }
                        }
                    }
                }

                return keyValues;
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

        protected ArrayList Clustered_GetKeysByTag(ArrayList dests, string[] tags, TagComparisonType comparisonType, bool excludeSelf, OperationContext operationContext)
        {
            ArrayList keys = new ArrayList();

            try
            {
                Function func = new Function((int)OpCodes.GetKeysByTag, new object[] { tags, comparisonType, operationContext }, excludeSelf);
                func.Cancellable = true;
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout * 10);
                if (results == null)
                {
                    return null;
                }

                ClusterHelper.ValidateResponses(results, typeof(ArrayList), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(ClusteredArrayList));

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
                        ICollection entries = (ICollection)rsp.Value;
                        if (entries != null)
                        {
                            IEnumerator ide = entries.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                keys.Add(ide.Current);
                            }
                        }
                    }
                }

                return keys;
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
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        protected Hashtable Clustered_GetData(ArrayList dests, string group, string subGroup, OperationContext operationContext)
        {
            Hashtable table = new Hashtable();
            try
            {
                Function func = new Function((int)OpCodes.GetData, new object[] { group, subGroup, operationContext }, true);
                func.Cancellable = true;
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }

                ClusterHelper.ValidateResponses(results, typeof(HashVector), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(HashVector));

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
                        Hashtable cTable = (Hashtable)rsp.Value;
                        if (cTable != null)
                        {
                            IDictionaryEnumerator ide = cTable.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                table[ide.Key] = ide.Value;
                            }
                        }
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

            return table;
        }

        #endregion

        #region /                       ---PartitionedCacheBase.Clustered_GetGroupInfo ---          /

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

        #endregion

        #region	/                 --- Partitioned ICache.Insert ---           /

        /// <summary>
        /// Removes all entries from the cluster.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="handleClear"/> on every server node in the cluster.
        /// </remarks>
        protected void Clustered_Clear(Caching.Notifications notification, string taskId, bool excludeSelf, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.Clear, new object[] { notification, taskId, operationContext }, excludeSelf);
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
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster.
        /// </remarks>
        protected CacheAddResult Clustered_Add(Address dest, object key, CacheEntry cacheEntry, string taskId, OperationContext operationContext)
        {
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.Add_1", "");
            CacheAddResult retVal = CacheAddResult.Success;
            CacheEntry cloneValue = null; 
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.Topology);
                cacheEntry?.MarkInUse(NCModulesConstants.Topology);
                 Array userPayLoad; long payLoadSize;
                _context.CachingSubSystemDataService.GetEntryClone(cacheEntry, out cloneValue, out userPayLoad, out payLoadSize);  

                Function func = new Function((int)OpCodes.Add, new object[] { key, cloneValue, taskId, operationContext });
                func.UserPayload = userPayLoad;
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                if (result is CacheAddResult)
                    retVal = (CacheAddResult)result; //retvals[0];
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
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);

                if (cloneValue != null)
                    cloneValue.MarkFree(NCModulesConstants.Global);
                cacheEntry?.MarkFree(NCModulesConstants.Topology);

                MiscUtil.ReturnEntryToPool(cloneValue, Context.TransactionalPoolManager);
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
                retVal = (bool)result; //retvals[0];
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
        /// Add the ExpirationHint to a specfied node in the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster.
        /// </remarks>
        protected bool Clustered_Add(Address dest, object key,  OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.Add_2", "");

            bool retVal = false;
            try
            {
                Function func = new Function((int)OpCodes.AddSyncDependency, new object[] { key, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (bool)result; //retvals[0];
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
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster.
        /// </remarks>
        protected Hashtable Clustered_Add(Address dest, object[] keys, CacheEntry[] cacheEntries, string taskId, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.AddBlk", "");

            Hashtable retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.Add, new object[] { keys, cacheEntries, taskId, operationContext });
                func.Cancellable = true;
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (Hashtable)result; //retvals[0];
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


        /// <summary>
        /// Verifies that joining node has no data integrity conflicts with other nodes of the 
        /// cluster.
        /// </summary>
        /// <returns>True, if no data integrity conflicts found, other wise false</returns>
        /// <remarks>Each partitioned node can have his data affinity. Data groups other than the
        /// strongly affiliated groups can be loadbalanced to any of the existing node. In such a
        /// situaltion if a new node joins and it has strong affinity with the groups whose data 
        /// was previously distributed evenly, then a data integrity conflict arises. To avoid such
        /// conflicts each joining node first varifes that no other node on the cluster has data
        /// of his groups. If it is so, then he has to leave the cluster.</remarks>
        public bool VerifyDataIntegrity()
        {
            bool integrityVarified = true;
            bool integrityIssue = false;


            try
            {
                if (Cluster.Servers.Count > 1)
                {
                    if (_stats != null && _stats.LocalNode.DataAffinity != null)
                    {
                        DataAffinity affinity = _stats.LocalNode.DataAffinity;

                        if (affinity.Groups != null && affinity.Groups.Count > 0)
                        {
                            Function fun = new Function((int)OpCodes.VerifyDataIntegrity, (object)affinity.Groups, false);
                            RspList results = Cluster.BroadcastToServers(fun, GroupRequest.GET_ALL, false);

                            if (results != null)
                            {
                                ClusterHelper.ValidateResponses(results, typeof(bool), Name);
                                Rsp response;
                                for (int i = 0; i < results.size(); i++)
                                {
                                    response = (Rsp)results.elementAt(i);
                                    if (response.wasReceived())
                                    {
                                        integrityIssue = Convert.ToBoolean(response.Value);
                                        if (integrityIssue)
                                        {
                                            Context.NCacheLog.Error("PartitionedCacheBase.Verifydataintegrity()", "data integrity issue from " + response.Sender.ToString());
                                            integrityVarified = false;
                                        }
                                    }
                                    else
                                    {
                                        Context.NCacheLog.Error("PartitionedCacheBase.Verifydataintegrity()", "data integrity varification not received from " + response.Sender.ToString());
                                        integrityVarified = false;
                                        break;
                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Context != null)
                {
                    Context.NCacheLog.Error("PartitionedCacheBase.Verifydataintegrity()", e.ToString());
                }
                integrityVarified = false;
            }

            return integrityVarified;
        }

      
        #region	/                 --- Partitioned ICache.Insert ---           /

        /// <summary>
        /// Updates or Adds the object to the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on the specified node.
        /// </remarks>
        protected CacheInsResultWithEntry Clustered_Insert(Address dest, object key, CacheEntry cacheEntry, string taskId, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.Insert", "");
            CacheInsResultWithEntry retVal = null;
            CacheEntry cloneValue = null;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.Topology);

                Array userPayLoad; long payLoadSize;
                _context.CachingSubSystemDataService.GetEntryClone(cacheEntry, out cloneValue, out userPayLoad, out payLoadSize);

                Function func = new Function((int)OpCodes.Insert, new object[] { key, cloneValue, taskId, lockId, accessType, version, operationContext });
                func.UserPayload = userPayLoad;
                func.ResponseExpected = true;
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST, false);
                if (result == null)
                {
                    return retVal;
                }

                retVal = (CacheInsResultWithEntry)((OperationResponse)result).SerializablePayload;
                if (retVal.Entry != null && ((OperationResponse)result).UserPayload!=null)
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
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);
                if(retVal==null)
                    retVal=CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.TransactionalPoolManager);

                MiscUtil.ReturnEntryToPool(cloneValue, Context.TransactionalPoolManager);
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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.InsertBlk", "");

            Hashtable inserted = null;
            try
            {
                Function func = new Function((int)OpCodes.Insert, new object[] { keys, cacheEntries, taskId, operationContext });
                func.Cancellable = true;
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
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster.
        /// </remarks>
        protected CacheEntry Clustered_Remove(Address dest, object key, ItemRemoveReason ir, Caching.Notifications notification, string taskId, string providerName, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.Topology);

                Function func = new Function((int)OpCodes.Remove, new object[] { key, ir, notify, notification, taskId, lockId, accessType, version, providerName, operationContext }, false);
                func.ResponseExpected = true;
  
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST, false);
                if (result != null)
                {
                    retVal = ((OperationResponse)result).SerializablePayload as CacheEntry;
                    if (retVal != null && ((OperationResponse)result).UserPayload!=null)
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
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);
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
        protected Hashtable Clustered_Remove(Address dest, object[] keys, ItemRemoveReason ir, Caching.Notifications notification, string taskId, string providerName, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCacheBase.RemoveBlk", "");

            Hashtable removedEntries = new Hashtable();
            ArrayList dests = new ArrayList();
            dests.Add(dest);
            try
            {
                Function func = new Function((int)OpCodes.Remove, new object[] { keys, ir, notify, notification, taskId, providerName, operationContext }, false);
                func.Cancellable = true;
                RspList results = Cluster.Multicast(dests, func, GetFirstResponse, false);

                if (results == null)
                {
                    return removedEntries;
                }

                if (results.SuspectedMembers.Count == dests.Count)
                {
                    //All the members of this group has gone down. 
                    //we must try this operation on some other group.
                    throw new Runtime.Exceptions.SuspectedException("operation failed because the group member was suspected");
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
                    if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
            return removedEntries;
        }

        /// <summary>
        /// Remove the objects from the cluster. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of failed keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster.
        /// </remarks>
        protected Hashtable Clustered_RemoveGroup(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            Hashtable removedEntries = new Hashtable();
            try
            {
                Function func = new Function((int)OpCodes.RemoveGroup, new object[] { group, subGroup, notify, operationContext }, false);
                func.Cancellable = true;
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL, false);

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


        protected Hashtable Clustered_RemoveByTag(ArrayList dests, string[] tags, TagComparisonType comparisonType, bool notify, bool excludeSelf, OperationContext operationContext)
        {
            Hashtable removedEntries = new Hashtable();
            try
            {
                Function func = new Function((int)OpCodes.RemoveByTag, new object[] { tags, comparisonType, notify, operationContext }, excludeSelf);
                func.Cancellable = true;
                RspList results = Cluster.BroadcastToMultiple(dests, func, GroupRequest.GET_ALL, false);

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

        #region /               --- Cascaded Dependencies ---                   /

        protected Hashtable Clustered_AddDepKeyList(Address dest, Hashtable table, OperationContext operationContext)
        {
            Hashtable retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.AddDepKeyList, new object[] { table, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (Hashtable)result; 
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

        protected Hashtable Clustered_RemoveDepKeyList(Address dest, Hashtable table, OperationContext operationContext)
        {
            Hashtable retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.RemoveDepKeyList, new object[] { table, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_ALL);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (Hashtable)result; 
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


