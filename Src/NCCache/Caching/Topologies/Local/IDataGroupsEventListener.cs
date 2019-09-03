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
namespace Alachisoft.NCache.Caching.DataGrouping
{
    /// <summary>
    /// An interface for the data group event listener.
    /// </summary>
    public interface IDataGroupsEventListener
    {
        /// <summary>
        /// Fired when a new data group is added in the cache.
        /// </summary>
        /// <param name="group">Newly added group</param>
        void OnDataGroupAdded(string group);

        /// <summary>
        /// Fired when an existing data group is removed from the cache.
        /// </summary>
        /// <param name="group">Removed data group</param>
        /// <param name="lastItemRemovedReason">Reason for the removal of last group item.</param>
        void OnDataGroupRemoved(string group, ItemRemoveReason lastItemRemovedReason);
    }
}