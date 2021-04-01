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

namespace Alachisoft.NCache.Common.Pooling
{
    /// <summary>
    /// Class constaining various numeric constants that correspond to NCache's modules.
    /// </summary>
    public sealed class NCModulesConstants
    {
        

        /// <summary>
        /// Represents the SocketServer layer of NCache where all requests 
        /// are received by NCache server in outproc caches.
        /// </summary>
        public const int SocketServer = 0;

        /// <summary>
        /// Represents the top level cache layer from where call invocations 
        /// on cache are started by the SocketServer.
        /// </summary>
        public const int CacheCore = 1;

        /// <summary>
        /// Represents the topology layer of cache.
        /// </summary>
        public const int Topology = 2;

        /// <summary>
        /// Represents all the layers that exist between the topology layer and 
        /// the local cache layer. This includes LocalCacheBase, HashedLocalCache 
        /// and IndexedLocalCache.
        /// </summary>
        public const int CacheInternal = 3;
        public const int Replication = 4;
        public const int StateTransfer = 5;
        public const int Events = 6;
        public const int BackingSource = 7;
        public const int AsyncCrud = 8;
        public const int AsyncResyncTask = 9;
        public const int Expiration = 10;
        public const int UserBinaryObject = 12;
        public const int CacheEntry = 13;

        /// <summary>
        /// Represents all the modules in NCache server. This exists for cases where 
        /// we are uncertain where the pooled item originated from.
        /// </summary>
        public const int Global = 14;
        public const int CacheSync = 15;
        public const int CacheImpl = 16;
        public const int LocalBase = 17;

        /// <summary>
        /// Represents the layer of cache on NCache server that communicates with the 
        /// caching store.
        /// </summary>
        public const int LocalCache = 18;

        /// <summary>
        /// Represents the cache store on NCache server for both InProc and OutProc 
        /// caches.
        /// </summary>
        public const int CacheStore = 19;
        public const int CompressedValueEntry = 20;
        public const int Recoder = 21;
        public const int Asynctask = 22;
    
        /// <summary>
        /// Represents the OperationContext.
        /// </summary>
        public const int OperationContext = 23;

        /// <summary>
        /// Represents the Cache Loader module for InProc caches.
        /// </summary>
        public const int InProcLoader = 24;

        /// <summary>
        /// Represents the Cache Sync Manager used to synchronize client cache via polling.
        /// </summary>
        public const int SyncManager = 26;

        /// <summary>
        /// Represents enumeration of cache as a module.
        /// </summary>
        public const int Enumeration = 27;
        
        /// <summary>
        /// Represents the whole Pub/Sub module.
        /// </summary>
        public const int PubSub = 29;

        /// <summary>
        /// Represents eviction policy as module (even though 
        /// it may not be regarded as one).
        /// </summary>
        public const int Eviction = 30;

        /// <summary>
        /// Represents the whole client-side code as a separate module.
        /// </summary>
        public const int Client = 31;

        public const int CacheInsResultWithEntry = 32;

        // NOTE : Always add a new module above this line.

        /* ***************************************************************************************
         * 
         * NOTE : Always increment the total modules count below when a new module is added.
         * 
         *************************************************************************************** */

        /// <summary>
        /// Represents the total number of modules that are pooling items.
        /// </summary>
        internal const int ModulesCount = 33;
    }
}
