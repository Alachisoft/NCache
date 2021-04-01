using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class ClientProfile : ICompactSerializable
    {

        public ClientProfile()
        {
        }
        public string ClientId { get; set; }
        public string IpAddress { get; set; }
        public string Mac { get; set; }
        public int Cores { get; set; }
        public string EditionID { get; set; }
        public string Version { get; set; }
        public string Install_type { get; set; }
        public int Memory { get; set; }
        public string OperatingSystem { get; set; }
        public string Platform { get; set; }

        #region ICompactSerializeable
        public void Deserialize(CompactReader reader)
        {
            IpAddress = reader.ReadObject() as string;
            Mac = reader.ReadObject() as string;
            Cores = reader.ReadInt32();
            ClientId = reader.ReadObject() as string;
            EditionID = reader.ReadObject() as string;
            Version = reader.ReadObject() as string;
            Install_type = reader.ReadObject() as string;
            Memory = reader.ReadInt32();
            OperatingSystem = reader.ReadObject() as string;
            Platform = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(IpAddress);
            writer.WriteObject(Mac);
            writer.Write(Cores);
            writer.WriteObject(ClientId);
            writer.WriteObject(EditionID);
            writer.WriteObject(Version);
            writer.WriteObject(Install_type);
            writer.Write(Memory);
            writer.WriteObject(OperatingSystem);
            writer.WriteObject(Platform);

        }
        #endregion
    }
}