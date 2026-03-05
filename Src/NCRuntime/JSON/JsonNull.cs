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

using Alachisoft.NCache.Runtime.Enum;
using System;

namespace Alachisoft.NCache.Runtime.JSON
{
    /// <summary>
    /// Represents NULL value in JSON standards
    /// </summary>
    [System.Serializable]
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public sealed class JsonNull : JsonValueBase
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public JsonNull() : base(null, JsonDataType.Null)
        {
        }

        /// <summary>
        /// String representation of JSON Null object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "null";
        }

        /// <summary>
        /// Checks if the obj is equal to JSONNull object
        /// </summary>
        /// <param name="obj">object to be compared</param>
        /// <returns>true if obj is JSONNull</returns>
        public override bool Equals(object obj)
        {
            return (obj as JsonNull) != null;
        }
    }
}
