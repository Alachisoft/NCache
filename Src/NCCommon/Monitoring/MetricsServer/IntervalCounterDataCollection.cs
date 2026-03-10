using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class IntervalCounterDataCollection : ICompactSerializable
    {
        public IDictionary<short, double> Values { get; set; }
        public DateTime Timestamp { get; set; }
        public bool FromReplica { get; set; }
        public Publisher PublisherType { get; set; }
        public string InstanceName { get; set; }

        public void Deserialize(CompactReader reader)
        {
            Values = SerializationUtility.DeserializeDictionary<short, double>(reader);
            Timestamp = reader.ReadDateTime();
            FromReplica = reader.ReadBoolean();
            PublisherType = (Publisher)reader.ReadInt32();
            InstanceName = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            SerializationUtility.SerializeDictionary<short,double>(Values, writer);
            writer.Write(Timestamp);
            writer.Write(FromReplica);
            writer.Write((int)PublisherType);
            writer.WriteObject(InstanceName);
        }
    }
}
