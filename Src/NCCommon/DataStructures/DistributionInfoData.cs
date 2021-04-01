//  Copyright (c) 2021 Alachisoft
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
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class DistributionInfoData
    {
        DistributionMode _distMode;
        ClusterActivity _clustActivity;
        ManualDistType _manualDistType;
        bool isUnderMaintenance;
        int _percentToMove;
        Address _source;
        PartNodeInfo _affectedNode;
        Address[] _destinations;

        public DistributionInfoData(DistributionMode distMode, ClusterActivity clustActivity, ManualDistType manDistType, int percentMove, Address source, Address[] dests)
        {
            _distMode = distMode;
            _clustActivity = clustActivity;
            _manualDistType = manDistType;
            _percentToMove = percentMove;
            _source = source;
            _destinations = dests;
        }

        public DistributionInfoData(DistributionMode distMode, ClusterActivity clustActivity, PartNodeInfo affectedNode, bool underMaintenance)
        {
            _distMode = distMode;
            _clustActivity = clustActivity;
            _affectedNode = affectedNode;
            isUnderMaintenance = underMaintenance;
        }

        public DistributionMode DistribMode
        {
            get { return _distMode; }
            set { _distMode = value; }
        }

        public ClusterActivity ClustActivity
        {
            get { return _clustActivity; }
            set { _clustActivity = value; }
        }

        public bool IsUnderMaintenance
        {
            get { return isUnderMaintenance; }
            set { isUnderMaintenance = value; }
        }

        public ManualDistType ManualDistType
        {
            get { return _manualDistType; }
            set { _manualDistType = value; }
        }

        public string Group
        {
            get { return _affectedNode.SubGroup; }
            set { _affectedNode.SubGroup = value; }
        }

        public int PercentToMove
        {
            get { return _percentToMove; }
            set { _percentToMove = value; }
        }

        public Address Source
        {
            get { return _source; }
            set { _source = value; }
        }

        public Address[] Destinations
        {
            get { return _destinations; }
            set { _destinations = value; }
        }

        public PartNodeInfo AffectedNode
        {
            get { return _affectedNode; }
            set { _affectedNode = value; }
        }
        public override string ToString()
        {
            return "DistributionInfoData( " + AffectedNode.ToString() + ")";
        }
    }
}