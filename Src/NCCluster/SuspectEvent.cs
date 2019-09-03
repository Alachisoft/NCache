// $Id: SuspectEvent.java,v 1.1.1.1 2003/09/09 01:24:08 belaban Exp $
using System;

namespace Alachisoft.NGroups
{
	
	internal class SuspectEvent
	{
		virtual public object Member
		{
			get
			{
				return suspected_mbr;
			}
			
		}
		internal object suspected_mbr;
		
		public SuspectEvent(object suspected_mbr)
		{
			this.suspected_mbr = suspected_mbr;
		}
		public override string ToString()
		{
			return "SuspectEvent";
		}
	}
}