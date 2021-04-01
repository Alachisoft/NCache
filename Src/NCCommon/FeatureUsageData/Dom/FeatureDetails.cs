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
    public class FeatureDetails : ICompactSerializable, ICloneable
    {
        private Feature[] _features;

        public FeatureDetails()
        { }

        [ConfigurationSection("feature")]
        public Feature[] Features
        {
            get { return _features; }
            set { _features = value; }
        }


        public object Clone()
        {
            FeatureDetails featureDetails = new FeatureDetails();
            featureDetails.Features = Features;
            return featureDetails;
        }

        public void Deserialize(CompactReader reader)
        {
            _features = reader.ReadObject() as Feature[];
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_features);
        }
    }
}
