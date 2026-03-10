using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class CounterMetadata : ICompactSerializable
    {
      
        public string Name { get; set; }
        public CounterType Type { get; set; }
        public string Description { get; set; }
        public Publisher Category { get; set; }
       // public int MappingID { get; set; }

        public void Deserialize(CompactReader reader)
        {
            Name = reader.ReadObject() as string;
            Type = (CounterType)reader.ReadInt32();
            Description = reader.ReadObject() as string;
            Category = (Publisher)reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Name);
            writer.Write((int)Type);
            writer.WriteObject(Description);
            writer.Write((int)Category);
        }
    }
}
