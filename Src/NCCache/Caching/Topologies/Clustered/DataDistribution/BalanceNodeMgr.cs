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
using System.Collections;
using System.Text;

using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    class BalanceNodeMgr
    {
        NodeBalanceData _nodeBalData;
        private long _weightToMove = 0;
        BalanceDataForNode _primaryNode;
        ArrayList _hashMap;
        ClusterCacheBase _parent;

        public BalanceNodeMgr(ClusterCacheBase parent)
        {
            _parent = parent;
        }
        
        public DistributionMaps BalanceNodes(DistributionInfoData distInfo, ArrayList hashMap, Hashtable bucketStats, ArrayList members)
        {
            _hashMap = hashMap;
            _nodeBalData = new NodeBalanceData(hashMap, bucketStats, members);
            
            //Check if any other state transfer is not in progress...
            bool bAllFunctional = this.SanityCheckForAllFunctional(hashMap);            
            //Add some status saying that node balancing is not possible at the moment.
            if (!bAllFunctional)
            {
                DistributionMaps result = new DistributionMaps(BalancingResult.AlreadyInBalancing);
                return result;
            }

            //Check if really the node needs some balancing or not.
            bool bShouldBalance = this.SanityCheckForCandidateNode((Address)distInfo.AffectedNode.NodeAddress);

            if (!bShouldBalance)
            {
                DistributionMaps result = new DistributionMaps(BalancingResult.NotRequired);
                return result;
            }
                        
            ArrayList dataListForNodes = _nodeBalData.BalanceDataListForNodes;
            ArrayList candidates = FilterCandidateNodes();
            
            foreach (AddressWeightPair awPair in candidates)
            {
                BalanceDataForNode secNode = GetBalDataForNode(awPair.NodeAddress);
                BalanceTwoNodes(_primaryNode, secNode, awPair.WeightShare);
                ApplyChangesInHashMap(secNode);
            }
            ApplyChangesInHashMap(_primaryNode);
            return new DistributionMaps(_hashMap, null);           

        } //end func.
        
        //data to be moved from Primary node to the secondary node.
        //As a result priNode and secNode got updated WeightIdPairLists.
        private static void BalanceTwoNodes(BalanceDataForNode priNode, BalanceDataForNode secNode, long dataToMove)
        {
            int priBucketCount = priNode.ItemsCount;
            int secBucketCount = secNode.ItemsCount;
            ArrayList priWIPairList = priNode.WeightIdList;
            ArrayList secWIPairList = secNode.WeightIdList;
            int cushionFactor = 10; // 10% cushion for balancing...   +- 10%
            long swapWeightGain = 0; // weight gain for this swap 
            long cushionWeight = Convert.ToInt64(((double)(dataToMove * cushionFactor) / (double)100));
            bool bTargetAchieved = false; //loop-invariant, in case we need to exit the loop in middle.
            long movedSoFar = 0;
            ArrayList usedIndex = new ArrayList(); //this list would keep all those indicies related to Inner loop that are consumed/used in swap. 
            
            //Making pivot node to be the secondary one, the one that needs to gain weight.
            //swapping or try to swap each element of secNode to all elements of priNode.
            //primary is traversed in Descending order, and secondary is traversed in ascending order.
            for (int i = 0; i < secBucketCount && !bTargetAchieved; i++)
            {
                WeightIdPair secWIPair = (WeightIdPair)secWIPairList[i];
                
                for (int j = priBucketCount - 1; j >= 0; j--)
                {
                    WeightIdPair priWIPair = (WeightIdPair) priWIPairList[j];    
                    
                    //only move when there is a gain.
                    if (priWIPair.Weight > secWIPair.Weight && !usedIndex.Contains(j))
                    {
                        swapWeightGain = priWIPair.Weight - secWIPair.Weight;
                        movedSoFar+= swapWeightGain;
                        
                        if (movedSoFar <= dataToMove)
                        {
                            if (dataToMove - movedSoFar <= cushionWeight)
                            {
                                //swap the buckets and exit
                                secWIPairList[i] = priWIPair;
                                priWIPairList[j] = secWIPair;
                                bTargetAchieved = true;
                                break;
                            }
                            else
                            {
                                secWIPairList[i] = priWIPair;
                                priWIPairList[j] = secWIPair;
                                usedIndex.Add(j);
                                break; //i need to move fwd now
                            }
                        } //end if 
                        else
                        {
                            if (movedSoFar - dataToMove <= cushionWeight)
                            {                                
                                //swap the buckets an exit
                                secWIPairList[i] = priWIPair;
                                priWIPairList[j] = secWIPair;
                                bTargetAchieved = true;
                                break;                                
                            }
                            else
                            {
                                movedSoFar-=swapWeightGain;
                            }
                                
                        } //end else
                    }//end if for priWeight > seWeight
                }//end inner for loop 
            }//end outer for loop
            //re-assign the WeightIdPairList to respective BalanceDataForNode
            priNode.WeightIdList = priWIPairList;
            priNode.WeightIdList.Sort();
            
            secNode.WeightIdList = secWIPairList;
            secNode.WeightIdList.Sort();
        }

        private bool SanityCheckForAllFunctional(ArrayList hashMap)
        {
            bool bAllFunctional = true;
            foreach (HashMapBucket hmBuck in hashMap)
            {
                if (!hmBuck.PermanentAddress.Equals(hmBuck.TempAddress))
                {
                    bAllFunctional = false;
                    break;
                }
            }
            return bAllFunctional;
        }

        //Need to check if the source node really needs any balancing?. If the weight is more then the Avg weight/Node then its true else false.
        private bool SanityCheckForCandidateNode(Address sourceNode)
        {
            ArrayList dataListForNodes = _nodeBalData.BalanceDataListForNodes;
            
            foreach (BalanceDataForNode balData in dataListForNodes)
            {
                if (balData.NodeAddress.Equals(sourceNode))
                {
                    if (balData.PercentData > _nodeBalData.PercentWeightPerNode)
                    {
                        this._weightToMove = balData.TotalWeight - this._nodeBalData.WeightPerNode; //Weight to move is the one that is above the Avg. weight the node Should bear.
                        _primaryNode = balData;
                        return true;
                    }
                    else
                        return false;
                }
            } //end foreach loop
            return false; //nothing found.
        }

        //returns list of those nodes that need to be participated in sharing load from Source node.        
        private ArrayList FilterCandidateNodes()
        {
            ArrayList dataListForNodes = _nodeBalData.BalanceDataListForNodes;
            ArrayList filteredNodes = new ArrayList();

            int totalPercentMissing = 0;
            int percentMissing = 0;
            int percentShareToGain = 0;


            //total percentage that the candidate nodes are missing...
            foreach (BalanceDataForNode balData in dataListForNodes)
            {
                if (balData.TotalWeight < _nodeBalData.WeightPerNode)
                {
                    totalPercentMissing += _nodeBalData.PercentWeightPerNode - balData.PercentData;
                }
            }


            //Assigning each candidate node its share from the Source node.
            foreach (BalanceDataForNode balData in dataListForNodes)
            {
                if (balData.TotalWeight < _nodeBalData.WeightPerNode)
                {
                    long weightToGain = 0;

                    percentMissing = _nodeBalData.PercentWeightPerNode - balData.PercentData;

                    try
                    {
                        percentShareToGain = Convert.ToInt32((double)((double)percentMissing / (double)totalPercentMissing) * 100);
                        weightToGain = Convert.ToInt64((double)(percentShareToGain * this._weightToMove) / (double)100);

                    }
                    catch (Exception) { }

                    AddressWeightPair awPair = new AddressWeightPair(balData.NodeAddress, weightToGain);
                    filteredNodes.Add(awPair);
                }
            }

            return filteredNodes;
        }

        //Returns BalancDataForNode instance for demanded node. from the list
        private BalanceDataForNode GetBalDataForNode(Address addr)
        {
            ArrayList dataListForNodes = _nodeBalData.BalanceDataListForNodes;            
            foreach (BalanceDataForNode balData in dataListForNodes)
            {
                if (balData.NodeAddress.Equals(addr))
                    return balData;                
            }
            return null;
        }

        private void ApplyChangesInHashMap(BalanceDataForNode secNode)
        {
            ArrayList weightIdPair = secNode.WeightIdList;
            Address newAddr = secNode.NodeAddress;
            HashMapBucket bucket = null;
            foreach (WeightIdPair widPair in weightIdPair)
            {
                //((HashMapBucket)_hashMap[widPair.BucketId]).PermanentAddress = newAddr;
                bucket = (HashMapBucket)_hashMap[widPair.BucketId];
                if (!newAddr.Equals(bucket.TempAddress))
                    bucket.Status = BucketStatus.NeedTransfer;
                bucket.TempAddress = newAddr;
            }
        }
        
        
        /// <summary>
        /// 
        /// </summary>
        internal class AddressWeightPair
        {
            Address _nodeAddr;
            long _weightShare = 0;

            public AddressWeightPair(Address address, long weightShare)
            {
                _nodeAddr = address;
                _weightShare = weightShare;
            }

            public Address NodeAddress
            {
                get { return _nodeAddr; }
            }

            public long WeightShare
            {
                get { return _weightShare; }
                set { _weightShare = value; }
            }
        }

    }

    //This class keeps information related to each individual node. 
}
