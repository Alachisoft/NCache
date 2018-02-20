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
// limitations under the License

using Alachisoft.NCache.Serialization;

namespace Alachisoft.NCache.Web.NOutputCache
{
    internal class NItem : Runtime.Serialization.ICompactSerializable
    {
        private byte[] _stream;
        private long _hashCode;

        public NItem(long hashCode, byte[] stream)
        {
            this._stream = stream;
            this._hashCode = hashCode;
        }

        /// <summary>
        /// Get page hash code
        /// </summary>
        public long HashCode
        {
            get { return this._hashCode; }
        }

        /// <summary>
        /// Get the rendered page buffer
        /// </summary>
        public byte[] Buffer
        {
            get { return this._stream; }
        }

        /// <summary>
        /// Register type with compact framework, so the item can be compact serializable
        /// </summary>
        public static void RegisterTypeWithCompactFramework()
        {
            CompactFormatterServices.RegisterCompactType(typeof(NItem), 131);
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this._hashCode = reader.ReadInt64();
            this._stream = reader.ReadBytes(reader.ReadInt32());
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(this._hashCode);
            if (this._stream != null)
            {
                writer.Write(this._stream.Length);
                writer.Write(this._stream);
            }
            else
            {
                writer.Write(0);
            }
        }

        #endregion
    }
}