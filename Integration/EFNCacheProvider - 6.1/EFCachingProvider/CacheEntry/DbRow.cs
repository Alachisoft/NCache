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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Collections;

namespace Alachisoft.NCache.Integrations.EntityFramework.CacheEntry
{
    /// <summary>
    /// This class represents a cacheable row
    /// </summary>
    [Serializable]
    public sealed class DbRow : IDisposable
    {
        /// <summary>
        /// Get the column values in current row.
        /// </summary>
        public object[] Values { get; internal set; }

        /// <summary>
        /// Get the depth of nesting for current row.
        /// </summary>
        public int Depth { get; internal set; }


        /// <summary>
        /// Get the number of columns in the current row.
        /// </summary>
        public int FieldCount { get { return this.Values.Length; } }

        /// <summary>
        /// Gets the value of a specified column as an instance of Object.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public object this[int ordinal] { get { return this.Values[ordinal]; } }

        #region IDisposable Members

        public void Dispose()
        {
            this.Values = null;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
