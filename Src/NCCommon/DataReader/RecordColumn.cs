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
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.DataReader
{
    public class RecordColumn : ICompactSerializable
    {
        private string _columnName;
        private ColumnType _columnType;
        private ColumnDataType _dataType;
        private AggregateFunctionType _aggregateFunctionType;
        private bool _isFilled;
        private bool _isHidden;

        /// <summary>
        /// Initializes a new RecordColumn with specified column name.
        /// </summary>
        /// <param name="name">Name of column</param>
        public RecordColumn(string name)
        {
            _columnName = name;
            _isFilled = true;
        }

        /// <summary>
        /// Gets or sets name of column.
        /// </summary>
        public string ColumnName
        {
            get { return _columnName; }
            set { _columnName = value; }
        }

        /// <summary>
        /// Gets or sets flag indicating whether or no current column is a hidden column.
        /// </summary>
        public bool IsHidden
        {
            get { return _isHidden; }
            set { _isHidden = value; }
        }

        /// <summary>
        /// Gets or sets <see cref="Alachisoft.NCache.Common.DataStructures.ColumnType"/> of column.
        /// </summary>
        public ColumnType ColumnType
        {
            get { return _columnType; }
            set { _columnType = value; }
        }

        /// <summary>
        /// Gets or sets <see cref="Alachisoft.NCache.Common.DataStructures.ColumnDataType"/> of column.
        /// </summary>
        public ColumnDataType DataType
        {
            get { return _dataType; }
            set { _dataType = value; }
        }

        /// <summary>
        /// Gets or sets <see cref="Alachisoft.NCache.Common.Enum.AggregateFunctionType"/> of column.
        /// </summary>
        public AggregateFunctionType AggregateFunctionType
        {
            get { return _aggregateFunctionType; }
            set { _aggregateFunctionType = value; }
        }

        /// <summary>
        /// Gets or sets flag indicating whether or not this column is filled with data.
        /// </summary>
        public bool IsFilled
        {
            get { return _isFilled; }
            set { _isFilled = value; }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _columnName = reader.ReadObject() as string;
            _columnType = (ColumnType)reader.ReadInt32();
            _dataType = (ColumnDataType)reader.ReadInt32();
            _aggregateFunctionType = (AggregateFunctionType)reader.ReadInt32();
            _isFilled = reader.ReadBoolean();
            _isHidden = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_columnName);
            writer.Write(Convert.ToInt32(_columnType));
            writer.Write(Convert.ToInt32(_dataType));
            writer.Write(Convert.ToInt32(_aggregateFunctionType));
            writer.Write(_isFilled);
            writer.Write(_isHidden);
        }
    }
}