// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;

namespace Alachisoft.NCache.Samples.CustomEvents.Utility
{
	/// <summary>
	/// Msg class is used to send and receive messages among room members.
	/// </summary>
	[Serializable]
	internal class Msg
	{
		/// <summary>
		/// Enumeration that defines the various opcodes for the message.
		/// </summary>
		public enum Code 
		{
            NewUser,
			Text,
            Status,
			UserLeft
		}

		/// <summary> </summary>
		private Code		code;
		/// <summary> </summary>
		private string		from;
		/// <summary> </summary>
		private string		to;
		/// <summary> </summary>
		private object		data;

		/// <summary>
		/// The constructor will set the values of the member variables
		/// </summary>
		/// <param name="operand">The operand to store in object related to Message Type</param>
		/// <param name="MessageType">Type of Message can be simple text message or status information</param>
		/// <param name="UserName">Name of user to whom the message is sent</param>
		public Msg(object Data, Code code, string from, string to)
		{
			data = Data;
			this.code = code;
			this.from = from;
			this.to = to;
			
		}

		/// <summary>
		/// The opcode of this message.
		/// </summary>
		public Code OpCode
		{
			get { return code; }
		}

		/// <summary>
		/// The sender of the message.
		/// </summary>
		public string From
		{
			get { return from; }			
		}

		/// <summary>
		/// The receipient of the message. null if broadcast message.
		/// </summary>
		public string To
		{
			get { return to; }
		}

		/// <summary>
		/// The operand containted in this message.
		/// </summary>
		public object Data
		{
			get { return data; }
		}
	}
}
