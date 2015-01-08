// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Protocol.Text
{
	internal static class TextSocketHelper
	{
		private const string GenericErrorResponse = "ERROR";
		private const string ClientErrorResponse = "CLIENT_ERROR ";
		private const string ServerErrorResponse = "SERVER_ERROR ";
		private const int ErrorResponseLength = 13;

		public const string CommandTerminator = "\r\n";

		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(TextSocketHelper));

		/// <summary>
		/// Reads the response of the server.
		/// </summary>
		/// <returns>The data sent by the memcached server.</returns>
		/// <exception cref="T:System.InvalidOperationException">The server did not sent a response or an empty line was returned.</exception>
		/// <exception cref="T:Enyim.Caching.Memcached.MemcachedException">The server did not specified any reason just returned the string ERROR. - or - The server returned a SERVER_ERROR, in this case the Message of the exception is the message returned by the server.</exception>
		/// <exception cref="T:Enyim.Caching.Memcached.MemcachedClientException">The server did not recognize the request sent by the client. The Message of the exception is the message returned by the server.</exception>
		public static string ReadResponse(PooledSocket socket)
		{
			string response = TextSocketHelper.ReadLine(socket);

			if (log.IsDebugEnabled)
				log.Debug("Received response: " + response);

			if (String.IsNullOrEmpty(response))
				throw new MemcachedClientException("Empty response received.");

			if (String.Compare(response, GenericErrorResponse, StringComparison.Ordinal) == 0)
				throw new NotSupportedException("Operation is not supported by the server or the request was malformed. If the latter please report the bug to the developers.");

			if (response.Length >= ErrorResponseLength)
			{
				if (String.Compare(response, 0, ClientErrorResponse, 0, ErrorResponseLength, StringComparison.Ordinal) == 0)
				{
					throw new MemcachedClientException(response.Remove(0, ErrorResponseLength));
				}
				else if (String.Compare(response, 0, ServerErrorResponse, 0, ErrorResponseLength, StringComparison.Ordinal) == 0)
				{
					throw new MemcachedException(response.Remove(0, ErrorResponseLength));
				}
			}

			return response;
		}


		/// <summary>
		/// Reads a line from the socket. A line is terninated by \r\n.
		/// </summary>
		/// <returns></returns>
		private static string ReadLine(PooledSocket socket)
		{
			MemoryStream ms = new MemoryStream(50);

			bool gotR = false;

			int data;

			while (true)
			{
				data = socket.ReadByte();

				if (data == 13)
				{
					gotR = true;
					continue;
				}

				if (gotR)
				{
					if (data == 10)
						break;

					ms.WriteByte(13);

					gotR = false;
				}

				ms.WriteByte((byte)data);
			}

			string retval = Encoding.ASCII.GetString(ms.GetBuffer(), 0, (int)ms.Length);

			if (log.IsDebugEnabled)
				log.Debug("ReadLine: " + retval);

			return retval;
		}

		/// <summary>
		/// Gets the bytes representing the specified command. returned buffer can be used to streamline multiple writes into one Write on the Socket
		/// using the <see cref="M:Enyim.Caching.Memcached.PooledSocket.Write(IList&lt;ArraySegment&lt;byte&gt;&gt;)"/>
		/// </summary>
		/// <param name="value">The command to be converted.</param>
		/// <returns>The buffer containing the bytes representing the command. The command must be terminated by \r\n.</returns>
		/// <remarks>The Nagle algorithm is disabled on the socket to speed things up, so it's recommended to convert a command into a buffer
		/// and use the <see cref="M:Enyim.Caching.Memcached.PooledSocket.Write(IList&lt;ArraySegment&lt;byte&gt;&gt;)"/> to send the command and the additional buffers in one transaction.</remarks>
		public unsafe static IList<ArraySegment<byte>> GetCommandBuffer(string value)
		{
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(value));

			return new ArraySegment<byte>[] { data };
		}

		public unsafe static IList<ArraySegment<byte>> GetCommandBuffer(string value, IList<ArraySegment<byte>> list)
		{
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(value));

			list.Add(data);

			return list;
		}

	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk�, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
