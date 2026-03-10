using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Config.NewDom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Alachisoft.NCache.Management.Management.ModuleManagement
{
    public class MapMerger : IMapMerger
    {
        public bool IsMergeAllow(DistributionInfo alreadyExistedMap, ArrayList affectedNodes, string cacheName, string cacheTopology)
        {
            bool equalNumOfNodes = true;

            if (alreadyExistedMap == null || alreadyExistedMap.DistributionMaps == null)
                throw new Exception($"Could not find existing configuration '{cacheName}.modulemap'. Please make sure that you use the same name and configuration of cache that was used to generate these existing indexes.");

            if (!ValidateCacheTopology(cacheTopology, alreadyExistedMap.PartitionNodesInfo))
                throw new Exception($"The configuration of this cache '{cacheName}' does not match the configuration of cache using which existing index directories were created. Mismatch of caching topology.");

            var existingNodes = alreadyExistedMap.DistributionMaps.BucketsOwnershipMap.Keys.GetEnumerator();
            Dictionary<string, string> existingNodesIPs = new Dictionary<string, string>();

            while (existingNodes.MoveNext())
            {
                Address nodeAddress = (Address)existingNodes.Current;
                string ipAddress = nodeAddress.IpAddress.ToString();
                if (!existingNodesIPs.ContainsKey(ipAddress))  
                    existingNodesIPs.Add(ipAddress, ipAddress);
            }

            if (existingNodesIPs.Count != affectedNodes.Count)
            {
                equalNumOfNodes = false;
            }
            else
            {
                for (int i = 0; i < affectedNodes.Count; i++)
                {
                    if (!existingNodesIPs.ContainsKey((affectedNodes[i] as ServerNode).IP))
                    {
                        equalNumOfNodes = false;
                        break;
                    }
                }
            }

            if (!equalNumOfNodes)
                throw new Exception($"The configuration of this cache '{cacheName}' does not match the configuration of cache using which existing index directories were created. Mismatch of participating nodes.");

            return true;
        }

        private bool ValidateCacheTopology(string cacheTopology, Hashtable partitionNodesInfo)
        {
            switch (cacheTopology)
            {
                case "partitioned-replica":
                    if (partitionNodesInfo != null && partitionNodesInfo.Count > 0)
                        return true;
                    else
                        return false;
                case "partitioned":
                case "local":
                    if (partitionNodesInfo != null && partitionNodesInfo.Count == 0)
                        return true;
                    else
                        return false;
               
                default:
                    return true;
                    break;
            }
            
        }
        public DistributionInfo MapUpdate(DistributionInfo alreadyExistedMap, int clusterPort, string cacheToplogy)
        {
            // hashmap contains address with cluster port so we find out the cluster port for both maps.
            int previousClusterPort = ((HashMapBucket)alreadyExistedMap.DistributionMaps.Hashmap[0]).PermanentAddress.Port;

            DistributionInfo updatedDistributionInfo = new DistributionInfo();
            ArrayList hashmap = alreadyExistedMap.DistributionMaps.Hashmap;
            updatedDistributionInfo.DistributionMaps = new DistributionMaps(BucketPortUpdate(hashmap, clusterPort, previousClusterPort));

            Hashtable updatedOwnershipMap = new Hashtable();
            var en = alreadyExistedMap.DistributionMaps.BucketsOwnershipMap.GetEnumerator();
            while (en.MoveNext())
            {
                Address address = en.Key as Address;
                Address updatedOwnerAddress = UpdatePort(address, clusterPort, previousClusterPort);
                ArrayList ownersBucket = en.Value as ArrayList;
                updatedOwnershipMap.Add(updatedOwnerAddress, BucketPortUpdate(ownersBucket, clusterPort, previousClusterPort));
            }
            updatedDistributionInfo.DistributionMaps.BucketsOwnershipMap = updatedOwnershipMap;

            if (cacheToplogy.Equals("partitioned-replica"))
            {
                Hashtable partNodeInfos = alreadyExistedMap.PartitionNodesInfo;
                updatedDistributionInfo.PartitionNodesInfo = UpdatePartNodeInfo(ref partNodeInfos, clusterPort, previousClusterPort);
            }

            updatedDistributionInfo.DistributionMaps.BalancingResult = alreadyExistedMap.DistributionMaps.BalancingResult;
            updatedDistributionInfo.DistributionMaps.OrphanedBuckets = alreadyExistedMap.DistributionMaps.OrphanedBuckets;
            updatedDistributionInfo.DistributionMaps.SpecialBucketOwners = alreadyExistedMap.DistributionMaps.SpecialBucketOwners;
            updatedDistributionInfo.BucketsStats = alreadyExistedMap.BucketsStats;
            updatedDistributionInfo.CacheNodes = alreadyExistedMap.CacheNodes;
            updatedDistributionInfo.MapVersion = alreadyExistedMap.MapVersion;
            updatedDistributionInfo.TotalBuckets = alreadyExistedMap.TotalBuckets;
            updatedDistributionInfo.MapId = alreadyExistedMap.MapId;
            updatedDistributionInfo.CacheName = alreadyExistedMap.CacheName;

            return updatedDistributionInfo;
        }

        private Hashtable UpdatePartNodeInfo(ref Hashtable partNodeInfos, int clusterPort, int previousClusterPort)
        {
            Hashtable updatedPartitionNodeInfos = new Hashtable();
            foreach (DictionaryEntry groupNodes in partNodeInfos)
            {
                var groupNode = groupNodes.Value as ArrayList;

                ArrayList updatedGroupNodes = new ArrayList();
                foreach (PartNodeInfo item in groupNode)
                {
                    PartNodeInfo partNodeInfo = new PartNodeInfo();
                    partNodeInfo.NodeAddress = UpdatePort(item.NodeAddress, clusterPort, previousClusterPort);
                    partNodeInfo.IsCoordinator = item.IsCoordinator;
                    partNodeInfo.PriorityIndex = item.PriorityIndex;
                    partNodeInfo.SubGroup = item.SubGroup;
                    updatedGroupNodes.Add(partNodeInfo);
                }

                updatedPartitionNodeInfos.Add(groupNodes.Key, updatedGroupNodes);
            }

            return updatedPartitionNodeInfos;
        }

        private ArrayList BucketPortUpdate(ArrayList hashMap, int clusterPort, int previousClusterPort)
        {
            ArrayList updatedHashmap = new ArrayList();
            foreach (HashMapBucket bucket in hashMap)
            {
                HashMapBucket updatedBucket = bucket.Clone() as HashMapBucket;
                updatedBucket.TempAddress = UpdatePort(updatedBucket.TempAddress, clusterPort, previousClusterPort);
                updatedBucket.PermanentAddress = UpdatePort(updatedBucket.PermanentAddress, clusterPort, previousClusterPort);
                updatedHashmap.Add(updatedBucket);
            }
            return updatedHashmap;
        }

        private Address UpdatePort(Address address, int clusterPort, int previousClusterPort)
        {
            Address newAddress = new Address(address.IpAddress, address.Port);

            if (address != null)
            {
                if (address.Port == previousClusterPort)
                    newAddress.Port = clusterPort;
                else if (address.Port == previousClusterPort + 1)
                    newAddress.Port = clusterPort + 1;
            }
            return newAddress;
        }
    }
}
