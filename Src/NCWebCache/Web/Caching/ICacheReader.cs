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
    public interface ICacheReader
    {
        /// <summary>
        /// Gets number of columns.
        /// </summary>
        int FieldCount { get;}

        /// <summary>
        /// Closes IDataReader
        /// </summary>
        void Close();

        /// <summary>
        /// Gets is Reader Closed
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Returns object at specified index
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>object value of specified index</returns>
        /// 
        [System.Runtime.CompilerServices.IndexerName("Item")]
        object this[int index] { get;}

        /// <summary>
        /// Returns object at specified index
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>object value of specified index</returns>
        /// 
        [System.Runtime.CompilerServices.IndexerName("Item")]
        object this[string columnName] { get; }

        /// <summary>
        /// Advances ICacheReader to next record
        /// </summary>
        /// <returns>true if there are more rows; otherwise false </returns>
        bool Read();

        /// <summary>
        /// Gets value of specified column as bool
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>bool value of specified column</returns>
        bool GetBoolean(int index);

        /// <summary>
        /// Gets value of specified column as string
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>string value of specified column</returns>
        string GetString(int index);

        /// <summary>
        /// Gets value of specified column as decimal
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>decimal value of specified column</returns>
        decimal GetDecimal(int index);

        /// <summary>
        /// Gets value of specified column as double
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>double value of specified column</returns>
        double GetDouble(int index);

        /// <summary>
        /// Gets value of specified column as 16 bit integer
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Int16 value of specified column</returns>
        short GetInt16(int index);

        /// <summary>
        /// Gets value of specified column as 32 bit integer
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Int32 value of specified column</returns>
        int GetInt32(int index);

        /// <summary>
        /// Gets value of specified column as 64 bit integer
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Int64 value of specified column</returns>
        long GetInt64(int index);

        /// <summary>
        /// Gets value at specified column index
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Value at specified column</returns>
        object GetValue(int index);
        
        /// <summary>
        /// Populates array of objects with values in current row
        /// </summary>
        /// <param name="objects">array of objects to be populated</param>
        /// <returns>No of objects copied in specified array</returns>
        int GetValues(object [] objects);

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
