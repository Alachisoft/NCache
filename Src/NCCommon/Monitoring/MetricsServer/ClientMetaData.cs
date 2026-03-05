using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
   public class ClientMetaData : ICompactSerializable
    {
        public string CacheName { get; set; }
        public string BindIP { get; set; }
        public string CacheConfigId { get; set; }

        public void Deserialize(CompactReader reader)
        {
            CacheName = reader.ReadObject() as string;
            BindIP = reader.ReadObject() as string;
            CacheConfigId = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(CacheName);
            writer.WriteObject(BindIP);
            writer.WriteObject(CacheConfigId);
        }
    }
}
