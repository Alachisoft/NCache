// $Id: RspList.java,v 1.3 2004/03/30 06:47:28 belaban Exp $
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Util
{
    /// <summary> Contains responses from all members. Marks faulty members.
    /// A RspList is a response list used in peer-to-peer protocols.
    /// </summary>
    public class RspList
	{
		public object First
		{
			get
			{
				return rsps.Count > 0?((Rsp) rsps[0]).Value:null;
			}
			
		}
		/// <summary>Returns the results from non-suspected members that are not null. </summary>
		public System.Collections.ArrayList Results
		{
			get
			{
				System.Collections.ArrayList ret = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
				Rsp rsp;
				object val;
				
				for (int i = 0; i < rsps.Count; i++)
				{
					rsp = (Rsp) rsps[i];
					if (rsp.wasReceived() && (val = rsp.Value) != null)
						ret.Add(val);
				}
				return ret;
			}
			
		}
		public System.Collections.ArrayList SuspectedMembers
		{
			get
			{
				System.Collections.ArrayList retval = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
				Rsp rsp;
				
				for (int i = 0; i < rsps.Count; i++)
				{
					rsp = (Rsp) rsps[i];
					if (rsp.wasSuspected())
						retval.Add(rsp.Sender);
				}
				return retval;
			}
			
		}
		internal System.Collections.ArrayList rsps = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		
		
		public void  reset()
		{
			rsps.Clear();
		}
		

        public void addRsp(Address sender, object retval)

        {
            Rsp rsp = find(sender);

            if (rsp != null)
            {
                rsp.sender = sender; rsp.retval = retval; rsp.received = true; rsp.suspected = false;
                return;
            }
            rsp = new Rsp(sender, retval);
            rsps.Add(rsp);
        }


        public void  addNotReceived(Address sender)
		{
			Rsp rsp = find(sender);
			
			if (rsp == null)
				rsps.Add(new Rsp(sender));
		}
		
		
		
		public void  addSuspect(Address sender)
		{
			Rsp rsp = find(sender);
			
			if (rsp != null)
			{
				rsp.sender = sender; rsp.retval = null; rsp.received = false; rsp.suspected = true;
				return ;
			}
			rsps.Add(new Rsp(sender, true));
		}
		
		
		public bool isReceived(Address sender)
		{
			Rsp rsp = find(sender);
			
			if (rsp == null)
				return false;
			return rsp.received;
		}
		
		
		public int numSuspectedMembers()
		{
			int num = 0;
			Rsp rsp;
			
			for (int i = 0; i < rsps.Count; i++)
			{
				rsp = (Rsp) rsps[i];
				if (rsp.wasSuspected())
					num++;
			}
			return num;
		}
		
		
		public bool isSuspected(Address sender)
		{
			Rsp rsp = find(sender);
			
			if (rsp == null)
				return false;
			return rsp.suspected;
		}
		
		
		public object get(Address sender)
		{
			Rsp rsp = find(sender);
			
			if (rsp == null)
				return null;
			return rsp.retval;
		}
		
		
		public int size()
		{
			return rsps.Count;
		}
		
		public object elementAt(int i)
		{
			return rsps[i];
		}

        public void removeElementAt(int i)
        {
            rsps.RemoveAt(i);
        }

        public void removeRsp(Rsp r)
        {
            rsps.Remove(r);
        }
		
		public override string ToString()
		{
			System.Text.StringBuilder ret = new System.Text.StringBuilder();
			Rsp rsp;
			
			for (int i = 0; i < rsps.Count; i++)
			{
				rsp = (Rsp) rsps[i];
				ret.Append("[" + rsp + "]\n");
			}
			return ret.ToString();
		}
		bool contains(Address sender)
		{
			Rsp rsp;
			
			for (int i = 0; i < rsps.Count; i++)
			{
				rsp = (Rsp) rsps[i];
				
				if (rsp.sender != null && sender != null && rsp.sender.Equals(sender))
					return true;
			}
			return false;
		}
		
		
		public Rsp find(Address sender)
		{
			Rsp rsp;
			
			for (int i = 0; i < rsps.Count; i++)
			{
				rsp = (Rsp) rsps[i];
				if (rsp.sender != null && sender != null && rsp.sender.Equals(sender))
					return rsp;
			}
			return null;
		}
	}
}