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

using System;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Queries.Filters;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    class CompoundKeyFilter : IKeyFilter
    {
        HashSet<Int32> filteredBuckets = null;
        public CompoundKeyFilter()
        {
            filteredBuckets = new HashSet<Int32>();
        }

        public void FilterBucket(int bucketID)
        {
            if (!filteredBuckets.Contains(bucketID))
                filteredBuckets.Add(bucketID);
        }

        public void RemoveBucket(int bucketID)
        {   if(filteredBuckets!=null)         
                filteredBuckets.Remove(bucketID);
        }

        public void ClearBuckets() 
        {
            filteredBuckets.Clear();
        }

        public bool Evaluate(string key)
        {
            int bucketId = FilterHelper.GetBucketID(key);
            
            if (this.filteredBuckets.Contains(bucketId))
                return false;

            return true;
        }

        public bool IsEmpty 
        {
            get 
            {
                if (filteredBuckets == null || filteredBuckets.Count == 0) return true;

                return false;

            }
        }
    }
}