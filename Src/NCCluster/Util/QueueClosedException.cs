using System;

namespace Alachisoft.NGroups.Util
{
	/// <summary>
	/// Exception by MQueue when a thread tries to access a closed queue.
	/// <p><b>Author:</b> Chris Koiak</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	internal class QueueClosedException : Exception 
	{
		/// <summary>
		/// Basic Exception
		/// </summary>
		public QueueClosedException() {}
		/// <summary>
		/// Exception with custom message
		/// </summary>
		/// <param name="msg">Message to display when exception is thrown</param>
		public QueueClosedException( String msg ) : base(msg){}

		/// <summary>
		/// Creates a String representation of the Exception
		/// </summary>
		/// <returns>A String representation of the Exception</returns>
		public String toString() 
		{
			if ( this.Message != null )
				return "QueueClosedException:" + this.Message;
			else
				return "QueueClosedException";
		}
	}

}
