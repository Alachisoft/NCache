// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache StreamingAPI sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Configuration;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the streaming api sample.
    /// </summary>
    public class Streaming
    {
        private static Cache _cache;

        /// <summary>
        /// Executing this method will perform the operations of the sample using streaming api.
        /// Streaming allows to read data from cache in chunks just like any buffered stream.
        /// </summary>
        public static void Run()
        {
            string key = "StreamingObject:1";

            // Generate a new byte buffer with some data.
            byte[] buffer = GenerateByteBuffer();

            // Initialize Cache 
            InitializeCache();

            // Write the byte buffer in cache using streaming.
            WriteUsingStream(key, buffer);

            // Read the data inserted using streaming api.
            ReadUsingStream(key);

            // Dispose the cache once done
            _cache.Dispose();
        }

        /// <summary>
        /// This method generates a new byte buffer with data.
        /// </summary>
        /// <returns> Returns a byte buffer with data.</returns>
        private static byte[] GenerateByteBuffer()
        {
            byte[] byteBuffer = new byte[1024];
            for (int i = 0; i < byteBuffer.Length; i++)
                byteBuffer[i] = Convert.ToByte(i % 256);

            return byteBuffer;
        }

        /// <summary>
        /// This method initializes the cache.
        /// </summary>
        private static void InitializeCache()
        {
            string cache = ConfigurationManager.AppSettings["CacheId"];

            if (String.IsNullOrEmpty(cache))
            {
                Console.WriteLine("The Cache Name cannot be null or empty.");
                return;
            }

            // Initialize an instance of the cache to begin performing operations:
            _cache = NCache.Web.Caching.NCache.InitializeCache(cache);
			
			Console.WriteLine("Cache initialized successfully");
        }

        /// <summary>
        /// This methods inserts data in the cache using cache stream.
        /// </summary>
        /// <param name="key"> The key against which stream will be written. </param>
        /// <param name="writeBuffer"> data that will be written in the stream. </param>
        private static void WriteUsingStream(string key, byte[] writeBuffer)
        {
            // Declaring NCacheStream
            CacheStream stream = _cache.GetCacheStream(key, StreamMode.Write);
            stream.Write(writeBuffer, 0, writeBuffer.Length);
            stream.Close();

            Console.WriteLine("Stream written to cache.");
        }

        /// <summary>
        /// This method fetches data from the cache using streams.
        /// </summary>
        /// <param name="key"> The key of the stream that needs to be fetched from the cache. </param>
        private static void ReadUsingStream(string key)
        {
            byte[] readBuffer = new byte[1024];

            // StramMode.Read allows only simultaneous reads but no writes!
            CacheStream stream = _cache.GetCacheStream(key, StreamMode.Read);

            // Now you have stream perform operations on it just like any regular stream.
            var readCount = stream.Read(readBuffer, 0, readBuffer.Length);
            stream.Close();

            Console.WriteLine("Bytes read = " + readCount);
        }
    }
}