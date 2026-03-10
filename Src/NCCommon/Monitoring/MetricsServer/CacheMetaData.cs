using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class CacheMetaData: ICompactSerializable
    {
        public CacheIdentifier Identifier { get; set; }
        public string Topology { get; set; }
        public int ConfiguredServersCount { get; set; } 
        public string ClusterIP { get; set; }
        public string ClientServerIP { get; set; }
        public string ClientServerPort { get; set; }
        public string SessionID { get; set; } //We may remove it
        public bool FromReplica { get; set; }
        public long CacheSize { get; set; }
        public string ConfiguredServers { get; set; }
        public string InstallationType { get; set; }

        public void Deserialize(CompactReader reader)
        {
            Identifier = reader.ReadObject() as CacheIdentifier;
            Topology = reader.ReadObject() as string;
            ConfiguredServersCount = reader.ReadInt32();
            ClusterIP = reader.ReadObject() as string;
            ClientServerIP = reader.ReadObject() as string;
            ClientServerPort = reader.ReadObject() as string;
            SessionID = reader.ReadObject() as string;
            FromReplica = reader.ReadBoolean();
            CacheSize = reader.ReadInt64();
            ConfiguredServers = reader.ReadObject() as string;
            InstallationType = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Identifier);
            writer.WriteObject(Topology);
            writer.Write(ConfiguredServersCount);
            writer.WriteObject(ClusterIP);
            writer.WriteObject(ClientServerIP);
            writer.WriteObject(ClientServerPort);
            writer.WriteObject(SessionID);
            writer.Write(FromReplica);
            writer.Write(CacheSize);
            writer.WriteObject(ConfiguredServers);
            writer.WriteObject(InstallationType);
        }

    }
}
