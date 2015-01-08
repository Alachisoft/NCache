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
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.MemcachedEncoding
{
    static class BinaryConverter
    {
        public static ushort ToUInt16(byte [] buffer, int offset)
        {
            return (ushort)((buffer[offset] << 8) | (buffer[offset + 1]));
        }

        public static int ToInt32(byte[] buffer, int offset)
        {
            return ((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2]) << 8 | buffer[offset + 3]);
        }

        public static uint ToUInt32(byte[] buffer, int offset)
        {
            uint value = BitConverter.ToUInt32(buffer, offset);
            if (BitConverter.IsLittleEndian)
                value = swapEndianness(value);
            return value;
        }

        public static ulong ToUInt64(byte[] buffer, int offset)
        {
            ulong value = BitConverter.ToUInt64(buffer, offset);
            if (BitConverter.IsLittleEndian)
                value = swapEndianness(value);
            return value;
        }

        private static uint swapEndianness(uint x)
        {
            return ((x & 0x000000ff) << 24) +  // First byte
                   ((x & 0x0000ff00) << 8) +   // Second byte
                   ((x & 0x00ff0000) >> 8) +   // Third byte
                   ((x & 0xff000000) >> 24);   // Fourth byte
        }
        private static ulong swapEndianness(ulong x)
        {
            return (ulong)(((swapEndianness((uint)x) & 0xffffffffL) << 0x20) |

                                        (swapEndianness((uint)(x >> 0x20)) & 0xffffffffL));
        }

        /// <summary>
        /// Converts sequence of bytes to string using ASCII encoding.
        /// </summary>
        /// <param name="data">Bytes to be converted</param>
        /// <param name="index">Starting index in data</param>
        /// <param name="count">Number of bytes o be decoded</param>
        /// <returns></returns>
        public static string GetString(byte[] data, int index, int count)
        {
            return Encoding.ASCII.GetString(data, index, count);
        }

        /// <summary>
        /// Converts sequence of bytes to string using ASCII encoding
        /// </summary>
        /// <param name="data">Bytes to be converted</param>
        /// <returns></returns>
        public static string GetString(byte[] data)
        {
            return Encoding.ASCII.GetString(data);
        }

        public static byte[] GetBytes(ulong value)
        {
            byte[] bytes = new byte[8];
            bytes[0] = (byte)(value >> 56);
            bytes[1] = (byte)(value >> 48);
            bytes[2] = (byte)(value >> 40);
            bytes[3] = (byte)(value >> 32);
            bytes[4] = (byte)(value >> 24);
            bytes[5] = (byte)(value >> 16);
            bytes[6] = (byte)(value >> 8);
            bytes[7] = (byte)(value & 255);

            return bytes;
        }

        public static byte[] GetBytes(int value)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(value >> 24);
            bytes[1] = (byte)(value >> 16);
            bytes[2] = (byte)(value >> 8);
            bytes[3] = (byte)(value & 255);
            return bytes;
        }

        public static byte[] GetBytes(uint value)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(value >> 24);
            bytes[1] = (byte)(value >> 16);
            bytes[2] = (byte)(value >> 8);
            bytes[3] = (byte)(value & 255);
            return bytes;
        }

        public static byte[] GetBytes(ushort value)
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)(value >> 8);
            bytes[1] = (byte)(value & 255);
            return bytes;
        }

        public static byte[] GetBytes(string value)
        {
            if (value == null)
                return new byte[] { };
            return Encoding.ASCII.GetBytes(value);
        }

    }
}
