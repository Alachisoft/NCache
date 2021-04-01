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
    /// <summary>
    /// A metadata class for every new stream opened.
    /// </summary>
    [Serializable]
    internal class StreamMetadata : Runtime.Serialization.ICompactSerializable
    {
        private Guid _id;
        private int _tBufferSequence;

        public Guid Id
        {
            get { return this._id; }
        }

        public int BuffSequenceNumber
        {
            get { return _tBufferSequence; }
            set { _tBufferSequence = value; }
        }

        public StreamMetadata()
        {
            this._id = Guid.NewGuid();
            this._tBufferSequence = 1;
        }

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _id = reader.ReadGuid();
            _tBufferSequence = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_id);
            writer.Write(_tBufferSequence);
        }

        #endregion
    }
}
