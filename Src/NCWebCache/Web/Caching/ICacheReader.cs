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

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Reads one or more than forward-only stream of result sets by executing OQ commands on cache source. 
    /// </summary>
    public interface ICacheReader : IDisposable
    {
        /// <summary>
        /// Gets number of columns.
        /// </summary>
        int FieldCount { get; }

        /// <summary>
        /// Closes IDataReader
        /// </summary>
        void Close();

        /// <summary>
        /// True, if reader is closed else false. 
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Returns object at specified index.
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>object value of specified index</returns>
        /// 
        [System.Runtime.CompilerServices.IndexerName("Item")]
        object this[int index] { get; }

        /// <summary>
        /// Returns object at specified column.
        /// </summary>
        /// <param name="columnName">Namme of the column.</param>
        /// <returns>value of specified column. </returns>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        object this[string columnName] { get; }

        /// <summary>
        /// Advances ICacheReader to next record
        /// </summary>
        /// <returns>true if there are more rows; else false </returns>
        bool Read();

        /// <summary>
        /// Gets value of specified index as bool
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>bool value on specified index</returns>
        bool GetBoolean(int index);

        /// <summary>
        /// Gets value of specified index as string
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>string value on specified column</returns>
        string GetString(int index);

        /// <summary>
        /// Gets value of specified index as decimal
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>decimal value on specified index</returns>
        decimal GetDecimal(int index);

        /// <summary>
        /// Gets value of specified index as double
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>double value on specified index</returns>
        double GetDouble(int index);

        /// <summary>
        /// Gets value of specified index as 16 bit integer
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Int16 value on specified index</returns>
        short GetInt16(int index);

        /// <summary>
        /// Gets value of specified index as 32 bit integer
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Int32 value on specified index</returns>
        int GetInt32(int index);

        /// <summary>
        /// Gets value of specified index as 64 bit integer
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Int64 value on specified index</returns>
        long GetInt64(int index);

        /// <summary>
        /// Gets value at specified column index
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>long value on specified index</returns>
        object GetValue(int index);

        /// <summary>
        /// Populates array of objects with values in current row
        /// </summary>
        /// <param name="objects">array of objects to be populated</param>
        /// <returns>No of objects copied in specified array</returns>
        int GetValues(object[] objects);

        /// <summary>
        /// Returns name of specidied column index
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Name of column</returns>
        string GetName(int index);

        /// <summary>
        /// Returns index of specified column name
        /// </summary>
        /// <param name="columnName">Name of column</param>
        /// <returns>Index of column</returns>
        int GetOrdinal(string columnName);

        /// <summary>
        /// Returns DateTime at specified column index
        /// </summary>
        /// <param name="index">Index of Column</param>
        /// <returns></returns>
        DateTime GetDateTime(int index);
    }
}