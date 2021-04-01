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