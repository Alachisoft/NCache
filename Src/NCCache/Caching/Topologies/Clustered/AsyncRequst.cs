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
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    class AsyncRequst
    {
        private object _operation;
        private object _synKey;
        private Address _src;
        private long _reqId;

        public AsyncRequst(object operation, object syncKey)
        {
            _operation = operation;
            _synKey = syncKey;
        }

        /// <summary>
        /// Gets or sets the operation.
        /// </summary>
        public object Operation
        {
            get { return _operation; }
            set { _operation = value; }
        }

        /// <summary>
        /// Gets or sets the SyncKey.
        /// </summary>
        public object SyncKey
        {
            get { return _synKey; }
            set { _synKey = value; }
        }
        /// <summary>
        /// Gets or sets the soruce address of the request.
        /// </summary>
        public Address Src
        {
            get { return _src; }
            set { _src = value; }
        }

        /// <summary>
        /// Gets or sets the request id.
        /// </summary>
        public long RequsetId
        {
            get { return _reqId; }
            set { _reqId = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _operation = reader.ReadObject();
            _synKey = reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_operation);
            writer.WriteObject(_synKey);
        }

        #endregion
    }
}