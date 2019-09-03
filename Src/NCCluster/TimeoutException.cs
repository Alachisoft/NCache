// $Id: TimeoutException.java,v 1.1.1.1 2003/09/09 01:24:08 belaban Exp $
using System;

namespace Alachisoft.NGroups
{
	[Serializable]
	public class TimeoutException:System.Exception
	{
		public System.Collections.IList failed_mbrs = null; // members that failed responding
		
		public TimeoutException():base("TimeoutExeption")
		{
		}
		
		public TimeoutException(string msg):base(msg)
		{
		}
		
		public TimeoutException(System.Collections.IList failed_mbrs):base("TimeoutExeption")
		{
			this.failed_mbrs = failed_mbrs;
		}
		
		
		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			
			sb.Append(base.ToString());
			
			if (failed_mbrs != null && failed_mbrs.Count > 0)
				sb.Append(" (failed members: ").Append(failed_mbrs);
			return sb.ToString();
		}
	}
}