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
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public class BinaryRequest
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(BinaryRequest));
		private static int InstanceCounter;

		public byte Operation;
		public readonly int CorrelationId;

		public string Key;
		public ulong Cas;

		public BinaryRequest(OpCode operation) : this((byte)operation) { }

		public BinaryRequest(byte commandCode)
		{
			this.Operation = commandCode;
			// session id
			this.CorrelationId = Interlocked.Increment(ref InstanceCounter);
		}

		public unsafe IList<ArraySegment<byte>> CreateBuffer()
		{
			return CreateBuffer(null);
		}

		public unsafe IList<ArraySegment<byte>> CreateBuffer(IList<ArraySegment<byte>> appendTo)
		{
			// key size 
			byte[] keyData = BinaryConverter.EncodeKey(this.Key);
			int keyLength = keyData == null ? 0 : keyData.Length;

			if (keyLength > 0xffff) throw new InvalidOperationException("KeyTooLong");

			// extra size
			ArraySegment<byte> extras = this.Extra;
			int extraLength = extras.Array == null ? 0 : extras.Count;
			if (extraLength > 0xff) throw new InvalidOperationException("ExtraTooLong");

			// body size
			ArraySegment<byte> body = this.Data;
			int bodyLength = body.Array == null ? 0 : body.Count;

			// total payload size
			int totalLength = extraLength + keyLength + bodyLength;

			//build the header
			byte[] header = new byte[24];

			fixed (byte* buffer = header)
			{
				buffer[0x00] = 0x80; 
				buffer[0x01] = this.Operation;

				// key length
				buffer[0x02] = (byte)(keyLength >> 8);
				buffer[0x03] = (byte)(keyLength & 255);

				// extra length
				buffer[0x04] = (byte)(extraLength);

				// 5 -- data type, 0 (RAW)
				// 6,7 -- reserved, always 0

				buffer[0x06] = (byte)(this.Reserved >> 8);
				buffer[0x07] = (byte)(this.Reserved & 255);

				// body length
				buffer[0x08] = (byte)(totalLength >> 24);
				buffer[0x09] = (byte)(totalLength >> 16);
				buffer[0x0a] = (byte)(totalLength >> 8);
				buffer[0x0b] = (byte)(totalLength & 255);

				buffer[0x0c] = (byte)(this.CorrelationId >> 24);
				buffer[0x0d] = (byte)(this.CorrelationId >> 16);
				buffer[0x0e] = (byte)(this.CorrelationId >> 8);
				buffer[0x0f] = (byte)(this.CorrelationId & 255);

				ulong cas = this.Cas;
				// CAS
				if (cas > 0)
				{
					// skip this if no cas is specfied
					buffer[0x10] = (byte)(cas >> 56);
					buffer[0x11] = (byte)(cas >> 48);
					buffer[0x12] = (byte)(cas >> 40);
					buffer[0x13] = (byte)(cas >> 32);
					buffer[0x14] = (byte)(cas >> 24);
					buffer[0x15] = (byte)(cas >> 16);
					buffer[0x16] = (byte)(cas >> 8);
					buffer[0x17] = (byte)(cas & 255);
				}
			}

			var retval = appendTo ?? new List<ArraySegment<byte>>(4);

			retval.Add(new ArraySegment<byte>(header));

			if (extraLength > 0) retval.Add(extras);

			// NOTE key must be already encoded and should not contain any invalid characters which are not allowed by the protocol
			if (keyLength > 0) retval.Add(new ArraySegment<byte>(keyData));
			if (bodyLength > 0) retval.Add(body);

#if DEBUG_PROTOCOL
			if (log.IsDebugEnabled)
			{
				log.Debug("Building binary request");
				StringBuilder sb = new StringBuilder(128).AppendLine();

				for (int i = 0; i < header.Length; i++)
				{
					byte value = header[i];
					sb.Append(value < 16 ? "0x0" : "0x").Append(value.ToString("X"));

					if (i % 4 == 3) sb.AppendLine(); else sb.Append(" ");
				}

				log.Debug(sb.ToString());
			}
#endif

			return retval;
		}

		public ushort Reserved;
		public ArraySegment<byte> Extra;
		public ArraySegment<byte> Data;
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
