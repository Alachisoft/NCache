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

using System.Collections;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class NodeBalanceData
    {
        private ArrayList _hashMapData;
        private ArrayList _balanceDataListForNodes; //ArrayList containing BalanceDataForNode instances for each member(Address).
        private ArrayList _members;        
        private long _cacheDataSum;
        private long _weightPerNode;
        private int _percentWeightPerNode;
        
        public NodeBalanceData(ArrayList hashMap, Hashtable bucketStatistics, ArrayList members)
        {
            _percentWeightPerNode = 100/members.Count;
            _members = members;
            int memberCount = _members.Count;
            _hashMapData = new ArrayList(memberCount);
            _cacheDataSum = 1;

            ArrayList _weightIdList = new ArrayList();
            for (int i = 0; i < DistributionManager.TotalBuckets; i++)
            {
                HashMapBucket hmapBuck = (HashMapBucket)hashMap[i];
                BucketStatistics buckStats = (BucketStatistics)bucketStatistics[i];
                if (hmapBuck.Status != BucketStatus.UnderStateTxfr) //include only those buckets that are Functional
                {
                    WeightIdPair listItem = new WeightIdPair(hmapBuck.BucketId, buckStats.DataSize, hmapBuck.PermanentAddress);
                    _weightIdList.Add(listItem);
                }
                _cacheDataSum += buckStats.DataSize; //Lets get the TOTAL weight of the cluster.
            }
            
            _weightPerNode = _cacheDataSum / memberCount;
            
            _balanceDataListForNodes = new ArrayList(memberCount);
            foreach (Address mbr in _members)
            {
                BalanceDataForNode balanceData = new BalanceDataForNode(_weightIdList, mbr,_cacheDataSum);
                _balanceDataListForNodes.Add(balanceData);
            }
        }

        public ArrayList BalanceDataListForNodes
        {
            get { return _balanceDataListForNodes; }
        }

        public long CacheDataSum
        {
            get { return _cacheDataSum; }
        }
        
        public long WeightPerNode
        {
            get { return _weightPerNode; }
        }

        public int PercentWeightPerNode
        {
            get {return _percentWeightPerNode;}
        }

    }
}
