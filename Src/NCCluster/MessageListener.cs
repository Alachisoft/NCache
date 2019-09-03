using System;

namespace Alachisoft.NGroups
{
	public interface MessageListener
	{
		void  receive(Message msg);
	}
}
