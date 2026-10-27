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
using System.Threading;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// Per-connection state the handshake commands need.
    /// <para>
    /// The dispatcher is shared by every connection, so anything a client sets about itself -
    /// CLIENT SETNAME, CLIENT SETINFO, the connection id reported by CLIENT ID and HELLO - has to
    /// live here rather than on the dispatcher.
    /// </para>
    /// <para>
    /// Only ever touched from its own connection's receive chain, so no locking is needed beyond
    /// the id counter.
    /// </para>
    /// </summary>
    internal sealed class RespConnectionState
    {
        private static long _nextId;

        public RespConnectionState(string remoteEndPoint)
        {
            Id = Interlocked.Increment(ref _nextId);
            RemoteEndPoint = remoteEndPoint ?? "?";
        }

        /// <summary>Monotonic per-process id, reported by CLIENT ID and in the HELLO reply.</summary>
        public long Id { get; }

        public string RemoteEndPoint { get; }

        /// <summary>Set by CLIENT SETNAME; empty until then, which is what Redis reports.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Set by CLIENT SETINFO LIB-NAME, which redis-py 5 and recent StackExchange.Redis send.</summary>
        public string LibraryName { get; set; } = string.Empty;

        /// <summary>Set by CLIENT SETINFO LIB-VER.</summary>
        public string LibraryVersion { get; set; } = string.Empty;

        /// <summary>
        /// True once the peer has identified itself as another NCache node forwarding a command.
        /// <para>
        /// This is the loop guard. Commands arriving on such a connection are served locally or
        /// fail; they are never forwarded onward. Without it, two nodes with momentarily
        /// disagreeing views of ownership - which is exactly what happens mid-rebalance - would
        /// bounce a command between each other indefinitely.
        /// </para>
        /// </summary>
        public bool IsPeerConnection { get; set; }

        /// <summary>
        /// The database index the client last asked for with SELECT. Only 0 exists - an NCache
        /// cache is a single keyspace - but a non-zero request is remembered rather than ignored,
        /// so that every command the client then sends "to" that database is refused instead of
        /// quietly landing in database 0. See <see cref="RespHandshakeCommands.DatabaseBlocks"/>.
        /// </summary>
        public long SelectedDatabase { get; set; }

        #region Subscriber mode

        /// <summary>
        /// Channels this connection has subscribed to, and the glob patterns it is watching.
        /// <para>
        /// Unlike everything else here these ARE touched from another thread - the pub/sub poller
        /// reads them while fanning a message out - so both are guarded by <see cref="SubSync"/>.
        /// The sets themselves stay small; a client with thousands of subscriptions is not a shape
        /// worth optimising for.
        /// </para>
        /// </summary>
        public HashSet<string> Channels { get; } = new HashSet<string>(StringComparer.Ordinal);

        public HashSet<string> Patterns { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// The subset of <see cref="Channels"/> that arrived through SSUBSCRIBE. Delivery is the
        /// same either way, but the wire differs: a sharded subscriber expects <c>ssubscribe</c>
        /// acknowledgements and <c>smessage</c> pushes, and a client library that asked for a
        /// sharded channel silently drops a plain <c>message</c> for it. Guarded by <see cref="SubSync"/>.
        /// </summary>


        /// <summary>Guards <see cref="Channels"/> and <see cref="Patterns"/>.</summary>
        public object SubSync { get; } = new object();

        /// <summary>True when this connection subscribed to <paramref name="channel"/> with SSUBSCRIBE.</summary>
        /// <summary>Channels plus patterns - the count Redis reports in a SUBSCRIBE reply.</summary>
        public int SubscriptionCount
        {
            get { lock (SubSync) { return Channels.Count + Patterns.Count; } }
        }

        /// <summary>
        /// True while the connection is in subscriber mode, where Redis accepts only a small set of
        /// commands and everything else is an error.
        /// </summary>
        public bool InSubscriberMode
        {
            get { lock (SubSync) { return Channels.Count > 0 || Patterns.Count > 0; } }
        }

        /// <summary>
        /// Where pushed messages go. Set by the connection when it starts; left null in tests that
        /// exercise the dispatcher without a socket, where a push is simply dropped.
        /// </summary>
        public IRespPushTarget PushTarget { get; set; }

        /// <summary>Sends an already-encoded push frame. False if there is nowhere to send it.</summary>
        public bool Push(byte[] frame)
        {
            var target = PushTarget;
            return target != null && target.Push(frame);
        }

        #endregion
    }

    /// <summary>
    /// Something a pushed message can be written to - in practice the connection's socket.
    /// <para>
    /// An interface rather than a direct reference so the pub/sub layer never sees the transport,
    /// and so a test can capture pushes without opening a socket.
    /// </para>
    /// </summary>
    internal interface IRespPushTarget
    {
        /// <summary>Writes a complete pre-encoded frame. Returns false if the peer is gone.</summary>
        bool Push(byte[] frame);
    }
}
