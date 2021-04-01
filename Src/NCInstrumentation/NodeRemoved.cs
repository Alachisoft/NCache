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
#if !NETCORE
using System;
using System.Management.Instrumentation;

namespace Alachisoft.NCache.Instrumentation
{
    /// <summary>
    /// NodeDown WMI based event fired each time a node gets removed from the Cluster
    /// </summary>
    public class NodeRemoved : BaseEvent
    {
        private string _nodeName;
        private string _clusterName;

        public NodeRemoved(String Cluster, string Node)
        {
            try
            {
                _nodeName = Node;
                _clusterName = Cluster;
            }
            catch { }
        }

        public string NodeName
        {
            get
            {
                return _nodeName;
            }
            set
            {
                try { _nodeName = value; }
                catch { }
            }
        }

        public string ClusterName
        {
            get
            {
                return _clusterName;
            }

            set
            {
                try
                {
                    _clusterName = value;
                }
                catch { }
            }
        }
    }
}
#endif