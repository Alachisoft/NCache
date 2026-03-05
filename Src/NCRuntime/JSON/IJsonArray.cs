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

using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.Runtime.JSON
{
    /// <summary>
    /// Interface that is used for custom implementation of JSON Array type to be used in NCache
    /// </summary>
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public interface IJsonArray : ICollection<JsonValueBase>, IEnumerable<JsonValueBase>
    {
        /// <summary>
        /// Indexer for the JSON Arary
        /// </summary>
        /// <param name="index"></param>
        /// <returns>JSON value on that index</returns>
        JsonValueBase this[int index]
        {
            get; set;
        }

        /// <summary>
        /// Adds item to JSON Array
        /// </summary>
        /// <param name="item">Item to be added</param>
        void Add(JsonValue item);

        /// <summary>
        /// Removes item from JSON Array
        /// </summary>
        /// <param name="item">Item to be removed</param>
        /// <returns>True if removed successfully</returns>
        bool Remove(JsonValue item);

        /// <summary>
        /// Checks whether the item exits in JSON Array
        /// </summary>
        /// <param name="item">value to check</param>
        /// <returns>True in case Array contains the item</returns>
        bool Contains(JsonValue item);
    }
}
