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
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class BalanceDataForNode
    {
        Address _address;
        ArrayList _filteredWeightIdList;
        int _percentData; //%age data of cluster this node is carrying.
        int _itemsCount; // total buckets this node is keeping  with.
        long _totalWeight; //total weight of this node.

        public BalanceDataForNode(ArrayList weightIdList, Address address, long clusterWeight)
        {
            _address = address;
            _filteredWeightIdList = new ArrayList();            
            _totalWeight = 1;

            foreach (WeightIdPair wiPair in weightIdList)
            {
                if (wiPair.Address.compare(address) == 0)
                {
                    _filteredWeightIdList.Add(wiPair);
                    _totalWeight += wiPair.Weight;
                }
            }
            _filteredWeightIdList.Sort();
            _itemsCount = _filteredWeightIdList.Count;
            _percentData = Convert.ToInt32(((double)_totalWeight / (double)clusterWeight) * 100);
        }


        public int PercentData
        {
            get { return _percentData; }
        }
        
        public long TotalWeight
        {
            get { return _totalWeight; }
        }

        public int ItemsCount
        {
            get { return _itemsCount; }
        }

        public ArrayList WeightIdList
        {
            get { return _filteredWeightIdList; }
            set
            {
                _filteredWeightIdList = value;
                _filteredWeightIdList.Sort();
            }
        }

        public Address NodeAddress
        {
            get{return _address;}
            set{ _address = value;}

        }
    }
}