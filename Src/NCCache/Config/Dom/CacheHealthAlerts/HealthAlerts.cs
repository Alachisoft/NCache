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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    [ConfigurationRoot("cache-health-alerts")]
    public class HealthAlerts : ICloneable, ICompactSerializable
    {
        private bool _enabled;
        private long _eventLoggingInterval = 1; //minutes
        private long _cacheLoggingInterval = 1; //minutes
        private Hashtable  _resource = null;

        public HealthAlerts()
        {
            _resource = new Hashtable();
        }

        [ConfigurationAttribute("enable")]
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }
        [ConfigurationAttribute("event-logging-interval")]
        public long EventLoggingInterval
        {
            get { return _eventLoggingInterval; }
            set { _eventLoggingInterval = value; }
        }

        [ConfigurationAttribute("cache-logging-interval")]
        public long CacheloggingInterval
        {
            get { return _cacheLoggingInterval; }
            set { _cacheLoggingInterval = value; }
        }

        [ConfigurationSection("resource")]
        public ResourceAtribute [] ResourceAttribute
        {
            get
            {
                ResourceAtribute[] attribs = new ResourceAtribute[_resource.Count];
                _resource.Values.CopyTo(attribs, 0);
                return attribs;
            }
            set
            {
                _resource.Clear();
                foreach (ResourceAtribute resourceAttribute in value)
                {
                    _resource.Add(resourceAttribute.Name, resourceAttribute);
                }
            }
        }

        public Hashtable Resources
        {
            get { return _resource; }
            set { _resource = value; }
        }


        #region ICloneable memebers

        public object Clone()
        {
            HealthAlerts healthAlerts = new HealthAlerts();

            healthAlerts.Enabled = Enabled;
            healthAlerts.EventLoggingInterval = EventLoggingInterval;
            healthAlerts.CacheloggingInterval = CacheloggingInterval;
            healthAlerts.Resources = Resources;

            return healthAlerts;
        }

        #endregion

        #region ICompactSerializable members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _enabled = reader.ReadBoolean();
            _eventLoggingInterval = reader.ReadInt64();
            _cacheLoggingInterval = reader.ReadInt64();
            _resource = reader.ReadObject() as Hashtable;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_enabled);
            writer.WriteObject(_eventLoggingInterval);
            writer.WriteObject(_cacheLoggingInterval);
            writer.WriteObject(_resource);
        }

        #endregion
    }
}
