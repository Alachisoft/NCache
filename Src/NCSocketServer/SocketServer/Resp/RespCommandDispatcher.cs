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
using System.Collections;
using System.Globalization;
using System.Text;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.SocketServer.Resp.Protocol;
using Alachisoft.NCache.SocketServer.Statistics;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// Maps RESP commands onto NCache operations.
    /// <para>
    /// The switch in <see cref="Dispatch"/> is the authoritative list of what is supported - CRUD,
    /// expiration, the INCR/DECR family, the collection types, streams, pub/sub and the cluster
    /// commands. Anything outside it answers with a clean RESP error naming the missing capability,
    /// rather than being silently ignored or killing the connection; see RespUnsupported.
    /// </para>
    /// <para>
    /// This comment used to say CRUD only, and listed several of the above as out of scope. They
    /// shipped; the comment did not keep up. Read the command table, not this paragraph.
    /// </para>
    /// </summary>
    internal sealed class RespCommandDispatcher : IRespCommandDispatcher
    {
        private const string Module = "RespCommandDispatcher";

        private readonly string _cacheName;
        private readonly PubSub.RespPubSubHub _pubsub;
        private readonly int _respPort;
        private readonly System.Net.IPAddress _selfIp;
        private readonly RespCacheGateway _gateway;

        /// <summary>
        /// The cache's log once the cache is resolved, otherwise null. Never looks the cache up:
        /// it is read on the accept path, which must not wait on the cache server's lock.
        /// </summary>
        internal Alachisoft.NCache.Common.Logger.ILogger CacheLogger
        {
            get { return _gateway.ResolvedCacheOrNull?.Context.NCacheLog; }
        }

        /// <summary>
        /// Like <see cref="CacheLogger"/>, but resolves the cache first if it is running. Only for
        /// callers that may wait on the cache server's lock - the cache-start callback, which runs
        /// on the thread doing the start and already holds it. Never call this from the accept path.
        /// </summary>
        internal Alachisoft.NCache.Common.Logger.ILogger ResolveCacheLogger()
        {
            return _gateway.CacheOrNull?.Context.NCacheLog;
        }

        /// <summary>Run once the cache has begun stopping.</summary>
        internal Action CacheStopped
        {
            set { _gateway.Stopped = value; }
        }

        /// <summary>
        /// Run inline on the thread stopping the cache, before <see cref="CacheStopped"/> is queued.
        /// Must not block: it exists so the RESP port is free before <c>Cache.Dispose</c> returns.
        /// </summary>
        internal Action CacheStopping
        {
            set { _gateway.Stopping = value; }
        }

        /// <summary>
        /// Resolves the cache now rather than waiting for the first command, and reports whether it
        /// is attached. Resolving is what subscribes to the cache's <c>CacheStopped</c>, so this is
        /// what makes the RESP port's release independent of whether any client ever sent anything.
        /// </summary>
        internal bool AttachToCache()
        {
            try { return _gateway.CacheOrNull != null; }
            catch { return false; }
        }

        /// <summary>
        /// This listener's statistics collector, supplied by the listener that owns it. Null when
        /// statistics could not be started, in which case every report is skipped.
        /// </summary>
        private readonly StatisticsCounter _stats;

        public RespCommandDispatcher(string cacheName, int respPort, System.Net.IPAddress selfIp)
            : this(cacheName, respPort, selfIp, null)
        {
        }

        public RespCommandDispatcher(string cacheName, int respPort, System.Net.IPAddress selfIp,
                                     StatisticsCounter stats)
        {
            _stats = stats;
            _cacheName = cacheName;
            _respPort = respPort;
            _selfIp = selfIp;
            _gateway = new RespCacheGateway(cacheName, respPort, selfIp);
            _pubsub = new PubSub.RespPubSubHub(_gateway, cacheName, respPort);
        }


        /// <summary>Drops a connection's subscriptions when it goes away.</summary>
        internal void OnConnectionClosed(RespConnectionState state)
        {
            try { _pubsub.RemoveConnection(state); }
            catch (Exception e) { RespLog.Detail(Module + ".Closed", e.Message); }
        }


        /// <summary>
        /// Live RESP connection count, reported by INFO. Set by the listener, which owns the
        /// connection set; left at zero if nothing wires it up.
        /// </summary>
        internal Func<long> ConnectedClientsProvider { get; set; }

        private long ConnectedClients
        {
            get
            {
                var provider = ConnectedClientsProvider;
                if (provider == null) return 0;
                try { return provider(); } catch { return 0; }
            }
        }
        public RespCommandOutcome Dispatch(RespRequest request, RespWriter writer, RespConnectionState state)
        {
            // Counted here rather than per branch below so that every command is included -
            // forwarded, refused and unknown alike - which is what Redis's
            // total_commands_processed means.
            RespInfo.CountCommand();

            // Requests/sec and Responses/sec on this listener's own collector. One per command, so
            // a pipelined burst of fifty registers as fifty rather than as one batch.
            if (_stats != null)
            {
                try
                {
                    _stats.IncrementRequestsPerSecStats(1);
                    _stats.IncrementResponsesPerSecStats(1);
                }
                catch (Exception e) { RespLog.Detail(Module + ".Stats", e.Message); }
            }

            // A dispatcher must never throw: an escaping exception would kill the connection and
            // tell the client nothing.
            try
            {
                // There is no authentication gate here, and there is none anywhere in this build:
                // AUTH is not part of NCache OSS, so every connection that reaches this point is
                // already fully privileged. The gate that used to stand here had to come first -
                // ahead of the peer handshake, or any client could announce itself as a peer and be
                // trusted; ahead of forwarding, or an unauthenticated command could be carried to
                // the owning node and the answer relayed back. That ordering is recorded because it
                // is what anything placed here again has to satisfy.
                //
                // The port is therefore as open as the network it is on. See RespListener's start
                // log, which says so on every start.

                // Another node announcing itself, having authenticated like anyone else. From here
                // on this connection is forwarded traffic: its commands are served locally or fail,
                // never forwarded onward.
                //if (request.Name == RespPeerProtocol.HandshakeCommand)
                //{
                //    state.IsPeerConnection = true;
                //    writer.WriteSimpleString(RespPeerProtocol.HandshakeReply);
                //    RespLog.Detail(Module + ".Peer",
                //        "connection " + state.RemoteEndPoint + " identified as a forwarding peer.");
                //    return RespCommandOutcome.Continue;
                //}


                // Only database 0 exists. A connection that asked for another one gets every
                // command refused, not quietly served from database 0 - see Select.
                if (RespHandshakeCommands.DatabaseBlocks(request, writer, state)) return RespCommandOutcome.Continue;

                // Redis narrows the command set to almost nothing once a connection holds a
                // subscription, and clients rely on that: sending a GET on a subscribed connection
                // should be refused, not silently answered on a link the client is treating as a
                // one-way message stream.
                if (state.InSubscriberMode && !PubSub.RespPubSubCommands.IsAllowedInSubscriberMode(request.Name))
                {
                    writer.WriteError("ERR Can't execute '" + (request.Name ?? "?").ToLowerInvariant() +
                                      "': only (P|S)SUBSCRIBE / (P|S)UNSUBSCRIBE / PING / QUIT / RESET " +
                                      "are allowed in this context");
                    return RespCommandOutcome.Continue;
                }

                switch (request.Name)
                {

                    // ---- string shape and absolute expiry ----
                    case "STRLEN":      return RespStringExtras.StrLen(request, writer, _gateway);
                    case "APPEND":      return RespStringExtras.Append(request, writer, _gateway);
                    case "SETRANGE":    return RespStringExtras.SetRange(request, writer, _gateway);
                    case "GETRANGE":
                    case "SUBSTR":      return RespStringExtras.GetRange(request, writer, _gateway);
                    case "EXPIRETIME":  return RespStringExtras.ExpireTime(request, writer, _gateway, 1000L);
                    case "PEXPIRETIME": return RespStringExtras.ExpireTime(request, writer, _gateway, 1L);

                    // ---- counters, held as strings ----
                    // There is no counter entry type in Open Source, so these are arithmetic on a
                    // string under the key lease. Redis stores INCR keys as strings too, so this is
                    // the closer model - see RespCounters.
                    case "INCR":        return RespCounters.IncrBy(request, writer, _gateway, "incr", 1, false, false);
                    case "DECR":        return RespCounters.IncrBy(request, writer, _gateway, "decr", -1, false, false);
                    case "INCRBY":      return RespCounters.IncrBy(request, writer, _gateway, "incrby", 0, true, false);
                    case "DECRBY":      return RespCounters.IncrBy(request, writer, _gateway, "decrby", 0, true, true);
                    case "INCRBYFLOAT": return RespCounters.IncrByFloat(request, writer, _gateway);
                    case "MSETNX":      return RespCounters.MSetNx(request, writer, _gateway);

                    // ---- keyspace ----
                    case "COPY":      return RespKeyspaceExtras.Copy(request, writer, _gateway);
                    case "RENAME":    return RespKeyspaceExtras.Rename(request, writer, _gateway, false);
                    case "RENAMENX":  return RespKeyspaceExtras.Rename(request, writer, _gateway, true);
                    case "OBJECT":    return RespKeyspaceExtras.Object(request, writer, _gateway);
                    case "RANDOMKEY": return RespKeyspaceExtras.RandomKey(request, writer, _gateway);

                    // ---- serialisation and string comparison ----
                    case "DUMP":      return RespSerialise.Dump(request, writer, _gateway);
                    case "RESTORE":   return RespSerialise.Restore(request, writer, _gateway);
                    case "LCS":       return RespSerialise.Lcs(request, writer, _gateway);

                    // ---- pub/sub ----
                    case "SUBSCRIBE":    return PubSub.RespPubSubCommands.Subscribe(request, writer, state, _pubsub);
                    case "UNSUBSCRIBE":  return PubSub.RespPubSubCommands.Unsubscribe(request, writer, state, _pubsub);
                    case "PSUBSCRIBE":   return PubSub.RespPubSubCommands.PSubscribe(request, writer, state, _pubsub);
                    case "PUNSUBSCRIBE": return PubSub.RespPubSubCommands.PUnsubscribe(request, writer, state, _pubsub);
                    case "PUBLISH":      return PubSub.RespPubSubCommands.Publish(request, writer, _pubsub);
                    case "PUBSUB":       return PubSub.RespPubSubCommands.PubSub(request, writer, _pubsub);

                    // Sharded pub/sub differs from plain pub/sub only in how Redis routes it
                    // between nodes. Ours is already cluster-wide, so the two behave identically.
                    case "SPUBLISH":     return PubSub.RespPubSubCommands.Publish(request, writer, _pubsub, sharded: true);
                    case "SSUBSCRIBE":   return PubSub.RespPubSubCommands.Subscribe(request, writer, state, _pubsub, sharded: true);
                    case "SUNSUBSCRIBE": return PubSub.RespPubSubCommands.Unsubscribe(request, writer, state, _pubsub, sharded: true);

                    // ---- connection / handshake ----
                    case "PING": return Ping(request, writer, state);
                    case "ECHO": return Echo(request, writer);
                    case "TIME": return Time(request, writer);
                    case "QUIT": return Quit(writer);
                    case "HELLO": return RespHandshakeCommands.Hello(request, writer, state, _cacheName);
                    case "SELECT": return RespHandshakeCommands.Select(request, writer, state);
                    case "RESET": return RespHandshakeCommands.Reset(writer, state);
                    case "CLIENT": return RespHandshakeCommands.Client(request, writer, state);
                    case "COMMAND": return RespHandshakeCommands.Command(request, writer);
                    case "CONFIG": return RespHandshakeCommands.Config(request, writer);
                    case "INFO": return RespInfo.Write(request, writer, _cacheName, _respPort, _gateway);

                    // ---- string CRUD ----
                    case "GET": return Get(request, writer);
                    case "GETEX": return GetEx(request, writer);
                    case "SET": return Set(request, writer);
                    case "SETNX": return SetNx(request, writer);
                    case "SETEX": return SetEx(request, writer, 1000L);
                    case "PSETEX": return SetEx(request, writer, 1L);
                    case "GETSET": return GetSet(request, writer);
                    case "GETDEL": return GetDel(request, writer);
                    case "DEL":
                    case "UNLINK": return Del(request, writer);
                    case "EXISTS": return Exists(request, writer);
                    case "TOUCH": return Touch(request, writer);
                    case "MGET": return MGet(request, writer);
                    case "MSET": return MSet(request, writer);
                    case "TYPE": return Type(request, writer);
                    case "FLUSHDB":
                    case "FLUSHALL": return Flush(request, writer);

                    // ---- expiration ----
                    case "EXPIRE": return Expire(request, writer, 1000L, false);
                    case "PEXPIRE": return Expire(request, writer, 1L, false);
                    case "EXPIREAT": return Expire(request, writer, 1000L, true);
                    case "PEXPIREAT": return Expire(request, writer, 1L, true);
                    case "TTL": return Ttl(request, writer, 1000L);
                    case "PTTL": return Ttl(request, writer, 1L);
                    case "PERSIST": return Persist(request, writer);

                    default:
                        return RespUnsupported.Write(request, writer);
                }
            }
            catch (Exception e)
            {
                RespLog.Error(Module + ".Dispatch",
                    "cache '" + _cacheName + "' command " + request.Name + " failed. " + e);
                RespErrorMapper.WriteError(writer, _gateway.TranslateStopped(e));
                return RespCommandOutcome.Continue;
            }
        }

        #region Connection

        /// <summary>
        /// PING, whose reply shape changes once the connection holds a subscription.
        /// <para>
        /// Outside subscriber mode it is <c>+PONG</c>, or the argument echoed back as a bulk
        /// string. Inside it, Redis answers a two-element array - <c>["pong", &lt;argument or ""&gt;]</c>
        /// - and clients depend on that. RESP2 delivers a pushed message as an array, so a
        /// subscribed connection is reading arrays; making the reply an array too is what lets a
        /// client parse the stream with one shape. StackExchange.Redis casts the reply to an array
        /// unconditionally and throws InvalidCastException on <c>+PONG</c>, which is how this was
        /// reported. Redis 7.4 standalone was the reference, down to the empty second element.
        /// </para>
        /// <para>
        /// Under RESP3 Redis answers <c>+PONG</c> even while subscribed, because a push is a
        /// distinct type there and needs no disguise. We never serve RESP3 - HELLO 3 is refused
        /// with NOPROTO - so the subscriber form is unconditional here rather than being chosen by
        /// protocol version.
        /// </para>
        /// </summary>
        private static RespCommandOutcome Ping(RespRequest request, RespWriter writer, RespConnectionState state)
        {
            if (request.ArgCount > 1) return WriteWrongArity(request, writer);

            if (state != null && state.InSubscriberMode)
            {
                writer.WriteArrayHeader(2);
                writer.WriteBulkString("pong");
                writer.WriteBulkString(request.ArgCount == 1 ? request.GetArg(0) : EmptyArgument);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgCount == 0) writer.WriteSimpleString("PONG");
            else writer.WriteBulkString(request.GetArg(0));

            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// The empty second element of a subscriber-mode PING reply. A zero-length array rather
        /// than null: null would encode as the null bulk string, and Redis sends an empty one.
        /// </summary>
        private static readonly byte[] EmptyArgument = new byte[0];

        private static RespCommandOutcome Echo(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount != 1) return WriteWrongArity(request, writer);

            writer.WriteBulkString(request.GetArg(0));
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// TIME - the server clock, as [unix seconds, microseconds within that second].
        /// <para>
        /// Reads nothing but the clock: no key, no cache lookup, no engine call. It sits here
        /// beside PING and ECHO rather than in the gateway because there is nothing for a gateway
        /// to do.
        /// </para>
        /// <para>
        /// Both elements are bulk strings, not integers. Redis returns them that way and clients
        /// parse the text, so writing integers would be the wrong RESP type even though the values
        /// are numeric.
        /// </para>
        /// <para>
        /// Each node answers from its own clock, exactly as a Redis cluster does. Two nodes can
        /// therefore disagree by whatever the machines' clock skew is; nothing here corrects for
        /// that, and no NCache behaviour depends on it.
        /// </para>
        /// </summary>
        private static RespCommandOutcome Time(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount != 0) return WriteWrongArity(request, writer);

            TimeSpan sinceEpoch = DateTime.UtcNow - UnixEpoch;

            long seconds = (long)sinceEpoch.TotalSeconds;

            // Microseconds elapsed within the current second - not the total. Ticks are 100ns, so
            // the remainder within one second divides down by 10.
            long microseconds = (sinceEpoch.Ticks % TimeSpan.TicksPerSecond) / 10;

            writer.WriteArrayHeader(2);
            writer.WriteBulkString(seconds.ToString(CultureInfo.InvariantCulture));
            writer.WriteBulkString(microseconds.ToString(CultureInfo.InvariantCulture));

            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome Quit(RespWriter writer)
        {
            writer.WriteSimpleString("OK");
            return RespCommandOutcome.CloseConnection;
        }

        #endregion

        #region String CRUD

        /// <summary>GETEX key [EX seconds | PX milliseconds | EXAT seconds | PXAT milliseconds | PERSIST]</summary>
        private RespCommandOutcome GetEx(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount < 1) return WriteWrongArity(request, writer);

            DateTime? deadline = null;
            bool persist = false;
            if (request.ArgCount > 1)
            {
                persist = request.ArgEquals(1, "PERSIST");
                if (persist)
                {
                    if (request.ArgCount != 2) return WriteSyntaxError(writer);
                }
                else
                {
                    bool seconds = request.ArgEquals(1, "EX") || request.ArgEquals(1, "EXAT");
                    bool absolute = request.ArgEquals(1, "EXAT") || request.ArgEquals(1, "PXAT");
                    if (request.ArgCount != 3 ||
                        !(seconds || request.ArgEquals(1, "PX") || request.ArgEquals(1, "PXAT")))
                        return WriteSyntaxError(writer);

                    long number;
                    if (!TryParseLong(request.GetArg(2), out number)) return WriteNotAnInteger(writer);
                    if (number <= 0) return WriteInvalidExpire(writer, "getex");
                    try
                    {
                        long milliseconds = checked(number * (seconds ? 1000L : 1L));
                        deadline = (absolute ? UnixEpoch : DateTime.UtcNow).AddTicks(
                            checked(milliseconds * TimeSpan.TicksPerMillisecond));
                    }
                    catch (OverflowException) { return WriteInvalidExpire(writer, "getex"); }
                    catch (ArgumentOutOfRangeException) { return WriteInvalidExpire(writer, "getex"); }
                }
            }

            string key = KeyOf(request, 0);
            
            EntryType? type;
            byte[] value = _gateway.Get(key, out type);
  
            if (value != null)
            {
                if (deadline.HasValue && deadline.Value <= DateTime.UtcNow) _gateway.Delete(key);
                else if (deadline.HasValue || persist)
                    _gateway.ReplaceExpiration(key, deadline);
            }

            writer.WriteBulkString(value);

            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome Get(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount != 1) return WriteWrongArity(request, writer);

            string key = KeyOf(request, 0);

            EntryType? type;
            byte[] value = _gateway.Get(key, out type);

            writer.WriteBulkString(value);
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// SET key value [EX s|PX ms|EXAT ts|PXAT ts|KEEPTTL] [NX|XX] [GET]
        /// </summary>
        private RespCommandOutcome Set(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount < 2) return WriteWrongArity(request, writer);

            string key = KeyOf(request, 0);
            byte[] value = request.GetArg(1);

            bool nx = false, xx = false, keepTtl = false, returnOld = false;
            DateTime? deadline = null;

            for (int i = 2; i < request.ArgCount; i++)
            {
                if (request.ArgEquals(i, "NX")) { nx = true; continue; }
                if (request.ArgEquals(i, "XX")) { xx = true; continue; }
                if (request.ArgEquals(i, "GET")) { returnOld = true; continue; }
                if (request.ArgEquals(i, "KEEPTTL")) { keepTtl = true; continue; }

                bool isEx = request.ArgEquals(i, "EX");
                bool isPx = request.ArgEquals(i, "PX");
                bool isExAt = request.ArgEquals(i, "EXAT");
                bool isPxAt = request.ArgEquals(i, "PXAT");

                if (isEx || isPx || isExAt || isPxAt)
                {
                    if (i + 1 >= request.ArgCount) return WriteSyntaxError(writer);

                    long number;
                    if (!TryParseLong(request.GetArg(i + 1), out number)) return WriteNotAnInteger(writer);

                    long multiplier = (isEx || isExAt) ? 1000L : 1L;

                    if (isExAt || isPxAt)
                    {
                        deadline = UnixEpoch.AddMilliseconds(number * multiplier);
                    }
                    else
                    {
                        if (number <= 0) return WriteInvalidExpire(writer, "set");
                        deadline = DateTime.UtcNow.AddMilliseconds(number * multiplier);
                    }

                    i++;
                    continue;
                }

                return WriteSyntaxError(writer);
            }

            if (nx && xx) return WriteSyntaxError(writer);
            if (keepTtl && deadline.HasValue) return WriteSyntaxError(writer);

            byte[] oldValue = returnOld ? _gateway.Get(key) : null;

            // An absolute deadline already in the past means "store then immediately expire",
            // which Redis collapses into deleting the key and reporting OK. Writing the value
            // first would leave it readable until the cleanup sweep noticed, which on NCache can
            // be up to fifteen seconds.
            if (deadline.HasValue && deadline.Value <= DateTime.UtcNow && !nx && !xx)
            {
                _gateway.Delete(key);
                if (returnOld) writer.WriteBulkString(oldValue); else writer.WriteSimpleString("OK");
                return RespCommandOutcome.Continue;
            }

            // NX is the one conditional path the engine enforces itself, so it is genuinely atomic.
            if (nx)
            {
                bool added = _gateway.Add(key, value, ExpirationFor(deadline));

                if (returnOld) writer.WriteBulkString(oldValue);
                else if (added) writer.WriteSimpleString("OK");
                else writer.WriteNull();

                return RespCommandOutcome.Continue;
            }

            // XX and KEEPTTL both need a read before the write, with no lock between them - a
            // concurrent writer on this key can be lost. See poc-divergences.md section 2.
            if (xx && !_gateway.Contains(key))
            {
                if (returnOld) writer.WriteBulkString(oldValue);
                else writer.WriteNull();
                return RespCommandOutcome.Continue;
            }

            ExpirationHint expiration = ExpirationFor(deadline);

            if (keepTtl)
            {
                long remaining = _gateway.GetTtlMilliseconds(key);
                if (remaining > 0) expiration = _gateway.CreateAbsoluteExpiration(DateTime.UtcNow.AddMilliseconds(remaining));
            }

            _gateway.Set(key, value, expiration);

            if (returnOld) writer.WriteBulkString(oldValue);
            else writer.WriteSimpleString("OK");

            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome SetNx(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount != 2) return WriteWrongArity(request, writer);

            bool added = _gateway.Add(KeyOf(request, 0), request.GetArg(1), null);
            writer.WriteInteger(added ? 1 : 0);
            return RespCommandOutcome.Continue;
        }

        /// <summary>SETEX key seconds value / PSETEX key milliseconds value.</summary>
        private RespCommandOutcome SetEx(RespRequest request, RespWriter writer, long unitInMilliseconds)
        {
            if (request.ArgCount != 3) return WriteWrongArity(request, writer);

            long ttl;
            if (!TryParseLong(request.GetArg(1), out ttl)) return WriteNotAnInteger(writer);
            if (ttl <= 0) return WriteInvalidExpire(writer, request.Name.ToLowerInvariant());

            var deadline = DateTime.UtcNow.AddMilliseconds(ttl * unitInMilliseconds);
            _gateway.Set(KeyOf(request, 0), request.GetArg(2), _gateway.CreateAbsoluteExpiration(deadline));

            writer.WriteSimpleString("OK");
            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome GetSet(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount != 2) return WriteWrongArity(request, writer);

            string key = KeyOf(request, 0);

            // Read then write with nothing in between; a racing SET is lost. poc-divergences.md 2.
            byte[] previous = _gateway.Get(key);
            _gateway.Set(key, request.GetArg(1), null);

            writer.WriteBulkString(previous);
            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome GetDel(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount != 1) return WriteWrongArity(request, writer);

            // Atomic: Remove hands back the value it removed in one call.
            writer.WriteBulkString(_gateway.Remove(KeyOf(request, 0)));
            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome Del(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount < 1) return WriteWrongArity(request, writer);

            int removed = 0;
            for (int i = 0; i < request.ArgCount; i++)
            {
                if (_gateway.Delete(KeyOf(request, i))) removed++;
            }

            writer.WriteInteger(removed);
            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome Exists(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount < 1) return WriteWrongArity(request, writer);

            // Redis counts duplicates: EXISTS k k on an existing k returns 2. Contains() reports
            // distinct keys, so each argument is counted individually to preserve that.
            int count = 0;
            for (int i = 0; i < request.ArgCount; i++)
            {
                if (_gateway.Contains(KeyOf(request, i))) count++;
            }

            writer.WriteInteger(count);
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// TOUCH key [key ...] - counts how many of the given keys exist.
        /// <para>
        /// The count is exactly Redis's, including the detail that a key named twice counts twice.
        /// </para>
        /// <para>
        /// KNOWN PARTIAL: Redis's TOUCH also refreshes each key's last-access time, so that a
        /// client can keep hot keys out of the LRU eviction path without transferring their values.
        /// That half is NOT implemented, and the omission is deliberate. The only way to refresh
        /// recency from here is to read the key, and a read in NCache also resets a sliding
        /// expiration - IdleExpiration.Reset fires on access. That would silently extend the
        /// lifetime of any key carrying an idle hint, which Redis's TOUCH never does. Failing to
        /// refresh eviction order is a no-op; silently postponing expiry changes data lifetime, and
        /// between the two the no-op is the safer wrong answer.
        /// </para>
        /// <para>
        /// So TOUCH is currently an alias for EXISTS. Anyone using it to protect keys from eviction
        /// will find it does nothing for them. Doing this properly needs an engine call that
        /// updates the eviction hint without touching the expiration hint.
        /// </para>
        /// </summary>
        private RespCommandOutcome Touch(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount < 1) return WriteWrongArity(request, writer);

            var keys = new ArrayList(request.ArgCount);
            for (int i = 0; i < request.ArgCount; i++) keys.Add(KeyOf(request, i));

            writer.WriteInteger(_gateway.CountExisting(keys));
            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome MGet(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount < 1) return WriteWrongArity(request, writer);

            var keys = new string[request.ArgCount];
            for (int i = 0; i < request.ArgCount; i++) keys[i] = KeyOf(request, i);

            EntryType?[] types;
            byte[][] values = _gateway.GetBulk(keys, out types);

            writer.WriteArrayHeader(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                writer.WriteBulkString(values[i]);
            }

            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome MSet(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount < 2 || request.ArgCount % 2 != 0) return WriteWrongArity(request, writer);

            for (int i = 0; i < request.ArgCount; i += 2)
            {
                _gateway.Set(KeyOf(request, i), request.GetArg(i + 1), null);
            }

            writer.WriteSimpleString("OK");
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// FLUSHDB / FLUSHALL [ASYNC|SYNC] - empties the cache.
        /// <para>
        /// The two are the same operation here. Redis distinguishes them because it has sixteen
        /// numbered databases and FLUSHDB clears only the current one; an NCache cache is a single
        /// keyspace and SELECT accepts nothing but 0, so "this database" and "all databases" name
        /// the same thing.
        /// </para>
        /// <para>
        /// ASYNC and SYNC are accepted and both behave synchronously. In Redis the flag only
        /// chooses whether memory is reclaimed on a background thread - the keyspace is empty
        /// immediately either way, and the reply is +OK either way. NCache's ClearAsync is a
        /// genuinely fire-and-forget call, so using it for ASYNC would let a client see keys after
        /// its FLUSH returned OK, which is the one thing Redis guarantees will not happen.
        /// </para>
        /// <para>
        /// BLAST RADIUS: this is cluster-wide, unlike Redis, where it clears one node. See
        /// <see cref="RespCacheGateway.Clear"/>. Combined with authentication being off by default,
        /// anything that can reach this port can empty the entire cache in one command.
        /// </para>
        /// </summary>
        private RespCommandOutcome Flush(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount > 1) return WriteSyntaxError(writer);

            if (request.ArgCount == 1 &&
                !request.ArgEquals(0, "ASYNC") && !request.ArgEquals(0, "SYNC"))
            {
                return WriteSyntaxError(writer);
            }

            string name = (request.Name ?? "FLUSHDB").ToUpperInvariant();

            // Logged at a level an operator will actually see. A cache being emptied is not
            // something anyone should have to reconstruct from a client's history afterwards.
            RespLog.Critical(Module + ".Flush",
                name + " on cache '" + _cacheName + "': clearing every key. This is cluster-wide.");

            _gateway.Clear();

            writer.WriteSimpleString("OK");
            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome Type(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount != 1) return WriteWrongArity(request, writer);

            string key = KeyOf(request, 0);

            EntryType? type = _gateway.GetEntryType(key);

            writer.WriteSimpleString(type == null ? "none" : RedisTypeName(key, type.Value));
            return RespCommandOutcome.Continue;
        }

        private string RedisTypeName(string key, EntryType type)
        {
            switch (type)
            {
                default: return "string";       // CacheItem, and Counter by the note above
            }
        }

        #endregion

        #region Expiration

        /// <summary>
        /// Whether an NX / XX / GT / LT condition permits a new expiry to be applied.
        /// <para>
        /// A key with no TTL is treated as expiring infinitely far in the future, which is what
        /// makes the two comparison forms asymmetric in Redis: GT can never beat "no expiry", so it
        /// always declines, while LT always beats it and always applies. Getting that backwards is
        /// the classic way these four flags go wrong.
        /// </para>
        /// <para>
        /// Read-then-write, with no lock: a concurrent writer can change the TTL between the
        /// comparison and the update. Redis evaluates this atomically. The window is small and the
        /// alternative is holding a key lease across an operation Redis clients expect to be
        /// instant.
        /// </para>
        /// </summary>
        private bool ExpiryConditionHolds(string key, string condition, DateTime deadline)
        {
            long remaining = _gateway.GetTtlMilliseconds(key);

            // -2 is "no such key". Every condition declines, and Redis answers 0 either way.
            if (remaining == -2) return false;

            bool hasTtl = remaining >= 0;

            switch (condition)
            {
                case "NX": return !hasTtl;
                case "XX": return hasTtl;
                case "GT":
                    if (!hasTtl) return false;      // nothing is greater than no expiry at all
                    return deadline > DateTime.UtcNow.AddMilliseconds(remaining);
                case "LT":
                    if (!hasTtl) return true;       // everything is sooner than no expiry at all
                    return deadline < DateTime.UtcNow.AddMilliseconds(remaining);
                default: return true;
            }
        }

        private RespCommandOutcome Expire(RespRequest request, RespWriter writer, long unitInMilliseconds, bool absolute)
        {
            // Redis 7 added an optional NX / XX / GT / LT condition after the deadline.
            if (request.ArgCount < 2 || request.ArgCount > 3) return WriteWrongArity(request, writer);

            long number;
            if (!TryParseLong(request.GetArg(1), out number)) return WriteNotAnInteger(writer);

            string condition = null;
            if (request.ArgCount == 3)
            {
                condition = Encoding.ASCII.GetString(request.GetArg(2) ?? new byte[0]).ToUpperInvariant();
                if (condition != "NX" && condition != "XX" && condition != "GT" && condition != "LT")
                {
                    writer.WriteError("ERR Unsupported option " + condition);
                    return RespCommandOutcome.Continue;
                }
            }

            string key = KeyOf(request, 0);

            DateTime deadline = absolute
                ? UnixEpoch.AddMilliseconds(number * unitInMilliseconds)
                : DateTime.UtcNow.AddMilliseconds(number * unitInMilliseconds);

            if (condition != null && !ExpiryConditionHolds(key, condition, deadline))
            {
                writer.WriteInteger(0);
                return RespCommandOutcome.Continue;
            }

            // Redis deletes the key outright when the deadline is already past.
            if (deadline <= DateTime.UtcNow)
            {
                writer.WriteInteger(_gateway.Delete(key) ? 1 : 0);
                return RespCommandOutcome.Continue;
            }

            bool applied = _gateway.AddExpiration(key, _gateway.CreateAbsoluteExpiration(deadline));

            if (!applied)
            {
                RespLog.Detail(Module + ".Expire",
                    "AddExpirationHint refused key '" + key + "'; it is either absent or already " +
                    "carries an incompatible (sliding) hint.");
            }

            writer.WriteInteger(applied ? 1 : 0);
            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome Ttl(RespRequest request, RespWriter writer, long unitInMilliseconds)
        {
            if (request.ArgCount != 1) return WriteWrongArity(request, writer);

            long milliseconds = _gateway.GetTtlMilliseconds(KeyOf(request, 0));

            if (milliseconds < 0)
            {
                writer.WriteInteger(milliseconds);   // -2 no such key, -1 no expiry
                return RespCommandOutcome.Continue;
            }

            long value = unitInMilliseconds == 1L
                ? milliseconds
                : (milliseconds + unitInMilliseconds - 1) / unitInMilliseconds;   // round up, as Redis does

            writer.WriteInteger(value);
            return RespCommandOutcome.Continue;
        }

        private RespCommandOutcome Persist(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount != 1) return WriteWrongArity(request, writer);

            writer.WriteInteger(_gateway.Persist(KeyOf(request, 0)) ? 1 : 0);
            return RespCommandOutcome.Continue;
        }

        private ExpirationHint ExpirationFor(DateTime? deadline)
        {
            return deadline.HasValue ? _gateway.CreateAbsoluteExpiration(deadline.Value) : null;
        }

        #endregion

        // The refusal table moved to RespUnsupported.cs, where each entry names the engine
        // capability the command would need. It had drifted badly here - still claiming INCR and
        // pub/sub were unsupported months after both shipped.

        #region Helpers

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static string KeyOf(RespRequest request, int argIndex)
        {
            // Keys are UTF-8 text on the NCache side; values stay raw bytes.
            return Encoding.UTF8.GetString(request.GetArg(argIndex));
        }

        private static bool TryParseLong(byte[] raw, out long value)
        {
            value = 0;
            if (raw == null || raw.Length == 0) return false;

            return long.TryParse(
                Encoding.ASCII.GetString(raw), NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out value);
        }

        private static RespCommandOutcome WriteWrongArity(RespRequest request, RespWriter writer)
        {
            writer.WriteError("ERR wrong number of arguments for '" + request.Name.ToLowerInvariant() + "' command");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome WriteSyntaxError(RespWriter writer)
        {
            writer.WriteError("ERR syntax error");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome WriteNotAnInteger(RespWriter writer)
        {
            writer.WriteError("ERR value is not an integer or out of range");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome WriteInvalidExpire(RespWriter writer, string command)
        {
            writer.WriteError("ERR invalid expire time in '" + command + "' command");
            return RespCommandOutcome.Continue;
        }

        #endregion
    }
}
