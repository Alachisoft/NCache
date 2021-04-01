using System;

namespace Alachisoft.NGroups
{
	/// <summary>
	/// Allows messages to be received from the Channel, used to pass messages
	/// to a 'Building Block'
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	public interface UpHandler 
	{
		/// <summary>
		/// Receives an Event from the Channel
		/// </summary>
		/// <param name="evt">Event received</param>
		void up(Event evt);
	}
}
