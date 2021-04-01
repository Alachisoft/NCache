using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.FeatureUsageData.Dom
{
    [Serializable]
    public class Feature : ICloneable, ICompactSerializable
    {
        private string _name;
        private string _lastUsedOn;
        private Feature[] _subfeatures;

        public Feature()
        {
        }

        [ConfigurationAttribute("name")]
        public string Name
        {
            set { _name = value; }
            get { return _name; }
        }

        [ConfigurationAttribute("last-used-on")]
        public string LastUsedOn
        {
            set { _lastUsedOn = value; }
            get { return _lastUsedOn; }
        }


        [ConfigurationAttribute("creation-time")]
        public string CreationTime { get; set; }


        [ConfigurationSection("feature")]
        public Feature[] Subfeatures
        {
            get { return _subfeatures; }
            set { _subfeatures = value; }
        }


        public object Clone()
        {
            Feature feature = new Feature
            {
                Name = Name,
                LastUsedOn = LastUsedOn,
                CreationTime = CreationTime,
                Subfeatures = Subfeatures
            };
            return feature;
        }

        public void Deserialize(CompactReader reader)
        {
            _name = reader.ReadObject() as string;
            _lastUsedOn = reader.ReadObject() as string;
            CreationTime = reader.ReadObject() as string;
            _subfeatures = reader.ReadObject() as Feature[];
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_name);
            writer.WriteObject(_lastUsedOn);
            writer.WriteObject(CreationTime);
            writer.WriteObject(_subfeatures);
        }
    }
}
