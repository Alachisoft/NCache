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

using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations
{
    public class WriteToStreamOperation : ICompactSerializable
    {
        string _key;
        string _lockHandle; 
        VirtualArray _vBuffer;
        int _srcOffset;
        int _dstOffset; 
        int _length;
        OperationContext _operationContext;

        public WriteToStreamOperation() { }

        public WriteToStreamOperation(string key, string lockHandle, VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {
            _key = key;
            _lockHandle = lockHandle;
            _vBuffer = vBuffer;
            _srcOffset = srcOffset;
            _dstOffset = dstOffset;
            _length = length;
            _operationContext = operationContext;
        }

        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public string LockHandle
        {
            get { return _lockHandle; }
            set { _lockHandle = value; }
        }

        public VirtualArray Buffer
        {
            get { return _vBuffer; }
            set { _vBuffer = value; }
        }

        public int SrcOffset
        {
            get { return _srcOffset; }
            set { _srcOffset = value; }
        }

        public int DstOffset
        {
            get { return _dstOffset; }
            set { _dstOffset = value; }
        }

        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }

        public OperationContext OperationContext
        {
            get { return _operationContext; }
            set { _operationContext = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _key = reader.ReadObject() as string;
            _lockHandle = reader.ReadObject() as string;
            _vBuffer = reader.ReadObject() as VirtualArray;
            _srcOffset = reader.ReadInt32();
            _dstOffset = reader.ReadInt32();
            _length = reader.ReadInt32();
            _operationContext = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Key);
            writer.WriteObject(LockHandle);
            writer.WriteObject(_vBuffer);
            writer.Write(_srcOffset);
            writer.Write(_dstOffset);
            writer.Write(_length);
            writer.WriteObject(_operationContext);
        }

        #endregion
    }
}
