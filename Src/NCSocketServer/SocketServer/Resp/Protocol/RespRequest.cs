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
    /// One fully parsed RESP command.
    /// <para>
    /// Argument 0 is the command name; the rest are its operands. Arguments are held as byte arrays
    /// rather than strings because RESP is a binary protocol: keys and values may contain bytes that
    /// are not valid UTF-8, and a value must survive a round trip byte for byte.
    /// </para>
    /// </summary>
    internal sealed class RespRequest
    {
        private readonly byte[][] _arguments;

        public RespRequest(byte[][] arguments)
        {
            _arguments = arguments ?? new byte[0][];
            Name = _arguments.Length > 0 ? AsciiUpper(_arguments[0]) : string.Empty;
        }

        /// <summary>Command name, upper-cased. RESP command names are case insensitive.</summary>
        public string Name { get; }

        /// <summary>Total token count, including the command name.</summary>
        public int Count
        {
            get { return _arguments.Length; }
        }

        /// <summary>Operand count, excluding the command name.</summary>
        public int ArgCount
        {
            get { return _arguments.Length > 0 ? _arguments.Length - 1 : 0; }
        }

        /// <summary>Raw bytes of argument <paramref name="index"/>, where 0 is the command name.</summary>
        public byte[] this[int index]
        {
            get { return _arguments[index]; }
        }

        /// <summary>Operand <paramref name="argIndex"/> (0 = first operand after the command name).</summary>
        public byte[] GetArg(int argIndex)
        {
            return _arguments[argIndex + 1];
        }

        /// <summary>
        /// Operand decoded as UTF-8. Only for tokens that are genuinely textual - option keywords,
        /// integers, sub-commands. Never use this for a value that must round trip.
        /// </summary>
        public string GetArgAsString(int argIndex)
        {
            return Encoding.UTF8.GetString(_arguments[argIndex + 1]);
        }

        /// <summary>Case-insensitive comparison of an operand against an ASCII keyword.</summary>
        public bool ArgEquals(int argIndex, string keyword)
        {
            return AsciiEqualsIgnoreCase(_arguments[argIndex + 1], keyword);
        }

        /// <summary>
        /// Renders the first operands for an error message, the way Redis does in
        /// "unknown command 'FOO', with args beginning with: 'bar'".
        /// Long or binary operands are truncated so an error reply cannot echo back a whole value.
        /// </summary>
        public string DescribeArgs(int maxArgs = 3, int maxArgLength = 32)
        {
            if (ArgCount == 0) return string.Empty;

            var sb = new StringBuilder();
            int count = Math.Min(ArgCount, maxArgs);

            for (int i = 0; i < count; i++)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append('\'').Append(Printable(_arguments[i + 1], maxArgLength)).Append('\'');
            }
            return sb.ToString();
        }

        private static string Printable(byte[] value, int maxLength)
        {
            if (value == null) return string.Empty;

            int length = Math.Min(value.Length, maxLength);
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                byte b = value[i];
                // Keep printable ASCII only. Anything else - including CR and LF, which would
                // corrupt the framing of the error reply we are building - becomes a dot.
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            if (value.Length > maxLength) sb.Append("...");
            return sb.ToString();
        }

        internal static string AsciiUpper(byte[] value)
        {
            if (value == null || value.Length == 0) return string.Empty;

            var chars = new char[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                byte b = value[i];
                chars[i] = (char)(b >= (byte)'a' && b <= (byte)'z' ? b - 32 : b);
            }
            return new string(chars);
        }

        internal static bool AsciiEqualsIgnoreCase(byte[] value, string keyword)
        {
            if (value == null || value.Length != keyword.Length) return false;

            for (int i = 0; i < value.Length; i++)
            {
                byte b = value[i];
                if (b >= (byte)'a' && b <= (byte)'z') b -= 32;

                char k = keyword[i];
                if (k >= 'a' && k <= 'z') k = (char)(k - 32);

                if (b != (byte)k) return false;
            }
            return true;
        }
    }
}
