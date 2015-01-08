// Copyright (c) 2015 Alachisoft
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

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Enumeration that defines the result of a Put operation.
    /// </summary>

    [Serializable]
    public enum CacheInsResult
    {
        /// <summary> The item was inserted. </summary>
        Success,
        /// <summary> The item was updated. </summary>
        SuccessOverwrite,

        /// <summary> The item was inserted. </summary>
        SuccessNearEvicition,
        /// <summary> The item was updated. </summary>
        SuccessOverwriteNearEviction,

        /// <summary> The operation failed, since there is not enough space. </summary>
        NeedsEviction,
        /// <summary> The operation failed. </summary>
        Failure,
        /// <summary> 
        /// Apply only in case of partitioned caches. 
        /// This result is sent when a bucket has been transfered to another node
        /// but it is not fully functionaly yet.
        /// The operations must wait untile they get an indication that the bucket 
        /// has become fully functional on the new node.
        /// </summary>
        BucketTransfered,
        /// <summary> 
        /// In case of Insert if all nodes return NeedsEviction Response so then 
        ///there is no need of sending remove call cluster wide, mean the new item 
        /// was not isuted on any of the nodes and the cache is still synchronized 
        /// </summary>
        NeedsEvictionNotRemove,
        /// <summary>
        /// This result that Insert operation failed and the failded reason is that item present
        /// in the cache was already locked.
        /// </summary>
        ItemLocked, 
        /// <summary> Operation timedout on all of the nodes. </summary>
        FullTimeout,
        /// <summary> Operation timedout on some of the nodes. </summary>
        PartialTimeout,
    }
}
