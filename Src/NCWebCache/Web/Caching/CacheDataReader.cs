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
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Web.Caching

{
    /// <summary>
    /// To read query results contained in RecordSet. Suppresses hidden columns.
    /// </summary>
    internal class CacheDataReader : ICacheReader
    {
        private IRecordSetEnumerator _recordSetEnumerator;
        private RecordRow _currentRow;
        private ColumnCollection _columns;
        private int _hiddenColumnCount = 0;

        internal CacheDataReader(IRecordSetEnumerator recordSetEnumerator)
        {
            _recordSetEnumerator = recordSetEnumerator;
            _columns = _recordSetEnumerator != null ? _recordSetEnumerator.ColumnCollection : null;
            if (_columns != null)
                _hiddenColumnCount = _columns.HiddenColumnCount;
        }

        /// <summary>
        /// Gets number of visible columns in RecordSet 
        /// </summary>
        public int FieldCount
        {
            get
            {
                return _columns == null ? 0 : _columns.Count - _hiddenColumnCount;
            }
        }

        public void Close()
        {
            if (_recordSetEnumerator != null)
                _recordSetEnumerator.Dispose();
        }

        
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public object this[int index]
        {
            get
            {
                object obj = _currentRow[index];
                if (obj == null)
                    throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
                if (obj != null && obj is Alachisoft.NCache.Common.Queries.AverageResult)
                    return ((Alachisoft.NCache.Common.Queries.AverageResult)obj).Average;
                else
                    return obj;
            }
        }

        [System.Runtime.CompilerServices.IndexerName("Item")]
        public object this[string columnName]
        {
            get
            {
                object obj = _currentRow[columnName];
                if (obj == null)
                    throw new ArgumentException("Invalid columnName. Specified column does not exist in RecordSet.");
                if (obj != null && obj is Alachisoft.NCache.Common.Queries.AverageResult)
                    return ((Alachisoft.NCache.Common.Queries.AverageResult)obj).Average;
                else
                    return obj;
            }
        }
        
        public bool Read()
        {
            if (_recordSetEnumerator == null) return false;
            bool next = _recordSetEnumerator.MoveNext();
            if (next)
                _currentRow = _recordSetEnumerator.Current;
            return next;

        }

        public bool GetBoolean(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                return Convert.ToBoolean(_currentRow[index]);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
        }

        public string GetString(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                return Convert.ToString(_currentRow[index]);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");

        }

        public decimal GetDecimal(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                return Convert.ToDecimal(_currentRow[index]);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
        }

        public double GetDouble(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");            
            if (index >= 0 && index < FieldCount)
            {
                return Convert.ToDouble(_currentRow[index]);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
        }

        public short GetInt16(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                return Convert.ToInt16(_currentRow[index]);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
            
        }

        public int GetInt32(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                return Convert.ToInt32(_currentRow[index]);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
           
        }

        public long GetInt64(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                return Convert.ToInt64(_currentRow[index]);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
           
        }

        public object GetValue(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                object obj = _currentRow.GetColumnValue(index);

                if (!(_currentRow.Columns[index].IsHidden))
                {
                    if (obj is Alachisoft.NCache.Common.Queries.AverageResult)
                        return ((Alachisoft.NCache.Common.Queries.AverageResult)obj).Average;
                    else
                        return obj;
                }
                else
                    throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");

        }

        private object GetValue(string columnName)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (_currentRow.Columns.Contains(columnName))
            {
                object obj = _currentRow.GetColumnValue(columnName);
                if (!_currentRow.Columns[columnName].IsHidden)
                {
                    if (obj is Alachisoft.NCache.Common.Queries.AverageResult)
                        return ((Alachisoft.NCache.Common.Queries.AverageResult)obj).Average;
                    else
                        return obj;
                }
                else
                    throw new ArgumentException("Invalid columnName. Specified column does not exist in RecordSet.");
            }
            else
                throw new ArgumentException("Invalid columnName. Specified column does not exist in RecordSet.");
        }

        public int GetValues(object[] objects)
        {
            if (objects == null)
                throw new ArgumentNullException("objects");
            if (objects.Length == 0)
                throw new ArgumentException("Index was outside the bounds of the array.");
            
            //for (int i = 0; i < objects.Length; i++)
            //{
            //    if (objects[i] == null)
            //        throw new ArgumentNullException("objects");
            //    else if (objects[i].Equals(""))
            //        throw new ArgumentException("object contain empty string");
            //}
            //if (objects.Length > this.FieldCount)
            //    throw new ArgumentException("Objects length is outside the bounds of array");
            if (objects.Length < this.FieldCount)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    objects[i] = this.GetSingleValue(i);
                }
                return objects.Length;
            }
            else
            {
                for (int i = 0; i < this.FieldCount; i++)
                {
                    objects[i] = this.GetSingleValue(i);
                }
                return this.FieldCount;
            }
           
        }

        private object GetSingleValue(int index)
        {
            if (_currentRow == null)               
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                object obj = _currentRow.GetColumnValue(index);
                if (!_currentRow.Columns[index].IsHidden)
                {
                    if (obj is Alachisoft.NCache.Common.Queries.AverageResult)
                        return ((Alachisoft.NCache.Common.Queries.AverageResult)obj).Average;
                    else
                        return obj;
                }
                return null;

            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
          
        }

        public string GetName(int columnIndex)
        {

            if (_columns == null)
                return null;
            if (columnIndex >= 0 && columnIndex < FieldCount)
            {
                if (_columns[columnIndex].IsHidden)
                    throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");

                return _columns.GetColumnName(columnIndex);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");

        }

        public int GetOrdinal(string columnName)
        {
            if (_columns == null)
                return -1;
            if (_columns.Contains(columnName))
            {
                if (_columns[columnName].IsHidden)
                    throw new ArgumentException("Invalid columnName. Specified column does not exist in RecordSet.");
                return _columns.GetColumnIndex(columnName);
            }
            else
                throw new ArgumentException("Invalid columnName. Specified column does not exist in RecordSet.");

        }


        public bool IsClosed
        {
            get { return _recordSetEnumerator == null; }
        }

        public DateTime GetDateTime(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                object obj = _currentRow.GetColumnValue(index);
                return Convert.ToDateTime(obj);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
            
        }

    }
}
