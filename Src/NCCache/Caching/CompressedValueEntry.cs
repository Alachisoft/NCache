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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    public class CompressedValueEntry: ICompactSerializable
    {
        public object Value;
        public BitSet Flag;

        public CompressedValueEntry()
        {
        }

        public CompressedValueEntry(object value, BitSet flag)
        {
            this.Value = value;
            this.Flag = flag;
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            Value = reader.ReadObject();
            Flag = reader.ReadObject() as BitSet;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Value);
            writer.WriteObject(Flag);
        }

        #endregion
    }
}
