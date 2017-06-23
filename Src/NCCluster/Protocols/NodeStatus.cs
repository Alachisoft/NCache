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

using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Protocols
{
    internal class NodeStatus
    {
        Address _node;
        byte _status;
        public const byte IS_ALIVE = 1;
        public const byte IS_DEAD = 2;
        public const byte IS_LEAVING = 3;

        public NodeStatus(Address node, byte status)
        {
            _node = node;
            _status = status;
        }

        public Address Node
        {
            get { return _node; }
        }
        public byte Status
        {
            get { return _status; }
        }

        public override string ToString()
        {
            string toString = _node != null ? _node.ToString() + ":" : ":";
            switch (_status)
            {
                case IS_ALIVE: toString += "IS_ALIVE"; break;
                case IS_DEAD: toString += "IS_DEAD"; break;
                case IS_LEAVING: toString += "IS_LEAVING"; break;
                default: toString += "NA"; break;
            }
            return toString;
        }
    }
}
