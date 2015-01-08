// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.IO;
using System.Runtime.Serialization;



using Alachisoft.NCache.Runtime.Serialization.IO;


using Alachisoft.NCache.Runtime.Serialization;



using Alachisoft.NCache.Common;

namespace Alachisoft.NGroups.Protocols
{
	[Serializable]
	class TcpHeader : Header, ICompactSerializable, IRentableObject, ICustomSerializable
	{
		public System.String group_addr = null;
        private int rentId;

		public TcpHeader()
		{
		} // used for externalization
		
		public TcpHeader(System.String n)
		{
			group_addr = n;
		}
		
		public override System.String ToString()
		{
			return "[TCP:group_addr=" + group_addr + ']';
		}
        
		#region ICompactSerializable Members

		public void Deserialize(CompactReader reader)
		{
			group_addr = reader.ReadString();
		}

		public void Serialize(CompactWriter writer)
		{
			writer.Write(group_addr);
		}
        
        #endregion

        public override object Clone(ObjectProvider provider)
        {
            TcpHeader hdr = null;
            if (provider != null)
                hdr = (TcpHeader)provider.RentAnObject();
            else
                hdr = new TcpHeader();
            hdr.group_addr = group_addr;

            return hdr;
        }
        #region IRentableObject Members

        public int RentId
        {
            get
            {
                return rentId;
            }
            set
            {
                rentId = value;
            }
        }

        #endregion

        #region ICustomSerializable Members

        public static TcpHeader ReadTcpHeader(CompactReader reader)
        {
            byte isNull = reader.ReadByte();
            if (isNull == 1)
                return null;
            TcpHeader newHdr = new TcpHeader();
            newHdr.Deserialize(reader);
            return newHdr;
        }

        public static void WriteTcpHeader(CompactWriter writer, TcpHeader hdr)
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
        
        public void DeserializeLocal(BinaryReader reader)
        {
            group_addr = reader.ReadString();
        }

        public void SerializeLocal(BinaryWriter writer)
        {
            writer.Write(group_addr);
        }

        #endregion
    }
}
