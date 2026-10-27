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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// The arguments of a container command, after the command name.
    /// <para>
    /// Only a handful of members take this, and the reason is worth stating because it looks like a
    /// loophole in a layer whose point is typed signatures. Redis has two kinds of command. Most are
    /// leaves - GET takes a key, HSET takes a key and pairs - and those get real parameters. A
    /// minority are containers: CLUSTER, CLIENT, CONFIG, COMMAND, XINFO, OBJECT, ACL and the rest
    /// are a dozen unrelated operations sharing a first word, and Redis itself dispatches them on
    /// their subcommand.
    /// </para>
    /// <para>
    /// Modelling those as one typed method per subcommand would mean inventing roughly eighty
    /// signatures for operations this build mostly refuses, and guessing now at shapes that should
    /// be decided when each is implemented. Taking the arguments instead is the honest option: it
    /// says "this command is a family, and the family dispatches internally", which is true of
    /// Redis and not a concession.
    /// </para>
    /// <para>
    /// What matters for the layering is that this is not <c>RespRequest</c>. There is no wire format
    /// here, no connection, no writer, and nothing to parse - just the arguments as bytes. An
    /// implementation can be called from a test with a string array.
    /// </para>
    /// </summary>
    internal sealed class RespArgs
    {
        private static readonly byte[][] Empty = new byte[0][];
        private readonly byte[][] _args;

        public RespArgs(byte[][] arguments)
        {
            _args = arguments ?? Empty;
        }

        /// <summary>Builds arguments from text. For tests and for callers that have strings already.</summary>
        public static RespArgs FromStrings(params string[] arguments)
        {
            if (arguments == null) return new RespArgs(Empty);

            var raw = new byte[arguments.Length][];
            for (int i = 0; i < arguments.Length; i++)
                raw[i] = arguments[i] == null ? null : Encoding.UTF8.GetBytes(arguments[i]);

            return new RespArgs(raw);
        }

        /// <summary>How many arguments followed the command name.</summary>
        public int Count
        {
            get { return _args.Length; }
        }

        /// <summary>The argument at this position, or null when there is none - never an exception.</summary>
        public byte[] this[int index]
        {
            get { return index >= 0 && index < _args.Length ? _args[index] : null; }
        }

        /// <summary>The argument at this position as text, or null when there is none.</summary>
        public string Text(int index)
        {
            byte[] raw = this[index];
            return raw == null ? null : Encoding.UTF8.GetString(raw);
        }

        /// <summary>
        /// The argument at this position compared to a keyword, case-insensitively - how a
        /// subcommand is recognised. Redis accepts subcommands in any case.
        /// </summary>
        public bool Matches(int index, string keyword)
        {
            byte[] raw = this[index];
            if (raw == null || keyword == null || raw.Length != keyword.Length) return false;

            for (int i = 0; i < raw.Length; i++)
            {
                int a = raw[i];
                int b = keyword[i];
                if (a >= 'a' && a <= 'z') a -= 32;
                if (b >= 'a' && b <= 'z') b -= 32;
                if (a != b) return false;
            }

            return true;
        }

        /// <summary>The subcommand - argument zero, uppercased - or null when none was given.</summary>
        public string Subcommand
        {
            get
            {
                string first = Text(0);
                return first == null ? null : first.ToUpperInvariant();
            }
        }
    }
}
