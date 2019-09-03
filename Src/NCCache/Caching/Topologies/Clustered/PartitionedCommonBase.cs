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
using System.Net;
using System.Collections;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

using Alachisoft.NGroups.Blocks;
using Alachisoft.NCache.Runtime.Caching;
using System.Threading;
using System.Collections.Generic;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    internal class PartitionedCommonBase : ClusterCacheBase
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
            public Identity(bool hasStorage, int renderPort, IPAddress renderAddress, bool isStartedAsMirror) : base(hasStorage, renderPort, renderAddress, isStartedAsMirror)
            { }

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
        protected const string MCAST_DOMAIN = ".pr20";

        /// <summary>Distributes the data among partitions.</summary>
        internal protected DistributionManager DistributionMgr { get; set; }

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public PartitionedCommonBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
        }

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public PartitionedCommonBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, IClusterEventsListener clusterListener)
            : base(properties, listener, context, clusterListener)
        {
        }

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
                    Context.NCacheLog.Warn("PartitionedCommonBase.AuthenticateNode()", "A non-recognized node attempted to join cluster -> " + address);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
            }
            return false;
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
        /// Return the Target node where this key exists.
        /// </summary>
        /// <returns></returns>
        protected Address GetTargetNode(string key, string group)
        {
            return DistributionMgr.SelectNode(key, group);
        }

      
    }
}
