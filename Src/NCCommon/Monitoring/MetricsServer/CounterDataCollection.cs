
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class CounterDataCollection : ICompactSerializable
    {
        public Publisher Category { get; set; }
        public string Version { get; set; }
        public string InstanceName { get; set; }

        public bool FromReplica { get; set; }
        /// <summary>
        /// This is to be used for bridge caches. UniqueCacheId consists of cache name + alias (Cache complete name)
        /// </summary>
        public string CacheUniqueID { get; set; }

        public List<IntervalCounterDataCollection> Values { get; set; }
        public IntervalCounterDataCollection SystemMetricValues { get; set; }

        public void AddCounterData(IntervalCounterDataCollection counterData)
        {

        }

        public void Merge(CounterDataCollection other)
        {

        }
        public void Deserialize(CompactReader reader)
        {
            Category = (Publisher)reader.ReadInt32();
            Version = reader.ReadObject() as string;
            Values = SerializationUtility.DeserializeList<IntervalCounterDataCollection>(reader);
            SystemMetricValues = reader.ReadObject() as IntervalCounterDataCollection;
            CacheUniqueID = reader.ReadObject() as string;
            InstanceName = reader.ReadObject() as string;
            FromReplica = reader.ReadBoolean();
        }
        public void Serialize(CompactWriter writer)
        {
            writer.Write((int)Category);
            writer.WriteObject(Version);
            SerializationUtility.SerializeList(Values, writer);
            writer.WriteObject(SystemMetricValues);
            writer.WriteObject(CacheUniqueID);
            writer.WriteObject(InstanceName);
            writer.Write(FromReplica);
        }
    }

    

}
