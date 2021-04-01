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
using Alachisoft.NCache.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    [ConfigurationRoot("cache")]
    public class CachePortsConfigParameters
    {
        string _cacheId;
        int _port;

        [ConfigurationAttribute("cache-id")]
        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [ConfigurationAttribute("port")]
        public int MangementPort
        {
            get { return _port; }
            set { _port = value; }
        }

       
        #region ICloneable Members

        public object Clone()
        {
            CachePortsConfigParameters config = new CachePortsConfigParameters();
            config.CacheId = CacheId != null ? (string)CacheId.Clone() : null;
            config.MangementPort = this.MangementPort;
            return config;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            CacheId = reader.ReadObject() as string;
            MangementPort = reader.ReadInt32();
          
        }


        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_cacheId);
            writer.WriteObject(_port.ToString());

        }
        #endregion
    }
}
