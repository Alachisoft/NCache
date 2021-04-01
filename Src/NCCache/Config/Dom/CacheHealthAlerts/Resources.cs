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
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class ResourceAtribute : ICloneable, ICompactSerializable
    {
        string _name;
        long _minThreshold;
        long _maxThreshold;
        int _duration;

        public ResourceAtribute() { }

        [ConfigurationAttribute("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [ConfigurationAttribute("min-threshold")]
        public long MinThreshold
        {
            get { return _minThreshold; }
            set { _minThreshold = value; }
        }

        [ConfigurationAttribute("max-threshold")]
        public long MaxThreshold
        {
            get { return _maxThreshold; }
            set { _maxThreshold = value; }
        }

        [ConfigurationAttribute("duration")]
        public int Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            ResourceAtribute resourceAttribute = new ResourceAtribute();

            resourceAttribute.Name = Name;
            resourceAttribute.MaxThreshold = MaxThreshold;
            resourceAttribute.MinThreshold = MinThreshold;
            resourceAttribute.Duration = Duration;

            return resourceAttribute;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _name = reader.ReadObject() as string;
            _minThreshold = reader.ReadInt64();
            _maxThreshold = reader.ReadInt64();
            _duration = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_name);
            writer.WriteObject(_minThreshold);
            writer.WriteObject(_maxThreshold);
            writer.WriteObject(_duration);
        }

        #endregion
    }
}
