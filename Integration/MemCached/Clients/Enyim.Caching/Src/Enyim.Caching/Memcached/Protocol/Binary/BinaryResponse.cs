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
using System.Text;
using System.Diagnostics;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public class BinaryResponse
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(BinaryResponse));

		private const byte MAGIC_VALUE = 0x81;
		private const int HeaderLength = 24;

		private const int HEADER_OPCODE = 1;
		private const int HEADER_KEY = 2; // 2-3
		private const int HEADER_EXTRA = 4;
		private const int HEADER_DATATYPE = 5;
		private const int HEADER_STATUS = 6; // 6-7
		private const int HEADER_BODY = 8; // 8-11
		private const int HEADER_OPAQUE = 12; // 12-15
		private const int HEADER_CAS = 16; // 16-23

		public byte Opcode;
		public int KeyLength;
		public byte DataType;
		public int StatusCode;

		public int CorrelationId;
		public ulong CAS;

		public ArraySegment<byte> Extra;
		public ArraySegment<byte> Data;

		private string responseMessage;

		public BinaryResponse()
		{
			this.StatusCode = -1;
		}

		public string GetStatusMessage()
		{
			return this.Data.Array == null
					? null
					: (this.responseMessage
						?? (this.responseMessage = Encoding.ASCII.GetString(this.Data.Array, this.Data.Offset, this.Data.Count)));
		}

		public unsafe bool Read(PooledSocket socket)
		{
			this.StatusCode = -1;

			if (!socket.IsAlive)
				return false;

			var header = new byte[HeaderLength];
			socket.Read(header, 0, header.Length);

			int dataLength, extraLength;

			DeserializeHeader(header, out dataLength, out extraLength);

			if (dataLength > 0)
			{
				var data = new byte[dataLength];
				socket.Read(data, 0, dataLength);

				this.Extra = new ArraySegment<byte>(data, 0, extraLength);
				this.Data = new ArraySegment<byte>(data, extraLength, data.Length - extraLength);
			}

			return this.StatusCode == 0;
		}

		/// <summary>
		/// Reads the response from the socket asynchronously.
		/// </summary>
		/// <param name="socket">The socket to read from.</param>
		/// <param name="next">The delegate whihc will continue processing the response. This is only called if the read completes asynchronoulsy.</param>
		/// <param name="ioPending">Set totrue if the read is still pending when ReadASync returns. In this case 'next' will be called when the read is finished.</param>
		/// <returns>
		/// If the socket is already dead, ReadAsync returns false, next is not called, ioPending = false
		/// If the read completes synchronously (e.g. data is received from the buffer), it returns true/false depending on the StatusCode, and ioPending is set to true, 'next' will not be called.
		/// It returns true if it has to read from the socket, so the operation will complate asynchronously at a later time. ioPending will be true, and 'next' will be called to handle the data
		/// </returns>
		public bool ReadAsync(PooledSocket socket, Action<bool> next, out bool ioPending)
		{
			this.StatusCode = -1;
			this.currentSocket = socket;
			this.next = next;

			var asyncEvent = new AsyncIOArgs();

			asyncEvent.Count = HeaderLength;
			asyncEvent.Next = this.DoDecodeHeaderAsync;

			this.shouldCallNext = true;

			if (socket.ReceiveAsync(asyncEvent))
			{
				ioPending = true;
				return true;
			}

			ioPending = false;
			this.shouldCallNext = false;

			return asyncEvent.Fail
					? false
					: this.DoDecodeHeader(asyncEvent, out ioPending);
		}

		private PooledSocket currentSocket;
		private int dataLength;
		private int extraLength;
		private bool shouldCallNext;
		private Action<bool> next;

		private void DoDecodeHeaderAsync(AsyncIOArgs asyncEvent)
		{
			this.shouldCallNext = true;
			bool tmp;

			this.DoDecodeHeader(asyncEvent, out tmp);
		}

		private bool DoDecodeHeader(AsyncIOArgs asyncEvent, out bool pendingIO)
		{
			pendingIO = false;

			if (asyncEvent.Fail)
			{
				if (this.shouldCallNext) this.next(false);

				return false;
			}

			this.DeserializeHeader(asyncEvent.Result, out this.dataLength, out this.extraLength);
			var retval = this.StatusCode == 0;

			if (this.dataLength == 0)
			{
				if (this.shouldCallNext) this.next(retval);
			}
			else
			{
				asyncEvent.Count = this.dataLength;
				asyncEvent.Next = this.DoDecodeBodyAsync;

				if (this.currentSocket.ReceiveAsync(asyncEvent))
				{
					pendingIO = true;
				}
				else
				{
					if (asyncEvent.Fail) return false;

					this.DoDecodeBody(asyncEvent);
				}
			}

			return retval;
		}

		private void DoDecodeBodyAsync(AsyncIOArgs asyncEvent)
		{
			this.shouldCallNext = true;
			DoDecodeBody(asyncEvent);
		}

		private void DoDecodeBody(AsyncIOArgs asyncEvent)
		{
			if (asyncEvent.Fail)
			{
				if (this.shouldCallNext) this.next(false);

				return;
			}

			this.Extra = new ArraySegment<byte>(asyncEvent.Result, 0, this.extraLength);
			this.Data = new ArraySegment<byte>(asyncEvent.Result, this.extraLength, this.dataLength - this.extraLength);

			if (this.shouldCallNext) this.next(true);
		}

		private unsafe void DeserializeHeader(byte[] header, out int dataLength, out int extraLength)
		{
			fixed (byte* buffer = header)
			{
				if (buffer[0] != MAGIC_VALUE)
					throw new InvalidOperationException("Expected magic value " + MAGIC_VALUE + ", received: " + buffer[0]);

				this.DataType = buffer[HEADER_DATATYPE];
				this.Opcode = buffer[HEADER_OPCODE];
				this.StatusCode = BinaryConverter.DecodeUInt16(buffer, HEADER_STATUS);

				this.KeyLength = BinaryConverter.DecodeUInt16(buffer, HEADER_KEY);
				this.CorrelationId = BinaryConverter.DecodeInt32(buffer, HEADER_OPAQUE);
				this.CAS = BinaryConverter.DecodeUInt64(buffer, HEADER_CAS);

				dataLength = BinaryConverter.DecodeInt32(buffer, HEADER_BODY);
				extraLength = buffer[HEADER_EXTRA];
			}
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
