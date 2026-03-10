using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Management.Management
{
    public class MemoryDumpMetainfo : ICompactSerializable
    {
        public int DumpProcessId
        {
            get;
            set;
        }
        public string FileName
        {
            get;
            set;
        }

        #region ICompactSerializeable

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            FileName = (string)reader.ReadObject();
            DumpProcessId = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(FileName);
            writer.Write(DumpProcessId);

        }

        #endregion
    }
}
