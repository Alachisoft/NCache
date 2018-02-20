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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NGroups.Protocols
{
    public class TCPHearBeat : Header, ICompactSerializable
    {
        byte _type;

        public const byte SEND_HEART_BEAT = 1;
        public const byte HEART_BEAT = 2;
        public const byte ARE_YOU_ALIVE = 3;
        public const byte I_AM_NOT_DEAD = 4;
        public const byte I_AM_LEAVING = 5;
        public const byte I_AM_STARTING = 6;

        public TCPHearBeat() { }
        public TCPHearBeat(byte type)
        {
            _type = type;
        }

        public byte Type
        {
            get { return _type; }
            set { _type = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _type = reader.ReadByte();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_type);
        }

        #endregion

        public override string ToString()
        {
            string toString = "NA";
            switch (_type)
            {
                case SEND_HEART_BEAT:
                    toString = "SEND_HEART_BEAT";
                    break;

                case HEART_BEAT:
                    toString = "HEART_BEAT";
                    break;

                case ARE_YOU_ALIVE:
                    toString = "ARE_YOU_ALIVE";
                    break;

                case I_AM_NOT_DEAD:
                    toString = "I_AM_NOT_DEAD";
                    break;

                case I_AM_LEAVING:
                    toString = "I_AM_LEAVING";
                    break;

                case I_AM_STARTING:
                    toString = "I_AM_STARTING";
                    break;

            }
            return toString;
        }
    }
}
