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
    internal class ClusterOperationResult :ICompactSerializable
    {
        internal enum Result :byte
        {
            Completed,
            ParitalTimeout,
            FullTimeout
        }

        private Result _result;
        private string _lockId;

        public ClusterOperationResult() { }

        public ClusterOperationResult(Result executed)
        {
            _result = executed;
        }

        public Result ExecutionResult
        {
            get { return _result; }
            set { _result = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _result = (Result)reader.ReadByte();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write((byte)_result);
        }

        #endregion
    }
}
