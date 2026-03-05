using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class LogsMetaData : ICompactSerializable
    {
      
        public LogLevel LogLevel { get; set; }
        public string Module { get; set; }
        public string Message { get; set; }
        public int ThreadId { get; set; }
        public string AppDomain { get; set; }

        public void Deserialize(CompactReader reader)
        {
            LogLevel = (LogLevel)reader.ReadInt32();
            Module = reader.ReadObject() as string;
            Message = reader.ReadObject() as string;
            ThreadId = reader.ReadInt32();
            AppDomain = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write((int)LogLevel);
            writer.WriteObject(Module);
            writer.WriteObject(Message);
            writer.Write(ThreadId);
            writer.Write(AppDomain);
        }
    }
}
