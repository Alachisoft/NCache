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
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>What the connection should do after a command has been handled.</summary>
    internal enum RespCommandOutcome
    {
        /// <summary>Keep reading from this connection.</summary>
        Continue,

        /// <summary>Flush whatever has been written, then close - QUIT, or a fatal protocol error.</summary>
        CloseConnection
    }

    /// <summary>
    /// Turns a parsed request into a reply.
    /// <para>
    /// Kept behind an interface so the transport can be tested against a stub, and so the codec and
    /// connection handling stay independent of anything that touches the cache.
    /// </para>
    /// <para>
    /// Implementations must not throw: every failure belongs on the wire as a RESP error. They are
    /// called on the connection's receive thread, one command at a time per connection, which is
    /// what keeps pipelined replies in request order.
    /// </para>
    /// </summary>
    internal interface IRespCommandDispatcher
    {
        /// <param name="state">
        /// The calling connection's own state. One dispatcher serves every connection, so anything
        /// per-client - CLIENT SETNAME, the connection id - has to come in from here.
        /// </param>
        RespCommandOutcome Dispatch(RespRequest request, RespWriter writer, RespConnectionState state);
    }
}
