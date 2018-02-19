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

using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.Enum;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.DataReader
{

    public class RecordRow : ICloneable, ICompactSerializable
    {

        private ColumnCollection _columns;
        private ClusteredArray<object> _objects;
        private object _tag;

        internal RecordRow(ColumnCollection columns)
        {
            _columns = columns;
            _objects = new ClusteredArray<object>(columns.Count);
        }

        public object this[int index]
        {
            get { return _objects[index]; }
            set { _objects[index] = value; }
        }

        public object this[string columnName]
        {
            get { return _objects[_columns.GetColumnIndex(columnName)]; }
            set { _objects[_columns.GetColumnIndex(columnName)] = value; }
        }

        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        public ColumnCollection Columns
        {
            get { return _columns; }
            internal set { _columns = value; }
        }


        public void SetColumnValue(string columnName, object value)
        {
            _objects[_columns.GetColumnIndex(columnName)] = value;
        }

        public void SetColumnValue(int columnIndex, object value)
        {
            _objects[columnIndex] = value;
        }

        public void SetAll(ClusteredArrayList values)
        {
            values.CopyTo(_objects.ToArray(), 0);
        }

        public object GetColumnValue(string columnName)
        {
            return _objects[_columns.GetColumnIndex(columnName)];
        }

        public object GetColumnValue(int columnIndex)
        {
            return _objects[columnIndex];
        }

        public object[] GetAll()
        {
            return (object[])_objects.Clone();
        }

        public int GetSize()
        {
            int size = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                switch (_columns[i].DataType)
                {
                    case ColumnDataType.AverageResult:
                        size += 32;
                        break;
                    case ColumnDataType.Bool:
                        size += 1;
                        break;
                    case ColumnDataType.Byte:
                        size += 1;
                        break;
                    case ColumnDataType.Char:
                        size += 2;
                        break;
                    case ColumnDataType.DateTime:
                        size += 64;
                        break;
                    case ColumnDataType.Decimal:
                        size += 16;
                        break;
                    case ColumnDataType.Double:
                        size += 8;
                        break;
                    case ColumnDataType.Float:
                        size += 4;
                        break;
                    case ColumnDataType.Int16:
                        size += 2;
                        break;
                    case ColumnDataType.Int32:
                        size += 4;
                        break;
                    case ColumnDataType.Int64:
                        size += 8;
                        break;
                    case ColumnDataType.Object:
                        break;
                    case ColumnDataType.SByte:
                        size += 1;
                        break;
                    case ColumnDataType.String:
                        string obj = _objects[i] as string;
                        if (obj != null)
                            size += obj.Length * 2;
                        break;
                    case ColumnDataType.UInt16:
                        size += 2;
                        break;
                    case ColumnDataType.UInt32:
                        size += 4;
                        break;
                    case ColumnDataType.UInt64:
                        size += 8;
                        break;
                }
            }
            return size;
        }

        public object Clone()
        {
            RecordRow row = new RecordRow(this._columns);
            row._objects = (ClusteredArray<object>)this._objects.Clone();
            return row;
        }

        public int CompareOrder(RecordRow row, List<OrderByArgument> orderBy)
        {
            int result = 0;
            foreach (OrderByArgument oba in orderBy)
            {
                switch (_columns[oba.AttributeName].DataType)
                {
                    case ColumnDataType.Bool:
                        result = ((bool)this.GetColumnValue(oba.AttributeName)).CompareTo(((bool)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.Byte:
                        result = ((byte)this.GetColumnValue(oba.AttributeName)).CompareTo(((byte)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.Char:
                        result = ((char)this.GetColumnValue(oba.AttributeName)).CompareTo(((char)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.DateTime:
                        result = ((DateTime)this.GetColumnValue(oba.AttributeName)).CompareTo(((DateTime)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.Decimal:
                        result = ((decimal)this.GetColumnValue(oba.AttributeName)).CompareTo(((decimal)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.Double:
                        result = ((double)this.GetColumnValue(oba.AttributeName)).CompareTo(((double)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.Float:
                        result = ((float)this.GetColumnValue(oba.AttributeName)).CompareTo(((float)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.Int16:
                        result = ((Int16)this.GetColumnValue(oba.AttributeName)).CompareTo(((Int16)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.Int32:
                        result = ((Int32)this.GetColumnValue(oba.AttributeName)).CompareTo(((Int32)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.Int64:
                        result = ((Int64)this.GetColumnValue(oba.AttributeName)).CompareTo(((Int64)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.SByte:
                        result = ((sbyte)this.GetColumnValue(oba.AttributeName)).CompareTo(((sbyte)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.String:
                        result = ((string)this.GetColumnValue(oba.AttributeName)).CompareTo(((string)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.UInt16:
                        result = ((UInt16)this.GetColumnValue(oba.AttributeName)).CompareTo(((UInt16)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.UInt32:
                        result = ((UInt32)this.GetColumnValue(oba.AttributeName)).CompareTo(((UInt32)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.UInt64:
                        result = ((UInt64)this.GetColumnValue(oba.AttributeName)).CompareTo(((UInt64)row.GetColumnValue(oba.AttributeName)));
                        break;
                    case ColumnDataType.CompressedValueEntry:
                    case ColumnDataType.Object:
                        break;
                }

                if (result != 0)
                {
                    if (oba.Order == Order.DESC)
                        result = -result;
                    break;
                }
            }
            return result;
        }

        public void Merge(RecordRow row)
        {
            for (int i = 0; i < this._columns.Count; i++)
            {
                if (this._columns[i].ColumnType == ColumnType.AggregateResultColumn)
                {
                    switch (this._columns[i].AggregateFunctionType)
                    {
                        case AggregateFunctionType.SUM:
                            decimal a;
                            decimal b;

                            object thisVal = this._objects[i];
                            object otherVal = row._objects[i];

                            decimal? sum = null;

                            if (thisVal == null && otherVal != null)
                            {
                                sum = (decimal)otherVal;
                            }
                            else if (thisVal != null && otherVal == null)
                            {
                                sum = (decimal)thisVal;
                            }
                            else if (thisVal != null && otherVal != null)
                            {
                                a = (decimal)thisVal;
                                b = (decimal)otherVal;
                                sum = a + b;
                            }

                            if (sum != null)
                            {
                                this._objects[i] = sum;
                            }
                            else
                            {
                                this._objects[i] = null;
                            }
                            break;

                        case AggregateFunctionType.COUNT:
                            a = (decimal)this._objects[i];
                            b = (decimal)row._objects[i];
                            decimal count = a + b;

                            this._objects[i] = count;
                            break;

                        case AggregateFunctionType.MIN:
                            IComparable thisValue = (IComparable)this._objects[i];
                            IComparable otherValue = (IComparable)row._objects[i];
                            IComparable min = thisValue;

                            if (thisValue == null && otherValue != null)
                            {
                                min = otherValue;
                            }
                            else if (thisValue != null && otherValue == null)
                            {
                                min = thisValue;
                            }
                            else if (thisValue == null && otherValue == null)
                            {
                                min = null;
                            }
                            else if (otherValue.CompareTo(thisValue) < 0)
                            {
                                min = otherValue;
                            }

                            this._objects[i] = min;
                            break;

                        case AggregateFunctionType.MAX:
                            thisValue = (IComparable)this._objects[i];
                            otherValue = (IComparable)row._objects[i];
                            IComparable max = thisValue;

                            if (thisValue == null && otherValue != null)
                            {
                                max = otherValue;
                            }
                            else if (thisValue != null && otherValue == null)
                            {
                                max = thisValue;
                            }
                            else if (thisValue == null && otherValue == null)
                            {
                                max = null;
                            }
                            else if (otherValue.CompareTo(thisValue) > 0)
                            {
                                max = otherValue;
                            }

                            this._objects[i] = max;
                            break;

                        case AggregateFunctionType.AVG:
                            thisVal = this._objects[i];
                            otherVal = row._objects[i];

                            AverageResult avg = null;
                            if (thisVal == null && otherVal != null)
                            {
                                avg = (AverageResult)otherVal;
                            }
                            else if (thisVal != null && otherVal == null)
                            {
                                avg = (AverageResult)thisVal;
                            }
                            else if (thisVal != null && otherVal != null)
                            {
                                AverageResult thisResult = (AverageResult)thisVal;
                                AverageResult otherResult = (AverageResult)otherVal;

                                avg = new AverageResult();
                                avg.Sum = thisResult.Sum + otherResult.Sum;
                                avg.Count = thisResult.Count + otherResult.Count;
                            }

                            if (avg != null)
                            {
                                this._objects[i] = avg;
                            }
                            else
                            {
                                this._objects[i] = null;
                            }
                            break;
                    }
                }
            }

        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _columns = reader.ReadObject() as ColumnCollection;
            _objects = new ClusteredArray<object>(_columns.Count);
            for (int i = 0; i < _objects.Length; i++)
            {
                _objects[i] = reader.ReadObject();
            }
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_columns);
            for (int i = 0; i < _objects.Length; i++)
            {
                writer.WriteObject(_objects[i]);
            }
        }
    }

}
