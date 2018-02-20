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

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class RowsBalanceResult
    {
        int[] _resultIndicies; //set of buckets to be given away.
        long _distanceFromTarget; //to compare two sets. The set with least distance is the one to be selected.

        public RowsBalanceResult()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public int[] ResultIndicies
        {
            get { return _resultIndicies; }
            set { _resultIndicies = value; }
        }

        public long TargetDistance
        {
            get { return _distanceFromTarget; }
            set { _distanceFromTarget = value; }
        }
    }
}