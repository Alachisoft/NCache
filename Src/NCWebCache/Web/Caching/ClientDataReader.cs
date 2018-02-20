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
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// To used for reader on client side and read query results.
    /// </summary>
    internal class ClientDataReader : ICacheReader
    {
        private IClientRecordSetEnumerator _clientRecordSetEnumerator;
        ClusteredArray<object> _currentRow;
        const string keyColumnName = "$KEY$";
        const string valueColumnName = "$VALUE$";

        internal ClientDataReader(IClientRecordSetEnumerator clientRecordSetEnumerator)
        {
            _clientRecordSetEnumerator = clientRecordSetEnumerator;
        }

        public void Close()
        {
            if (_clientRecordSetEnumerator != null)
                _clientRecordSetEnumerator.Dispose();
        }


        [System.Runtime.CompilerServices.IndexerName("Item")]
        public object this[int index]
        {
            get
            {
                object obj = _currentRow[index];
                if (obj == null)
                    throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
                return obj;
            }
        }

        [System.Runtime.CompilerServices.IndexerName("Item")]
        public object this[string columnName]
        {
            get
            {
                int index = columnName.Equals(keyColumnName, StringComparison.InvariantCultureIgnoreCase) ? 0 : 1;
                object obj = _currentRow[index];
                if (obj == null)
                    throw new ArgumentException("Invalid columnName. Specified column does not exist in RecordSet.");
                return obj;
            }
        }

        public bool Read()
        {
            if (_clientRecordSetEnumerator == null) return false;
            bool next = _clientRecordSetEnumerator.MoveNext();
            if (next)
                _currentRow = _clientRecordSetEnumerator.Current;
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
                return _currentRow[index];
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
        }

        private object GetValue(string columnName)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            int index = GetOrdinal(columnName);
            return _currentRow[index];
        }

        public int GetValues(object[] objects)
        {
            if (objects == null)
                throw new ArgumentNullException("objects");
            if (objects.Length == 0)
                throw new ArgumentException("Index was outside the bounds of the array.");

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
                return _currentRow[index];
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
        }

        public string GetName(int columnIndex)
        {
            if (columnIndex >= 0 && columnIndex < FieldCount)
            {
                string columnName = String.Empty;
                switch (columnIndex)
                {
                    case 0:
                        columnName = keyColumnName;
                        break;
                    case 1:
                        columnName = valueColumnName;
                        break;
                }

                return columnName;
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
        }

        public int GetOrdinal(string columnName)
        {
            if (columnName.Equals(keyColumnName, StringComparison.InvariantCultureIgnoreCase))
                return 0;
            else if (columnName.Equals(valueColumnName, StringComparison.InvariantCultureIgnoreCase))
                return 1;
            else
                throw new ArgumentException("Invalid columnName. Specified column does not exist in RecordSet.");
        }

        public bool IsClosed
        {
            get { return _clientRecordSetEnumerator == null; }
        }

        public DateTime GetDateTime(int index)
        {
            if (_currentRow == null)
                throw new OperationFailedException("Operation is not valid due to the current state of the object");
            if (index >= 0 && index < FieldCount)
            {
                object obj = _currentRow[index];
                return Convert.ToDateTime(obj);
            }
            else
                throw new ArgumentException("Invalid index. Specified index does not exist in RecordSet.");
        }

        public int FieldCount
        {
            get { return _clientRecordSetEnumerator == null ? 0 : _clientRecordSetEnumerator.FieldCount; }
        }


        private void Dispose(bool disposing)
        {
            Close();
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            _currentRow = null;
        }


        public void Dispose()
        {
            Dispose(true);
        }

        ~ClientDataReader()
        {
            Dispose(false);
        }
    }
}