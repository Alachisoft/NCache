// $Id: RequestHandler.java,v 1.1.1.1 2003/09/09 01:24:08 belaban Exp $
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
