// Copyright (c) 2018 Alachisoft
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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Runtime.CacheManagement

{

    #region Public Enums CacheStatus CacheTopology

    #endregion

    /// <summary>
    /// The <see cref="Alachisoft.NCache.Web.Management"/> namespace provides classes for management operations on cache
    /// This includes the <see cref="CacheHealth"/> class, use to store cache health information.
    /// </summary>


    public class CacheHealth
    {
        private string _name;
        private CacheTopology _topology;
        private CacheStatus _status;        
        private NodeStatus[] _serverNodesStatus;

        #region Public Properties

        /// <summary>
        /// Cache name
        /// </summary>
        public string CacheName 
        {
            set { _name = value; }
            get { return _name; }
        }

        /// <summary>
        /// Cache topology
        /// </summary>
        public CacheTopology Topology 
        {
            set { _topology = value; }
            get { return _topology; }
        }

        /// <summary>
        /// Cache status
        /// </summary>
        public CacheStatus Status 
        {
            set { _status = value; }
            get { return _status; }
        }
      
        /// <summary>
        /// Status of cache server nodes
        /// </summary>
        public NodeStatus[] ServerNodesStatus
        {

            set { _serverNodesStatus = value; }
            get { return _serverNodesStatus; }

        }

        #endregion


    }
}
