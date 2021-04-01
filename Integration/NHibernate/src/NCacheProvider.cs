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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Cache;
using Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration;

namespace Alachisoft.NCache.Integrations.NHibernate.Cache
{

    class NCacheProvider : ICacheProvider
    {
        private static readonly IInternalLogger _logger = LoggerProvider.LoggerFor(typeof(Alachisoft.NCache.Integrations.NHibernate.Cache.NCacheProvider));
        
        #region ICacheProvider Members
            
        public ICache BuildCache(string regionName, IDictionary<string, string> properties)
        {
            if (_logger.IsDebugEnabled)
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, string> kvp in properties)
                {
                    sb.Append("name=");
                    sb.Append(kvp.Key.ToString());
                    sb.Append("&value=");
                    sb.Append(kvp.Value.ToString());
                    sb.Append(";");
                }
                _logger.Debug("building cache with region: " + regionName + ", properties: " + sb.ToString());
            }

            return new NCache(regionName, properties);
        }

        public long NextTimestamp()
        {
            return Timestamper.Next();
        }

        public void Start(IDictionary<string, string> properties)
        {
            //do nothing
        }

        public void Stop()
        {
            //do nothing
        }

        #endregion
    }
}
