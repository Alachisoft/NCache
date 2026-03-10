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
    /// Interface that is used for custom implementation of JSON Object type to be used in NCache
    /// </summary>

    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public interface IJsonObject : IEnumerable<KeyValuePair<string, JsonValueBase>>
    {
        /// <summary>
        /// Key based indexer for JSON Object 
        /// </summary>
        /// <param name="attributeName">key</param>
        /// <returns>value associated with the attribute name</returns>
        JsonValueBase this[string attributeName]
        {
            get; set;
        }

        /// <summary>
        /// Number of items in the collection
        /// </summary>
        int Count
        {
            get;
        }

        /// <summary>
        /// Clears all the items in the JSONObject and brings the count of attributes to 0
        /// </summary>
        void Clear();

        /// <summary>
        /// Adds JSONValue object with the unique attribute name
        /// </summary>
        /// <param name="attributeName">key against which JSONValue is identified</param>
        /// <param name="attributeValue">JSONValue to be added</param>
        void AddAttribute(string attributeName, JsonValue attributeValue);

        /// <summary>
        /// Adds JSONValueBase object with the unique attribute name
        /// </summary>
        /// <param name="attributeName">key against which JSONValueBase is identified</param>
        /// <param name="attributeValue">JSONValueBase to be added</param>
        void AddAttribute(string attributeName, JsonValueBase attributeValue);

        /// <summary>
        /// Gets all the attribute names
        /// </summary>
        /// <returns>System.Collections.Generic.ICollection<string> which contains all the keys</string></returns>
        ICollection<string> GetAttributeNames();

        /// <summary>
        /// Removes the attribute entry identified by the attribute name
        /// </summary>
        /// <param name="attributeName">Unique key that identifies the attribute</param>
        /// <returns>true if attribute removed successfully</returns>
        bool RemoveAttribute(string attributeName);

        /// <summary>
        /// Checks if any entry exits against attribute name
        /// </summary>
        /// <param name="attributeName">key to search in the JSONObject</param>
        /// <returns>true if the key exists</returns>
        bool ContainsAttribute(string attributeName);

        /// <summary>
        /// Gets attribute against the specified attribute name
        /// </summary>
        /// <param name="attributeName">key that identifies the JSONObject</param>
        /// <returns>JSONObject against the key specified if exits</returns>
        JsonValueBase GetAttributeValue(string attributeName);
    }
}
