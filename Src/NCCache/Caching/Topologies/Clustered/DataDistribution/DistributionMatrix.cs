//  Copyright (c) 2019 Alachisoft
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
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class DistributionMatrix
    {
        Address _address;
        ArrayList _filteredWeightIdList;
        long[,] _weightPercentMatrix;//Two-D matrix always of [Height,2] dimension. At 0th index Im keeping %age weight of each row, at 1st index Im keeping the weight corresponding to that %age
        MatrixDimensions  _mDimensions; //Keeps the matrix dimensions
        long[,] _weightMatrix;
        int[,] _idMatrix;
        int _itemsCount;
        long _totalWeight;
        long _weightToSacrifice; //weight this node needs to give away
        int _bucketsToSacrifice;
        int _percentWeightToSacrifice; //weight to sacrifice in percent. (w.r.t the same node data).        
        int _percentWeightOfCluster; //%age weight of cluster, THIS node is keeping .This helps in calculating this node's share
        int _cushionFactor; //Cushion +- to be considered as Algorithm is Aprroximate rather Exact.
        DistributionData _distData; //provide information about buckets and other calculations.        
        public static int WeightBalanceThresholdPercent = 10; //Percent weight threshold before balancing weight        
        private long _maxCacheSize = 1073741824; //Default One GB = 1024 * 1024 * 1024 (Byte * KB * MB = GB) User provided in if specified at UI.        
        private long _weightBalanceThreshold =  0 ; //at what weight should the node be treated as contributor to incoming nodes.
        
        public DistributionMatrix(ArrayList weightIdList, Address address, DistributionData distData, ILogger NCacheLog)
        {            
            _address = address;
            _distData = distData;
            _filteredWeightIdList = new ArrayList();
            _itemsCount = weightIdList.Count;
            _totalWeight = 1;
            _weightToSacrifice = 0;
            _cushionFactor = 10;            
            _percentWeightToSacrifice = 0;
            _weightBalanceThreshold = Convert.ToInt32((_maxCacheSize * WeightBalanceThresholdPercent) / 100); //10%, threshold at which we feel to balance weight for incoming nodes. its value is percent of MaxCacheSize 
            if (NCacheLog.IsInfoEnabled) NCacheLog.Error("DistributionMatrix.ctor", "Address->" + address.ToString() + ", DistributionData->" + distData.ToString());
            //this is the temp code just to put some trace...
            int bucketCount = 0;
            foreach (WeightIdPair wiPair in weightIdList)
            {
                if (wiPair.Address.compare(address) == 0)
                {
                    if(NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMatrix.ctor", "waitPair" + wiPair.Address.ToString() + ", wiPait->" + wiPair.BucketId);
                    _filteredWeightIdList.Add(wiPair);
                    bucketCount++;
                }
            }
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMatrix..ctor", address + " owns " + bucketCount + " buckets");
            _filteredWeightIdList.Sort();

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("DistributionMatrix.ctor", "_filterWeightIdList.Count:" + _filteredWeightIdList.Count + ", distData.BucketPerNode: " + distData.BucketsPerNode);
                    
            //Current bucket count - bucketss count after division gives buckets count to be sacrificed.
            _bucketsToSacrifice = _filteredWeightIdList.Count - distData.BucketsPerNode;
            if (_bucketsToSacrifice <= 0)
            {
                NCacheLog.Error("DistributionMatrix", "Address::" + address.ToString() + " cant sacrifice any bucket. Buckets/Node = " + distData.BucketsPerNode + " My Buckets Count = " + _filteredWeightIdList.Count);
                return;
            }
            int rows = Convert.ToInt32(Math.Ceiling((double)((decimal)_filteredWeightIdList.Count /(decimal)_bucketsToSacrifice)));
            int cols = _bucketsToSacrifice;
            InitializeMatrix(rows, cols);
        }

        private void InitializeMatrix(int rows, int cols)
        {
            _mDimensions = new MatrixDimensions(rows, cols);
            _weightMatrix = new long[rows, cols];
            _idMatrix = new int[rows, cols];
            _weightPercentMatrix = new long[rows, 2];

            int nLoopCount = 0;

            for (int i = 0; i < rows; i++)
            {
                long rowSum = 0;
                for (int j = 0; j < cols; j++)
                {
                    if (nLoopCount < _filteredWeightIdList.Count)
                    {
                        WeightIdPair tmpPair = (WeightIdPair)_filteredWeightIdList[nLoopCount];
                        _weightMatrix[i,j] = tmpPair.Weight;
                        _idMatrix[i,j] = tmpPair.BucketId;
                        rowSum += tmpPair.Weight;
                    }
                    else
                    {
                        _weightMatrix[i,j] = -1;
                        _idMatrix[i,j] = -1;
                    }                    
                    nLoopCount++;
                }
                _weightPercentMatrix[i, 1] = rowSum; //populate weightPercent Matrix while populating the weight and Id matrices.
                _totalWeight += rowSum;
            }

            //Here I am calculationg sum along with %age weight each row is keeping in. This would help while finding the right 
            // set of buckets to be given off.
            for (int i = 0; i < _mDimensions.Rows; i++)
            {                
                _weightPercentMatrix[i, 0] = Convert.ToInt64(Math.Ceiling(((double)_weightPercentMatrix[i, 1] / (double)_totalWeight) * 100));
            }

            //Calculate how much %age weight THIS NODE is keeping w.r.t overall cluster.
            _percentWeightOfCluster = Convert.ToInt32(((_totalWeight * 100) / _distData.CacheDataSum));            
            

            // Although buckets are sacrificed equally, but data is not.
            // Every node would share w.r.t the percentage that it is keeping in the Cluster.
            // If a node is keeping 50% share of the data, it would give away 50% of the required weight for the coming node.
            _weightToSacrifice = Convert.ToInt64(Math.Ceiling(((double)_distData.WeightPerNode * (double)_percentWeightOfCluster) / 100));
            _percentWeightToSacrifice = Convert.ToInt32(Math.Ceiling(((double)_weightToSacrifice /(double)_totalWeight) * 100));

        }

        public long[,] WeightPercentMatrix
        {
            get { return _weightPercentMatrix; }
        }

        public int[,] IdMatrix
        {
            get { return _idMatrix; }
        }

        public long[,] Matrix
        {
            get { return _weightMatrix; }
        }

        public long WeightToSacrifice
        {
            get { return _weightToSacrifice; }
            set { _weightToSacrifice = value; }
        }

        public int PercentWeightToSacrifice
        {
            get { return _percentWeightToSacrifice; }
        }

        public long TotalWeight
        {
            get { return _totalWeight; }
        }

        public int PercentWeightOfCluster
        {
            get { return _percentWeightOfCluster; }
        }

        public int CushionFactor
        {
            get { return Convert.ToInt32(Math.Ceiling((double)((double)_percentWeightToSacrifice /(double)_cushionFactor))); }
        }

        public MatrixDimensions MatrixDimension
        {
            get { return _mDimensions; }
        }

        //Do we really need to balance the weight while a node joins ?. This would let us know.
        //Addition of this property is to deal with the case when buckets are sequentially assigned while the cluster is in start.
        public bool DoWeightBalance
        {
            get
            {
                if (_totalWeight > this.WeightBalanceThreshold)
                    return true;
                return false;
            }
        }

        public long MaxCacheSize
        {
            get { return _maxCacheSize;}
            set { if (value > 0) _maxCacheSize = value; }
        }

        public long WeightBalanceThreshold
        {
            get 
            { 
                _weightBalanceThreshold = Convert.ToInt64((_maxCacheSize * WeightBalanceThresholdPercent) / 100); //10%, threshold at which we feel to balance weight for incoming nodes. its value is percent of MaxCacheSize ; 
                return _weightBalanceThreshold;    
            }
            set { _weightBalanceThreshold = value; }
        }
    }
}