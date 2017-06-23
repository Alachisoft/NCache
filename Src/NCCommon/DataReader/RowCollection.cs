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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Common.DataReader
{
    public class RowCollection : ICompactSerializable
    {
        private ColumnCollection _columns;
        private IDictionary _rows;
        private int _lastRow = -1;

        public RowCollection(ColumnCollection columns)
        {
            _rows = new HashVector();
            _columns = columns;
        }

        public int Count
        { get { return _rows.Count; } }

        public RecordRow this[int index]
        { get { return (RecordRow)_rows[index]; } }

        public RecordRow GetRow(int index)
        {
            return _rows[index] != null ? (RecordRow)_rows[index] : null;
        }


        public void Add(RecordRow row)
        {
            _rows.Add(++_lastRow, row);
        }

        public bool Contains(int rowID)
        {
            return _rows[rowID] != null ? true : false;
        }

        public bool RemoveRow(int rowID)
        {
            bool ret;
            ret = _rows[rowID] != null ? true : false;
            if (ret)
                _rows.Remove(rowID);
            return ret;
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _columns = reader.ReadObject() as ColumnCollection;

            _rows = new HashVector();
            int rowCount = reader.ReadInt32();
            for (int i = 0; i < rowCount; i++)
            {
                _rows.Add(reader.ReadInt32(), reader.ReadObject() as RecordRow);
            }
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_columns);
            writer.Write((int)_rows.Count);
            foreach (DictionaryEntry kv in _rows)
            {
                writer.Write((int)kv.Key);
                writer.WriteObject((RecordRow)kv.Value);
            }
        }
    }

}
