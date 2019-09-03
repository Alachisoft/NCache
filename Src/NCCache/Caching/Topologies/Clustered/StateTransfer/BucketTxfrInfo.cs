//  Copyright (c) 2019 Alachisoft
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
using System.Collections;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public struct BucketTxfrInfo
    {
        public ArrayList bucketIds;
        public bool isSparsed;
        public Address owner;
        public bool end;

        public BucketTxfrInfo(bool end)
        {
            bucketIds = null;
            owner = null;
            isSparsed = false;
            this.end = end;
        }

        public BucketTxfrInfo(ArrayList bucketIds, bool isSparsed, Address owner)
        {
            this.bucketIds = bucketIds;
            this.isSparsed = isSparsed;
            this.owner = owner;
            this.end = false;
        }
    }
}