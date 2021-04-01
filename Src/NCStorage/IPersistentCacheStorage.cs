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
namespace Alachisoft.NCache.Storage
{
    /// <summary>
    /// Interface that defines the standard operations to be implemented by all persistent stores.
    /// </summary>
    public interface IPersistentCacheStorage
    {
        /// <summary>
        /// Load store state and data from persistent medium.
        /// </summary>
        void LoadStorageState();

        /// <summary>
        /// Save store state and data to persistent medium.
        /// </summary>
        void SaveStorageState();
    }
}