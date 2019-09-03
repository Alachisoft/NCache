using System;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NGroups.Protocols
{
	[Serializable]
	internal class PingHeader : Header, ICompactSerializable
	{
		public const byte GET_MBRS_REQ = 1; // arg = null
        public const byte GET_MBRS_RSP = 2; // arg = PingRsp(local_addr, coord_addr)
		public string group_addr = null;
		public byte[] userId = null;
		public byte[] password = null;

        public byte type = 0;
		public System.Object arg = null;
		
		public PingHeader()
		{
		} // for externalization
		
		public PingHeader(byte type, System.Object arg)
		{
			this.type = type;
			this.arg = arg;
		}
		public PingHeader(byte type, System.Object arg,string group_addr)
		{
			this.type = type;
			this.arg = arg;
			this.group_addr = group_addr;
		}
		
		public PingHeader(byte type, System.Object arg, string group_addr, byte[] userId, byte[] password)
		{
			this.type = type;
			this.arg = arg;
			this.group_addr = group_addr;
			this.userId = userId;
			this.password = password;
		}
		
		public override long size()
		{
			return 100;
		}
		
		public override System.String ToString()
		{
			return "[PING: type=" + type2Str(type) + ", arg=" + arg + ']';
		}
		
		internal virtual System.String type2Str(int t)
		{
			switch (t)
			{
				
				case GET_MBRS_REQ: 
					return "GET_MBRS_REQ";
				
				case GET_MBRS_RSP: 
					return "GET_MBRS_RSP";
				
				default: 
					return "<unkown type (" + t + ")>";
				
			}
		}

		#region ICompactSerializable Members

		public void Deserialize(CompactReader reader)
		{
			type = reader.ReadByte();
			group_addr = (string)reader.ReadObject();
            arg = reader.ReadObject();
			userId = (byte[])reader.ReadObject();
            password = (byte[])reader.ReadObject();
		}

		public void Serialize(CompactWriter writer)
		{
			writer.Write(type);
			writer.WriteObject((object)group_addr);
            writer.WriteObject(arg);
			writer.WriteObject(userId);
			writer.WriteObject(password);
		}

		#endregion

        public static PingHeader ReadPingHeader(CompactReader reader)
        {
            byte isNull = reader.ReadByte();
            if (isNull == 1)
                return null;
            PingHeader newHdr = new PingHeader();
            newHdr.Deserialize(reader);
            return newHdr;
        }

        public static void WritePingHeader(CompactWriter writer, PingHeader hdr)
        {
            byte isNull = 1;
            if (hdr == null)
                writer.Write(isNull);
            else
            {
                isNull = 0;
                writer.Write(isNull);
                hdr.Serialize(writer);
            }
            return;
        }  		    
	}
}
