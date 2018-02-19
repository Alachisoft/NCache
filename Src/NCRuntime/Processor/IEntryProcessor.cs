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
    /// EntryProcesser interface to implement the process logic for server.
    /// </summary>
    public interface IEntryProcessor
    {
        /// <summary>
        /// Contains logic to be executed on server side. 
        /// </summary>
        /// <param name="entry">An instance of IMutableEntry</param>
        /// <param name="arguments">Arraqy of the parameters for the cache</param>
        /// <returns>Returns any required results. </returns>
        /// <example>
        /// Argument list is optional. 
        /// Following example demonstrates how to implement ProcessEntry.
        /// <code>
        /// public object ProcessEntry(IMutableEntry entry, params object[] arguments)
        /// {
        ///     if (entry.Key.Equals("1"))
        ///     {
        ///       if (entry.Exists())
        ///         {
        ///            entry.Remove();
        ///            return 0;
        ///         }
        ///       else
        ///         {
        ///            entry.Remove();
        ///            return -1;
        ///         }
        ///      }
        ///     else if (entry.Equals("15"))
        ///     {
        ///        object value = "Greater Than 10";
        ///        entry.Value = value;
        ///        return value;
        ///     }
        ///
        ///    return 1;
        /// }
        ///
        /// </code>
        /// </example>
        object ProcessEntry(IMutableEntry entry, params object[] arguments);
        /// <summary>
        /// If entry required by EntryProcesser is locked by the application, In case of true, lock is ignored to access the entry and method is executed.
        /// </summary>
        /// <returns>True or false depending upon the logic.</returns>
        /// <example>
        /// Following examples demonstrates how to implement IgnoreLock.
        /// <code>
        /// public bool IgnoreLock()
        /// {
        ///     // implement logic
        ///      return true;
        /// }
        /// </code>
        /// </example>
        bool IgnoreLock();
    }
}
