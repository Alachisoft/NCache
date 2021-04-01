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
using System.IO;

namespace Alachisoft.NCache.SocketServer.MultiBufferReceive
{
    internal enum DeserializationState
    {
        DeserializingLength,
        DeserailizingCommad
    }

    internal sealed class SocketBufferStream : Stream
    {
        private readonly SocketBufferManager _bufferManager;

        private int

            // Command start in the first buffer.
            _commandStart = 0,

            // Command end in the last buffer.
            _commandEnd = 0,

            // Total command length.
            _commandLength = 0,

            // Current buffer index.
            _currBufferIndex = 0;

        // Current position on the stream.
        private long _position = 0;

        private DeserializationState _state;

        private IList<SocketBuffer> _buffers;

        internal SocketBufferStream(SocketBufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _buffers = new List<SocketBuffer>();
            _state = DeserializationState.DeserializingLength;
        }

        #region Stream Implementation.

        public override void Flush() { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return _commandLength; } }
        public override long Position { get { return _position; } set { _position = value; } }

        // TODO: Introduce the .Net 4.7.x's Span class here.
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0 /*|| _commandLength - _position < offset*/) throw new ArgumentOutOfRangeException("offset");
            if (count < 0 /*|| _commandLength - _position < count*/) throw new ArgumentOutOfRangeException("count");
            if (buffer.Length - offset < count) throw new ArgumentException("Argument_InvalidOffLen");
            if (_buffers.Count == 0) throw new Exception("No Buffer");

            int bytesRead = 0;
            while (bytesRead < count && _currBufferIndex < _buffers.Count)
            {
                SocketBuffer currentBuffer = _buffers[_currBufferIndex];

               
                bytesRead += SocketBufferUtil.ReadBytesFromSocketBuffer(buffer, offset + bytesRead, count - bytesRead, currentBuffer);
                _position += bytesRead;
                if (currentBuffer.UnreadBytes == 0) _currBufferIndex++;
            }
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        if (offset < 0 || _commandLength < offset)
                            throw new ArgumentOutOfRangeException("offset");

                        _position = offset;
                        _currBufferIndex = 0;

                        for (int i = 0; i < _buffers.Count; i++)
                        {
                            if (i == 0)
                            {
                                _buffers[i].BytesRead(_commandStart - (int)_buffers[i].ReadIndex);
                            }
                            else
                            {
                                _buffers[i].BytesRead(0 - _buffers[i].ReadIndex);
                            }
                        }

                        while (offset > 0)
                        {
                            SocketBuffer currentBuffer = _buffers[_currBufferIndex];

                            if (currentBuffer.UnreadBytes < offset)
                            {
                                offset -= currentBuffer.UnreadBytes;
                                currentBuffer.BytesRead(currentBuffer.UnreadBytes);
                                _currBufferIndex++;
                            }
                            else
                            {
                                currentBuffer.BytesRead(offset);
                            }
                        }
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        long tempPosition = unchecked(_position + offset);
                        if (tempPosition < 0 || tempPosition > _commandLength)
                            throw new IOException("IO.IO_SeekBeforeBegin");

                        _position = tempPosition;

                        if (offset < 0)
                        {
                            while (offset < 0)
                            {
                                SocketBuffer currentBuffer = _buffers[_currBufferIndex];
                                currentBuffer.BytesRead(0 - currentBuffer.ReadIndex);

                                if (currentBuffer.ReadIndex + offset < 0)
                                {
                                    offset += currentBuffer.ReadIndex;
                                    currentBuffer.BytesRead(0 - currentBuffer.ReadIndex);
                                    _currBufferIndex--;
                                }
                                else
                                {
                                    currentBuffer.BytesRead(offset);
                                }
                            }
                        }
                        else
                        {
                            while (offset > 0)
                            {
                                SocketBuffer currentBuffer = _buffers[_currBufferIndex];

                                if (currentBuffer.UnreadBytes < offset)
                                {
                                    offset -= currentBuffer.UnreadBytes;
                                    currentBuffer.BytesRead(currentBuffer.UnreadBytes);
                                    _currBufferIndex++;
                                }
                                else
                                {
                                    currentBuffer.BytesRead(offset);
                                }
                            }
                        }

                        break;
                    }
                case SeekOrigin.End:
                    {
                        if (offset > 0 || _commandLength < -1 * offset)
                            throw new ArgumentOutOfRangeException("offset");

                        _currBufferIndex = _buffers.Count - 1;
                        _position = _commandLength + offset;

                        for (int i = _buffers.Count - 1; i >= 0; i--)
                        {
                            if (i == _buffers.Count - 1)
                            {
                                _buffers[i].BytesRead(_commandEnd - _buffers[i].ReadIndex);
                            }
                            else
                            {
                                _buffers[i].BytesRead(_buffers[i].UnreadBytes);
                            }
                        }

                        while (offset < 0)
                        {
                            SocketBuffer currentBuffer = _buffers[_currBufferIndex];

                            if (currentBuffer.ReadIndex + offset < 0)
                            {
                                offset += currentBuffer.ReadIndex;
                                currentBuffer.BytesRead(0 - currentBuffer.ReadIndex);
                                _currBufferIndex--;
                            }
                            else
                            {
                                currentBuffer.BytesRead(offset);
                            }
                        }

                        break;
                    }
                default: throw new ArgumentException("Argument_InvalidSeekOrigin");
            }

            return _position;
        }

        #endregion

        #region Public methods

        public bool ReadCommand(out object command)
        {
            command = null;

            switch (_state)
            {
                case DeserializationState.DeserializingLength:
                    if (!ReadCommandLenght(out _commandLength)) return false;

                    _state = DeserializationState.DeserailizingCommad;
                    _buffers.Clear();
                    if (!GetDataBuffers(_commandLength, _buffers)) return false;

                    _position = _currBufferIndex = 0;
                    _commandStart = _commandEnd = 0;

                    if (_buffers.Count > 0)
                    {
                        _commandStart = (int)_buffers[0].ReadIndex;
                        int commandLength = _commandLength;

                        for (int i = 0; i < _buffers.Count; i++)
                        {
                            if (_buffers[i].UnreadBytes > commandLength)
                            {
                                _commandEnd = (int)_buffers[i].ReadIndex + commandLength;
                            }
                            else
                            {
                                commandLength -= (int)_buffers[i].UnreadBytes;
                            }
                        }
                    }

                    command = ProtoBuf.Serializer.Deserialize<Common.Protobuf.Command>(this);

                    _state = DeserializationState.DeserializingLength;
                    return true;

                case DeserializationState.DeserailizingCommad:
                    _buffers.Clear();
                    if (!GetDataBuffers(_commandLength, _buffers)) return false;

                    _position = _currBufferIndex = 0;
                    _commandStart = _commandEnd = 0;

                    if (_buffers.Count > 0)
                    {
                        _commandStart = (int)_buffers[0].ReadIndex;
                        int commandLength = _commandLength;

                        for (int i = 0; i < _buffers.Count; i++)
                        {
                            if (_buffers[i].UnreadBytes > commandLength)
                            {
                                _commandEnd = (int)_buffers[i].ReadIndex + commandLength;
                            }
                            else
                            {
                                commandLength -= (int)_buffers[i].UnreadBytes;
                            }
                        }
                    }

                    command = ProtoBuf.Serializer.Deserialize<Common.Protobuf.Command>(this);
                    _state = DeserializationState.DeserializingLength;
                    return true;
            }

            return false;
        }

        #endregion

        #region Private methods

        private bool ReadAcknowledgmentId(out int ackId)
        {
            // Check if acknowledgments are enabled.
            ackId = -1;
            var buffers = new List<SocketBuffer>();
            if (!GetDataBuffers(10, buffers))
            {
                return false;
            }
            else if (buffers.Count == 1)
            {
                ackId = BitConverter.ToInt32(SocketBufferUtil.ReadBytesFromSocketBuffer(10, buffers[0]), 0);
                return true;
            }
            else
            {
                var lengthBytes = new byte[10];
                byte[] partialBytes1 = SocketBufferUtil.ReadBytesFromSocketBuffer((int)buffers[0].UnreadBytes, buffers[0]);
                byte[] partialBytes2 = SocketBufferUtil.ReadBytesFromSocketBuffer(10 - partialBytes1.Length, buffers[1]);
                Array.Copy(partialBytes1, lengthBytes, partialBytes1.Length);
                Array.Copy(partialBytes2, 0, lengthBytes, partialBytes1.Length - 1, partialBytes2.Length);
                ackId = BitConverter.ToInt32(lengthBytes, 0);
                return true;
            }
        }

        private bool ReadCommandLenght(out int length)
        {
            length = -1;
            var buffers = new List<SocketBuffer>();
            if (!GetDataBuffers(10, buffers))
            {
                return false;
            }
            else if (buffers.Count == 1)
            {
                length = BitConverter.ToInt32(SocketBufferUtil.ReadBytesFromSocketBuffer(10, buffers[0]), 0);
                return true;
            }
            else
            {
                var lengthBytes = new byte[10];
                byte[] partialBytes1 = SocketBufferUtil.ReadBytesFromSocketBuffer((int)buffers[0].UnreadBytes, buffers[0]);
                byte[] partialBytes2 = SocketBufferUtil.ReadBytesFromSocketBuffer(10 - partialBytes1.Length, buffers[1]);
                Array.Copy(partialBytes1, lengthBytes, partialBytes1.Length);
                Array.Copy(partialBytes2, 0, lengthBytes, partialBytes1.Length - 1, partialBytes2.Length);
                length = BitConverter.ToInt32(lengthBytes, 0);
                return true;
            }
        }

        private bool GetDataBuffers(int count, IList<SocketBuffer> buffers, int startIndex = 0)
        {
            SocketBuffer currentBuffer;
            if (!_bufferManager.GetBusyBufferAt(startIndex, out currentBuffer))
            {
                return false;
            }
            else if (currentBuffer.UnreadBytes >= count)
            {
                buffers.Add(currentBuffer);
                return true;
            }
            else if (currentBuffer.UnreadBytes == 0)
            {
                if (currentBuffer.BusyReceiving)
                {
                    return false;
                }
                else
                {
                    _bufferManager.FreeTopBusyBuffer();
                    return GetDataBuffers(count, buffers, startIndex);
                }
            }
            else
            {
                // Buffer has partial data.
                if (currentBuffer.BusyReceiving)
                {
                    return false;
                }
                else
                {
                    buffers.Add(currentBuffer);
                    return GetDataBuffers(count - (int)currentBuffer.UnreadBytes, buffers, ++startIndex);
                }
            }
        }

        #endregion

       
    }
}
