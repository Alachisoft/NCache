using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NGroups.Blocks
{
    public class ConnectInfo : ICompactSerializable
    {
        public const byte CONNECT_FIRST_TIME = 1;
        public const byte RECONNECTING =2;
       

        

        byte _connectStatus = CONNECT_FIRST_TIME;
        int _id;

        public ConnectInfo()
        {
            _connectStatus = CONNECT_FIRST_TIME;
        }
        public ConnectInfo(byte connectStatus,int id)
        {
            _connectStatus = connectStatus;
            _id = id;
        }

        
        public byte ConnectStatus
        {
            get { return _connectStatus; }
            set { _connectStatus = value; }
        }

        
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public override string ToString()
        {
            string str = "[";

            str += _id;
            str += (_connectStatus == CONNECT_FIRST_TIME ? "CONNECT_FIRST_TIME" : "RECONNECTING");
            return str;
        }
        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _connectStatus = reader.ReadByte();
            _id = reader.ReadInt32();
            
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_connectStatus);
            writer.Write(_id);
           
        }

        #endregion
    }
}