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
    /// WMI based event called each time a node jons the cluster
    /// </summary>
    public class CacheStopped : BaseEvent
    {
        private string _nodeName;
        private string _cacheName;

        public CacheStopped(String Cluster, string nodeName)
        {
            _nodeName = nodeName;
            _cacheName = Cluster;
        }

        public string NodeName
        {
            get
            {
                return _nodeName;
            }
            set
            {
                _nodeName = value;
            }
        }

        public string CacheID
        {
            get
            {
                return _cacheName;
            }

            set
            {
                _cacheName = value;
            }
        }

    }
}
#endif