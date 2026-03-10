
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class CounterMetadataCollection : ICompactSerializable
    {
        public Publisher Category { get; set; }
        public List<CounterMetadata> Counters { get; set; }
        public string Version { get; set; }
        public bool FromReplica { get; set; }

        public string InstanceName { get; set; }
        /// <summary>
        /// This is to be used for bridge caches. UniqueCacheId consists of cache name + alias (Cache complete name)
        /// </summary>
        public string CacheUniqueID { get; set; }

        public string CacheConfigID { get; set; }

        public void Deserialize(CompactReader reader)
        {

            Category = (Publisher)reader.ReadInt32();
            Counters=SerializationUtility.DeserializeList<CounterMetadata>(reader);
            Version = reader.ReadObject() as string;
            FromReplica = reader.ReadBoolean();
            InstanceName = reader.ReadObject() as string;
            CacheUniqueID = reader.ReadObject() as string;
            CacheConfigID = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write((int)Category);
            SerializationUtility.SerializeList(Counters, writer);
            writer.WriteObject(Version);
            writer.Write(FromReplica);
            writer.WriteObject(InstanceName);
            writer.WriteObject(CacheUniqueID);
            writer.WriteObject(CacheConfigID);
        }
    }
}
