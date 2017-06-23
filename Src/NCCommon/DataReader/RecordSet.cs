// Copyright (c) 2017 Alachisoft
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

using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.Enum;
using Runtime = Alachisoft.NCache.Runtime;
namespace Alachisoft.NCache.Common.DataReader
{

    public class RecordSet : IRecordSet, ICompactSerializable
    {
        private ColumnCollection _columns;
        private RowCollection _rows;
        private SubsetInfo _subsetInfo;
        private int _nextIndex = 0;

        public RecordSet()
        {
            _columns = new ColumnCollection();
            _rows = new RowCollection(_columns);
        }

        public RecordSet(ColumnCollection columnMetaData)
        {
            _columns = columnMetaData;
            _rows = new RowCollection(_columns);
        }

        public int NextIndex
        {
            get { return _nextIndex; }
            set { _nextIndex = value; }
        }
        public SubsetInfo SubsetInfo
        {
            get { return _subsetInfo; }
            set { _subsetInfo = value; }
        }

        /// <summary>
        /// Returns <see cref="Alachisoft.NCache.Common.DataStructures.ColumnCollection"/> associated with current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        public ColumnCollection Columns
        { get { return _columns; } }

        /// <summary>
        /// Returns <see cref="Alachisoft.NCache.Common.DataStructures.RowCollection"/> present in current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        public RowCollection Rows
        { get { return _rows; } }

        /// <summary>
        /// Return a sub <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/> with same column matadata as current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/> but contains only specified rows.
        /// </summary>
        /// <param name="startingRowIndex">Starting row index for sub <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/> generation.</param>
        /// <param name="count">Total number of rows to be included in sub <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>.</param>
        /// <returns>Sub <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/></returns>
        public RecordSet GetSubRecordSet(int startingRowIndex, int count)
        {
            RecordSet subRecordSet = new RecordSet(this._columns);
            int i = -1;
            for (i = startingRowIndex; i < startingRowIndex + count && i < this.RowCount; i++)
            {
                subRecordSet.AddRow((RecordRow)this.GetRow(i).Clone());
            }
            subRecordSet.SubsetInfo = new SubsetInfo();
            subRecordSet.SubsetInfo.LastAccessedRowID = i < this.RowCount ? i - 1 : this.RowCount - 1;
            return subRecordSet;
        }

        /// <summary>
        /// Gets size of current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        /// <returns>Size in bytes of <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/></returns>
        public int GetSize()
        {
            return 0;
        }


        //IRecordSet Implementation

        /// <summary>
        /// Adds <see cref="Alachisoft.NCache.Common.DataStructures.RecordColumn"/> in current <see cref="Alachisoft.NCache.Common.DataStructures.RecorSet"/>
        /// </summary>
        /// <param name="column"><see cref="Alachisoft.NCache.Common.DataStructures.RecordColumn"/> to be added</param>
        public void AddColumn(RecordColumn column)
        {
            this._columns.Add(column);
        }

        /// <summary>
        /// Returns new <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> with column matadata of current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        /// <returns>Newly created <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/></returns>
        public RecordRow CreateRow()
        {
            RecordRow row = new RecordRow(_columns);
            return row;
        }

        /// <summary>
        /// Adds row to current <see cref="Alachisoft.NCache.Common.DataStructures.RecorSet"/>
        /// </summary>
        /// <param name="row"><see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> to be added in current <see cref="Alachisoft.NCache.Common.DataStructures.RecorSet"/></param>
        public void AddRow(RecordRow row)
        {
            _rows.Add(row);
        }

        /// <summary>
        /// Gets <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> associated with rowID
        /// </summary>
        /// <param name="rowID">Index of <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> required</param>
        /// <returns>Required <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> accoring to rowID</returns>
        public RecordRow GetRow(int rowID)
        {
            return _rows.GetRow(rowID);
        }

        public bool ContainsRow(int rowID)
        {
            return _rows.Contains(rowID);
        }

        /// <summary>
        /// Removes <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> associated with rowID
        /// </summary>
        /// <param name="rowID">Index of <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> to be removed</param>
        public void RemoveRow(int rowID)
        {
            _rows.RemoveRow(rowID);
        }

        /// <summary>
        /// Removes specified rows range from current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        /// <param name="startingIndex">Starting index of row's range to be removed.</param>
        /// <param name="count">Total number of rows to be removed.</param>
        /// <returns>Number of rows removed</returns>
        public int RemoveRows(int startingIndex, int count)
        {
            if (startingIndex < 0 || count < 0)
                return 0;
            int removed = 0;
            for (int i = startingIndex; i < startingIndex + count; i++)
            {
                if (_rows.RemoveRow(i))
                    removed++;
            }
            return removed;
        }

        public ColumnCollection GetColumnMetaData()
        {
            return _columns;
        }

        /// <summary>
        /// Gets number of rows in current <see cref="Alachisoft.NCache.Common.DataStructures.RecordSet"/>
        /// </summary>
        public int RowCount { get { return _rows.Count; } }


        public IRecordSetEnumerator GetEnumerator()
        {
            return new RecordSetEnumerator(this);
        }

        //Utility Functions for RecordSet

        /// <summary>
        /// Gets <see cref="Alachisoft.NCache.Common.DataStructures.ColumnDataType"/> of object
        /// </summary>
        /// <param name="obj">Object whose <see cref="Alachisoft.NCache.Common.DataStructures.ColumnDataType"/> is required</param>
        /// <returns><see cref="Alachisoft.NCache.Common.DataStructures.ColumnDataType"/> of object</returns>
        public static ColumnDataType ToColumnDataType(object obj)
        {
            if (obj is string)
                return ColumnDataType.String;
            else if (obj is decimal)
                return ColumnDataType.Decimal;
            else if (obj is Int16)
                return ColumnDataType.Int16;
            else if (obj is Int32)
                return ColumnDataType.Int32;
            else if (obj is Int64)
                return ColumnDataType.Int64;
            else if (obj is UInt16)
                return ColumnDataType.UInt16;
            else if (obj is UInt32)
                return ColumnDataType.UInt32;
            else if (obj is UInt64)
                return ColumnDataType.UInt64;
            else if (obj is double)
                return ColumnDataType.Double;
            else if (obj is float)
                return ColumnDataType.Float;
            else if (obj is byte)
                return ColumnDataType.Byte;
            else if (obj is sbyte)
                return ColumnDataType.SByte;
            else if (obj is bool)
                return ColumnDataType.Bool;
            else if (obj is char)
                return ColumnDataType.Char;
            else if (obj is DateTime)
                return ColumnDataType.DateTime;
            else if (obj is AverageResult)
                return ColumnDataType.AverageResult;
            else
                return ColumnDataType.Object;
        }

        /// <summary>
        /// Converts String represenation to appropriate object of specified <see cref="Alachisoft.NCache.Common.DataStructures.ColumnDataType"/>
        /// </summary>
        /// <param name="stringValue">String representation of object</param>
        /// <param name="dataType"><see cref="Alachisoft.NCache.Common.DataStructures.ColumnDataType"/> of object</param>
        /// <returns></returns>
        public static object ToObject(string stringValue, ColumnDataType dataType)
        {
            switch (dataType)
            {
                case ColumnDataType.Bool:
                    return bool.Parse(stringValue);
                case ColumnDataType.Byte:
                    return byte.Parse(stringValue);
                case ColumnDataType.Char:
                    return char.Parse(stringValue);
                case ColumnDataType.DateTime:
                    return DateTime.ParseExact(stringValue, "dd/MM/yyyy/HH/mm/ss/ffff/zzzz", System.Globalization.CultureInfo.InvariantCulture);
                case ColumnDataType.Decimal:
                    return decimal.Parse(stringValue);
                case ColumnDataType.Double:
                    return double.Parse(stringValue);
                case ColumnDataType.Float:
                    return float.Parse(stringValue);
                case ColumnDataType.Int16:
                    return Int16.Parse(stringValue);
                case ColumnDataType.Int32:
                    return Int32.Parse(stringValue);
                case ColumnDataType.Int64:
                    return Int64.Parse(stringValue);
                case ColumnDataType.SByte:
                    return sbyte.Parse(stringValue);
                case ColumnDataType.String:
                    return stringValue;
                case ColumnDataType.UInt16:
                    return UInt16.Parse(stringValue);
                case ColumnDataType.UInt32:
                    return UInt32.Parse(stringValue);
                case ColumnDataType.UInt64:
                    return UInt64.Parse(stringValue);
                default:
                    throw new InvalidCastException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static string GetString(object obj, ColumnDataType dataType)
        {
            switch (dataType)
            {
                case ColumnDataType.DateTime:
                    return ((DateTime)obj).ToString("dd/MM/yyyy/HH/mm/ss/ffff/zzzz");
                case ColumnDataType.String:
                    return (string)obj;
                default:
                    return obj.ToString();

            }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _columns = reader.ReadObject() as ColumnCollection;
            _rows = reader.ReadObject() as RowCollection;
            _subsetInfo = reader.ReadObject() as SubsetInfo;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_columns);
            writer.WriteObject(_rows);
            writer.WriteObject(_subsetInfo);
        }
    }

}
