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
    public class ClientProfileDom : ICloneable, ICompactSerializable
    {
        public ClientProfileDom()
        {
        }

        [ConfigurationAttribute("maximum-connected-clients")]
        public long MaximumConnectedClients { get; set; }

        [ConfigurationAttribute("max-cores")]
        public long MaximumCores { get; set; }

        [ConfigurationAttribute("min-cores")]
        public long MinimumCores { get; set; }

        [ConfigurationAttribute("max-memory")]
        public long MaximumMemory { get; set; }

        [ConfigurationAttribute("min-memory")]
        public long MinimumMemory { get; set; }

        [ConfigurationAttribute("os")]
        public string OperatingSystem { set; get; }

        [ConfigurationAttribute("platform")]
        public string Platform { set; get; }

        public static string ListToStringConvert(List<string> value)
        {
            if (value.Count <= 0)
                return "";

            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < value.Count - 1; i++)
            {
                var item = value[i];
                if (String.IsNullOrEmpty(item)) continue;
                stringBuilder.Append(item).Append(",");
            }

            stringBuilder.Append(value.Last<string>());

            return stringBuilder.ToString();
        }

        public static List<string> StringToList(string value)
        {
            if (String.IsNullOrEmpty(value))
                return new List<string>();

            return value.Split(',').ToList<string>();
        }

        public object Clone()
        {
            ClientProfileDom clientProfile = new ClientProfileDom();
            clientProfile.MinimumCores = MinimumCores;
            clientProfile.MinimumMemory = MinimumMemory;
            clientProfile.MaximumMemory = MaximumMemory;
            clientProfile.MaximumCores = MaximumCores;
            clientProfile.MaximumConnectedClients = MaximumConnectedClients;
            clientProfile.OperatingSystem = OperatingSystem;
            clientProfile.Platform = Platform;
            return clientProfile;
        }

        public void Deserialize(CompactReader reader)
        {
            MinimumCores = reader.ReadInt64();
            MaximumCores = reader.ReadInt64();
            MinimumMemory = reader.ReadInt64();
            MaximumMemory = reader.ReadInt64();
            MaximumConnectedClients = reader.ReadInt64();
            OperatingSystem = reader.ReadObject() as string;
            Platform = reader.ReadObject() as string;

        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(MinimumCores);
            writer.Write(MaximumCores);
            writer.Write(MinimumMemory);
            writer.Write(MaximumMemory);
            writer.Write(MaximumConnectedClients);
            writer.WriteObject(OperatingSystem);
            writer.WriteObject(Platform);
        }
    }
}
