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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class EnumerationPointer : ICompactSerializable
    {
        private string _id = Guid.NewGuid().ToString();
        private int _chunkId = -1;
        private bool _isDisposable = false;
        private Address _nodeIpAddress ;//= string.Empty;
        private bool _isSocketServerDispose = false;        

        public EnumerationPointer()
        {
        }

        public EnumerationPointer(string id, int chunkId)
        {
            _id = id;
            _chunkId = chunkId;
        }

        public virtual bool IsGroupPointer
        {
            get { return false; }
        }

        public bool HasFinished
        {
            get { return _chunkId == -1; }
        }

        public Address NodeIpAddress
        {
            get { return _nodeIpAddress; }
            set { _nodeIpAddress = value; }
        }

        public string Id
        {
            get { return _id; }
        }

        public int ChunkId
        {
            get { return _chunkId; }
            set { _chunkId = value; }
        }

        public bool isDisposable
        {
            get { return _isDisposable; }
            set { _isDisposable = value; }
        }

        public bool IsSocketServerDispose
        {
            get { return _isSocketServerDispose; }
            set { _isSocketServerDispose = value; }
        }

        public void Reset()
        {
            _chunkId = -1;
        }

        public override bool Equals(object obj)
        {
            bool equals = false;

            if (obj is EnumerationPointer)
            {
                EnumerationPointer other = obj as EnumerationPointer;
                equals = _id.Equals(other._id);
            }

            return equals;
        }

        public override int GetHashCode()
        {
            if (_id != null)
                return _id.GetHashCode();
            else
                return base.GetHashCode();
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _id = reader.ReadString();
            _chunkId = reader.ReadInt32();
            _isDisposable = reader.ReadBoolean();
            _nodeIpAddress = reader.ReadObject() as Address;
            _isSocketServerDispose = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_id);
            writer.Write(_chunkId);
            writer.Write(_isDisposable);
            writer.WriteObject(_nodeIpAddress);
            writer.Write(_isSocketServerDispose);
        }

        #endregion
    }
}
