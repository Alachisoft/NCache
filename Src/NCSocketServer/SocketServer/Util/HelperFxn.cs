//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Text;
using System.IO;

namespace Alachisoft.NCache.SocketServer.Util
{
    internal sealed class HelperFxn
    {
        /// <summary>
        /// Converts the byte into string using UTF8Encoding
        /// </summary>
        /// <param name="buffer">buffer containing value to be converted</param>
        /// <returns></returns>
        public static string ToString(byte[] buffer)
        {
            return UTF8Encoding.UTF8.GetString(buffer);
        }

        internal static string ToStringUni(byte[] buffer)
        {
            return UTF8Encoding.Unicode.GetString(buffer);
        }

        public static string ToString(byte[] buffer, int offset, int size)
        {
            return UTF8Encoding.UTF8.GetString(buffer, offset, size);
        }

        /// <summary>
        /// Converts byte array to string using UTF8Encoding
        /// </summary>
        /// <param name="value">value to be converted to byte</param>
        /// <returns></returns>
        public static byte[] ToBytes(string data)
        {
            return UTF8Encoding.UTF8.GetBytes(data);
        }

        internal static byte[] ToBytesUni(string data)
        {
            return UTF8Encoding.Unicode.GetBytes(data);
        }

        /// <summary>
        /// Converts the specified byte array to int. 
        /// It is callers responsibilty to ensure that value can be converted to Int32
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static int ToInt32(byte[] buffer)
        {
            int cInt = 0;
            try
            {
                cInt = Convert.ToInt32(UTF8Encoding.UTF8.GetString(buffer));
            }
            catch (Exception)
            {
                throw;
            }

            return cInt;
        }

        /// <summary>
        /// Convert the selected bytes to int
        /// </summary>
        /// <param name="buffer">buffer containing the value</param>
        /// <param name="offset">offset from which the int bytes starts</param>
        /// <param name="size">number of bytes</param>
        /// <returns></returns>
        public static int ToInt32(byte[] buffer, int offset, int size, string s)
        {
            int cInt = 0;
            try
            {
                cInt = Convert.ToInt32(UTF8Encoding.UTF8.GetString(buffer, offset, size));
            }
            catch (Exception)
            {
                throw;
            }

            return cInt;
        }

        public static long ToInt64(byte[] buffer, int offset, int size)
        {
            long cInt = 0;
            cInt = Convert.ToInt64(UTF8Encoding.UTF8.GetString(buffer, offset, size));
            return cInt;
        }

        /// <summary>
        /// Convert the selected bytes to int
        /// </summary>
        /// <param name="buffer">buffer containing the value</param>
        /// <param name="offset">offset from which the int bytes starts</param>
        /// <param name="size">number of bytes</param>
        /// <returns></returns>
        public static int ToInt32(byte[] buffer, int offset, int size)
        {
            int cInt = 0;
            try
            {
                cInt = Convert.ToInt32(UTF8Encoding.UTF8.GetString(buffer, offset, size));
            }
            catch (Exception)
            {
                throw;
            }

            return cInt;
        }

        /// <summary>
        /// Copy block of data from source array
        /// </summary>
        /// <param name="copyFrom">Source array</param>
        /// <param name="startIndex">Start index in the source array from where copy begins</param>
        /// <param name="endIndex">End index, until which the bytes are copied</param>
        /// <returns>Resultant array</returns>
        public static byte[] CopyPartial(byte[] copyFrom, int startIndex, int endIndex)
        {
            byte[] copyIn = new byte[endIndex - startIndex];

            for (int i = startIndex, count = 0; i < endIndex; i++, count++)
                copyIn[count] = copyFrom[i];

            return copyIn;
        }

        /// <summary>
        /// Copy block of data from source array
        /// </summary>
        /// <param name="copyFrom">Source array</param>
        /// <param name="startIndex">Start index in the source array from where copy begins</param>
        /// <param name="endIndex">End index, until which the bytes are copied</param>
        /// <returns>Resultant array</returns>
        public static void CopyPartial(byte[] copyFrom,byte[] copyTo,  int startIndex, int endIndex)
        {
            for (int i = startIndex, count = 0; i < endIndex; i++, count++)
                copyTo[count] = copyFrom[i];
        }

        /// <summary>
        /// Copy block of data from source array
        /// </summary>
        /// <param name="copyFrom">Source array</param>
        /// <param name="startIndex">Start index in the source array from where copy begins</param>
        /// <param name="length">Number of bytes to copy</param>
        /// <returns>Resultant array</returns>
        public static byte[] CopyTw(byte[] copyFrom, int startIndex, int length)
        {
            byte[] copyIn = new byte[length];
            int loop = length + startIndex;

            for (int i = startIndex, count = 0; i < loop; i++, count++)
                copyIn[count] = copyFrom[i];

            return copyIn;
        }

        /// <summary>
		/// Creates a byte buffer representation of a <c>int32</c>
		/// </summary>
		/// <param name="value"><c>int</c> to be converted</param>
		/// <returns>Byte Buffer representation of a <c>Int32</c></returns>
		public static byte[] WriteInt32(int value)
        {
            byte[] _byteBuffer = new byte[4];
            _byteBuffer[0] = (byte)value;
            _byteBuffer[1] = (byte)(value >> 8);
            _byteBuffer[2] = (byte)(value >> 16);
            _byteBuffer[3] = (byte)(value >> 24);

            return _byteBuffer;
        }
#if !CLIENT || NETCORE
        /// <summary>
        /// Creates a <c>Int32</c> from a byte buffer representation
        /// </summary>
        /// <param name="readOnlySpan">Byte Buffer representation of a <c>Int32</c></param>
        /// <returns></returns>
        public static int ConvertToInt32(ReadOnlySpan<byte> readOnlySpan)
        {
            return (int)((readOnlySpan[0] & 0xFF) |
                readOnlySpan[1] << 8 |
                readOnlySpan[2] << 16 |
                readOnlySpan[3] << 24);
        }
#endif
        public static int ConvertToInt32(byte[] buffer)
        {
            return (int)((buffer[0] & 0xFF) |
                buffer[1] << 8 |
                buffer[2] << 16 |
                buffer[3] << 24);
        }
#if !CLIENT || NETCORE
        public static short ConvertToShort(ReadOnlySpan<byte> readOnlySpan)
        {
            return (short)((readOnlySpan[0] & 0xFF) |
                readOnlySpan[1] << 8);
        }
#endif
        public static short ConvertToShort(byte[] buffer)
        {
            return (short)((buffer[0] & 0xFF) |
                buffer[1] << 8);
        }
    }
}
