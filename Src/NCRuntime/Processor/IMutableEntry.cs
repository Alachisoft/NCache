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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Runtime.Processor
{
    /// <summary>
    /// Interface to implement instance of MutableEntry. 
    /// </summary>
    public interface IMutableEntry
    {
        /// <summary>
        ///Gets the key of cache entry on which entry proceser has to be executed. 
        /// </summary>
        /// <returns>Returns key. </returns>
        string Key { get; }
        /// <summary>
        /// Checks, if the required key for Entry Processor exists in cache or not.
        /// </summary>
        /// <returns>Returns true, if key exists or vice versa.</returns>
        bool Exists();
        /// <summary>
        /// Removes the key from cache after executing the entry processer.
        /// </summary>
        void Remove();
        /// <summary>
        /// Sets/Gets data for relevant key. 
        /// </summary>
        object Value { get; set; }
        /// <summary>
        /// Convert data type of the value.
        /// </summary>
        /// <param name="type"> New data Type.</param>
        /// <returns>Value with updated datatype. </returns>
        object UnWrap(Type type);
    }
}
