////  Copyright (c) 2018 Alachisoft
////  
////  Licensed under the Apache License, Version 2.0 (the "License");
////  you may not use this file except in compliance with the License.
////  You may obtain a copy of the License at
////  
////     http://www.apache.org/licenses/LICENSE-2.0
////  
////  Unless required by applicable law or agreed to in writing, software
////  distributed under the License is distributed on an "AS IS" BASIS,
////  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
////  See the License for the specific language governing permissions and
////  limitations under the License

//using Alachisoft.NCache.Common.Enum;
//using Alachisoft.NCache.Runtime;
//using Alachisoft.NCache.Runtime.Dependencies;
//using Alachisoft.NCache.Runtime.Exceptions;
//using System;
//using System.IO;

//namespace Alachisoft.NCache.Client
//{
//    /// <summary>
//    /// CacheStream is derived from System.IO.Stream. It is designed to put/fetch BLOB using standard
//    /// Stream interface. 
//    /// </summary>
//    public class CacheStream : Stream
//    {
//        internal StreamMode _mode;
//        internal string _key;
//        internal DateTime _absExpiration;
//        internal TimeSpan _slidingExpiration;
//        internal CacheDependency _dependency;
//        internal long _position;
//        internal string _lockHandle;
//        internal Cache _cacheHandle;
//        internal bool _closed;
//        internal long _length;
//        internal CacheItemPriority _priority;

//        internal CacheStream(Cache cacheHandle)
//        {
//            _cacheHandle = cacheHandle;
//        }

//        internal void OpenStream(string key, StreamMode mode, string group, string subGroup, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheDependency dependency, CacheItemPriority priority)
//        {
//            _mode = mode;
//            _key = key;
//            _priority = priority;
//            if (mode == StreamMode.Write)
//            {
//                _absExpiration = absoluteExpiration;
//                _slidingExpiration = slidingExpiration;
//                _dependency = dependency;
//            }
//            if (_mode == StreamMode.Read)
//            {
//                _lockHandle = _cacheHandle.OpenStream(key, StreamModes.Read, group, subGroup, absoluteExpiration, slidingExpiration, dependency, _priority);
//            }
//            else if (mode == StreamMode.ReadWithoutLock)
//            {
//                string lockHandle = _cacheHandle.OpenStream(key, StreamModes.ReadWithoutLock, group, subGroup, absoluteExpiration, slidingExpiration, dependency, _priority);

//                if (lockHandle != null)
//                {
//                    return;
//                }
//                else
//                {
//                    throw new StreamNotFoundException();
//                }
//            }
//            else
//            {
//                _lockHandle = _cacheHandle.OpenStream(key, StreamModes.Write, group, subGroup, absoluteExpiration, slidingExpiration, dependency, _priority);
//            }
//            if (_lockHandle == null)
//                throw new StreamException("An error occurred while opening stream");

//            _length = _cacheHandle.GetStreamLength(key, _lockHandle);
//            if (_mode == StreamMode.Write)
//                _position = Length;
//        }

//        /// <summary>
//        /// Gets System.IO.BufferedStream of given buffer size.
//        /// </summary>
//        /// <param name="bufferSize">Buffer size in bytes.</param>
//        /// <returns>An instance of System.IO.BufferedStream.</returns>
//        /// <remarks>CacheStream does not buffer the data. Each read/write operation performed on the stream is
//        /// propagated to cache. However this can cause performance issues if small chunks of data are being
//        /// read/written from/to stream. System.IO.BufferedStream supports buffering of data. This method
//        /// returns an instance of System.IO.BufferedStream which encapsulates CacheStream.</remarks>
//        public Stream GetBufferedStream(int bufferSize)
//        {
//            if (bufferSize > 0)
//                return new BufferedStream(this, bufferSize);
//            else
//                throw new ArgumentException("buffer size should be greater than zero");
//        }

//        /// <summary>
//        /// Gets System.IO.BufferedStream of given buffer size.
//        /// </summary>
//        /// <returns>An instance of System.IO.BufferedStream with buffer size of 4Kb.</returns>
//        /// <remarks>CacheStream does not buffer the data. Each read/write operation performed on the stream is
//        /// propagated to cache. However this can cause performance issues if small chunks of data are being
//        /// read/written from/to stream. System.IO.BufferedStream supports buffering of data. This method
//        /// returns an instance of System.IO.BufferedStream which encapsulates CacheStream.</remarks>
//        public Stream GetBufferedStream()
//        {
//            return new BufferedStream(this, 4096);
//        }

//        /// <summary>
//        /// Gets a value indicating whether the current
//        /// stream supports reading.
//        /// </summary>
//        /// <remarks>Returns 'True' if stream is opened with either StreamMode.Read or StreamMode.ReadWithoutLock. </remarks>
//        public override bool CanRead
//        {
//            get { return _mode != StreamMode.Write ? true : false; }
//        }

//        /// <summary>
//        /// Gets a value indicating whether the current stream supports seeking. 
//        /// </summary>
//        /// <remarks>Always returns 'False' because CacheStream does not support seek operation.</remarks>
//        public override bool CanSeek
//        {
//            get
//            {
//                return false;
//            }
//        }

//        /// <summary>
//        /// Gets a value indicating whether the current
//        /// stream supports writing.
//        /// </summary>
//        /// <remarks>Returns 'True' if stream is opened with StreamMode.Write. </remarks>
//        public override bool CanWrite
//        {
//            get { return _mode == StreamMode.Write ? true : false; }
//        }

//        /// <summary>
//        /// Gets a value indicating whether stream is closed.
//        /// </summary>
//        internal bool Closed { get { return _closed; } }

//        /// <summary>
//        /// When overridden in a derived class, clears all buffers for this stream and causes any
//        /// buffered data to be written to the underlying device.
//        /// </summary>
//        /// <remarks>CacheStream does not buffer the data. Each read/write operations is performed on
//        /// the cache.</remarks>
//        public override void Flush()
//        {

//        }

//        /// <summary>
//        /// Gets the length of the stream.
//        /// </summary>
//        /// <exception cref="System.ObjectDisposedException">Stream is closed.</exception>
//        /// <exception cref="StreamAlreadyLockedException">Stream is already locked.</exception>
//        /// <exception cref="StreamInvalidLockException">Lock acquired by current stream has become invalid.</exception>
//        /// <exception cref="StreamNotFoundException">Stream is not found in the cache.</exception>
//        public override long Length
//        {
//            get
//            {
//                if (Closed) throw new System.ObjectDisposedException("Methods were called after the stream was closed.");
//                return _cacheHandle.GetStreamLength(_key, _lockHandle);
//            }
//        }

//        /// <summary>
//        /// Gets/Sets the position within current stream.
//        /// </summary>
//        ///<exception cref="NotSupportedException">Stream does not support seeking.</exception>
//        public override long Position
//        {
//            get
//            {
//                if (!CanSeek)
//                    throw new NotSupportedException();
//                return 0;
//            }
//            set
//            {
//                if (!CanSeek)
//                    throw new NotSupportedException();
//            }
//        }

//        /// <summary>
//        /// Reads a sequence of bytes from the current
//        /// stream and advances the position within the stream by the number of bytes
//        /// read.
//        /// </summary>
//        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified
//        /// byte array with the values between offset and (offset + count - 1) replaced
//        /// by the bytes read from the current source.</param>
//        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read
//        /// from the current stream.</param>
//        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
//        /// <returns>The total number of bytes read into the buffer. This can be less than the
//        /// number of bytes requested if that many bytes are not currently available,
//        /// or zero (0) if the end of the stream has been reached.</returns>
//        /// <remarks>This method is not thread-safe</remarks>
//        /// <exception cref="StreamAlreadyLockedException">Stream is already locked.</exception>
//        /// <exception cref="StreamInvalidLockException">Lock acquired by current stream has become invalid.</exception>
//        /// <exception cref="StreamNotFoundException">Stream is not found in the cache.</exception>
//        public override int Read(byte[] buffer, int offset, int count)
//        {
//            if (_mode == StreamMode.Write)
//                throw new OperationNotSupportedException("Stream does not support reading.");

//            if (Closed) throw new ObjectDisposedException("Methods were called after the stream was closed.");
//            if (buffer == null) throw new ArgumentNullException("buffer");
//            if (offset + count > buffer.Length) throw new ArgumentException("Sum of offset and count is greater than the buffer length.");
//            if (offset < 0 || count < 0) throw new ArgumentException("offset or count is negative.");

//            int bytesRead = _cacheHandle.ReadFromStream(ref buffer, _key, _lockHandle, offset, (int)_position, count);
//            lock (this)
//            {
//                _position += bytesRead;
//            }
//            return bytesRead;
//        }

//        /// <summary>
//        /// Sets the position within the current stream.</summary>
//        /// <param name="offset">A byte offset relative to the origin parameter.</param>
//        /// <param name="origin">A value of type SeekOrigin indicating the reference point used to obtain the new position. </param>
//        /// <remarks>CacheStream does not support seeking. </remarks>
//        /// <exception cref="NotSupportedException">Stream does not support seeking.</exception>
//        public override long Seek(long offset, SeekOrigin origin)
//        {
//            throw new NotSupportedException("Stream does not support seeking.");
//        }

//        /// <summary>
//        /// Sets the length of the stream.
//        /// </summary>
//        /// <param name="value">The desired length of the current stream in bytes.</param>
//        /// <exception cref="NotSupportedException">Stream does not support both writing and seeking</exception>
//        public override void SetLength(long value)
//        {
//            throw new NotSupportedException("Stream does not support both writing and seeking");
//        }


//        /// <summary>
//        /// Writes a sequence of bytes to the current stream and 
//        /// advances the current position within this stream by the number of bytes written.
//        /// </summary>
//        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer 
//        /// to the current stream.</param>
//        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying 
//        /// bytes to the current stream.</param>
//        /// <param name="count">The number of bytes to be written to the current stream.</param>
//        /// <remarks>This method is not thread-safe</remarks>
//        /// <exception cref="StreamAlreadyLockedException">Stream is already locked.</exception>
//        /// <exception cref="StreamInvalidLockException">Lock acquired by current stream has become invalid.</exception>
//        /// <exception cref="StreamNotFoundException">Stream is not found in the cache.</exception>
//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            if (_mode == StreamMode.Read || _mode == StreamMode.ReadWithoutLock)
//                throw new NotSupportedException("Stream does not support writing.");

//            if (Closed) throw new ObjectDisposedException("Methods were called after the stream was closed.");
//            if (buffer == null) throw new ArgumentNullException("buffer");
//            if (offset + count > buffer.Length) throw new ArgumentException("Sum of offset and count is greater than the buffer length.");
//            if (offset < 0 || count < 0) throw new ArgumentException("offset or count is negative.");

//            byte[] bufferCopy = buffer;

//            if (count != buffer.Length)
//            {
//                bufferCopy = new byte[count];
//                Buffer.BlockCopy(buffer, offset, bufferCopy, 0, count);
//                offset = 0;
//            }

//            _cacheHandle.WriteToStream(_key, _lockHandle, bufferCopy, offset, (int)_position, count);
//            lock (this)
//            {
//                _position += count;
//            }

//        }

//        /// <summary>
//        /// Closes the current stream and releases any resources 
//        /// associated with the current stream.
//        /// </summary>
//        /// <remarks>A call to Close is required if stream is opened with StreamMode.Read or StreamMode.Write
//        /// to release locks.</remarks>
//        /// <exception cref="StreamAlreadyLockedException">Stream is already locked.</exception>
//        /// <exception cref="StreamInvalidLockException">Lock acquired by current stream has become invalid.</exception>
//        /// <exception cref="StreamNotFoundException">Stream is not found in the cache.</exception>
//        public override void Close()
//        {
//            base.Close();
//            _closed = true;
//            //Lets release the lock on the stream.
//            if (_mode == StreamMode.Read || _mode == StreamMode.Write)
//                _cacheHandle.CloseStream(_key, _lockHandle);
//        }
//    }
//}
