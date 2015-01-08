// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class DistributionData
    {
        private ArrayList _hashMapData;
        private ArrayList _distMatrixForNodes;
        private ArrayList _members;        
        private long _cacheDataSum;
        private int _bucketsPerNode;
        private long _weightPerNode;

        public DistributionData(ArrayList hashMap, Hashtable bucketStatistics, ArrayList members, ILogger NCacheLog, long cacheSizePerNode)
        {
            _members = members;
            int memberCount = _members.Count;
            _hashMapData = new ArrayList(memberCount);
            _cacheDataSum = 1;

            ArrayList _weightIdList = new ArrayList();
            for (int i = 0; i < DistributionManager.TotalBuckets; i++)
            {
                HashMapBucket hmapBuck = (HashMapBucket)hashMap[i];
                BucketStatistics buckStats = (BucketStatistics)bucketStatistics[i];
                
                //Catering for situations when two nodes are balancing and a new node joins in OR
                // two nodes joins one after the other, first one started state transfer while second jumped in.
                if (hmapBuck.Status != BucketStatus.UnderStateTxfr) //include only those buckets that are Functional/NeedStateTr
                {
                    //We are selecting buckets based on temp address; although it is possible that these buckets
                    //might have not been transfered to TEMP owner but algorithm consider these are owned by TEMP owner.
                    WeightIdPair listItem = new WeightIdPair(hmapBuck.BucketId, buckStats.DataSize, hmapBuck.TempAddress);
                    _weightIdList.Add(listItem);
                }

                _cacheDataSum += buckStats.DataSize; //Lets get the TOTAL weight of the cluster.
            }

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionData()", "cacheDataSum = " + _cacheDataSum.ToString());

            //Initialize the two very important data pieces. All distribution is based on this.
            _bucketsPerNode = DistributionManager.TotalBuckets / (memberCount + 1);
            _weightPerNode = _cacheDataSum / (memberCount + 1);
            
            //

            _distMatrixForNodes = new ArrayList(memberCount);
            long maxCacheSize = cacheSizePerNode * memberCount; //in bytes..CacheSize/node is the one user has entered while creating the cluster
            foreach (Address mbr in _members)
            {
                DistributionMatrix distMatrix = new DistributionMatrix(_weightIdList, mbr,this,NCacheLog);
                distMatrix.MaxCacheSize = maxCacheSize;
                _distMatrixForNodes.Add(distMatrix);
            }
        }

        public ArrayList DistributionMatrixForNodes
        {
            get { return _distMatrixForNodes; }
        }

        public long CacheDataSum
        {
            get {return _cacheDataSum;}
            set {_cacheDataSum = value;}
        }
        
        public int BucketsPerNode
        {
            get {return _bucketsPerNode;}
            set {_bucketsPerNode = value;}        
        }
        
        public long WeightPerNode
        {
            get { return _weightPerNode; }
            set { _weightPerNode = value; }
        }
    }
}
