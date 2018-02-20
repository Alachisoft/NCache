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
// $Id: TOTAL.java,v 1.6 2004/07/05 14:17:16 belaban Exp $
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.IO;

namespace Alachisoft.NGroups.Protocols
{
    /// <summary> The header processed by the TOTAL layer and intended for TOTAL
    /// inter-stack communication
    /// </summary>
    [Serializable]
    internal class HDR : Alachisoft.NGroups.Header, ICompactSerializable, IRentableObject
    {
        // HDR types
        /// <summary>Null value for the IDs </summary>
        private const long NULL_ID = -1;
        /// <summary>Null value for the tag </summary>
        public const byte NULL_TYPE = 0;
        /// <summary>Request to broadcast by the source </summary>
        public const byte REQ = 1;
        /// <summary>Reply to broadcast request. </summary>
        public const byte REP = 2;
        /// <summary>Unicast message </summary>
        public const byte UCAST = 3;
        /// <summary>Broadcast Message </summary>
        public const byte BCAST = 4;
        /// <summary>Multicast Message</summary>
        public const byte MCAST = 5;
        /// <summary>Request to multicast by the source.</summary>
        public const byte REQMCAST = 6;
        /// <summary>Reply to a multicast request.</summary>
        public const byte REPMCAST = 7;

        public int rentId;
        /// <summary>The header's type tag </summary>
        public byte type;
        /// <summary> The ID used by the message source to match replies from the
        /// sequencer
        /// </summary>
        public long localSeqID;

        /// <summary>The ID imposing the total order of messages </summary>
        public long seqID;

        public int viewId;

        /// <summary> used for externalization</summary>
        public HDR()
        {
        }
        /// <summary> Create a header for the TOTAL layer</summary>
        /// <param name="type">the header's type
        /// </param>
        /// <param name="localSeqID">the ID used by the sender of broadcasts to match
        /// requests with replies from the sequencer
        /// </param>
        /// <param name="seqID">the ID imposing the total order of messages
        /// 
        /// </param>
        /// <throws>  IllegalArgumentException if the provided header type is unknown</throws>
        /// <summary>
        /// </summary>
        public HDR(byte type, long localSeqID, long seqID, int viewId)
            : base()
        {
            switch (type)
            {
                case REQ:
                case REP:
                case UCAST:
                case BCAST:
                case MCAST:
                case REQMCAST:
                case REPMCAST: this.type = type; break;

                default:
                    this.type = NULL_TYPE;
                    throw new System.ArgumentException("Invalid header type.");
            }
            this.localSeqID = localSeqID;
            this.seqID = seqID;
            this.viewId = viewId;
        }
        public override object Clone(ObjectProvider provider)
        {
            HDR hdr = null;
            if (provider != null)
                hdr = (HDR)provider.RentAnObject();
            else
                hdr = new HDR();
            hdr.type = this.type;
            hdr.seqID = seqID;
            hdr.localSeqID = localSeqID;
            hdr.viewId = viewId;
            return hdr;
        }
        /// <summary> For debugging purposes</summary>
        public override string ToString()
        {
            System.Text.StringBuilder buffer = new System.Text.StringBuilder();
            string typeName;
            buffer.Append("[TOTAL.HDR");
            switch (type)
            {
                case REQ: typeName = "REQ"; break;
                case REQMCAST: typeName = "REQMCAST"; break;
                case REP: typeName = "REP"; break;
                case REPMCAST: typeName = "REPMCAST"; break;
                case UCAST: typeName = "UCAST"; break;
                case BCAST: typeName = "BCAST"; break;
                case MCAST: typeName = "MCAST"; break;
                case NULL_TYPE: typeName = "NULL_TYPE"; break;
                default: typeName = ""; break;
            }
            buffer.Append(", type=" + typeName);
            buffer.Append(", " + "localID=" + localSeqID);
            buffer.Append(", " + "seqID=" + seqID);
            buffer.Append(", " + "viewId=" + viewId);
            buffer.Append(']');

            return (buffer.ToString());
        }

        public void Reset()
        {
            seqID = localSeqID = NULL_ID;
            type = NULL_TYPE;
        }
        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            type = reader.ReadByte();
            localSeqID = reader.ReadInt64();
            seqID = reader.ReadInt64();
            viewId = reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(type);
            writer.Write(localSeqID);
            writer.Write(seqID);
            writer.Write(viewId);
        }
        #endregion

        public static HDR ReadTotalHeader(CompactReader reader)
        {
            byte isNull = reader.ReadByte();
            if (isNull == 1)
                return null;
            HDR newHdr = new HDR();
            newHdr.Deserialize(reader);
            return newHdr;
        }

        public static void WriteTotalHeader(CompactWriter writer, HDR hdr)
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

        public void DeserializeLocal(BinaryReader reader)
        {
            type = reader.ReadByte();
            localSeqID = reader.ReadInt64();
            seqID = reader.ReadInt64();
            viewId = reader.ReadInt32();
        }

        public void SerializeLocal(BinaryWriter writer)
        {
            writer.Write(type);
            writer.Write(localSeqID);
            writer.Write(seqID);
            writer.Write(viewId);
        }

        #endregion
    }


}
