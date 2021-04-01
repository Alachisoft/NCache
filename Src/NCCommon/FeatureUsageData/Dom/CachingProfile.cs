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
    public class CachingProfile: ICloneable, ICompactSerializable
    {
        public CachingProfile()
        {
        }

        [ConfigurationSection("topology")]
        public Topology[] Topologies { get; set; }


        public object Clone()
        {
            CachingProfile topologies = new CachingProfile
            {
                Topologies = Topologies
            };
            return topologies;
        }

        public void Deserialize(CompactReader reader)
        {
            Topologies = reader.ReadObject() as Topology[];
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Topologies);
        }
    }
}
