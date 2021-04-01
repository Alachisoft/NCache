//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using System;

namespace Alachisoft.NCache.Client
{
    [Serializable]
    internal class TransientBuffer : Runtime.Serialization.ICompactSerializable
    {
        private byte[] _tbuffer;
        private int _size;
        private int _sequence;

        internal TransientBuffer(byte[] buffer, int size, int sequence)
        {
            this._tbuffer = buffer;
            this._size = size;
            this._sequence = sequence;
        }

        public int Size
        {
            get { return _size; }
        }
        public int Sequence
        {
            get { return _sequence; }
        }
        public byte[] TBuffer
        {
            get { return _tbuffer; }
        }

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _size = reader.ReadInt32();
            _tbuffer = reader.ReadBytes(_size);
            _sequence = reader.ReadInt32();

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_size);
            writer.Write(_tbuffer);
            writer.Write(_sequence);

        } 
        #endregion
    }
}
