using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class CacheConfigInfo : ICloneable, ICompactSerializable
    {

        public bool IsLocalCache { get; set; }
        public bool IsInproc { get; set; }
        public string InitialHostList { get; set; }
        public int TcpPort { get; set; }
        public string CacheType { get; set; }
        public object Clone()
        {
            CacheConfigInfo info = new CacheConfigInfo();

            info.IsLocalCache = this.IsLocalCache;
            info.IsInproc = this.IsInproc;
            info.InitialHostList = this.InitialHostList;
            info.TcpPort = this.TcpPort;
            info.CacheType = this.CacheType;
            return info;
        }

        public void Deserialize(CompactReader reader)
        {

            IsLocalCache = reader.ReadBoolean();
            IsInproc = reader.ReadBoolean();
            InitialHostList = reader.ReadObject() as String; ;
            TcpPort = reader.ReadInt32();
            CacheType = reader.ReadObject() as String;

        }

        public void Serialize(CompactWriter writer)
        {

            writer.Write(IsLocalCache);
            writer.Write(IsInproc);
            writer.WriteObject(InitialHostList);
            writer.Write(TcpPort);
            writer.WriteObject(CacheType);
        }
    }
}
