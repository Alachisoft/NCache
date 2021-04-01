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
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.Common.Util
{
    public class BufferPool
    {
        private static Queue _pool = new Queue();
        private static object _sync_lock = new object();

        static BufferPool()
        {
            
            AllocateNewBuffers();
        }

        private static void AllocateNewBuffers()
        {
            lock (_sync_lock)
            {
                ArrayList newBuffers = new ArrayList();
                for (int i = 1; i <= ServiceConfiguration.LOHPoolSize; i++)
                {
                    try
                    {
                        byte[] buffer = new byte[ServiceConfiguration.LOHPoolBufferSize];
                        _pool.Enqueue(buffer);
                    }
                    catch (OutOfMemoryException)
                    {
                        AppUtil.LogEvent("BufferPool can't allocate new buffer", System.Diagnostics.EventLogEntryType.Error);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// Gets a larg buffer from the pool.
        /// </summary>
        /// <returns></returns>
        public static byte[] CheckoutBuffer(int size)
        {
            if (size != -1 && size > ServiceConfiguration.LOHPoolBufferSize)
               return new byte[size];

            lock (_sync_lock)
            {
                if (_pool.Count == 0)
                {
                    AllocateNewBuffers();
                }

                if (_pool.Count > 0)
                {
                    return _pool.Dequeue() as byte[];
                }
                else
                    return new byte[ServiceConfiguration.LOHPoolBufferSize];
            }
        }

        /// <summary>
        /// Frees a buffer allocated from the pool.
        /// </summary>
        /// <param name="buffer"></param>
        public static void CheckinBuffer(byte[] buffer)
        {
            if (buffer == null) return;
            if (buffer.Length > ServiceConfiguration.LOHPoolBufferSize) return; //This is not a pool buffer.
            lock (_sync_lock)
            {
                _pool.Enqueue(buffer);
            }
        }
        /// <summary>
        /// Releases all the buffers.
        /// </summary>
        public static void Clear()
        {
            lock (_sync_lock)
            {
                _pool.Clear();
            }
        }
    }
}
