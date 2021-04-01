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


namespace Alachisoft.NCache.Caching.Topologies.Clustered.Results
{
    class GetStreamLengthResult : ClusterOperationResult , ICompactSerializable
    {
        private long _length;

        public GetStreamLengthResult() { }

        public GetStreamLengthResult(ClusterOperationResult.Result result, long length)
            : base(result)
        {
            _length = length;
        }

        public long Length
        {
            get { return _length; }
            set { _length = value; }
        }
        
        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _length = reader.ReadInt64();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_length);
        }

        #endregion
    }
}
