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

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    internal class ClusteredOperationResult
    {
        private object _result;
        private Address _sender;

        public ClusteredOperationResult() { }
        public ClusteredOperationResult(Address sender, object result)
        {
            _sender = sender;
            _result = result;
        }

        public object Result
        {
            get { return _result; }
            set { _result = value; }
        }

        public Address Sender
        {
            get { return _sender; }
            set { _sender = value; }
        }
    }
}