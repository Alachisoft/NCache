using System;

namespace Alachisoft.NGroups
{
    [Serializable]
    internal class ChannelClosedException:ChannelException
    {
		
        public ChannelClosedException():base()
        {
        }
		
        public ChannelClosedException(string msg):base(msg)
        {
        }
		
        public override string ToString()
        {
            return "ChannelClosedException";
        }
    }
}