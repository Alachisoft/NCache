
using System;
using Message = Alachisoft.NGroups.Message;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Blocks
{
	
	
	public interface RequestHandler
	{
		object handle(Message msg);

        object handleNHopRequest(Message msg, out Address destination, out Message replicationMsg);
	}
}
