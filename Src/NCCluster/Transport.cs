using System;

namespace Alachisoft.NGroups
{
	/// <summary>
	/// Simply Transport interface
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	public interface Transport 
	{    
		/// <summary>Sends a message on the transport</summary>
		/// <param name="msg">Message to send</param>
		void		send(Message msg);

		/// <summary>Receives a message from the transport</summary>
		Object		receive(long timeout);

	}
}
