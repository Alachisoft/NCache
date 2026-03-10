//  Copyright (c) 2026 Alachisoft
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
using System;
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class DistributeHashMap
    {
        public static ArrayList DistributeOrphanBuckets(ArrayList hashMap, Address leavingNode, ArrayList members, out Hashtable orphanedBuckets)
        {
            orphanedBuckets = new Hashtable();
            HashMapBucket tempBuck;
            Dictionary<Address, int> bucketsInEachNode = NodeBucketsCount(hashMap, members); //node vs bucket count.
            bool bAssigned = false;

            int memberCount = members.Count;
            if (memberCount == 0)
                return null;

            int bucketsPerNode = hashMap.Count / members.Count;

            for (int i = 0, j = 0; i < hashMap.Count; i++)
            {
                j = (j == memberCount) ? 0 : j;
                tempBuck = (HashMapBucket)hashMap[i];

                if (tempBuck.PermanentAddress.CompareTo(leavingNode) == 0 || tempBuck.TempAddress.CompareTo(leavingNode) == 0)
                {
                    bAssigned = false;
                    bool usePrefferedServer = true;
                    for (int k = 0; k < memberCount; k++)
                    {
                        //In case bucket was assigned to leaving node, but not transferred to leaving node.
                        //So we give priority to the node whree this bucket resides provided it can accommodate this bucket.
                        Address prefferedServer = null;
                        if (usePrefferedServer && bucketsInEachNode.ContainsKey(tempBuck.PermanentAddress))
                        {
                            if (bucketsInEachNode[tempBuck.PermanentAddress] < bucketsPerNode)
                                prefferedServer = tempBuck.PermanentAddress;
                        }

                        if (prefferedServer == null && bucketsInEachNode[(Address)members[j]] < bucketsPerNode)
                            prefferedServer = (Address)members[j];

                        if (prefferedServer != null)
                        {
                            HashMapBucket clonedBucket = tempBuck.Clone() as HashMapBucket;
                            orphanedBuckets.Add(clonedBucket.BucketId, clonedBucket);

                            bucketsInEachNode[prefferedServer] = bucketsInEachNode[prefferedServer] + 1; //increment bucket count as next j is incremented.
                            
                            
                            // if bucket is functional
                            if (tempBuck.Status == BucketStatus.Functional)
                            {
                                tempBuck.PermanentAddress = prefferedServer;
                                tempBuck.TempAddress = prefferedServer;
                            }
                            // if buckets is not functional 
                            else
                            {
                                tempBuck.TempAddress = prefferedServer;
                                // when permanent node leave before state transfer, parmanent equal to temp address and bucket marked functional,.
                                if (tempBuck.PermanentAddress.CompareTo(leavingNode) == 0)
                                {
                                    tempBuck.PermanentAddress = prefferedServer;
                                    tempBuck.Status = BucketStatus.Functional;
                                }
                                //assign to same node 
                                else if (tempBuck.PermanentAddress.CompareTo(tempBuck.TempAddress) == 0)
                                    tempBuck.Status = BucketStatus.Functional;
                            }
                            
                            j++;
                            bAssigned = true;
                            break;
                        }
                        else
                        {
                            usePrefferedServer = false;
                            j++;
                            j = (j == memberCount) ? 0 : j;
                        }

                    }
                    //exceptional case when last node gets few more buckets. Assign those leftover buckets to ANY node.
                    if (bAssigned == false)
                    {
                        Address mbr = (Address)members[j++]; ;
                        tempBuck.PermanentAddress = tempBuck.TempAddress = mbr;
                        tempBuck.Status = BucketStatus.Functional;
                    }

                }
                
            }

            return hashMap;
        }

        //While a node leaves, all those buckets that were in transfer of buckets to the leaving node, should sieze transfer and would 
        // clear up tempAdd to be the same as the perm. addr.
        private static ArrayList ReAllocBucketsInTransfer(ArrayList hashMap, Address leavingNode)
        {
            for (int i = 0; i < hashMap.Count; i++)
            {
                HashMapBucket bucket = (HashMapBucket)hashMap[i];
                if (bucket.TempAddress.CompareTo(leavingNode) == 0)
                {
                    bucket.TempAddress = ((HashMapBucket)hashMap[i]).PermanentAddress;
                    bucket.Status = BucketStatus.Functional;
                }
            }
            return hashMap;
        }

        //Returns int array of bucket-count against each member. This is to deal with cases when node leaves while transfer.
        //resulting very un-even bucket distribution over the cluster.
        private static Dictionary<Address, int> NodeBucketsCount(ArrayList hashMap, ArrayList members)
        {
            Dictionary<Address, int> bucketsCount = new Dictionary<Address, int>();

            //Bad code... need to re-visit later on
            for (int i = 0; i < members.Count; i++)
            {
                Address addr = (Address)members[i];
                int buckCount = 0;
                for (int j = 0; j < hashMap.Count; j++)
                {
                    if (((HashMapBucket)hashMap[j]).TempAddress.CompareTo(addr) == 0)
                        buckCount++;
                }
                bucketsCount[addr] = buckCount;
            }
            return bucketsCount;
        }

        public static ArrayList BalanceBuckets(DistributionInfoData distInfo, ArrayList hashMap, Hashtable bucketStats, ArrayList members, long cacheSizePerNode, int totalBuckets, ILogger NCacheLog)
        {
            DistributionData distData = new DistributionData(hashMap, bucketStats, members, NCacheLog, cacheSizePerNode, totalBuckets);
            Boolean bShouldBalanceWeight = false;

            if (distInfo.DistribMode == DistributionMode.AvgWeightTime) //If weight and time to move has to be avg. Cut the weight to half.
            {
                if (NCacheLog != null && NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionImpl.BalanceBuckets()", "Request comes with DistributionMode.AvgWeightTime");
                distData.WeightPerNode /= 2;
            }

            ArrayList distMatrix = distData.DistributionMatrixForNodes;
            ArrayList finalBuckets = new ArrayList();

            //We need to cater for the cases where we dont need to actually balance the data over nodes, as cluster itself is starting
            //and no actual load is present within a cluster and on each node.

            foreach (DistributionMatrix dMatrix in distMatrix)
            {
                if (dMatrix.DoWeightBalance == true)
                {
                    bShouldBalanceWeight = true;
                    break;
                }
            }

            //If cluster is not loaded only shuffled disribution is required. No need to balance any weight.
            if (bShouldBalanceWeight == false)
            {
                if (NCacheLog != null && NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionImpl.BalanceBuckets()", "Cluster is not loaded only shuffled disribution is required. No need to balance any weight.");
                distInfo.DistribMode = DistributionMode.ShuffleBuckets;
            }

            //For cases below we also need to calculate Weight to be balanced along with buckets sacrifices.
            switch (distInfo.DistribMode)
            {
                case DistributionMode.OptimalTime:
                    foreach (DistributionMatrix dMatrix in distMatrix)
                    {
                        int[,] IdMatrix = dMatrix.IdMatrix;
                        for (int i = 0; i < dMatrix.MatrixDimension.Cols; i++)
                            finalBuckets.Add(IdMatrix[0, i]); //Always first row of the matrix to be given                         
                    }
                    if (NCacheLog != null && NCacheLog.IsInfoEnabled)
                    {
                        NCacheLog.Info("DistributionImpl.BalanceBuckets()", "Request is DistributionMode.OptimalTime");
                        NCacheLog.Info("Selected Buckets are: -");
                        for (int i = 0; i < finalBuckets.Count; i++)
                            NCacheLog.Info(finalBuckets[i].ToString());
                    }
                    return finalBuckets;

                case DistributionMode.ShuffleBuckets: //Although code replication is observed here. Still I prefer to make its copy rather puting fewer if-else to control. I need some time efficiency here.
                    foreach (DistributionMatrix dMatrix in distMatrix)
                    {
                        int[,] IdMatrix = dMatrix.IdMatrix;
                        int[] resultIndices;
                        RowsBalanceResult rbResult = DistributionCore.ShuffleSelect(dMatrix);
                        resultIndices = rbResult.ResultIndicies;

                        for (int i = 0, j = 0; i < resultIndices.Length; i++)
                        {
                            int index = resultIndices[i]; //Index would never be zero, rather the value corresponding in the Matrix be zero.

                            //Get row and col on the basis of matrix index (index of one-D array).                                
                            int row = index / dMatrix.MatrixDimension.Cols;
                            int col = index % dMatrix.MatrixDimension.Cols;

                            if (IdMatrix[row, col] == -1) //dealing with exceptional case when last row is selected and it got few non-indices.So replace those with lowest most indices in the matrix.
                            {
                                finalBuckets.Add(IdMatrix[0, j]);
                                j++;
                            }
                            else
                            {
                                finalBuckets.Add(IdMatrix[row, col]);
                            }
                        }
                    }
                    if (NCacheLog != null && NCacheLog.IsInfoEnabled)
                    {
                        NCacheLog.Info("DistributionImpl.BalanceBuckets()", "Request is DistributionMode.ShuffleBuckets");
                        NCacheLog.Info("Selected Buckets are: -");
                        for (int i = 0; i < finalBuckets.Count; i++)
                            NCacheLog.Info(finalBuckets[i].ToString());
                    }
                    return finalBuckets;

                case DistributionMode.OptimalWeight: //For both same code works. Change is only in weight that is modified above . it is called FallThrough in switch statements.
                case DistributionMode.AvgWeightTime:

                    foreach (DistributionMatrix dMatrix in distMatrix)
                    {
                        int[,] IdMatrix = dMatrix.IdMatrix;
                        int[] resultIndices;
                        RowsBalanceResult rbResult = DistributionCore.CompareAndSelect(dMatrix);
                        resultIndices = rbResult.ResultIndicies;

                        for (int i = 0, j = 0; i < resultIndices.Length; i++)
                        {
                            int index = resultIndices[i]; //Index would never be zero, rather the value corresponding in the Matrix be zero.

                            //Get row and col on the basis of matrix index (index of one-D array).                                
                            int row = index / dMatrix.MatrixDimension.Cols;
                            int col = index % dMatrix.MatrixDimension.Cols;

                            if (IdMatrix[row, col] == -1) //dealing with exceptional case when last row is selected and it got few non-indices.So replace those with lowest most indices in the matrix.
                            {
                                finalBuckets.Add(IdMatrix[0, j]);
                                j++;
                            }
                            else
                            {
                                finalBuckets.Add(IdMatrix[row, col]);
                            }
                        }
                    }
                    if (NCacheLog != null && NCacheLog.IsInfoEnabled)
                    {
                        NCacheLog.Info("DistributionImpl.BalanceBuckets()", "Request is DistributionMode.AvgWeightTime/ DistributionMode.OptimalWeight");
                        NCacheLog.Info("Selected Buckets are: -");
                        for (int i = 0; i < finalBuckets.Count; i++)
                            NCacheLog.Info(finalBuckets[i].ToString());
                    }
                    return finalBuckets;

                default:
                    break;
            } //end switch
            return null;

        } //end func.    
    }
}
