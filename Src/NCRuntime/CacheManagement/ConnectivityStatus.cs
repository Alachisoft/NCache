//  Copyright (c) 2026 Alachisoft
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
namespace Alachisoft.NCache.Runtime.CacheManagement
{
    /// <summary>
    /// Cache connectivity status contains the connectivity status of Cache nodes
    /// </summary>
    public enum ConnectivityStatus
    {        
        /// <summary>
        /// if cache node is stop then Connectivity status will be set to CacheStoped
        /// </summary>
        CacheStoped,
        /// <summary>
        /// if Cache node is running then connectivity status will be set to Running
        /// </summary>
        Running,
        /// <summary>
        /// if Cache is fully connected then connectivity status is FullyConnected
        /// </summary>
        FullyConnected,
        /// <summary>
        /// if Cache is partially connected then connectivity status is  PartialConnected
        /// </summary>
        PartialConnected,
    }
}