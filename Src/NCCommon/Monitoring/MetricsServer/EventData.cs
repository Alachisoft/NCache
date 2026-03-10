using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Alachisoft.NCache.Common.Monitoring
{
   
    public class EventData : ICompactSerializable
    {
        public Publisher Publisher { get; set; }
        public string Version { get; set; }
        public long EventId { get; set; }
        public EventsLevel Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool FromReplica { get; set; }

        public void Deserialize(CompactReader reader)
        {
            EventId = reader.ReadInt64();
            Level = (EventsLevel)reader.ReadObject();
            Source = reader.ReadObject() as string;
             Message = reader.ReadObject() as string;
            Timestamp = reader.ReadDateTime();
            Version = reader.ReadObject() as string;
            Publisher = (Publisher)reader.ReadObject();
            FromReplica = reader.ReadBoolean();

        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(EventId);
            writer.WriteObject(Level);
            writer.WriteObject(Source);
            writer.WriteObject(Message);
            writer.Write(Timestamp);
            writer.WriteObject(Version);
            writer.WriteObject(Publisher);
            writer.Write(FromReplica);
        }
    }
}
