using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Management.Management
{
    public class FileMetaInfo: ICompactSerializable
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public string DateCreated { get; set; }

        #region ICompactSerializeable
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            FileName = (string)reader.ReadObject();
            DateCreated = (string)reader.ReadObject();
            Size = reader.ReadInt64();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(FileName);
            writer.WriteObject(DateCreated);
            writer.Write(Size);
        }
        #endregion
    }
}
