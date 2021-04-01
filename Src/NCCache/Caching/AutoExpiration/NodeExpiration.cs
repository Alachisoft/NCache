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
#if !( DEVELOPMENT || CLIENT)
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NGroups;
#endif

using Alachisoft.NCache.Runtime.Serialization.IO;


using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// Node expiration based derivative of ExpirationHint.
	/// </summary>

	[Serializable]
	internal class NodeExpiration : DependencyHint
	{
		/// <summary> The node on which this hint depends. </summary>
		[CLSCompliant(false)]
		protected Address _node;

        public NodeExpiration()
        {
            _hintType = ExpirationHintType.NodeExpiration;            
        }

        #region Creating NodeExpiration

        public static NodeExpiration Create(PoolManager poolManager)
        {
            var instance = poolManager.GetNodeExpirationPool()?.Rent(true) ?? new NodeExpiration();
            instance._startAfter = DateTime.Now;
            return instance;
        }

        public static NodeExpiration Create(PoolManager poolManager, Address node)
        {
            var expiration = Create(poolManager);
            expiration._node = node;

            return expiration;
        }

        #endregion

#if !(DEVELOPMENT || CLIENT)
        /// <summary>
        /// virtual method that returns true when the expiration has taken place, returns 
        /// false otherwise.
        /// </summary>
        internal override bool DetermineExpiration(CacheRuntimeContext context)
		{ 
			if(HasExpired) return true;

			if (context.IsClusteredImpl)
			{
                if(((ClusterCacheBase)context.CacheImpl).Cluster.IsMember(_node) == false)
                {
					this.NotifyExpiration(this, null);
				}
			}

			return HasExpired;
		}

        public Address GetNode()
        { return _node; }


        public Address Node
        {
            get { return _node; }
            internal set { _node = value; }
        }

#endif
        /// <summary>
        /// returns false if given node is alive, returns true otherwise.
        /// </summary>
        public override bool HasChanged { get { return false; } }

		#region	/                 --- ICompactSerializable ---           /

		public override void Deserialize(CompactReader reader)
		{
            base.Deserialize(reader);
            _node = Address.ReadAddress(reader);
		}

		public override void Serialize(CompactWriter writer)
		{
            base.Serialize(writer);
            Address.WriteAddress(writer, _node);            
		}

        #endregion

        #region ILeasable

        public sealed override void ResetLeasable()
        {
            base.ResetLeasable();

            _node = default(Address);
            _hintType = ExpirationHintType.NodeExpiration;
        }

        public sealed override void ReturnLeasableToPool()
        {

        }

        #endregion

        #region - [Deep Cloning] -

        public sealed override ExpirationHint DeepClone(PoolManager poolManager)
        {
            var clonedHint = poolManager.GetNodeExpirationPool()?.Rent(false) ?? new NodeExpiration();
            DeepCloneInternal(poolManager, clonedHint);
            return clonedHint;
        }

        protected sealed override void DeepCloneInternal(PoolManager poolManager, ExpirationHint clonedHint)
        {
            if (clonedHint == null)
                return;

            base.DeepCloneInternal(poolManager, clonedHint);

            if (clonedHint is NodeExpiration clonedNodeExpirationHint)
            {
                clonedNodeExpirationHint._node = _node.Clone() as Address;
            }
        }

        #endregion
    }
}


