//  Copyright (c) 2018 Alachisoft
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
using System.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    class PORDistributionManager : DistributionManager
    {
        private Hashtable _partitionNodesInfo = new Hashtable();
        private Hashtable _subGroupMap = new Hashtable();

        public PORDistributionManager(int autoBalancingThreshold, long cacheSizePerNode) : base(autoBalancingThreshold, cacheSizePerNode) { }

        public Hashtable PartNodesInfo
        {
            get { return _partitionNodesInfo; }
            set { _partitionNodesInfo = value; }
        }

        // Lets check if the node to be removed is co-orinator or not
        private bool IsCoordinatorNodeInPartition(PartNodeInfo nInfo)
        {
            ArrayList groupNodes = (ArrayList)_partitionNodesInfo[nInfo.SubGroup];
            PartNodeInfo temp = (PartNodeInfo)groupNodes[0];
            if (temp.NodeAddress.Equals(nInfo.NodeAddress))
            {
                nInfo.IsCoordinator = true;
                return true;
            }
            return false;
        }

        private bool IsLastNodeInPartition(PartNodeInfo nInfo)
        {
            ArrayList groupNodes = (ArrayList)_partitionNodesInfo[nInfo.SubGroup];
            if (groupNodes.Count == 1)
                return true;
            return false;
        }

        /// <summary>
        ///Called when within POR, coordinator replica leaves and back upreplica comes forth. No original buckets transfer just ownership change ocurs.
        /// </summary>
        /// <param name="oldCoordinator"></param>
        /// <param name="newCoordinator"></param>
        /// <returns></returns>
        protected ArrayList UpgradeToCoordinatorOfReplica(Address oldCoordinator, Address newCoordinator)
        {
            HashMapBucket tempBuck;
            ArrayList tmpMap = _lastCreatedHashMap == null ? new ArrayList() : _lastCreatedHashMap.Clone() as ArrayList;

            for (int i = 0; i < tmpMap.Count; i++)
            {
                tempBuck = (HashMapBucket)tmpMap[i];
                if (tempBuck.PermanentAddress.Equals(oldCoordinator))
                {
                    //bucket was completely owned by the old coordinator.
                    //we dont need to changed the temporary ownership so if some other 
                    //node is doing state transfer from the old coordinator, it will resume it
                    //with the new coordinator.

                    tempBuck.PermanentAddress = newCoordinator;
                }
                else if (oldCoordinator.Equals(tempBuck.TempAddress))
                {
                    //bucket was assigned to old coordinator but could not transfer it and left.
                    tempBuck.TempAddress = newCoordinator;
                    //new coordinator will resume the sate transfer with its primary owner.
                    tempBuck.Status = BucketStatus.NeedTransfer;
                }
            }

            return tmpMap;
        }
        public override Hashtable GetBucketsOwnershipMap(ArrayList hashMap)
        {
            Hashtable bucketsOwnerShipMap = new Hashtable();
            try
            {
                Sync.AcquireReaderLock(Timeout.Infinite);
                Hashtable coordinatorNodesOwnershipMap = base.GetBucketsOwnershipMap(hashMap);
                Hashtable currentOwnershipMap = _bucketsOwnershipMap != null ? _bucketsOwnershipMap.Clone() as Hashtable : null;
                ArrayList replicas = null;
                Address partitionCoordinator = null;

                if (coordinatorNodesOwnershipMap != null)
                {
                    IDictionaryEnumerator ide = coordinatorNodesOwnershipMap.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        partitionCoordinator = ide.Key as Address;
                        ArrayList coordinatorBuckets = ide.Value as ArrayList;

                        string subgroup = _subGroupMap[partitionCoordinator] as string;
                        if (subgroup != null)
                        {
                            replicas = _partitionNodesInfo[subgroup] as ArrayList;

                            if (replicas != null)
                            {
                                foreach (PartNodeInfo node in replicas)
                                {
                                    if (node.IsCoordinator)
                                    {
                                        bucketsOwnerShipMap.Add(node.NodeAddress, coordinatorBuckets);
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PoRDistMgr.GetBucketsOwnerShipMap", subgroup + ": " + node.NodeAddress.ToString() + " has got " + coordinatorBuckets.Count + " buckets");
                                    }
                                    else
                                    {
                                        ArrayList currentBuckets = currentOwnershipMap != null ? currentOwnershipMap[node.NodeAddress] as ArrayList : null;
                                        ArrayList updatedBucketsList = new ArrayList();
                                        if (currentBuckets != null)
                                        {
                                            //Node was already in the partitioned.
                                            foreach (HashMapBucket bucket in currentBuckets)
                                            {
                                                //if bucket is not transferred to the replica yet then we
                                                //change the temp address to make sure that if the coordinator
                                                //of the partitioned is changed, it is reflected in the map.
                                                if (coordinatorBuckets.Contains(bucket))
                                                {
                                                    if (bucket.TempAddress != null && !bucket.PermanentAddress.Equals(bucket.TempAddress))
                                                    {
                                                        bucket.PermanentAddress = partitionCoordinator;
                                                    }

                                                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PoRDistMgr.GetBucketsOwnerShipMap", bucket.ToString() + " after coordinator left");
                                                    updatedBucketsList.Add(bucket.Clone());
                                                }
                                            }
                                            //during loadbalancing; some new buckets may be assigned to a replica coordinator.
                                            foreach (HashMapBucket coodinatorBucket in coordinatorBuckets)
                                            {
                                                if (!currentBuckets.Contains(coodinatorBucket))
                                                {
                                                    HashMapBucket newNodeBucket = coodinatorBucket.Clone() as HashMapBucket;
                                                    newNodeBucket.PermanentAddress = partitionCoordinator;
                                                    newNodeBucket.TempAddress = node.NodeAddress;
                                                    //replica node need to state transfer from his coordinator.
                                                    newNodeBucket.Status = BucketStatus.NeedTransfer;
                                                    updatedBucketsList.Add(newNodeBucket);
                                                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PoRDistMgr.GetBucketsOwnerShipMap", newNodeBucket.ToString() + " new bucket assigned to replica");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //it is a new replica node,so we create a new bucket list for this node
                                            foreach (HashMapBucket coodinatorBucket in coordinatorBuckets)
                                            {
                                                HashMapBucket newNodeBucket = coodinatorBucket.Clone() as HashMapBucket;
                                                newNodeBucket.PermanentAddress = partitionCoordinator;
                                                newNodeBucket.TempAddress = node.NodeAddress;
                                                //replica node need to state transfer from his coordinator.
                                                newNodeBucket.Status = BucketStatus.NeedTransfer;
                                                updatedBucketsList.Add(newNodeBucket);
                                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PoRDistMgr.GetBucketsOwnerShipMap", newNodeBucket.ToString() + " fresh replica");

                                            }
                                        }
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PoRDistMgr.GetBucketsOwnerShipMap", subgroup + ": " + node.NodeAddress.ToString() + " has got " + updatedBucketsList.Count + " buckets");
                                        bucketsOwnerShipMap.Add(node.NodeAddress, updatedBucketsList);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                NCacheLog.Error("PoRDistMgr.GetBucketsOwnerShipMap", e.ToString());
            }
            finally
            {
                Sync.ReleaseReaderLock();
            }
            return bucketsOwnerShipMap;
        }
        public override DistributionMaps GetMaps(DistributionInfoData distInfoData)
        {
            ArrayList tmpMap = null;
            Hashtable bucketsOwnershipMap = null;
            ArrayList partitionNodes = new ArrayList();

            if (_installedHashMap == null)
            {
                tmpMap = new ArrayList(TotalBuckets);
                for (int i = 0; i < TotalBuckets; i++)
                {
                    HashMapBucket bucket = new HashMapBucket(distInfoData.AffectedNode.NodeAddress, i, BucketStatus.Functional);
                    tmpMap.Add(bucket);
                }

                _existingMembers.Add(distInfoData.AffectedNode.NodeAddress);
                _subGroupMap[distInfoData.AffectedNode.NodeAddress] = distInfoData.AffectedNode.SubGroup;

                //for each new group we are keeping list of members. For only Partition it will be one ..for POR can be greater then one.
                //This is new member, the first one. So create the list here.
                distInfoData.AffectedNode.IsCoordinator = true;
                partitionNodes.Add(distInfoData.AffectedNode);
                _partitionNodesInfo.Add(distInfoData.AffectedNode.SubGroup, partitionNodes); //A hash table keeping list of addresses against each GROUP/Partition.

                _lastCreatedHashMap = tmpMap.Clone() as ArrayList;

                bucketsOwnershipMap = GetBucketsOwnershipMap(_lastCreatedHashMap);
                return new DistributionMaps(_lastCreatedHashMap, bucketsOwnershipMap);
            }
                //for non-coordinator node that recently becomes coordinator...
            else if (_lastCreatedHashMap == null)
            {
                _lastCreatedHashMap = _installedHashMap.Clone() as ArrayList;
            }

            switch (distInfoData.ClustActivity)
            {
                case ClusterActivity.NodeJoin:
                    try
                    {
                        //assuming existing members doesnot contain the newly added member.
                        if (!_partitionNodesInfo.ContainsKey(distInfoData.AffectedNode.SubGroup))
                        {
                            partitionNodes = new ArrayList();
                            distInfoData.AffectedNode.IsCoordinator = true;
                            partitionNodes.Add(distInfoData.AffectedNode);
                            _subGroupMap[distInfoData.AffectedNode.NodeAddress] = distInfoData.AffectedNode.SubGroup;
                            _partitionNodesInfo.Add(distInfoData.AffectedNode.SubGroup, partitionNodes); //A hash table keeping list of addresses against each GROUP/Partition.
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.GetMaps()", "Sending new map as a new node joined the cluster");
                            return GetMapsOnNodeJoining(distInfoData);
                        }
                        else
                        {
                            partitionNodes = (ArrayList)_partitionNodesInfo[distInfoData.AffectedNode.SubGroup];
                            partitionNodes.Add(distInfoData.AffectedNode);
                            _subGroupMap[distInfoData.AffectedNode.NodeAddress] = distInfoData.AffectedNode.SubGroup;
                            return new DistributionMaps(_lastCreatedHashMap, GetBucketsOwnershipMap(_lastCreatedHashMap));
                        }
                    }
                    catch (Exception e)
                    {
                        if (NCacheLog.IsErrorEnabled) NCacheLog.Error("DistributionMgr.GetMaps()", e.ToString());
                        break;
                    }

                case ClusterActivity.NodeLeave:
                    //assuming existing members do not containt the node to be removed/left.                    
                    _existingMembers.Remove(distInfoData.AffectedNode.NodeAddress);
                    _subGroupMap.Remove(distInfoData.AffectedNode.NodeAddress);

                    //Check if this node is the only one in partition or not.So better do distribution
                    if (IsLastNodeInPartition(distInfoData.AffectedNode))
                    {
                        _partitionNodesInfo.Remove(distInfoData.AffectedNode.SubGroup);
                        return GetMapsOnNodeLeaving(distInfoData);
                    }
                    else //this mean we still have nodes available for this partition.
                    {
                        ArrayList groupNodes = (ArrayList)_partitionNodesInfo[distInfoData.AffectedNode.SubGroup];
                        if (IsCoordinatorNodeInPartition(distInfoData.AffectedNode))
                        {
                            groupNodes.Remove((object)distInfoData.AffectedNode);
                            ((PartNodeInfo)groupNodes[0]).IsCoordinator = true;
                            _partitionNodesInfo[distInfoData.AffectedNode.SubGroup] = groupNodes;
                            _existingMembers.Add(((PartNodeInfo)groupNodes[0]).NodeAddress);
                            tmpMap = UpgradeToCoordinatorOfReplica(distInfoData.AffectedNode.NodeAddress, ((PartNodeInfo)groupNodes[0]).NodeAddress);

                            _lastCreatedHashMap = tmpMap.Clone() as ArrayList;

                            bucketsOwnershipMap = GetBucketsOwnershipMap(_lastCreatedHashMap);
                            return new DistributionMaps(_lastCreatedHashMap, bucketsOwnershipMap);
                        }
                        else
                        {
                            //simply remove the node and get a new bucket ownership map.
                            groupNodes.Remove(distInfoData.AffectedNode);
                            return new DistributionMaps(_lastCreatedHashMap, GetBucketsOwnershipMap(_lastCreatedHashMap));
                        }
                    }

                case ClusterActivity.None:
                    BalanceNodeMgr bnMgr = new BalanceNodeMgr(null);
                    DistributionMaps result = bnMgr.BalanceNodes(distInfoData, _lastCreatedHashMap, _bucketsStats, _existingMembers);
                    if (result.Hashmap != null)
                    {
                        _lastCreatedHashMap = result.Hashmap.Clone() as ArrayList;
                        result.BucketsOwnershipMap = GetBucketsOwnershipMap(_lastCreatedHashMap);
                    }
                    return result;

                default:
                    break;
            }
            return null;
        }

        internal override void OnMemberJoined(Address mbr, NodeIdentity identity)
        {
            ArrayList partitionNodes;
            PartNodeInfo partNodeInfo = new PartNodeInfo(mbr, identity.SubGroupName, false);
            //assuming existing members doesnot contain the newly added member.
            if (!_partitionNodesInfo.ContainsKey(partNodeInfo.SubGroup))
            {
                partitionNodes = new ArrayList();
                partNodeInfo.IsCoordinator = true;
                partitionNodes.Add(partNodeInfo);
                _subGroupMap[partNodeInfo.NodeAddress] = partNodeInfo.SubGroup;
                _partitionNodesInfo.Add(partNodeInfo.SubGroup, partitionNodes);
                base.OnMemberJoined(mbr, identity);
            }
            else
            {
                partitionNodes = (ArrayList)_partitionNodesInfo[partNodeInfo.SubGroup];
                if (!partitionNodes.Contains(partNodeInfo))
                {
                    partitionNodes.Add(partNodeInfo);
                    _subGroupMap[partNodeInfo.NodeAddress] = partNodeInfo.SubGroup;
                }
            }
        }
        internal override void OnMemberLeft(Address mbr, NodeIdentity identity)
        {
            PartNodeInfo partNodeInfo = new PartNodeInfo(mbr, identity.SubGroupName, false);
            _existingMembers.Remove(partNodeInfo.NodeAddress);
            _subGroupMap.Remove(partNodeInfo.NodeAddress);

            ArrayList groupNodes = (ArrayList)_partitionNodesInfo[partNodeInfo.SubGroup];
            PartNodeInfo coordinator;
            if (groupNodes != null)
            {
                if (groupNodes.Contains(partNodeInfo))
                {
                    groupNodes.Remove(partNodeInfo);
                    if (groupNodes.Count > 0)
                    {
                        coordinator = (PartNodeInfo)groupNodes[0];
                        coordinator.IsCoordinator = true;
                        if (!_existingMembers.Contains(coordinator.NodeAddress))
                            _existingMembers.Add(coordinator.NodeAddress);
                    }
                    else
                        _partitionNodesInfo.Remove(partNodeInfo.SubGroup);
                }
            }
        }
    }
}