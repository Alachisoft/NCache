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
using System;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Net;
using System.Net;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Common.DataStructures;


namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// 
    /// </summary>
    internal class MirrorCacheBase : ClusterCacheBase
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
        public MirrorCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
        }

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public MirrorCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, IClusterEventsListener clusterListener)
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
                    Context.NCacheLog.Warn("MirrorCacheBase.AuthenticateNode()", "A non-recognized node attempted to join cluster -> " + address);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

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
            public ClusteredEnumerator(MirrorCacheBase cache, Address address, object[] keyList, bool isUserOperation)
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
                MirrorCacheBase cache = _cache as MirrorCacheBase;

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
                        operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                        obj = cache.Clustered_Get(targetNode, key, operationContext, _isUserOperation);
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
        internal IDictionaryEnumerator Clustered_GetEnumerator(Address targetNode)
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

                return new ClusteredEnumerator(this, (Address)targetNode.Clone(), result as object[], isUserOperation);
            }
            catch (CacheException e)
            {
                Context.NCacheLog.Error("MirrorCacheBase.GetSubClusterEnumerator()", e.ToString());
                throw;
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("MirrorCacheBase.GetSubClusterEnumerator()", e.ToString());
                throw new GeneralFailureException(e.Message, e);
            }
        }

        #endregion

        #region MirrorStateTransfer

        internal ReplicaStateTxfrInfo Clustered_GetEntries(Address targetNode)
        {
            try
            {
                Function func = new Function((int)OpCodes.TransferEntries, null);
                object result = Cluster.SendMessage((Address)targetNode.Clone(),
                    func,
                    GroupRequest.GET_FIRST,
                    Cluster.Timeout * 10);
                return (ReplicaStateTxfrInfo)result;
            }
            catch (CacheException e)
            {
                Context.NCacheLog.Error("MirrorCacheBase.Clustered_GetEntries()", e.ToString());
                throw;
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("MirrorCacheBase.Clustered_GetEntries()", e.ToString());
                throw new GeneralFailureException(e.Message, e);
            }
        }

        #endregion

        protected void Clustered_GetNextChunk(ArrayList dests, EnumerationPointer pointer, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.GetNextChunk, new object[] { pointer, operationContext }, true);
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


