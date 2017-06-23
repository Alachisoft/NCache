// Copyright (c) 2017 Alachisoft
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

namespace Alachisoft.NCache.Web.Caching.Util

{
    internal class HelperFxn
    {
        /// <summary>
        /// Converts the byte into string using UTF8Encoding
        /// </summary>
        /// <param name="buffer">buffer containing values to be converted</param>
        /// <returns></returns>
        internal static string ToString(byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer);
        }

        internal static string ToStringUni(byte[] buffer)
        {
            return Encoding.Unicode.GetString(buffer);
        }

        internal static string ToString(byte[] buffer, int offset, int size)
        {
            return Encoding.UTF8.GetString(buffer, offset, size);
        }

        /// <summary>
        /// Converts byte array to string using UTF8Encoding
        /// </summary>
        /// <param name="data">values to be converted to byte</param>
        /// <returns></returns>
        internal static byte[] ToBytes(string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }

        internal static byte[] ToBytesUni(string data)
        {
            return Encoding.Unicode.GetBytes(data);
        }
      
        internal static int ToInt32(byte[] buffer, int offset, int size)
        {
            int cInt = 0;
            try
            {
                cInt = Convert.ToInt32(Encoding.UTF8.GetString(buffer, offset, size));
            }
            catch (Exception)
            {
                throw new InvalidCastException("Input endIndex is not in correct format.");
            }

            return cInt;
        }

        internal static byte[] ParseToByteArray(byte value)
        {
            byte[] tempArray = new byte[1];
            tempArray[0] = value;
            return tempArray;
        }

    }
}