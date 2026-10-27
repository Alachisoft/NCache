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

namespace Alachisoft.NCache.SocketServer.Resp.Protocol
{
    /// <summary>
    /// Thrown when an incoming byte stream is not valid RESP.
    /// <para>
    /// A protocol error is unrecoverable for the connection: once framing is lost there is no way to
    /// find the start of the next command. The connection handler must reply with
    /// <c>-ERR Protocol error: &lt;reason&gt;</c> and then close the socket, which is what a real
    /// Redis server does.
    /// </para>
    /// </summary>
    internal sealed class RespProtocolException : Exception
    {
        public RespProtocolException(string reason)
            : base(reason)
        {
        }
    }
}
