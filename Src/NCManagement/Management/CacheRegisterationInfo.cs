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
using System;
using System.Collections;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Management
{
    [Serializable]
    public class CacheRegisterationInfo : ICompactSerializable
    {
        private CacheServerConfig _updatedCacheConfig;
        private ArrayList _affectedNodesList;
        private ArrayList _affectedPartitions;

        public CacheRegisterationInfo(CacheServerConfig cacheConfig, ArrayList nodesList, ArrayList affectedPartitions)
        {
            _updatedCacheConfig = cacheConfig;
            _affectedNodesList = nodesList;
            _affectedPartitions = affectedPartitions;
        }

        public CacheServerConfig UpdatedCacheConfig
        {
            get { return _updatedCacheConfig; }
        }

        public ArrayList AffectedNodes
        {
            get { return _affectedNodesList; }
        }

        public ArrayList AffectedPartitions
        {
            get { return _affectedPartitions; }
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _updatedCacheConfig = reader.ReadObject() as CacheServerConfig;
            _affectedNodesList = reader.ReadObject() as ArrayList;
            _affectedPartitions = reader.ReadObject() as ArrayList;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_updatedCacheConfig);
            writer.WriteObject(_affectedNodesList);
            writer.WriteObject(_affectedPartitions);
        }

        #endregion
    }
}