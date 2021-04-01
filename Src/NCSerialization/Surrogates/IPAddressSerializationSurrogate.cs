using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    class IPAddressSerializationSurrogate : SerializationSurrogate
    {
        public IPAddressSerializationSurrogate() : base(typeof(IPAddress), null) { }
        public override object Read(CompactBinaryReader reader)
        {
            string ip = reader.ReadString();
            return IPAddress.Parse(ip);
        }

        public override void Skip(CompactBinaryReader reader)
        {
            reader.SkipString();
        }

        public override void Write(CompactBinaryWriter writer, object graph)
        {
            IPAddress iPAddress = (IPAddress)graph;
            string ip = iPAddress.ToString();
            writer.Write(ip);
        }
    }
}
