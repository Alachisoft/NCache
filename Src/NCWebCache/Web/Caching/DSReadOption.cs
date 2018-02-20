// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Enumeration that defines the fetch operation on cache can read from data source if item not found
    /// </summary>
    public enum DSReadOption
    {
        /// <summary>
        /// Return null if item not found
        /// </summary>
        None = 0,

        /// <summary>
        /// Look data source for item if not found
        /// </summary>
        ReadThru = 1
    }
}