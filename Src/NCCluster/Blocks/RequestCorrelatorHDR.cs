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
// $Id: RequestCorrelator.java,v 1.12 2004/09/05 04:54:21 ovidiuf Exp $
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.IO;

namespace Alachisoft.NGroups.Blocks
{

    /// <summary> The header for <tt>RequestCorrelator</tt> messages</summary>
    [Serializable]
    internal class RequestCorrelatorHDR : Alachisoft.NGroups.Header, ICompactSerializable, IRentableObject
    {
        public const byte REQ = 0;
        public const byte RSP = 1;
        public const byte GET_REQ_STATUS = 3;
        public const byte GET_REQ_STATUS_RSP = 4;
        public const byte NHOP_REQ = 5;
        public const byte NHOP_RSP = 6;

        public int rentid;
        /// <summary>Type of header: request or reply </summary>
        public byte type = REQ;

        /// <summary> The id of this request to distinguish among other requests from
        /// the same <tt>RequestCorrelator</tt>
        /// </summary>
        public long id = 0;

        /// <summary>msg is synchronous if true </summary>
        public bool rsp_expected = true;

        /// <summary>The unique name of the associated <tt>RequestCorrelator</tt> </summary>
        //public string name = null;

        /// <summary>Contains senders (e.g. P --> Q --> R) </summary>
        public System.Collections.ArrayList call_stack = null;

        /// <summary>Contains a list of members who should receive the request (others will drop). Ignored if null </summary>
        public System.Collections.ArrayList dest_mbrs = null;

        public bool serializeFlag = true;
        public RequestStatus reqStatus;
        public long status_reqId;
        public Address whomToReply;
        public Address expectResponseFrom;

        public bool doProcess = true;
        /// <summary> Used for externalization</summary>
        public RequestCorrelatorHDR()
        {
        }

        /// <param name="type">type of header (<tt>REQ</tt>/<tt>RSP</tt>)
        /// </param>
        /// <param name="id">id of this header relative to ids of other requests
        /// originating from the same correlator
        /// </param>
        /// <param name="rsp_expected">whether it's a sync or async request
        /// </param>
        /// <param name="name">the name of the <tt>RequestCorrelator</tt> from which
        /// this header originates
        /// </param>
        public RequestCorrelatorHDR(byte type, long id, bool rsp_expected, string name)
        {
            this.type = type;
            this.id = id;
            this.rsp_expected = rsp_expected;
            //this.name = name;
        }

        /// <param name="type">type of header (<tt>REQ</tt>/<tt>RSP</tt>)
        /// </param>
        /// <param name="id">id of this header relative to ids of other requests
        /// originating from the same correlator
        /// </param>
        /// <param name="rsp_expected">whether it's a sync or async request
        /// </param>
        /// <param name="name">the name of the <tt>RequestCorrelator</tt> from which
        /// this header originates
        /// <param name="apptimeTaken">Time taken to complete an operation by the receiving application.</param>
        /// </param>
        public RequestCorrelatorHDR(byte type, long id, bool rsp_expected, string name, long apptimeTaken)
        {
            this.type = type;
            this.id = id;
            this.rsp_expected = rsp_expected;
        }

        public override string ToString()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder();
            string typeStr = "<unknown>";
            switch (type)
            {
                case REQ:
                    typeStr = "REQ";
                    break;

                case RSP:
                    typeStr = "RSP";
                    break;

                case GET_REQ_STATUS:
                    typeStr = "GET_REQ_STATUS";
                    break;

                case GET_REQ_STATUS_RSP:
                    typeStr = "GET_REQ_STATUS_RSP";
                    break;


            }
            ret.Append(typeStr);
            ret.Append(", id=" + id);
            ret.Append(", rsp_expected=" + rsp_expected + ']');
            if (dest_mbrs != null)
                ret.Append(", dest_mbrs=").Append(dest_mbrs);
            return ret.ToString();
        }
        public void DeserializeLocal(BinaryReader reader)
        {
            type = reader.ReadByte();
            id = reader.ReadInt64();
            rsp_expected = reader.ReadBoolean();
            doProcess = reader.ReadBoolean();

            bool getWhomToReply = reader.ReadBoolean();

            if (getWhomToReply)
            {
                this.whomToReply = new Address();
                this.whomToReply.DeserializeLocal(reader);
            }

            bool getExpectResponseFrom = reader.ReadBoolean();
            if (getExpectResponseFrom)
            {
                this.expectResponseFrom = new Address();
                this.expectResponseFrom.DeserializeLocal(reader);
            }
        }

        public void SerializeLocal(BinaryWriter writer)
        {
            writer.Write(type);
            writer.Write(id);
            writer.Write(rsp_expected);
            writer.Write(doProcess);

            if (whomToReply != null)
            {
                writer.Write(true);
                whomToReply.SerializeLocal(writer);
            }
            else
                writer.Write(false);

            if (expectResponseFrom != null)
            {
                writer.Write(true);
                expectResponseFrom.SerializeLocal(writer);
            }
            else
                writer.Write(false);
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            type = reader.ReadByte();
            id = reader.ReadInt64();
            rsp_expected = reader.ReadBoolean();
            reqStatus = reader.ReadObject() as RequestStatus;
            status_reqId = reader.ReadInt64();
            //name = reader.ReadString();
            //call_stack = (System.Collections.ArrayList)reader.ReadObject();
            //byte[] arr = (byte[])reader.ReadObject();
            //dest_mbrs = arr != null ?(System.Collections.IList)CompactBinaryFormatter.FromByteBuffer(arr, null): null;
            //dest_mbrs = (System.Collections.IList)reader.ReadObject();
            dest_mbrs = (System.Collections.ArrayList)reader.ReadObject();
            doProcess = reader.ReadBoolean();
            whomToReply = (Address)reader.ReadObject();
            expectResponseFrom = (Address)reader.ReadObject();

        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(type);
            writer.Write(id);
            writer.Write(rsp_expected);
            writer.WriteObject(reqStatus);
            writer.Write(status_reqId);
            //writer.Write(name);
            //writer.WriteObject(call_stack);
            //byte[] s = dest_mbrs != null? CompactBinaryFormatter.ToByteBuffer(dest_mbrs, null): null;
            //if (s != null)
            //    NCacheLog.Error(_cacheName, "RequestCorrelator.Hdr", "dest-add : " + s.Length.ToString());
            if (serializeFlag)
                writer.WriteObject(dest_mbrs);
            else
                writer.WriteObject(null);

            writer.Write(doProcess);
            writer.WriteObject(whomToReply);
            writer.WriteObject(expectResponseFrom);

        }

        public static RequestCorrelatorHDR ReadCorHeader(CompactReader reader)
        {
            byte isNull = reader.ReadByte();
            if (isNull == 1)
                return null;
            RequestCorrelatorHDR newHdr = new RequestCorrelatorHDR();
            newHdr.Deserialize(reader);
            return newHdr;
        }

        public static void WriteCorHeader(CompactWriter writer, RequestCorrelatorHDR hdr)
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


        public void Reset()
        {
            dest_mbrs = call_stack = null;
            doProcess = rsp_expected = true;

            type = RequestCorrelatorHDR.REQ;
        }
        #endregion

        #region IRentableObject Members

        public int RentId
        {
            get
            {
                return rentid;
            }
            set
            {
                rentid = value;
            }
        }

        #endregion
    }
}
