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

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public static class BinaryConverter
	{
		public static unsafe ushort DecodeUInt16(byte[] buffer, int offset)
		{
			return (ushort)((buffer[offset] << 8) + buffer[offset + 1]);
		}

		public static unsafe ushort DecodeUInt16(byte* buffer, int offset)
		{
			return (ushort)((buffer[offset] << 8) + buffer[offset + 1]);
		}

		public static unsafe int DecodeInt32(ArraySegment<byte> segment, int offset)
		{
			fixed (byte* buffer = segment.Array)
			{
				byte* ptr = buffer + segment.Offset + offset;

				return DecodeInt32(buffer, 0);
			}
		}

		public static unsafe int DecodeInt32(byte* buffer, int offset)
		{
			buffer += offset;

			return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
		}

		public static unsafe int DecodeInt32(byte[] buffer, int offset)
		{
			return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
		}

		public static unsafe ulong DecodeUInt64(byte[] buffer, int offset)
		{
			fixed (byte* ptr = buffer)
			{
				return DecodeUInt64(ptr, offset);
			}
		}

		public static unsafe ulong DecodeUInt64(byte* buffer, int offset)
		{
			buffer += offset;

			var part1 = (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
			var part2 = (uint)((buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7]);

			return ((ulong)part1 << 32) | part2;
		}

		public static unsafe void EncodeUInt16(uint value, byte[] buffer, int offset)
		{
			fixed (byte* bufferPtr = buffer)
			{
				EncodeUInt16(value, bufferPtr, offset);
			}
		}

		public static unsafe void EncodeUInt16(uint value, byte* buffer, int offset)
		{
			byte* ptr = buffer + offset;

			ptr[0] = (byte)(value >> 8);
			ptr[1] = (byte)(value & 255);
		}

		public static unsafe void EncodeUInt32(uint value, byte[] buffer, int offset)
		{
			fixed (byte* bufferPtr = buffer)
			{
				EncodeUInt32(value, bufferPtr, offset);
			}
		}

		public static unsafe void EncodeUInt32(uint value, byte* buffer, int offset)
		{
			byte* ptr = buffer + offset;

			ptr[0] = (byte)(value >> 24);
			ptr[1] = (byte)(value >> 16);
			ptr[2] = (byte)(value >> 8);
			ptr[3] = (byte)(value & 255);
		}

		public static unsafe void EncodeUInt64(ulong value, byte[] buffer, int offset)
		{
			fixed (byte* bufferPtr = buffer)
			{
				EncodeUInt64(value, bufferPtr, offset);
			}
		}

		public static unsafe void EncodeUInt64(ulong value, byte* buffer, int offset)
		{
			byte* ptr = buffer + offset;

			ptr[0] = (byte)(value >> 56);
			ptr[1] = (byte)(value >> 48);
			ptr[2] = (byte)(value >> 40);
			ptr[3] = (byte)(value >> 32);
			ptr[4] = (byte)(value >> 24);
			ptr[5] = (byte)(value >> 16);
			ptr[6] = (byte)(value >> 8);
			ptr[7] = (byte)(value & 255);
		}

		public static byte[] EncodeKey(string key)
		{
			if (String.IsNullOrEmpty(key)) return null;

			return Encoding.UTF8.GetBytes(key);
		}

		public static string DecodeKey(byte[] data)
		{
			if (data == null || data.Length == 0) return null;

			return Encoding.UTF8.GetString(data);
		}

		public static string DecodeKey(byte[] data, int index, int count)
		{
			if (data == null || data.Length == 0 || count == 0) return null;

			return Encoding.UTF8.GetString(data, index, count);
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
