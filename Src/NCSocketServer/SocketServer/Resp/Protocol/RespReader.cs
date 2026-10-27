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
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Resp.Protocol
{
    /// <summary>
    /// Incremental parser for RESP requests.
    /// <para>
    /// Usage is feed-then-drain: hand every received chunk to <see cref="Feed"/>, then call
    /// <see cref="TryReadRequest"/> in a loop until it returns <c>false</c>.
    /// </para>
    /// <para>
    /// The incremental behaviour is not optional. Unlike NCache's protobuf framing, which reads a
    /// fixed-size length header before the payload, RESP gives no up-front size for a request, so a
    /// command can be split across any number of TCP segments and several commands can arrive in one
    /// segment (pipelining). This parser therefore keeps its own buffer, returns <c>false</c> for
    /// "need more data" without consuming anything, and resumes cleanly on the next feed.
    /// </para>
    /// <para>
    /// Not thread safe: one instance per connection, driven by that connection's receive chain.
    /// </para>
    /// </summary>
    internal sealed class RespReader
    {
        private const int InitialCapacity = 1024;

        private byte[] _buffer;
        private int _start;   // first unconsumed byte
        private int _end;     // one past the last buffered byte

        public RespReader()
        {
            _buffer = new byte[InitialCapacity];
        }

        /// <summary>Bytes received but not yet consumed. Exposed for tests and diagnostics.</summary>
        public int Buffered
        {
            get { return _end - _start; }
        }

        /// <summary>Appends a received chunk to the parse buffer.</summary>
        public void Feed(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || count < 0 || offset + count > data.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;

            EnsureCapacity(count);
            Buffer.BlockCopy(data, offset, _buffer, _end, count);
            _end += count;
        }

        /// <summary>Convenience overload for tests.</summary>
        public void Feed(byte[] data)
        {
            Feed(data, 0, data == null ? 0 : data.Length);
        }

        /// <summary>
        /// Tries to take one complete command off the buffer.
        /// </summary>
        /// <returns>
        /// <c>true</c> with <paramref name="request"/> set when a whole command was available;
        /// <c>false</c> when more data is needed. Nothing is consumed when <c>false</c> is returned.
        /// </returns>
        /// <exception cref="RespProtocolException">
        /// The stream is not valid RESP. The connection must be closed after replying with the error.
        /// </exception>
        public bool TryReadRequest(out RespRequest request)
        {
            request = null;

            while (true)
            {
                if (_start >= _end)
                {
                    Compact();
                    return false;
                }

                int position = _start;
                RespRequest parsed;
                bool complete = _buffer[position] == (byte)'*'
                    ? TryParseMultibulk(ref position, out parsed)
                    : TryParseInline(ref position, out parsed);

                if (!complete)
                {
                    Compact();
                    return false;
                }

                _start = position;

                // A well-formed but empty command - "*0\r\n", "*-1\r\n" or a blank inline line.
                // Redis consumes these and waits for the next one rather than erroring.
                if (parsed == null) continue;

                request = parsed;
                return true;
            }
        }

        #region Multibulk  ( *N\r\n $len\r\n bytes\r\n ... )

        private bool TryParseMultibulk(ref int position, out RespRequest request)
        {
            request = null;

            int p = position + 1; // skip '*'
            int lineStart, lineLength;

            if (!TryReadCrlfLine(ref p, out lineStart, out lineLength)) return false;

            long count = ParseInteger(lineStart, lineLength, "invalid multibulk length");
            if (count > RespLimits.MaxMultibulkLength)
                throw new RespProtocolException("invalid multibulk length");

            if (count <= 0)
            {
                position = p;   // consumed, no command produced
                return true;
            }

            var arguments = new byte[count][];

            for (int i = 0; i < count; i++)
            {
                if (p >= _end) return false;

                if (_buffer[p] != (byte)'$')
                    throw new RespProtocolException(
                        "expected '$', got '" + Printable(_buffer[p]) + "'");

                p++;
                if (!TryReadCrlfLine(ref p, out lineStart, out lineLength)) return false;

                long length = ParseInteger(lineStart, lineLength, "invalid bulk length");

                // A negative bulk length is legal in a *reply* (null bulk string) but never in a
                // request, so it is a protocol error here rather than an empty argument.
                if (length < 0 || length > RespLimits.MaxBulkLength)
                    throw new RespProtocolException("invalid bulk length");

                if (_end - p < length + 2) return false;   // payload + trailing CRLF not here yet

                var argument = new byte[length];
                Buffer.BlockCopy(_buffer, p, argument, 0, (int)length);
                p += (int)length;

                if (_buffer[p] != (byte)'\r' || _buffer[p + 1] != (byte)'\n')
                    throw new RespProtocolException("bulk string is not terminated by CRLF");
                p += 2;

                arguments[i] = argument;
            }

            request = new RespRequest(arguments);
            position = p;
            return true;
        }

        #endregion

        #region Inline  ( PING\r\n )

        private bool TryParseInline(ref int position, out RespRequest request)
        {
            request = null;

            int newline = -1;
            for (int i = position; i < _end; i++)
            {
                if (_buffer[i] == (byte)'\n') { newline = i; break; }
            }

            if (newline < 0)
            {
                if (_end - position > RespLimits.MaxInlineRequestLength)
                    throw new RespProtocolException("too big inline request");
                return false;
            }

            int lineEnd = newline;
            if (lineEnd > position && _buffer[lineEnd - 1] == (byte)'\r') lineEnd--;

            int lineLength = lineEnd - position;
            if (lineLength > RespLimits.MaxInlineRequestLength)
                throw new RespProtocolException("too big inline request");

            List<byte[]> tokens = SplitInline(position, lineLength);
            position = newline + 1;

            if (tokens.Count == 0) return true;   // blank line, consumed and skipped

            request = new RespRequest(tokens.ToArray());
            return true;
        }

        /// <summary>
        /// Splits an inline command on whitespace, honouring single and double quotes so that
        /// <c>ECHO "hello world"</c> typed at a telnet prompt behaves the way it does against Redis.
        /// </summary>
        private List<byte[]> SplitInline(int start, int length)
        {
            var tokens = new List<byte[]>();
            var current = new List<byte>();
            bool inToken = false;

            int i = start;
            int end = start + length;

            while (i < end)
            {
                byte b = _buffer[i];

                if (b == (byte)' ' || b == (byte)'\t')
                {
                    if (inToken)
                    {
                        tokens.Add(current.ToArray());
                        current.Clear();
                        inToken = false;
                    }
                    i++;
                    continue;
                }

                if (b == (byte)'"' || b == (byte)'\'')
                {
                    byte quote = b;
                    i++;

                    while (i < end && _buffer[i] != quote)
                    {
                        if (quote == (byte)'"' && _buffer[i] == (byte)'\\' && i + 1 < end)
                        {
                            i++;
                            current.Add(Unescape(_buffer[i]));
                        }
                        else
                        {
                            current.Add(_buffer[i]);
                        }
                        i++;
                    }

                    if (i >= end) throw new RespProtocolException("unbalanced quotes in request");

                    i++;              // closing quote
                    inToken = true;   // "" is a valid empty argument
                    continue;
                }

                current.Add(b);
                inToken = true;
                i++;
            }

            if (inToken) tokens.Add(current.ToArray());
            return tokens;
        }

        private static byte Unescape(byte escaped)
        {
            switch (escaped)
            {
                case (byte)'n': return (byte)'\n';
                case (byte)'r': return (byte)'\r';
                case (byte)'t': return (byte)'\t';
                case (byte)'b': return 8;
                case (byte)'a': return 7;
                default: return escaped;   // covers \\ and \"
            }
        }

        #endregion

        #region Buffer and scalar helpers

        /// <summary>
        /// Reads one CRLF-terminated line starting at <paramref name="p"/>, advancing past the CRLF.
        /// Returns false when the terminator has not arrived yet.
        /// </summary>
        private bool TryReadCrlfLine(ref int p, out int lineStart, out int lineLength)
        {
            lineStart = p;
            lineLength = 0;

            for (int i = p; i + 1 < _end; i++)
            {
                if (_buffer[i] == (byte)'\r' && _buffer[i + 1] == (byte)'\n')
                {
                    lineLength = i - p;
                    p = i + 2;
                    return true;
                }
            }

            if (_end - p > RespLimits.MaxLineLength)
                throw new RespProtocolException("too big inline request");

            return false;
        }

        private long ParseInteger(int start, int length, string errorMessage)
        {
            if (length == 0) throw new RespProtocolException(errorMessage);

            int i = start;
            int end = start + length;
            bool negative = false;

            if (_buffer[i] == (byte)'-')
            {
                negative = true;
                i++;
                if (i == end) throw new RespProtocolException(errorMessage);
            }
            else if (_buffer[i] == (byte)'+')
            {
                i++;
                if (i == end) throw new RespProtocolException(errorMessage);
            }

            long value = 0;
            for (; i < end; i++)
            {
                byte b = _buffer[i];
                if (b < (byte)'0' || b > (byte)'9') throw new RespProtocolException(errorMessage);

                value = (value * 10) + (b - (byte)'0');

                // Bail out well before overflow; any real length is far below this.
                if (value > int.MaxValue) throw new RespProtocolException(errorMessage);
            }

            return negative ? -value : value;
        }

        private static string Printable(byte b)
        {
            return b >= 0x20 && b < 0x7F ? ((char)b).ToString() : "\\x" + b.ToString("x2");
        }

        private void EnsureCapacity(int additional)
        {
            if (_end + additional <= _buffer.Length) return;

            Compact();
            if (_end + additional <= _buffer.Length) return;

            int capacity = _buffer.Length == 0 ? InitialCapacity : _buffer.Length;
            while (capacity < _end + additional) capacity *= 2;

            var grown = new byte[capacity];
            Buffer.BlockCopy(_buffer, 0, grown, 0, _end);
            _buffer = grown;
        }

        /// <summary>Slides unconsumed bytes back to the front so the buffer does not creep forward.</summary>
        private void Compact()
        {
            if (_start == 0) return;

            int remaining = _end - _start;
            if (remaining > 0) Buffer.BlockCopy(_buffer, _start, _buffer, 0, remaining);

            _start = 0;
            _end = remaining;
        }

        #endregion
    }
}
