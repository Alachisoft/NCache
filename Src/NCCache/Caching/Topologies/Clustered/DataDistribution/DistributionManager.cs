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
using System;
using System.Collections;
using System.Threading;

using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Util;
using System.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    class DistributionManager
    {

        #region/                        --- inner classes ---           /

        internal class CandidateNodeForLoadBalance : IComparable
        {
            Address _node;
            int _percentageAboveAvg;

            public CandidateNodeForLoadBalance(Address node, int percentageAboveAvg)
            {
                _node = node;
                _percentageAboveAvg = percentageAboveAvg;
            }

            public Address Node
            {
                get { return _node; }
            }

            public int PercentageAboveAverage
            {
                get { return _percentageAboveAvg; }
            }

            #region IComparable Members

            public int CompareTo(object obj)
            {
                if (obj != null && obj is CandidateNodeForLoadBalance)
                {
                    CandidateNodeForLoadBalance other = obj as CandidateNodeForLoadBalance;
                    if (other._percentageAboveAvg > this._percentageAboveAvg)
                        return 1;
                    else if (other._percentageAboveAvg < _percentageAboveAvg)
                        return -1;
                    else
                        return 0;

                }
                return 0;
            }

            #endregion
        }

        #endregion

        protected ArrayList _installedHashMap;
        protected ArrayList _lastCreatedHashMap;
        protected Hashtable _bucketsOwnershipMap;
        protected Hashtable _bucketsStats;
        protected ArrayList _existingMembers;
        protected Address _newMember;
        protected int _bucketSize;
        protected int _memberCount = 0;
        protected ReaderWriterLock _sync = new ReaderWriterLock();
        protected Address _localAddrss;
        protected int _autoBalancingThreshold;
        public static int TotalBuckets = 1000;

        private ILogger _ncacheLog;
        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
            set { _ncacheLog = value; }
        }

        protected long _cacheSizePerNode;
        private object _status_wait_mutex = new object();


        internal DistributionManager(int autoBalancingThreshold, long cacheSizePerNode)
        {
            Initialize();
            _existingMembers = new ArrayList();
            _autoBalancingThreshold = autoBalancingThreshold;
            _cacheSizePerNode = cacheSizePerNode;
        }

        private void Initialize()
        {
            long intRange = Int32.MinValue;
            _bucketSize = (int)Math.Ceiling((double)(intRange * -1) / (double)1000);
        }
        /// <summary>
        /// Gets/sets the local address.
        /// </summary>
        public Address LocalAddress
        {
            get { return _localAddrss; }
            set { _localAddrss = value; }
        }
        public ArrayList Members
        {
            get { return _existingMembers; }
        }

        public int BucketSize
        {
            get { return _bucketSize; }
            set { _bucketSize = value; }
        }

        public ArrayList InstalledHashMap
        {
            get { return _installedHashMap; }
            set { _installedHashMap = value; }
        }

        public ArrayList LastCreatedHashMap
        {
            get { return _lastCreatedHashMap; }
            set { _lastCreatedHashMap = value; }
        }

        public Hashtable BucketStats
        {
            get { return _bucketsStats; }
            set { _bucketsStats = value; }
        }

        /// <summary>
        /// returns the Total data size contained by the cluster
        /// </summary>
        public long TotalDataSize
        {
            get
            {
                long size = 0;
                try
                {
                    Sync.AcquireReaderLock(Timeout.Infinite);
                    if (BucketStats != null && BucketStats.Count > 0)
                    {
                        IDictionaryEnumerator ide = BucketStats.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            BucketStatistics stats = (BucketStatistics)ide.Value;
                            size += stats.DataSize;
                        }
                    }
                    return size;
                }
                finally
                {
                    Sync.ReleaseReaderLock();
                }
            }
        }

        /// <summary>
        /// returns the total data size / number of nodes
        /// </summary>
        public long AvgDataSize
        {
            get
            {
                try
                {
                    Sync.AcquireReaderLock(Timeout.Infinite);
                    if (_existingMembers != null && _existingMembers.Count > 0)
                    {
                        return TotalDataSize / _existingMembers.Count;
                    }
                    return 0;
                }
                finally
                {
                    Sync.ReleaseReaderLock();
                }
            }
        }

        public Hashtable DataSizePerNode
        {
            get
            {
                Hashtable tmp = new Hashtable();
                try
                {
                    Sync.AcquireReaderLock(Timeout.Infinite);

                    IEnumerator ie = null;
                    if (_lastCreatedHashMap != null)
                        ie = _lastCreatedHashMap.GetEnumerator();
                    else if (_installedHashMap != null)
                        ie = InstalledHashMap.GetEnumerator();

                    if (ie != null)
                    {
                        while (ie.MoveNext())
                        {
                            HashMapBucket bucket = (HashMapBucket)ie.Current;
                            if (tmp.Contains(bucket.TempAddress))
                            {
                                long size = (long)tmp[bucket.TempAddress];
                                size += ((BucketStatistics)BucketStats[bucket.BucketId]).DataSize;
                                tmp[bucket.TempAddress] = size;
                            }
                            else
                            {
                                long size = ((BucketStatistics)BucketStats[bucket.BucketId]).DataSize;
                                tmp[bucket.TempAddress] = size;
                            }
                        }
                    }
                    return tmp;
                }
                finally
                {
                    Sync.ReleaseReaderLock();
                }
            }
        }


        /// <summary>
        /// returns the list of nodes that are getting more data than the average
        /// </summary>
        public ArrayList CandidateNodesForBalance
        {
            get
            {
                ArrayList candidateNodes = new ArrayList();
                IDictionaryEnumerator ide = DataSizePerNode.GetEnumerator();
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("loadbalancing condition : AvgDataSize (MB): " + MiscUtil.ConvertToMegaBytes(AvgDataSize));
                while (ide.MoveNext())
                {
                    Address owner = ide.Key as Address;
                    long size = (long)ide.Value;
                    sb.Append(" [" + owner + " = " + MiscUtil.ConvertToMegaBytes(size) + " (MB) ]");

                    if (size > AvgDataSize)
                    {
                        long difference = size - AvgDataSize;
                        int percentageAboveAvg = GetPercentAboveAvg(difference);
                        if (percentageAboveAvg > _autoBalancingThreshold)
                        {
                            candidateNodes.Add(new CandidateNodeForLoadBalance(owner, percentageAboveAvg));
                        }
                    }
                }
                candidateNodes.Sort();
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.CandidateNodesForBalance", sb.ToString());
                return candidateNodes;

            }
        }

        public ReaderWriterLock Sync
        {
            get { return _sync; }
        }

        public Hashtable BucketsOwnershipMap
        {
            get { return _bucketsOwnershipMap; }
            set { _bucketsOwnershipMap = value; }
        }

        /// <summary>
        /// This methods calculates in percentage how much data size on this 
        /// node is above the average data size...
        /// </summary>
        /// <param name="difference">DataSizeOnThisNode - AverageDataSize</param>
        /// <returns></returns>
        private int GetPercentAboveAvg(long difference)
        {
            long cent = difference * 100;
            long percent = cent / AvgDataSize;
            //return (int)((difference * 100) / AvgDataSize);
            return (int)percent;
        }

        public bool IsBucketFunctional(Address owner, object key)
        {
            int bucketId = GetBucketId(key as string);
            Sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (BucketsOwnershipMap != null && BucketsOwnershipMap.Contains(owner))
                {
                    ArrayList buckets = BucketsOwnershipMap[owner] as ArrayList;
                    if (buckets != null)
                    {
                        int index = buckets.IndexOf(new HashMapBucket(null, bucketId));
                        if (index != -1)
                        {
                            HashMapBucket bucket = buckets[index] as HashMapBucket;
                            if (bucket.Status == BucketStatus.Functional)
                                return true;
                        }
                    }
                }
            }
            finally
            {
                Sync.ReleaseReaderLock();
            }
            return false;
        }

        public Address SelectNode(string key)
        {
            //Three retries
            for (int retries = 0; retries < 3; retries++)
            {
                try
                {
                    return SelectNodeInternal(key);
                }
                catch (ArgumentException e)
                {
                    if (retries == 2)
                        throw;
                    Thread.Sleep(10);
                }
            }
            return null;
        }

        private Address SelectNodeInternal(string key)
        {
            int hashCode = AppUtil.GetHashCode(key);
            int index = hashCode / this.BucketSize;

            if (index < 0)
                index = index * -1;

            _sync.AcquireReaderLock(Timeout.Infinite);

            try
            {
                if (_installedHashMap != null)
                {
                    HashMapBucket bucket = _installedHashMap[index] as HashMapBucket;
                    if (bucket != null)
                    {
                        //if (bucket.Status != BucketStatus.Functional)
                        {
                            /// This is special case that handles operations during stateTransfer.
                            /// If a bucket is not yet transfered to the new coordinator from the replica. then
                            /// the replica's address is returned.
                            Address coordinatorNodeAddress = bucket.TempAddress; // this should be the sub-coordinator addres
                            ArrayList ownershipMap = _bucketsOwnershipMap[coordinatorNodeAddress] as ArrayList;
                            if (ownershipMap == null)
                            {
                                NCacheLog.Warn("DistributionManager.SelectNodeInternal()", "ownershipMap is null. Returning permanent address of bucket.");
                                return bucket.PermanentAddress;
                            }
                                int indexOfOwnedBucket = ownershipMap.IndexOf(bucket);

                                if (indexOfOwnedBucket != -1)
                                {
                                    HashMapBucket ownedBucket = ownershipMap[indexOfOwnedBucket] as HashMapBucket;
                                    return ownedBucket.PermanentAddress;
                                }
                        }
                        return bucket.PermanentAddress;
                    }
                }
                return null;
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
        }

        public int GetBucketId(string key)
        {
            int hashCode = AppUtil.GetHashCode(key);
            int index = hashCode / this.BucketSize;

            if (index < 0)
                index = index * -1;

            HashMapBucket bucket = _installedHashMap[index] as HashMapBucket;
            return bucket.BucketId;
        }

        /// <summary>
        /// A new map is required when a member leaves or joins the cluster.
        /// This method returns a new map based on the input paramameters.
        /// </summary>
        /// <param name="member">Address of the member that has either left
        /// or joined the cluster</param>
        /// <param name="isNew">A flag. True if the member has joined otherwise false.</param>
        /// <returns>A new hashmap instance</returns>
        public virtual DistributionMaps GetMaps(DistributionInfoData distInfoData)
        {
            ArrayList tmpMap = null;
            Hashtable bucketsOwnershipMap = null;
            ArrayList partitionNodes = new ArrayList();

            _sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_installedHashMap == null)
                {
                    tmpMap = new ArrayList(TotalBuckets);
                    for (int i = 0; i < TotalBuckets; i++)
                    {
                        HashMapBucket bucket = new HashMapBucket(distInfoData.AffectedNode.NodeAddress, i, BucketStatus.Functional);
                        tmpMap.Add(bucket);

                    }

                    _existingMembers.Add(distInfoData.AffectedNode.NodeAddress);

                    _lastCreatedHashMap = tmpMap.Clone() as ArrayList;

                    bucketsOwnershipMap = GetBucketsOwnershipMap(_lastCreatedHashMap);
                    return new DistributionMaps(_lastCreatedHashMap, bucketsOwnershipMap);
                }
                else if (_lastCreatedHashMap == null)
                {
                    _lastCreatedHashMap = _installedHashMap.Clone() as ArrayList;
                }

                switch (distInfoData.ClustActivity)
                {
                    case ClusterActivity.NodeJoin:
                        try
                        {
                            return GetMapsOnNodeJoining(distInfoData);
                        }
                        catch (Exception e)
                        {
                            if (NCacheLog.IsErrorEnabled) NCacheLog.Error("DistributionMgr.GetMaps()", e.ToString());
                            break;
                        }

                    case ClusterActivity.NodeLeave:

                        try
                        {
                            return GetMapsOnNodeLeaving(distInfoData);
                        }
                        catch (Exception e)
                        {
                            if (NCacheLog.IsErrorEnabled) NCacheLog.Error("DistributionMgr.GetMaps()", e.ToString());
                            break;
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
            }
            finally
            {
                _sync.ReleaseWriterLock();
            }
            return null;
        }

        protected DistributionMaps GetMapsOnNodeJoining(DistributionInfoData distInfoData)
        {
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.GetMapsOnNodeJoining()", "Total Data Size : " + TotalDataSize.ToString());
            ArrayList tmpMap = null;
            Hashtable bucketsOwnershipMap = null;
            ArrayList partitionNodes = new ArrayList();

            ArrayList newHashMap = DistributeHashMap.BalanceBuckets(distInfoData, _lastCreatedHashMap, _bucketsStats, _existingMembers, _cacheSizePerNode, NCacheLog);
            _existingMembers.Add(distInfoData.AffectedNode.NodeAddress);

            tmpMap = ChangeOwnerShip(newHashMap, distInfoData.AffectedNode.NodeAddress);

            _lastCreatedHashMap = tmpMap.Clone() as ArrayList;

            bucketsOwnershipMap = GetBucketsOwnershipMap(_lastCreatedHashMap);
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.GetMaps()", "Sending new map as a new node joined the cluster");
            return new DistributionMaps(_lastCreatedHashMap, bucketsOwnershipMap);
        }

        protected DistributionMaps GetMapsOnNodeLeaving(DistributionInfoData distInfoData)
        {
            ArrayList tmpMap = null;
            Hashtable bucketsOwnershipMap = null;
            _existingMembers.Remove(distInfoData.AffectedNode.NodeAddress);
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.GetMapsOnNodeLeaving()", "Before Calling DistributeOrphanBuckets() ---- Leaving Node:" + distInfoData.AffectedNode.NodeAddress.ToString() + " Existing Members Count:0" + _existingMembers.Count);
            tmpMap = DistributeHashMap.DistributeOrphanBuckets(_lastCreatedHashMap, distInfoData.AffectedNode.NodeAddress, _existingMembers);

            if (tmpMap == null) return null;
            _lastCreatedHashMap = tmpMap.Clone() as ArrayList;

            bucketsOwnershipMap = GetBucketsOwnershipMap(_lastCreatedHashMap);
            return new DistributionMaps(_lastCreatedHashMap, bucketsOwnershipMap);
        }

        protected ArrayList ChangeOwnerShip(ArrayList affectedBuckets, Address newMember)
        {
            ArrayList tmpMap = _lastCreatedHashMap == null ? new ArrayList() : _lastCreatedHashMap.Clone() as ArrayList;
            HashMapBucket bucket;
            for (int i = 0; i < affectedBuckets.Count; i++)
            {
                int buckId = (int)affectedBuckets[i];
                bucket = (HashMapBucket)tmpMap[buckId];
                bucket.TempAddress = newMember;
                bucket.Status = BucketStatus.NeedTransfer;
            }

            return tmpMap;
        }

        internal virtual void OnMemberJoined(Address mbr, NodeIdentity identity)
        {
            if (_existingMembers != null && !_existingMembers.Contains(mbr))
            {
                _existingMembers.Add(mbr);
            }
        }

        internal virtual void OnMemberLeft(Address mbr, NodeIdentity identity)
        {
            if (_existingMembers != null && _existingMembers.Contains(mbr))
            {
                _existingMembers.Remove(mbr);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual Hashtable GetBucketsOwnershipMap(ArrayList hashMap)
        {
            Hashtable bucketsOwnershipMap = new Hashtable();

            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (hashMap != null)
                {
                    IEnumerator ie = hashMap.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        HashMapBucket bucket = ie.Current as HashMapBucket;

                        Address owner = bucket.TempAddress != null ? bucket.TempAddress : bucket.PermanentAddress;

                        if (bucketsOwnershipMap.Contains(owner))
                        {
                            ArrayList buckets = bucketsOwnershipMap[owner] as ArrayList;
                            if (!buckets.Contains(bucket))
                            {
                                buckets.Add(bucket.Clone());
                            }
                        }
                        else
                        {
                            ArrayList buckets = new ArrayList();
                            buckets.Add(bucket.Clone());
                            bucketsOwnershipMap[owner] = buckets;
                        }
                    }
                }
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
            return bucketsOwnershipMap;
        }

        /// <summary>
        /// Returns a table that has buckets owners and there respective hashcode ranges
        /// </summary>
        /// <returns>a hashtable, with bucket id's as keys and owners address as value</returns>
        internal Hashtable GetOwnerHashMapTable(Hashtable renderers)
        {
            Hashtable ownerHashCodeTable = new Hashtable(TotalBuckets);
            try
            {
                _sync.AcquireReaderLock(Timeout.Infinite);
                if (_installedHashMap != null)
                {
                    for (int i = 0; i < _installedHashMap.Count; i++)
                    {
                        HashMapBucket bucket = _installedHashMap[i] as HashMapBucket;
                        if (bucket != null)
                        {
                            switch (bucket.Status)
                            {
                                case BucketStatus.Functional:
                                    ownerHashCodeTable.Add(i, GetServerAddress(renderers, bucket.PermanentAddress));
                                    break;
                                case BucketStatus.NeedTransfer:
                                case BucketStatus.UnderStateTxfr:
                                    ownerHashCodeTable.Add(i, GetServerAddress(renderers, bucket.TempAddress));
                                    break;
                            }
                        }
                    }
                }
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
            return ownerHashCodeTable;
        }

        public static string GetServerAddress(Hashtable renderers, Address clusterAddress)
        {
            string serverAddress = string.Empty;
            if (renderers != null)
            {
                if (renderers.Contains(clusterAddress))
                {
                    if (((Address)renderers[clusterAddress]).IpAddress != null)
                        serverAddress = ((Address)renderers[clusterAddress]).IpAddress.ToString();
                    else
                        serverAddress = clusterAddress.IpAddress.ToString();
                }
            }
            return serverAddress;
        }

        /// <summary>
        /// Returns a table that has buckets owners and there respective hashcode ranges
        /// </summary>
        /// <param name="bucketSize">out parameter, holds the individual bucket size</param>
        /// <returns>a hashtable, with bucket id's as keys and owners address as value</returns>        
        internal Hashtable GetOwnerHashMapTable(Hashtable renderers, out int bucketSize)
        {
            bucketSize = this.BucketSize;
            return GetOwnerHashMapTable(renderers);
        }

        public void UpdateBucketStats(NodeInfo localNode)
        {
            try
            {
                Sync.AcquireWriterLock(Timeout.Infinite);

                if (localNode == null) return;

                if (_bucketsStats == null)
                    _bucketsStats = new Hashtable();

                if (localNode.Statistics != null && localNode.Statistics.LocalBuckets != null)
                {
                    Hashtable bucketStats = localNode.Statistics.LocalBuckets.Clone() as Hashtable;
                    if (bucketStats != null)
                    {
                        IDictionaryEnumerator ide = bucketStats.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            //muds:
                            //see if this node is the permanent owner of the bucket
                            //otherwise its quite possible that we override the 
                            //stats of the bucket from the temporary owner.
                            HashMapBucket bucket = (HashMapBucket)_installedHashMap[(int)ide.Key];
                            if (bucket.PermanentAddress.Equals(localNode.Address))
                            {
                                BucketStatistics stats = ide.Value as BucketStatistics;
                                _bucketsStats[ide.Key] = ide.Value;
                            }
                            else
                            {
                            }
                        }
                    }

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.UpdateBucketStats()", "bucketStats = " + _bucketsStats == null ? "null" : _bucketsStats.Count.ToString());
                }
            }
            catch (Exception e)
            {
                if (NCacheLog.IsErrorEnabled) NCacheLog.Error("DistributionMgr.UpdateBucketStats()", e.ToString());
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public void WaitForBucketToBeFunctional(int bucket)
        {
            WaitForBucketStatus(bucket, BucketStatus.Functional);
        }

        protected virtual void WaitForBucketStatus(int bucket, byte status)
        {
            HashMapBucket bkt = _installedHashMap[bucket] as HashMapBucket;
            if (bkt != null)
            {
                bkt.StateTxfrLatch.WaitForAny(status);
            }
        }

        public virtual void WaitForBucketStatus(int bucket, byte status, Address owner)
        {
            if (owner != null)
            {
                while (true)
                {
                    HashMapBucket hashBucket = GetBucketForWait(bucket, owner);

                    if (hashBucket == null) return;
                    if (hashBucket.StateTxfrLatch.IsAnyBitsSet(status) || !owner.Equals(hashBucket.TempAddress)) return;
                    lock (_status_wait_mutex)
                    {
                        if (!owner.Equals(hashBucket.TempAddress) || hashBucket.StateTxfrLatch.IsAnyBitsSet(status))
                            return;
                        Monitor.Wait(_status_wait_mutex);
                    }
                }
            }
        }

        public virtual void NotifyBucketUpdate()
        {
            lock (_status_wait_mutex)
            {
                Monitor.PulseAll(_status_wait_mutex);
            }
        }

        public virtual HashMapBucket GetBucketForWait(int bucket, Address owner)
        {
            HashMapBucket bkt = _installedHashMap[bucket] as HashMapBucket;
            return bkt;
        }

        public ArrayList GetBucketIdOwnedbyMe()
        {
            ArrayList myBuckets = new ArrayList();
            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                IDictionaryEnumerator ide = _bucketsOwnershipMap.GetEnumerator();
                while (ide.MoveNext())
                {
                    ArrayList buckets = ide.Value as ArrayList;
                    if (buckets != null)
                    {
                        foreach (HashMapBucket bucket in buckets)
                        {
                            if (bucket.TempAddress.Equals(_localAddrss) || bucket.PermanentAddress.Equals(_localAddrss))
                            {
                                if (!myBuckets.Contains(bucket.BucketId))
                                    myBuckets.Add(bucket.BucketId);
                            }
                        }
                    }
                }

            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
            return myBuckets;
        }

        public ArrayList GetBucketsList(Address ofNode)
        {
            _sync.AcquireReaderLock(Timeout.Infinite);
            ArrayList myBuckets = null;
            try
            {

                if (ofNode != null)
                {
                    myBuckets = _bucketsOwnershipMap[ofNode] as ArrayList;
                }
                if (myBuckets != null) myBuckets = myBuckets.Clone() as ArrayList;
            }
            catch (NullReferenceException) { }
            finally
            {
                _sync.ReleaseReaderLock();
            }

            return myBuckets;
        }

        /// <summary>
        /// Set the status of the bucket to state transfer and in this way this
        /// bucket becomes locked. A locked bucket can not be assigned during
        /// loadbalancing.
        /// </summary>
        /// <param name="buckets"></param>
        /// <param name="node"></param>
        public void ChangeBucketStatusToStateTransfer(ArrayList buckets, Address node)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (buckets != null)
                {
                    IEnumerator ie = buckets.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        lock (InstalledHashMap.SyncRoot)
                        {
                            HashMapBucket bucket = (HashMapBucket)InstalledHashMap[(int)ie.Current];
                            if (node.Equals(bucket.TempAddress))
                            {
                                bucket.Status = BucketStatus.UnderStateTxfr;
                         
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.ChangeBucketStatus", bucket.ToString());
                            }
                        }
                    }

                    if (_bucketsOwnershipMap != null)
                    {
                        ArrayList nodeBuckets = _bucketsOwnershipMap[node] as ArrayList;
                        if (nodeBuckets != null)
                        {
                            foreach (int bucketId in buckets)
                            {
                                int indexOfBucket = nodeBuckets.IndexOf(new HashMapBucket(null, bucketId));
                                if (indexOfBucket != -1)
                                {
                                    HashMapBucket bucket = nodeBuckets[indexOfBucket] as HashMapBucket;
                                    if (node.Equals(bucket.TempAddress))
                                    {
                                        bucket.Status = BucketStatus.UnderStateTxfr;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Locks the buckets which are under the process of state transfer. A locked
        /// bucket can not be assigned to a node while loadbalancing. Only a coordinator
        /// node can lock the buckets.
        /// </summary>
        /// <param name="buckets"></param>
        /// <param name="requestingNode"></param>
        /// <returns></returns>
        public virtual Hashtable LockBuckets(ArrayList buckets, Address requestingNode)
        {
            ArrayList lockAcquired = new ArrayList();
            ArrayList ownerChanged = new ArrayList();

            Hashtable result = new Hashtable();
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (buckets != null)
                {
                    IEnumerator ie = buckets.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        lock (InstalledHashMap.SyncRoot)
                        {
                            HashMapBucket bucket = (HashMapBucket)InstalledHashMap[(int)ie.Current];

                            if (requestingNode.Equals(bucket.TempAddress))
                            {
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.lockbuckets", "acquired locked on bucket [" + bucket.BucketId + "] by " + requestingNode);

                                bucket.Status = BucketStatus.UnderStateTxfr;
                                if (!lockAcquired.Contains(ie.Current))
                                    lockAcquired.Add(ie.Current);
                            }
                            else if (!ownerChanged.Contains(ie.Current))
                            {
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMgr.lockbuckets", "bucket [" + bucket.BucketId + "] owner ship is changed; new owner is " + bucket.TempAddress);
                                ownerChanged.Add(ie.Current);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NCacheLog.Error("DistributionMgr.lockbuckets", e.ToString());
                return result;
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
            result[BucketLockResult.OwnerChanged] = ownerChanged;
            result[BucketLockResult.LockAcquired] = lockAcquired;

            return result;
        }
        /// <summary>
        /// Releases a bucket by setting its status again to functional. Only 
        /// node who has set its status to state trxfr can change its status.
        /// </summary>
        /// <param name="buckets"></param>
        /// <param name="node"></param>
        public void ReleaseBuckets(ArrayList buckets, Address requestingNode)
        {
            try
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    if (buckets != null)
                    {
                        IEnumerator ie = buckets.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            lock (InstalledHashMap.SyncRoot)
                            {
                                HashMapBucket bucket = (HashMapBucket)InstalledHashMap[(int)ie.Current];
                                if (requestingNode.Equals(bucket.TempAddress))
                                {
                                    bucket.Status = BucketStatus.Functional;
                                    //Change permnant address only when node who locked the bucket 
                                    //has sent request to release after he has transfered the bucket completely.
                                    bucket.PermanentAddress = bucket.TempAddress;
                                }
                                //NCacheLog.Error("DistributionMgr.ReleaseBuckets", " Released bucket from InstalledHashMap " + bucket.ToString() + ". Requested by " + requestingNode);
                            }
                        }
                    }

                    if (_bucketsOwnershipMap != null)
                    {
                        ArrayList nodeBuckets = _bucketsOwnershipMap[requestingNode] as ArrayList;
                        if (nodeBuckets != null)
                        {
                            foreach (int bucketId in buckets)
                            {
                                int indexOfBucket = -1;
                                int startIndex = 0;
                                do
                                {
                                    //TempFix: Updates status for multipile occurances of the same bucket in ownership map for replica.
                                    indexOfBucket = nodeBuckets.IndexOf(new HashMapBucket(null, bucketId), startIndex);
                                    if (indexOfBucket != -1)
                                    {
                                        HashMapBucket bucket = nodeBuckets[indexOfBucket] as HashMapBucket;
                                        if (requestingNode.Equals(bucket.TempAddress))
                                        {
                                            bucket.Status = BucketStatus.Functional;
                                            //Change permnant address only when node who locked the bucket 
                                            //has sent request to release after he has transfered the bucket completely.

                                            bucket.PermanentAddress = requestingNode;
                                        }
                                        startIndex = indexOfBucket + 1;
                                    }
                                } while (indexOfBucket >= 0);
                               
                            }
                        }
                    }
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                    NotifyBucketUpdate();
                }
            }
            catch (NullReferenceException e)
            {
                //Null is expected at start of cache
            }
            catch (Exception e)
            {
                NCacheLog.Error("DistributionMgr.ReleaseBuckets", e.ToString());
            }
        }
        /// <summary>
        /// Verifies whether the given node is temorary owner of the bucket or not.
        /// </summary>
        /// <param name="bucketId"></param>
        /// <param name="temOwner"></param>
        /// <returns></returns>
        public virtual bool VerifyTemporaryOwnership(int bucketId, Address temOwner)
        {
            try
            {
                if (temOwner != null)
                {
                    Sync.AcquireReaderLock(Timeout.Infinite);
                    try
                    {
                        lock (InstalledHashMap.SyncRoot)
                        {
                            HashMapBucket bucket = (HashMapBucket)InstalledHashMap[bucketId];
                            return temOwner.Equals(bucket.TempAddress);
                        }

                    }
                    finally
                    {
                        Sync.ReleaseReaderLock();
                    }
                }
            }
            catch (NullReferenceException e)
            {
                NCacheLog.Error("DistributionMgr.VerifyTemporaryOwnership", e.ToString());
            }
            catch (Exception e)
            {
                NCacheLog.Error("DistributionMgr.VerifyTemporaryOwnership", e.ToString());
            }
            return false;
        }
        /// <summary>
        /// Verifies whether a given node is the permanent owner of the bucket or not
        /// </summary>
        /// <param name="bucketId"></param>
        /// <param name="perOwner"></param>
        /// <returns></returns>
        public bool VerifyPermanentOwnership(int bucketId, Address perOwner)
        {
            try
            {
                if (perOwner != null)
                {
                    Sync.AcquireReaderLock(Timeout.Infinite);
                    try
                    {
                        lock (InstalledHashMap.SyncRoot)
                        {
                            HashMapBucket bucket = (HashMapBucket)InstalledHashMap[bucketId];
                            return perOwner.Equals(bucket.TempAddress);
                        }

                    }
                    finally
                    {
                        Sync.ReleaseReaderLock();
                    }
                }
            }
            catch (Exception e)
            {
                NCacheLog.Error("DistributionMgr.VerifyPermanentOwnership", e.ToString());
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newMap"></param>
        /// <param name="newBucketsOwnershipMap"></param>
        /// <param name="leftMbrs"></param>
        public void InstallHashMap(DistributionMaps distributionMaps, ArrayList leftMbrs)
        {
            ArrayList newMap = null;
            Hashtable newBucketsOwnershipMap = null;

            _sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (distributionMaps == null)
                    return;

                newMap = distributionMaps.Hashmap;
                newBucketsOwnershipMap = distributionMaps.BucketsOwnershipMap;

                if (newMap == null || newBucketsOwnershipMap == null)
                    return;

                if (_installedHashMap != null)
                {
                    for (int i = 0; i < newMap.Count; i++)
                    {
                        HashMapBucket newBucket = (HashMapBucket)newMap[i];
                        int index = _installedHashMap.IndexOf(newBucket);
                        HashMapBucket oldBucket = (HashMapBucket)_installedHashMap[index];

                        if (!oldBucket.PermanentAddress.Equals(newBucket.PermanentAddress) && oldBucket.TempAddress.Equals(newBucket.TempAddress))
                        {
                            NCacheLog.Error("Install Hasmap", "BucketID: " + index.ToString() + "\toldBucket: " + oldBucket.PermanentAddress.ToString() + "\toldBucket.Temp: " + oldBucket.TempAddress.ToString() + "\tnewBucket: " + newBucket.PermanentAddress.ToString() + "\tnewBucekt.Temp: " + newBucket.TempAddress.ToString());
                        }
                        else
                        {
                            oldBucket.PermanentAddress = newBucket.PermanentAddress;
                            oldBucket.TempAddress = newBucket.TempAddress;
                            oldBucket.Status = newBucket.Status;
                        }


                    }
                }
                else
                {
                    _installedHashMap = newMap;
                }
                _bucketsOwnershipMap = newBucketsOwnershipMap;

                NotifyBucketUpdate();
            }
            finally
            {
                _sync.ReleaseWriterLock();
            }
        }

        public virtual void Wait(object key)
        {
            if (key != null)
            {
                if (_installedHashMap != null)
                {
                    int bucketId = GetBucketId(key as string);
                    HashMapBucket bucket = (HashMapBucket)_installedHashMap[bucketId];
                    bucket.StateTxfrLatch.WaitForAny(BucketStatus.Functional | BucketStatus.NeedTransfer);
                }
                else
                    NCacheLog.Error("DistributionManager.Wait", "_installedHashMap == null");
            }
        }

        public void Set(int bucketId, string group)
        {
            if (_installedHashMap != null)
            {
                HashMapBucket bucket = (HashMapBucket)_installedHashMap[bucketId];
                bucket.StateTxfrLatch.SetStatusBit(BucketStatus.Functional, BucketStatus.UnderStateTxfr);
            }
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (_installedHashMap != null)
            {
                for (int i = 0; i < _installedHashMap.Count; i++)
                {
                    if (_installedHashMap[i] != null)
                    {
                        sb.Append("Key: " + i.ToString() + "\tValue: " + _installedHashMap[i].ToString() + "\n");
                    }
                    else
                        sb.Append("Key: " + i.ToString() + "\tValue: NULL\n");
                }
            }
            return sb.ToString();
        }

        internal ArrayList GetPermanentAddress(ArrayList activePartitions)
        {
            ArrayList list = new ArrayList();
            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                foreach (Address server in activePartitions)
                {
                    foreach (HashMapBucket bucket in _bucketsOwnershipMap[server] as ArrayList)
                    {
                        Address address = bucket.PermanentAddress;
                        if (!list.Contains(address))
                        {
                            list.Add(address.Clone());
                        }
                    }
                    if (!list.Contains(server))
                    {
                        list.Add(server.Clone());
                    }
                }
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
            return list;
        }

        internal bool InStateTransfer()
        {
            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                foreach (HashMapBucket bucket in _installedHashMap)
                {
                    if (bucket.Status == BucketStatus.UnderStateTxfr || bucket.Status == BucketStatus.NeedTransfer)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
            return false;
        }
    }
}
