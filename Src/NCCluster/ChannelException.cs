// $Id: ChannelException.java,v 1.4 2004/08/04 14:26:34 belaban Exp $
using System;
using System.Runtime.Serialization;

namespace Alachisoft.NGroups
{
	
	/// <summary> This class represents the super class for all exception types thrown by
	/// JGroups.
	/// </summary>
	[Serializable]
	internal class ChannelException:System.Exception, ISerializable
	{
		/// <summary> Retrieves the cause of this exception as passed to the constructor.
		/// <p>
		/// This method is provided so that in the case that a 1.3 VM is used,
		/// 1.4-like exception chaining functionality is possible.  If a 1.4 VM is
		/// used, this method will override <code>Throwable.getCause()</code> with a
		/// version that does exactly the same thing.
		/// 
		/// </summary>
		/// <returns> the cause of this exception.
		/// </returns>
		virtual public Exception Cause
		{
			get
			{
				return _cause;
			}
			
		}
		
		// Instance-level implementation.
		private Exception _cause;
		
		public ChannelException():base()
		{
		}
		
		public ChannelException(string reason):base(reason)
		{
		}
		
		public ChannelException(String reason, Exception cause):base(reason)
		{
			_cause = cause;
		}
		
		public override String ToString()
		{
			return "ChannelException: " + Message;
		}
		
		#region /                 --- ISerializable ---           / 

		/// <summary> 
		/// overloaded constructor, manual serialization. 
		/// </summary>
		protected ChannelException(SerializationInfo info, StreamingContext context):base(info, context) 
	{
	}

		/// <summary>
		/// manual serialization
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}

		#endregion
		/*
		* Throwable implementation.
		*/
		
		/// <summary> Prints this exception's stack trace to standard error.
		/// <p>
		/// This method is provided so that in the case that a 1.3 VM is used, calls
		/// to <code>printStackTrace</code> can be intercepted so that 1.4-like
		/// exception chaining functionality is possible.
		/// </summary>
		public void  printStackTrace()
		{
		}
		
		/// <summary> Prints this exception's stack trace to the provided stream.
		/// <p>
		/// This method implements the 1.4-like exception chaining functionality when
		/// printing stack traces for 1.3 VMs.  If a 1.4 VM is used, this call is
		/// delegated only to the super class.
		/// 
		/// </summary>
		/// <param name="ps">the stream to which the stack trace will be "printed".
		/// </param>
		public void  printStackTrace(System.IO.StreamWriter ps)
		{
		}
		
		
		private void  printCauseStackTrace(System.IO.StreamWriter pw)
		{
		}

	}
}