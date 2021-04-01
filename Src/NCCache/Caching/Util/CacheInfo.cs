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
using Alachisoft.NCache.Config.Dom;

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// A class used to hold cache information
    /// </summary>
    public class CacheInfo
    {
        /// <summary> The name of the cache instance. </summary>
        private string	_name = string.Empty;
        /// <summary> The scheme of the cache. </summary>
        private string	_class = string.Empty;
        /// <summary> The property string used to create the cache. </summary>
        private string	_configString = string.Empty;
       
        private string  _currentPartId;

        private CacheServerConfig   _config;

        public string CurrentPartitionId
        {
            get { return _currentPartId; }
            set { _currentPartId = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string ClassName
        {
            get { return _class; }
            set { _class = value; }
        }

        public string ConfigString
        {
            get { return _configString; }
            set { _configString = value; }
        }

        public CacheServerConfig Configuration
        {
            get { return this._config; }
            set { this._config = value; }
        }

        /// <summary>
        /// Gets the value which indicates whether the cache is clustered cache or not.
        /// </summary>
        public bool IsClusteredCache
        {
            get 
            {
                bool isClustered = false;
                if (_class != null)
                {
                    if (_class.CompareTo("replicated-server") == 0
                        || _class.CompareTo("mirror-server") == 0
                        || _class.CompareTo("partitioned-replicas-server") == 0
                        || _class.CompareTo("partitioned-server") == 0
                        || _class.CompareTo("replicated-server") == 0)
                        isClustered = true;
                }
                return isClustered;
            }
        }
    }
}