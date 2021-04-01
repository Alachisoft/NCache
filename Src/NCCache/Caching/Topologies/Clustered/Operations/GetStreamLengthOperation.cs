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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations
{
    class GetStreamLengthOperation:ICompactSerializable
    {
        private string _key;
        private string _lockHandle;
        private OperationContext _operationContext;

        public GetStreamLengthOperation() { }

        public GetStreamLengthOperation(string key, string lockHandle, OperationContext operationContext)
        {
            _key = key;
            _lockHandle = lockHandle;
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
            _operationContext = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Key);
            writer.WriteObject(LockHandle);
            writer.WriteObject(_operationContext);
        }

        #endregion
    }
}
