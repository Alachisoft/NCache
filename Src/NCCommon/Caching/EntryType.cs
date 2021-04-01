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

namespace Alachisoft.NCache.Common.Caching
{
    /// <summary>
    /// An enumeration specifying the type of entry in cache.
    /// </summary>
    public enum EntryType : byte
    {
        /// <summary>
        /// Specifies that the entry under question is a cache item.
        /// </summary>
        CacheItem = 1,

        ///// <summary>
        ///// Specifies that the entry under question is a collection item of type list.
        ///// </summary>
        //List = 2,

        ///// <summary>
        ///// Specifies that the entry under question is a collection item of type queue.
        ///// </summary>
        //Queue = 3,

        ///// <summary>
        ///// Specifies that the entry under question is a collection item of type set.
        ///// </summary>
        //Set = 4,

        ///// <summary>
        ///// Specifies that the entry under question is a collection item of type dictionary.
        ///// </summary>
        //Dictionary = 5,

        ///// <summary>
        ///// Specifies that the entry under question is a collection item of type counter.
        ///// </summary>
        //Counter = 6
    }
}
