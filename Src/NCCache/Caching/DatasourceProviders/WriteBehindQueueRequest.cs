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
// limitations under the License.

using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;
namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    internal class WriteBehindQueueRequest:ICompactSerializable
    {
        string _nextChunkId;
        string _prevChunkId;

        public WriteBehindQueueRequest(string nextChunkId, string prevChunkId)
        {
            _nextChunkId = nextChunkId;
            _prevChunkId = prevChunkId;
        }

        public string NextChunkId
        {
            get { return _nextChunkId; }
            set { _nextChunkId = value; }
        }

        public string PrevChunkId
        {
            get { return _prevChunkId; }
            set { _prevChunkId = value; }
        }


        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _nextChunkId = reader.ReadObject() as string;
            _prevChunkId = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_nextChunkId);
            writer.WriteObject(_prevChunkId);
        }

        #endregion
    }
}
