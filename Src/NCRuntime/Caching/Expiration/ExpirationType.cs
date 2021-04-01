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

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// The type of expiration to be used while expiring items in cache. The 
    /// value of this type varies from item to item in cache.
    /// </summary>
    public enum ExpirationType
    {
        /// <summary>
        /// Indicates that no expiration is to take place.
        /// </summary>
        None,

        /// <summary>
        /// Indicates that item expiration in cache is to follow 
        /// idle expiration.
        /// </summary>
        Sliding,

        /// <summary>
        /// Indicates that item expiration in cache is to follow 
        /// fixed expiration.
        /// </summary>

        Absolute
    }
}
