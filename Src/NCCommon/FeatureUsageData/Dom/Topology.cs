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
    public class Topology : ICloneable, ICompactSerializable
    {
        private string _name;
        private int _noOfCaches;
        private int _maxClusterSize;

        public Topology()
        {
        }

        [ConfigurationAttribute("name")]
        public string Name
        {
            set { _name = value; }
            get { return _name; }
        }

        [ConfigurationAttribute("no-of-caches")]
        public int NoOfCaches
        {
            set { _noOfCaches = value; }
            get { return _noOfCaches; }
        }


        [ConfigurationAttribute("max-cluster-size")]
        public int MaxClusterSize
        {
            get { return _maxClusterSize; }
            set { _maxClusterSize = value; }
        }


        public object Clone()
        {
            Topology topologies = new Topology();
            topologies.Name = Name;
            topologies.NoOfCaches = NoOfCaches;
            topologies.MaxClusterSize = MaxClusterSize;
            return topologies;
        }

        public void Deserialize(CompactReader reader)
        {
            _name = reader.ReadObject() as string;
            _noOfCaches = reader.ReadInt32();
            _maxClusterSize = reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_name);
            writer.WriteObject(_noOfCaches);
            writer.WriteObject(_maxClusterSize);
        }
    }
}
