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

namespace Alachisoft.NCache.Integrations.EntityFramework.CacheEntry
{
    /// <summary>
    /// This class represents a cacheable database table with rows and columns
    /// </summary>
    [Serializable]
    public sealed class DbQueryResults : IDisposable
    {
        private List<DbRow> rows;
        private Dictionary<int, DbColumnInfo> columnInfo;

        /// <summary>
        /// Create a new table.
        /// </summary>
        /// <param name="reader">Instance of database reader</param>
        public DbQueryResults(DbDataReader reader)
        {           
            this.rows = new List<DbRow>();
            this.columnInfo = new Dictionary<int, DbColumnInfo>(reader.FieldCount);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                this.columnInfo.Add(i, new DbColumnInfo()
                {
                    Ordinal = i,
                    Name = reader.GetName(i),
                    DbTypeName = reader.GetDataTypeName(i),
                    TypeName = reader.GetFieldType(i).ToString()
                });
            }
        }

        /// <summary>
        /// Get the number of columns in the current row.
        /// </summary>
        public int FieldCount { get; internal set; }

        /// <summary>
        /// Get the value indicating whether the table contains one or more rows.
        /// </summary>
        public bool HasRows { get { return (this.rows != null && this.rows.Count > 0); } }

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        /// <remarks>As we are caching only SELECT statement results, the property always returns -1.</remarks>
        public int RecordsAffected { get { return -1; } }

        /// <summary>
        /// Gets the number of fields in the DbDataReader that are not hidden.
        /// </summary>
        public int VisibleFieldCount { get; internal set; }

        /// <summary>
        /// Get the number of rows read so far
        /// </summary>
        public int RowsCount { get { return (this.rows != null ? this.rows.Count : 0); } }

        /// <summary>
        /// Get or set the row at specified index.
        /// </summary>
        /// <param name="index">Zero-based index of row to get or set.</param>
        /// <returns>Row at specified index.</returns>
        public DbRow this[int index]
        {
            get { return this.Get(index); }
            set { this.rows[index] = value; }
        }

        /// <summary>
        /// Add a new row at the end of collection.
        /// </summary>
        /// <param name="item">Row to be added. The value will only be added if its not null.</param>
        public void Add(DbRow item)
        {
            if (item != null)
            {
                this.rows.Add(item);
            }
        }

        /// <summary>
        /// Get the row at specified index.
        /// </summary>
        /// <param name="index">Zero-based index of row to get.</param>
        /// <returns>Row at specified index.</returns>
        public DbRow Get(int index)
        {
            return this.rows[index];
        }

        /// <summary>
        /// Get the column ordinal, given the name of the column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The zero-based column ordinal.</returns>
        public int GetOrdinal(string name)
        {
            KeyValuePair<int, DbColumnInfo> info = this.columnInfo.FirstOrDefault(
                (pair) => (pair.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                );

            if (info.Value == null)
            {
                throw new IndexOutOfRangeException(name);
            }
            return info.Value.Ordinal;
        }

        /// <summary>
        /// Gets the name of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The name of the specified column.</returns>
        public string GetName(int ordinal)
        {
            return this.GetColumnInfo(ordinal).Name;
        }

        /// <summary>
        /// Gets a string representing the data type of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based ordinal position of the column to find.</param>
        /// <returns> A string representing the name of the data type.</returns>
        public string GetDataTypeName(int ordinal)
        {
            return this.GetColumnInfo(ordinal).DbTypeName;
        }

        /// <summary>
        /// Gets the System.Type that is the data type of the object.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The System.Type that is the data type of the object. If the type does not
        /// exist on the client, in the case of a User-Defined Type (UDT) returned from
        /// the database, GetFieldType returns null.</returns>
        public Type GetFieldType(int ordinal)
        {
            return Type.GetType(this.GetColumnInfo(ordinal).TypeName);
        }

        /// <summary>
        /// Get the column information for specified ordinal.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>DbColumnInfo at specified ordinal</returns>
        private DbColumnInfo GetColumnInfo(int ordinal)
        {
            DbColumnInfo info = this.columnInfo[ordinal];
            if (info == null)
            {
                throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            }
            return info;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (this.rows != null)
            {
                this.rows.RemoveAll(
                    row =>
                    {
                        row.Dispose();
                        return true;
                    });
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
