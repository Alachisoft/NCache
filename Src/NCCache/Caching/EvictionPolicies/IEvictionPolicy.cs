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
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.EvictionPolicies
{
    /// <summary>
    ///	Allows end users to implement their own scavenging algorithm.
    /// </summary>

    internal interface IEvictionPolicy:ISizableIndex
    {
        /// <summary>
        /// Updates the data associated with indices. 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="oldHint"></param>
        /// <param name="newHint"></param>
        //void UpdateIndex(object key, EvictionHint oldHint, EvictionHint newHint);
        void Notify(object key, EvictionHint oldhint, EvictionHint newHint);

        /// <summary>
        /// Flush the data associated with eviction policy including indices.
        /// </summary>
        void Clear();

        /// <summary>
        /// Check if the provided eviction hint is compatible with the policy
        /// and return the compatible eviction hint
        /// </summary>
        /// <param name="eh">eviction hint.</param>
        /// <returns>a hint compatible to the eviction policy.</returns>
        EvictionHint CompatibleHint(EvictionHint eh,PoolManager poolManager);

        /// <summary>
        /// Get the list of items that are selected for eviction.
        /// </summary>
        /// <param name="size">size of data in store, in bytes</param>
        /// <returns>list of items selected for eviction.</returns>
        //ArrayList SelectItemsForEviction(long count);
        long Execute(CacheBase cache,CacheRuntimeContext context, long size);

        /// <summary>
        /// Remove the specified key from the index.
        /// </summary>
        /// <param name="key"></param>
        void Remove(object key, EvictionHint hint);

        float EvictRatio
        {
            get;
            set; 
        }
    }
}