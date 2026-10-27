using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public struct PerfmonCounterDetails : ICompactSerializable
    {
        public string Category { get; set; }
        public string Instance { get; set; }
        public string Counter { get; set; }
        public double Value { get; set; }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            Category = reader.ReadObject() as string;
            Instance = reader.ReadObject() as string;
            Counter = reader.ReadObject() as string;
            Value = reader.ReadDouble();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(Category);
            writer.WriteObject(Instance);
            writer.WriteObject(Counter);
            writer.Write(Value);
        }
    }
}
