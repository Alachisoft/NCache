using System;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups
{
	public interface MembershipListener
	{
		
		
		/// <summary>Called by JGroups to notify the target object of a change of membership.
		/// <b>No long running actions should be done in this callback in the case of Ensemble,
		/// as this would block Ensemble.</b> If some long running action needs to be performed,
		/// it should be done in a separate thread (cf. <code>../Tests/QuoteServer.java</code>).
		/// </summary>
		void  viewAccepted(View new_view);
		
		
		/// <summary>Called when a member is suspected </summary>
		void  suspect(Address suspected_mbr);
		
		
		/// <summary>Block sending and receiving of messages until viewAccepted() is called </summary>
		void  block();

        /// <summary>Whether to allow the joining of new node </summary>
        bool AllowJoin();
	}
}
