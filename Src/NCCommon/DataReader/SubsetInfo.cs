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

namespace Alachisoft.NCache.Common.DataReader
{
    public class SubsetInfo : ICompactSerializable
    {
        private int _startIndex;
        private int _lastAccessedRowID;

        public int LastAccessedRowID
        {
            get { return _lastAccessedRowID; }
            set { _lastAccessedRowID = value; }
        }
        public int StartIndex
        {
            get { return _startIndex; }
            set { _startIndex = value; }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _startIndex = reader.ReadInt32();
            _lastAccessedRowID = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_startIndex);
            writer.Write(_lastAccessedRowID);
        }
    }

}
