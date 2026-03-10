using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
   public class CacheIdentifier : ICompactSerializable
    {
        public string CacheId { get; set; }
        public string ConfigId { get; set; }

        public override bool Equals(object obj)
        {
            var isEqual = false;
            CacheIdentifier other = obj as CacheIdentifier;
            if (other != null)
            {
                isEqual = (this.ConfigId.Equals(other.ConfigId)) && (this.CacheId.Equals(other.CacheId));
            }
            return isEqual;
        }

        public override int GetHashCode()
        {
            if (CacheId != null && ConfigId != null)
                return (CacheId + ConfigId).GetHashCode();
            return base.GetHashCode();
        }

        public void Deserialize(CompactReader reader)
        {
            CacheId = reader.ReadObject() as string;
            ConfigId = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(CacheId);
            writer.WriteObject(ConfigId);
        }
    }
}
