// $Id: SuspectedException.java,v 1.1.1.1 2003/09/09 01:24:08 belaban Exp $
using System;
namespace Alachisoft.NGroups
{
	[Serializable]
	public class SuspectedException:System.Exception
	{
		public object suspect = null;
		
		public SuspectedException()
		{
		}
		public SuspectedException(object suspect)
		{
			this.suspect = suspect;
		}
		
		public override string ToString()
		{
			return "SuspectedException";
		}
	}
}