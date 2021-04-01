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
using System.IO;
using System.IO.Compression;


namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Util class that compress and decompress values uing GZip provided solution
    /// </summary>
    public class CompressionUtil
    {
        private const int Compressed = BitSetConstants.Compressed;

        /// <summary>
        /// Compress value only if its greater then threshold
        /// </summary>
        /// <param name="value">value to be compressed</param>
        /// <param name="flag">flag to be set if compression is successful</param>
        /// <param name="threshold">threshold limit</param>
        /// <returns>resultant value</returns>
        public static byte[] Compress(byte[] value, ref BitSet flag, long threshold)
        {
            if (value == null) return value;
            if (flag == null) return value;
            if (value.Length <= threshold) return value;
            return Compress(value, ref flag);
        }

        /// <summary>
        /// Compresses the value
        /// </summary>
        /// <param name="value">value to be compressed</param>
        /// <param name="flag">flag to be set if compression is successful</param>
        /// <returns>compressed value</returns>
        public static byte[] Compress(byte[] value, ref BitSet flag)
        {
            if (value == null) return value;
            if (flag == null) return value;

            return value;
        }

        /// <summary>
        /// Compresses the value
        /// </summary>
        /// <param name="value">value to be compressed</param>
        /// <param name="flag">flag to be set if compression is successful</param>
        /// <returns>compressed value</returns>
        public static byte[] Compress(byte[] value, int offset,int count)
        {
            return value;
        }

        /// <summary>
        /// Decompress the value
        /// </summary>
        /// <param name="value">value to be decomrpessed</param>
        /// <param name="flag">flag</param>
        /// <returns>decompressed value</returns>
        public static byte[] Decompress(byte[] value, BitSet flag)
        {
            if (value == null) return value;
            if (flag == null) return value;
            return value;
        }

        /// <summary>
        /// Decompress the value
        /// </summary>
        /// <param name="value">value to be decomrpessed</param>
        /// <returns>decompressed value</returns>
        public static byte[] Decompress(byte[] value)
        {
            return value;
        }

        /// <summary>
        /// Decompress the value
        /// </summary>
        /// <param name="value">value to be decomrpessed</param>
        /// <returns>decompressed value</returns>
        public static byte[] Decompress(byte[] value,int offset,int count)
        {
            return value;
        }
    }
}