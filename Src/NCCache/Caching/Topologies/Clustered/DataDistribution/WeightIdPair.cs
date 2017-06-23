// Copyright (c) 2017 Alachisoft
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

using System;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Topologies.Clustered

{    
    public class WeightIdPair : IComparable
    {
        private int _bucketId;
        private long _weight;
        private Address _address;

        public WeightIdPair(int buckId, long weight, Address address)
        {
            _bucketId = buckId;
            _weight = weight;
            _address = address;
        }

        public int BucketId
        {
            get { return _bucketId; }
        }
        public long Weight
        {
            get { return _weight; }
        }

        public Address Address
        {
            get { return _address; }

        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            WeightIdPair wiPair = (WeightIdPair)obj;
            return this._weight.CompareTo(wiPair.Weight);
        }

        #endregion
    }
}
