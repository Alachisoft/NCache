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

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Enumeration that defines the result of a Put operation.
    /// </summary>

    [Serializable]
    internal enum CacheAddResult
    {
        /// <summary> The item was added. </summary>
        Success,
        /// <summary> The item was added but cache is near to full </summary>
        SuccessNearEviction,
        /// <summary> The item already exists. </summary>
        KeyExists,
        /// <summary> The operation failed, since there is not enough space. </summary>
        NeedsEviction,
        /// <summary> General failure. </summary>
        Failure,
        /// <summary> 
        /// Apply only in case of partitioned caches. 
        /// This result is sent when a bucket has been transfered to another node
        /// but it is not fully functionally yet.
        /// The operations must wait until they get an indication that the bucket 
        /// has become fully functional on the new node.
        /// </summary>
        BucketTransfered,
        /// <summary> Operation timeout on all of the nodes. </summary>
        FullTimeout,
        /// <summary> Operation timeout on some of the nodes. </summary>
        PartialTimeout,
    }
}