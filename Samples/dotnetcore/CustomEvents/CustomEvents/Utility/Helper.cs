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
using System.IO;
using System.Text;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Configuration;

namespace Alachisoft.NCache.Samples.CustomEvents.Utility
{
	/// <summary>
	/// Helper class that helps do common serialization and deserialization tasks.
	/// </summary>
	internal class Helper
	{
        private static string _cacheName;

		/// <summary>
		/// serialize an object graph to a stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="value"></param>
		public static bool Serialize(Stream stream, object value)
		{
			try
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(stream, value);
				return true;
			}
			catch(Exception /*e*/)
			{
				// do nothing ?
			}
			return false;
		}
        

		/// <summary>
		/// deserialize an object graph from the stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public static object Deserialize(Stream stream)
		{
			try
			{
				BinaryFormatter formatter = new BinaryFormatter();
				return formatter.Deserialize(stream);
			}
			catch(Exception /*e*/)
			{
				// do nothing ?
			}
			return null;
		}

		/// <summary>
		/// Creates an object from a byte buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public static object FromByteBuffer(byte[] buffer)
		{
			if(buffer == null) return null;
			using(MemoryStream ms = new MemoryStream(buffer))
			{
				return Deserialize(ms);
			}
		}
			
		/// <summary>
		/// Serializes an object into a byte buffer.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static byte[] ToByteBuffer(object value)
		{
			if(value == null) return null;
			//if(obj.GetType().Equals(typeof(byte[]))) return obj as byte[];
			try
			{
				using(MemoryStream ms = new MemoryStream())
				{
					Serialize(ms, value);
					return ms.ToArray();
				}
			}
			catch(Exception /*e*/)
			{
				// do nothing ?
			}
			return null;
		}		
	}
}
