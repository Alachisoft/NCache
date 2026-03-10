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
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Net;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public interface IDistributionManager
    {
        Hashtable BucketsOwnershipMap { get; set; }
        Hashtable BucketStats { get; set; }
        ArrayList InstalledHashMap { get; set; }
        Address LocalAddress { get; set; }
        List<int> LocalBucketIds { get; }
        int MapVersion { get; set; }
        ArrayList Members { get; set; }
        ArrayList PermanentMembers { get; }
        ILogger NCacheLog { get; set; }
        List<int> PermanentLocalBucketIds { get; }
        int TotalBuckets { get; set; }

        bool IsBucketUnderStateTransfer(Address owner, int bucketId);
        void ChangeBucketStatusToStateTransfer(ArrayList buckets, Address node);
        int GetBucketId(string key);
        List<int> GetBucketIdOwnedbyMe();
        ArrayList GetBucketsList(Address ofNode);
        Hashtable GetBucketsOwnershipMap(ArrayList hashMap);
        DistributionInfo GetDistributionInfo();
        Hashtable GetOwnerModuleHashMapTable(Hashtable renderers, out int bucketSize);
        void InstallHashMap(DistributionMaps distributionMaps, ArrayList leftMbrs);
        Hashtable LockBuckets(ArrayList buckets, Address requestingNode);
        void ReleaseBuckets(ArrayList buckets, Address requestingNode);
        Address SelectNode(string key, string group);
        void UpdateBucketStats(BucketStatistics[] localBuckets, Address address);
        bool VerifyTemporaryOwnership(int bucketId, Address temOwner);

        void WaitForBucketToBeFunctional(int bucket);

        ArrayList GetPermanentAddress();
        IList GetMyFunctionalBucketIds();

        int[] GetCountsOfBucketsRequiringStateTransfer();
        bool IsBucketFunctional(Address owner, int bucketId);
    }
}
