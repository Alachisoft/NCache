//  Copyright (c) 2018 Alachisoft
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

namespace Alachisoft.NCache.SocketServer.Resp.Protocol
{
    /// <summary>
    /// Serializes RESP2 replies into a growable buffer.
    /// <para>
    /// One instance per connection. Replies for a pipelined batch accumulate here and are flushed in
    /// a single send, which both matches what clients expect and keeps the syscall count down.
    /// </para>
    /// <para>
    /// Not thread safe: driven by its connection's receive chain only.
    /// </para>
    /// </summary>
    internal sealed class RespWriter
    {
        private const int InitialCapacity = 512;

        private static readonly byte[] Crlf = { (byte)'\r', (byte)'\n' };
        private static readonly byte[] NullBulkString = Encoding.ASCII.GetBytes("$-1\r\n");
        private static readonly byte[] NullArray = Encoding.ASCII.GetBytes("*-1\r\n");
        private static readonly byte[] EmptyArray = Encoding.ASCII.GetBytes("*0\r\n");

        private byte[] _buffer = new byte[InitialCapacity];
        private int _length;

        /// <summary>Bytes written since the last <see cref="Reset"/>.</summary>
        public int Length
        {
            get { return _length; }
        }

        public bool IsEmpty
        {
            get { return _length == 0; }
        }

        /// <summary>The written bytes, without copying. Valid until the next write.</summary>
        public ArraySegment<byte> GetBuffer()
        {
            return new ArraySegment<byte>(_buffer, 0, _length);
        }

        public byte[] ToArray()
        {
            var copy = new byte[_length];
            Buffer.BlockCopy(_buffer, 0, copy, 0, _length);
            return copy;
        }

        /// <summary>Drops everything written so far, keeping the allocated capacity.</summary>
        public void Reset()
        {
            _length = 0;
        }

        /// <summary>Simple string: <c>+OK\r\n</c>.</summary>
        public void WriteSimpleString(string value)
        {
            Append((byte)'+');
            AppendSanitizedAscii(value);
            Append(Crlf);
        }

        /// <summary>
        /// Error: <c>-ERR message\r\n</c>.
        /// <para>
        /// <paramref name="codeAndMessage"/> must include the leading error code, e.g.
        /// <c>"ERR unknown command 'FOO'"</c> or <c>"OOM command not allowed ..."</c>. Client
        /// libraries branch on that code, so it is the caller's job to pick the right one.
        /// </para>
        /// </summary>
        public void WriteError(string codeAndMessage)
        {
            Append((byte)'-');
            AppendSanitizedAscii(codeAndMessage);
            Append(Crlf);
        }

        /// <summary>Integer: <c>:42\r\n</c>.</summary>
        public void WriteInteger(long value)
        {
            Append((byte)':');
            AppendInteger(value);
            Append(Crlf);
        }

        /// <summary>
        /// Bulk string: <c>$5\r\nhello\r\n</c>, or the null bulk string <c>$-1\r\n</c> when
        /// <paramref name="value"/> is null. Binary safe - the payload is length prefixed and is
        /// never inspected or escaped.
        /// </summary>
        public void WriteBulkString(byte[] value)
        {
            if (value == null)
            {
                Append(NullBulkString);
                return;
            }

            Append((byte)'$');
            AppendInteger(value.Length);
            Append(Crlf);
            Append(value);
            Append(Crlf);
        }

        /// <summary>UTF-8 bulk string. For textual replies only; use the byte[] overload for values.</summary>
        public void WriteBulkString(string value)
        {
            WriteBulkString(value == null ? null : Encoding.UTF8.GetBytes(value));
        }

        /// <summary>The null bulk string, <c>$-1\r\n</c> - a cache miss.</summary>
        public void WriteNull()
        {
            Append(NullBulkString);
        }

        /// <summary>
        /// Array header: <c>*3\r\n</c>. The caller then writes exactly
        /// <paramref name="count"/> elements. A negative count writes the null array.
        /// </summary>
        public void WriteArrayHeader(int count)
        {
            if (count < 0)
            {
                Append(NullArray);
                return;
            }
            if (count == 0)
            {
                Append(EmptyArray);
                return;
            }

            Append((byte)'*');
            AppendInteger(count);
            Append(Crlf);
        }

        public void WriteEmptyArray()
        {
            Append(EmptyArray);
        }

        /// <summary>
        /// Copies already-encoded RESP through untouched.
        /// <para>
        /// Used when relaying a reply from the node that owns a key: those bytes are already
        /// exactly what our client needs, and decoding then re-encoding them could only lose
        /// fidelity. The caller is responsible for the bytes being one well-formed RESP value -
        /// <see cref="Cluster.RespReplyFramer"/> is what establishes that.
        /// </para>
        /// </summary>
        public void WriteRaw(byte[] encoded)
        {
            Append(encoded);
        }

        #region Internals

        /// <summary>
        /// Writes ASCII, replacing CR and LF with spaces.
        /// <para>
        /// This is a framing guard, not cosmetics. Simple strings and errors are terminated by CRLF,
        /// so a single stray newline inside a message would desynchronise the client for the rest of
        /// the connection. NCache exception messages routinely carry paths and multi-line text, and
        /// those messages end up in error replies.
        /// </para>
        /// </summary>
        private void AppendSanitizedAscii(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            EnsureCapacity(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\r' || c == '\n') c = ' ';
                _buffer[_length++] = c < 0x80 ? (byte)c : (byte)'?';
            }
        }

        private void AppendInteger(long value)
        {
            if (value == 0)
            {
                Append((byte)'0');
                return;
            }

            // long.MinValue has no positive counterpart; the generic path below would overflow.
            if (value == long.MinValue)
            {
                Append(Encoding.ASCII.GetBytes(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                return;
            }

            if (value < 0)
            {
                Append((byte)'-');
                value = -value;
            }

            // Longest positive long is 19 digits.
            var digits = new byte[20];
            int index = digits.Length;

            while (value > 0)
            {
                digits[--index] = (byte)('0' + (int)(value % 10));
                value /= 10;
            }

            EnsureCapacity(digits.Length - index);
            Buffer.BlockCopy(digits, index, _buffer, _length, digits.Length - index);
            _length += digits.Length - index;
        }

        private void Append(byte value)
        {
            EnsureCapacity(1);
            _buffer[_length++] = value;
        }

        private void Append(byte[] value)
        {
            if (value == null || value.Length == 0) return;

            EnsureCapacity(value.Length);
            Buffer.BlockCopy(value, 0, _buffer, _length, value.Length);
            _length += value.Length;
        }

        private void EnsureCapacity(int additional)
        {
            if (_length + additional <= _buffer.Length) return;

            int capacity = _buffer.Length == 0 ? InitialCapacity : _buffer.Length;
            while (capacity < _length + additional) capacity *= 2;

            var grown = new byte[capacity];
            Buffer.BlockCopy(_buffer, 0, grown, 0, _length);
            _buffer = grown;
        }

        #endregion
    }
}
