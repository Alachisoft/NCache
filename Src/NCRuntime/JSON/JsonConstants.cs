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
using System.Globalization;

namespace Alachisoft.NCache.Runtime.JSON
{
    /// <summary>
    /// Utility class for JSON  
    /// </summary>
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public class JsonConstants
    {
        internal const string JsonExtraAttributeIdName = "$id";
        internal const string JsonExtraAttributeTypeName = "$type";

        /// <summary>
        /// Standard DateTime format to be used for all JSON based implementations used for serializing and deserializing DateTime
        /// </summary>
        public const string SerializedDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

        /// <summary>
        /// Standard DateTimeCulture format to be used for all JSON based implementations used for serializing and deserializing DateTimeCulture
        /// </summary>
        public static readonly CultureInfo SerializedDateTimeCulture = new CultureInfo("en-US");
    }
}
