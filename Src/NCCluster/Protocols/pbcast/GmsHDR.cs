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
// $Id: GMS.java,v 1.17 2004/09/03 12:28:04 belaban Exp $
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections;

namespace Alachisoft.NGroups.Protocols.pbcast
{
    internal class GmsHDR : Header, ICompactSerializable
    {
        public const byte JOIN_REQ = 1;
        public const byte JOIN_RSP = 2;
        public const byte LEAVE_REQ = 3;
        public const byte LEAVE_RSP = 4;
        public const byte VIEW = 5;
        public const byte MERGE_REQ = 6;
        public const byte MERGE_RSP = 7;
        public const byte INSTALL_MERGE_VIEW = 8;
        public const byte CANCEL_MERGE = 9;
        public const byte CAN_NOT_CONNECT_TO = 10;
        public const byte LEAVE_CLUSTER = 11;
        public const byte CONNECTION_BROKEN = 12;
        public const byte CONNECTED_NODES_REQUEST = 13;
        public const byte CONNECTED_NODES_RESPONSE = 14;
        public const byte VIEW_REJECTED = 15;
        public const byte INFORM_NODE_REJOINING = 16;
        public const byte RESET_ON_NODE_REJOINING = 17;
        public const byte RE_CHECK_CLUSTER_HEALTH = 18;
        public const byte VIEW_RESPONSE = 19;
        public const byte SPECIAL_JOIN_REQUEST = 20;
        public const byte INFORM_ABOUT_NODE_DEATH = 21;
        public const byte IS_NODE_IN_STATE_TRANSFER = 22;
        public const byte IS_NODE_IN_STATE_TRANSFER_RSP = 23;

        internal byte type = 0;
        internal View view = null; // used when type=VIEW or MERGE_RSP or INSTALL_MERGE_VIEW
        internal Address mbr = null; // used when type=JOIN_REQ or LEAVE_REQ
        internal JoinRsp join_rsp = null; // used when type=JOIN_RSP
        internal Digest digest = null; // used when type=MERGE_RSP or INSTALL_MERGE_VIEW
        internal object merge_id = null; // used when type=MERGE_REQ or MERGE_RSP or INSTALL_MERGE_VIEW or CANCEL_MERGE
        internal bool merge_rejected = false; // used when type=MERGE_RSP
        internal string subGroup_name = null; // to identify the subgroup of the current member.
        internal ArrayList nodeList;//nodes to which this can not establish the connection.
        internal object arg;
        internal string gms_id;

        public GmsHDR()
        {
        } // used for Externalization

        public GmsHDR(byte type)
        {
            this.type = type;
        }

        public GmsHDR(byte type, object argument)
        {
            this.type = type;
            this.arg = argument;
        }

        /// <summary>Used for VIEW header </summary>
        public GmsHDR(byte type, View view)
        {
            this.type = type;
            this.view = view;
        }


        /// <summary>Used for JOIN_REQ or LEAVE_REQ header </summary>
        public GmsHDR(byte type, Address mbr)
        {
            this.type = type;
            this.mbr = mbr;
        }

        /// <summary>Used for JOIN_REQ or LEAVE_REQ header </summary>
        public GmsHDR(byte type, Address mbr, string subGroup_name)
        {
            this.type = type;
            this.mbr = mbr;
            this.subGroup_name = subGroup_name;
        }


        /// <summary>Used for JOIN_RSP header </summary>
        public GmsHDR(byte type, JoinRsp join_rsp)
        {
            this.type = type;
            this.join_rsp = join_rsp;
        }

        public string GMSId
        {
            get { return gms_id; }
            set { gms_id = value; }
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("HDR");
            sb.Append('[' + type2String(type) + ']');
            switch (type)
            {


                case JOIN_REQ:
                    sb.Append(": mbr=" + mbr);
                    break;

                case SPECIAL_JOIN_REQUEST:
                    sb.Append(": mbr=" + mbr);
                    break;

                case RE_CHECK_CLUSTER_HEALTH:
                    sb.Append(": mbr=" + mbr);
                    break;

                case JOIN_RSP:
                    sb.Append(": join_rsp=" + join_rsp);
                    break;


                case LEAVE_REQ:
                    sb.Append(": mbr=" + mbr);
                    break;


                case LEAVE_RSP:
                    break;


                case VIEW:
                    sb.Append(": view=" + view);
                    break;


                case MERGE_REQ:
                    sb.Append(": merge_id=" + merge_id);
                    break;


                case MERGE_RSP:
                    sb.Append(": view=" + view + ", digest=" + digest + ", merge_rejected=" + merge_rejected + ", merge_id=" + merge_id);
                    break;


                case INSTALL_MERGE_VIEW:
                    sb.Append(": view=" + view + ", digest=" + digest);
                    break;


                case CANCEL_MERGE:
                    sb.Append(", <merge cancelled>, merge_id=" + merge_id);
                    break;

                case CONNECTION_BROKEN:
                    sb.Append("<suspected member : " + mbr + " >");
                    break;

                case VIEW_REJECTED:
                    sb.Append("<rejected by : " + mbr + " >");

                    break;

                case INFORM_NODE_REJOINING:
                    sb.Append("INFORM_NODE_REJOINING");
                    break;

                case RESET_ON_NODE_REJOINING:
                    sb.Append("RESET_ON_NODE_REJOINING");
                    break;

                case VIEW_RESPONSE:
                    sb.Append("VIEW_RESPONSE");
                    break;

                case IS_NODE_IN_STATE_TRANSFER:
                    sb.Append("IS_NODE_IN_STATE_TRANSFER");
                    break;

                case IS_NODE_IN_STATE_TRANSFER_RSP:
                    sb.Append("IS_NODE_IN_STATE_TRANSFER_RSP->" + arg);
                    break;

                case INFORM_ABOUT_NODE_DEATH:
                    sb.Append("INFORM_ABOUT_NODE_DEATH (" + arg + ")");
                    break;
            }
            sb.Append('\n');
            return sb.ToString();
        }


        public static string type2String(int type)
        {
            switch (type)
            {

                case JOIN_REQ:
                    return "JOIN_REQ";

                case SPECIAL_JOIN_REQUEST:
                    return "SPECIAL_JOIN_REQUEST";

                case JOIN_RSP:
                    return "JOIN_RSP";

                case LEAVE_REQ:
                    return "LEAVE_REQ";

                case LEAVE_RSP:
                    return "LEAVE_RSP";

                case VIEW:
                    return "VIEW";

                case MERGE_REQ:
                    return "MERGE_REQ";

                case MERGE_RSP:
                    return "MERGE_RSP";

                case INSTALL_MERGE_VIEW:
                    return "INSTALL_MERGE_VIEW";

                case CANCEL_MERGE:
                    return "CANCEL_MERGE";

                case CAN_NOT_CONNECT_TO:
                    return "CAN_NOT_CONNECT_TO";

                case LEAVE_CLUSTER:
                    return "LEAVE_CLUSTER";

                case CONNECTION_BROKEN:
                    return "CONNECTION_BROKEN";

                case CONNECTED_NODES_REQUEST:
                    return "CONNECTED_NODES_REQUEST";

                case CONNECTED_NODES_RESPONSE:
                    return "CONNECTED_NODES_RESPONSE";

                case VIEW_REJECTED:
                    return "VIEW_REJECTED";

                case RE_CHECK_CLUSTER_HEALTH:
                    return "RE_CHECK_CLUSTER_HEALTH";

                case INFORM_ABOUT_NODE_DEATH:
                    return "RE_CHECK_CLUSTER_HEALTH";

                case IS_NODE_IN_STATE_TRANSFER:
                    return "IS_NODE_IN_STATE_TRANSFER";

                case IS_NODE_IN_STATE_TRANSFER_RSP:
                    return "IS_NODE_IN_STATE_TRANSFER_RSP";


                default:
                    return "<unknown>";
            }
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            type = reader.ReadByte();
            view = View.ReadView(reader);
            mbr = Address.ReadAddress(reader);
            join_rsp = (JoinRsp)reader.ReadObject();
            digest = (Digest)reader.ReadObject();
            merge_id = reader.ReadObject();
            merge_rejected = reader.ReadBoolean();
            subGroup_name = reader.ReadString();
            nodeList = reader.ReadObject() as ArrayList;
            arg = reader.ReadObject();
            gms_id = reader.ReadObject() as string;
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.Write(type);
            View.WriteView(writer, view);
            Address.WriteAddress(writer, mbr);
            writer.WriteObject(join_rsp);
            writer.WriteObject(digest);
            writer.WriteObject(merge_id);
            writer.Write(merge_rejected);
            writer.Write(subGroup_name);
            writer.WriteObject(nodeList);
            writer.WriteObject(arg);
            writer.WriteObject(gms_id);
        }


        #endregion
    }
}
