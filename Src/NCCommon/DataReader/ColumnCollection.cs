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
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections;

namespace Alachisoft.NCache.Common.DataReader
{
    public class ColumnCollection : ICompactSerializable
    {
        IDictionary _columns = new HashVector();
        IDictionary _nameToIndex = new HashVector();

        public int Count
        { get { return _columns.Count; } }

        public int HiddenColumnCount
        {
            get
            {
                int hiddenColumns = 0;
                foreach (DictionaryEntry columnsEntry in _columns)
                {
                    if (((RecordColumn)columnsEntry.Value).IsHidden)
                        hiddenColumns++;
                }
                return hiddenColumns;
            }
        }

        public void Add(RecordColumn column)
        {
            if (_nameToIndex[column.ColumnName] != null)
                throw new Exception("Same Column cannot be added twice.");
            _nameToIndex.Add(column.ColumnName, _columns.Count);
            _columns.Add(_columns.Count, column);
        }

        public RecordColumn Get(string columnName)
        {
            return (RecordColumn)_columns[_nameToIndex[columnName]];
        }

        public RecordColumn Get(int index)
        {
            return (RecordColumn)_columns[index];
        }

        public int GetColumnIndex(string columnName)
        {
            return (int)_nameToIndex[columnName];
        }

        public string GetColumnName(int index)
        {
            return ((RecordColumn)_columns[index]).ColumnName;
        }

        public RecordColumn this[string columnName]
        { get { return (RecordColumn)_columns[_nameToIndex[columnName]]; } }

        public RecordColumn this[int index]
        { get { return (RecordColumn)_columns[index]; } }

        public bool Contains(string columnName)
        {
            return _nameToIndex[columnName] != null;
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _columns = new HashVector();
            int columnCount = reader.ReadInt32();
            for (int i = 0; i < columnCount; i++)
            {
                _columns.Add(reader.ReadInt32(), reader.ReadObject() as RecordColumn);
            }

            _nameToIndex = new HashVector();
            int indexCount = reader.ReadInt32();
            for (int i = 0; i < indexCount; i++)
            {
                _nameToIndex.Add(reader.ReadObject() as string, reader.ReadInt32());
            }
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_columns.Count);
            foreach (DictionaryEntry kv in _columns)
            {
                writer.Write((int)kv.Key);
                writer.WriteObject((RecordColumn)kv.Value);
            }

            writer.Write(_nameToIndex.Count);
            foreach (DictionaryEntry kv in _nameToIndex)
            {
                writer.WriteObject((string)kv.Key);
                writer.Write((int)kv.Value);
            }
        }
    }

}
