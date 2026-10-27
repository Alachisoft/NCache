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

namespace Alachisoft.NCache.SocketServer.Resp.Protocol
{
    /// <summary>
    /// Hard bounds applied while parsing a request.
    /// <para>
    /// These are what make the port safe to bind: without them a single client could pin arbitrary
    /// amounts of server memory by announcing a huge bulk length and then stalling. The values match
    /// the ones a real Redis server enforces so that well-behaved clients never notice them.
    /// </para>
    /// </summary>
    internal static class RespLimits
    {
        /// <summary>Longest inline (non-multibulk) request accepted, in bytes. Redis: 64 KB.</summary>
        public const int MaxInlineRequestLength = 64 * 1024;

        /// <summary>Largest single bulk string accepted, in bytes. Redis proto-max-bulk-len: 512 MB.</summary>
        public const int MaxBulkLength = 512 * 1024 * 1024;

        /// <summary>Most elements accepted in one multibulk request. Redis: 1024 * 1024.</summary>
        public const int MaxMultibulkLength = 1024 * 1024;

        /// <summary>
        /// Longest protocol line (the text between a type byte and its CRLF) accepted while looking
        /// for a terminator. Guards against a client that opens with '*' and never sends a CRLF.
        /// </summary>
        public const int MaxLineLength = 64 * 1024;
    }
}
