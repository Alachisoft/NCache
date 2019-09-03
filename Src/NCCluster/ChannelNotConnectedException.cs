// $Id: ChannelClosedException.java,v 1.2 2003/11/27 21:36:46 belaban Exp $
using System;

namespace Alachisoft.NGroups
{
	[Serializable]
	internal class ChannelNotConnectedException:ChannelException
	{
		
		public ChannelNotConnectedException()
		{
		}
		
		public ChannelNotConnectedException(string reason):base(reason)
		{
		}
		
		public override string ToString()
		{
			return "ChannelNotConnectedException";
		}
	}
}