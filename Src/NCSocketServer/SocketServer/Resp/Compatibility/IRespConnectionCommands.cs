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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// The connection and handshake commands - 8 of them, on 8 members: all 8 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// These are answered by the listener itself, not by a data provider - they act on the
    /// connection, not the keyspace. They are declared here anyway, because the point of this
    /// interface is that the command surface is complete and nothing is missing by accident. A
    /// provider that only stores data leaves every one of them alone.
    /// </para>
    /// </summary>
    internal interface IRespConnectionCommands
    {
        /// <summary>AUTH. A null username is the one-argument form, which is the node credential rather than a user.</summary>
        [RespCommand("AUTH")]
        bool Authenticate(string username, byte[] password);

        /// <summary>HELLO. A container command: it carries an optional protocol version and an AUTH clause.</summary>
        [RespCommand("HELLO")]
        RespReply Hello(RespArgs args);

        /// <summary>PING. Answers PONG, or echoes the message when one is given.</summary>
        [RespCommand("PING")]
        RespReply Ping(byte[] message);

        /// <summary>ECHO.</summary>
        [RespCommand("ECHO")]
        byte[] Echo(byte[] message);

        /// <summary>QUIT. Replies OK and closes.</summary>
        [RespCommand("QUIT")]
        void Quit();

        /// <summary>RESET. Drops subscriptions, authentication and any other per-connection state.</summary>
        [RespCommand("RESET")]
        void Reset();

        /// <summary>SELECT. An NCache cache is a single keyspace, so only database 0 is accepted.</summary>
        [RespCommand("SELECT")]
        void Select(int database);

        /// <summary>CLIENT, a container command.</summary>
        [RespCommand("CLIENT")]
        RespReply Client(RespArgs args);
    }
}
