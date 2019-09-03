using System;
using System.Security.Permissions;
using System.Runtime.Serialization;

namespace Alachisoft.NCache.Common.DataStructures
{
	///<summary>
	/// The RedBlackException class distinguishes read black tree exceptions from .NET
	/// exceptions. 
	///</summary>
	///
	[Serializable]
	internal class RedBlackException : Exception, ISerializable
	{
		/// <summary> 
		/// default constructor. 
		/// </summary>
		public RedBlackException()
        {
    	}
			
		/// <summary> 
		/// overloaded constructor, takes the reason as parameter. 
		/// </summary>
		/// <param name="reason">reason for exception</param>
		public RedBlackException(string reason) : base(reason) 
        {
		}

		/// <summary>
		/// overloaded constructor. 
		/// </summary>
		/// <param name="reason">reason for exception</param>
		/// <param name="inner">nested exception</param>
		public RedBlackException(string reason, Exception inner):base(reason, inner) 
		{
		}

		#region /                 --- ISerializable ---           / 

		/// <summary> 
		/// overloaded constructor, manual serialization. 
		/// </summary>
		protected RedBlackException(SerializationInfo info, StreamingContext context):base(info, context) 
		{
		}

		/// <summary>
		/// manual serialization
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}

		#endregion
	}
}
