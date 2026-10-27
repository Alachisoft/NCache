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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Communication.Exceptions;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.MetricsServer;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.Common.FeatureUsageData;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// TCP listener that speaks the Redis Serialization Protocol, running inside the cache host
    /// alongside the existing protobuf listener and driving the same cache engine.
    /// <para>
    /// A cache host process serves exactly one cache, so the port identifies the cache and RESP
    /// needs no equivalent of the protobuf INIT handshake or the service's TransferConnection
    /// routing. One instance per cache host.
    /// </para>
    /// <para>
    /// SECURITY: the port is plaintext and has no authentication - neither TLS nor AUTH is part
    /// of NCache OSS. Anything that can reach it has full read and write access to the cache, so
    /// what reaches it is the only control there is.
    /// </para>
    /// </summary>
    public sealed class RespListener : IDisposable
    {
        private const string Module = "RespListener";
        private const int ReceiveBufferSize = 16 * 1024;

        private readonly string _cacheName;
        private readonly RespServerConfig _config;
        private readonly IPAddress _bindIp;
        private readonly IRespCommandDispatcher _dispatcher;
        private readonly HashSet<RespConnection> _connections = new HashSet<RespConnection>();
        private readonly object _connectionsLock = new object();

        private Socket _serverSocket;

        // Runs only when idle-timeout is set; null otherwise, so a listener with the setting off
        // carries no timer and no cost at all.
        private Timer _idleSweeper;
        private volatile bool _isListening;

        // Set once, by whichever call runs the full teardown. The port is released on one thread
        // and the connections are torn down on another, so neither "still listening" nor "socket
        // is already null" can stand in for "stopped".
        private int _stopped;
        private readonly RespCommandDispatcher _ownedDispatcher;

        /// <summary>The start entry still waiting for the cache log to exist; null once written.</summary>
        private string _startedMessage;

        /// <summary>
        /// This listener's own statistics collector, built and owned here exactly as the socket
        /// server builds one for its ConnectionManager (SocketServer.cs:266). Separate from the
        /// protobuf collector, so RESP traffic is reported as its own instance rather than folded
        /// into the client port's numbers.
        /// </summary>
        private StatisticsCounter _stats;

        private RespListener(string cacheName, RespServerConfig config, IPAddress bindIp,
                             IRespCommandDispatcher dispatcher, StatisticsCounter stats)
        {
            _cacheName = cacheName;
            _config = config;
            _bindIp = bindIp;
            _dispatcher = dispatcher;
            _ownedDispatcher = dispatcher as RespCommandDispatcher;
            _stats = stats;
        }

        public void AttachStatsSource(StatisticsCounter socketServerStats)
        {
            _stats = socketServerStats;
        }

        public int Port
        {
            get { return _config.Port; }
        }

        public string CacheName
        {
            get { return _cacheName; }
        }

        /// <summary>Live RESP connections, reported through INFO.</summary>
        internal long ConnectionCount
        {
            get { lock (_connectionsLock) return _connections.Count; }
        }

        /// <summary>
        /// Counts live connections that are not in subscriber mode. Each non-subscriber socket
        /// counts once, regardless of CLIENT SETNAME; channel and pattern subscribers are excluded.
        /// </summary>
        internal long GetClientNumdber()
        {
            RespConnection[] snapshot;
            lock (_connectionsLock)
            {
                snapshot = new RespConnection[_connections.Count];
                _connections.CopyTo(snapshot);
            }

            // Snapshot under the lock, count outside it, so a large set never holds up an accept.
            long count = 0;

            foreach (var connection in snapshot)
            {
                if (connection == null) continue;

                var state = connection.ConnectionState;
                if (state == null) continue;

                if (!state.InSubscriberMode) count++;
            }

            return count;
        }

        /// <summary>
        /// CLIENT LIST: one line per live connection.
        /// <para>
        /// The set is copied under the lock and formatted outside it, so a slow or large report
        /// never holds up an accept or a disconnect.
        /// </para>
        /// </summary>
        internal string DescribeClients()
        {
            RespConnection[] snapshot;
            lock (_connectionsLock)
            {
                snapshot = new RespConnection[_connections.Count];
                _connections.CopyTo(snapshot);
            }

            var sb = new System.Text.StringBuilder();
            foreach (var connection in snapshot)
            {
                string line = connection == null ? null : connection.Describe();
                if (string.IsNullOrEmpty(line)) continue;
                sb.Append(line).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Publishes the live RESP non-subscriber client count.
        /// <para>
        /// ConncetedClients is an absolute gauge rather than an increment, which is why this
        /// assigns the whole count instead of adjusting by one. That works cleanly here only
        /// because the collector belongs to this listener: the socket server assigns its own
        /// collector from ConnectionTable.Count on every protobuf connect and disconnect
        /// (ConnectionManager.cs:533, 615), and sharing one would have meant each overwriting the
        /// other's total.
        /// </para>
        /// </summary>
        internal void PublishConnectedClients()
        {
            var stats = _stats;
            if (stats == null) return;

            try { stats.ConncetedClients = GetClientNumdber(); }
            catch (Exception e) { RespLog.Detail(Module + ".Stats", e.Message); }
        }

        private long _connectionsReceived;
        private long _connectionsRejected;

        /// <summary>Connections refused by the max-connections cap, reported by INFO.</summary>
        internal long ConnectionsRejected
        {
            get { return System.Threading.Interlocked.Read(ref _connectionsRejected); }
        }

        /// <summary>
        /// Connections accepted since start, reported by INFO as total_connections_received.
        /// Counted after admission, so a client turned away by max-connections is not included -
        /// Redis counts those separately as rejected_connections.
        /// </summary>
        internal long ConnectionsReceived
        {
            get { return System.Threading.Interlocked.Read(ref _connectionsReceived); }
        }

        #region Start

        /// <summary>
        /// Starts a RESP listener for <paramref name="cacheName"/> if its configuration asks for one.
        /// <para>
        /// Returns null, quietly, when RESP is simply not wanted - suppressed, or no enabled entry
        /// in resp.ncconf. When RESP IS wanted and cannot be provided - a port outside the valid
        /// range, or a port that is already taken - it throws, so the cache host does not start. A
        /// cache whose clients are configured for a RESP port must not come up without it and look
        /// healthy; refusing to start is what tells the operator why.
        /// </para>
        /// <para>
        /// <paramref name="ErrorCode"/> is set to the matching <c>ProcessExitCodes</c> entry before
        /// the throw, which is what the service reads from the exited host process.
        /// </para>
        /// </summary>
        /// <returns>The running listener, or null when RESP is not enabled.</returns>
        public static RespListener Start(string cacheName, IPAddress fallbackBindIp, StatisticsCounter stats, ref int ErrorCode)
        {
            try
            {
                if (string.IsNullOrEmpty(cacheName)) return null;

                if (!ServiceConfiguration.EnableRespServer)
                {
                    RespLog.Detail(Module + ".Start",
                        "RESP listener suppressed for cache '" + cacheName +
                        "': NCacheServer.EnableRespServer is false.");
                    return null;
                }

                // resp.ncconf decides, and it decides alone: a cache listens only when the file
                // names it with enabled="true". Absent file, absent entry and enabled="false" all
                // return null here, each already logged with which of the three it was.
                RespServerConfig config = RespServerConfig.ForCache(cacheName);
                if (config == null) return null;

                if (config.Port <= 0 || config.Port > 65535)
                {
                    string portErrorMessage = "RESP listener not started for cache '" + cacheName + "': port " + config.Port +
                        " is outside the valid range 1-65535.";

                    RespLog.Error(Module + ".Start", portErrorMessage);

                    ErrorCode = 9;
                    throw new ChannelException(portErrorMessage);
                }

                IPAddress bindIp = ResolveBindIp(config, fallbackBindIp);

                var dispatcher = new RespCommandDispatcher(cacheName, config.Port, bindIp, stats);
                var listener = new RespListener(cacheName, config, bindIp, dispatcher, stats);

                // INFO reports connected_clients, and the listener is what owns the connection set.
                dispatcher.ConnectedClientsProvider = () => listener.ConnectionCount;

                // A cache host serves one cache and exits once it stops, which also stops this
                // listener. Stopping it as soon as the cache begins stopping disconnects RESP
                // clients the way protobuf clients are disconnected, instead of leaving them
                // connected to a cache that is being disposed.
                //
                // Two hooks, because the two halves of the teardown have different deadlines.
                // CacheStopping runs inline as the cache begins stopping and only gives the port
                // back; CacheStopped is queued and does the rest, so disposing the clients still
                // never holds up Cache.Dispose.
                dispatcher.CacheStopping = () => listener.StopAccepting();
                dispatcher.CacheStopped = () => listener.Stop();

                // The rest of what INFO reports about connections also lives here: the running
                // total accepted, and the configured cap it is measured against.
                RespInfo.ConnectedClientsProvider = () => listener.ConnectionCount;
                RespInfo.ConnectionsReceivedProvider = () => listener.ConnectionsReceived;
                RespInfo.MaxConnectionsProvider = () => config.MaxConnections;
                RespInfo.RejectedConnectionsProvider = () => listener.ConnectionsRejected;

                // CLIENT LIST and CONFIG GET report on the same state the listener owns.
                RespHandshakeCommands.ClientListProvider = () => listener.DescribeClients();
                RespHandshakeCommands.PortProvider = () => config.Port;
                RespHandshakeCommands.MaxConnectionsProvider = () => config.MaxConnections;
                RespHandshakeCommands.IdleTimeoutProvider = () => config.IdleTimeout;

                listener.Bind(ref ErrorCode);

                return listener;
            }
            catch (Exception e)
            {
                RespLog.Error(Module + ".Start",
                    "RESP listener not started for cache '" + cacheName + "'. " + e);
                throw;
            }
        }

        private static IPAddress ResolveBindIp(RespServerConfig config, IPAddress fallbackBindIp)
        {
            if (!string.IsNullOrEmpty(config.BindIp))
            {
                IPAddress parsed;
                if (IPAddress.TryParse(config.BindIp.Trim(), out parsed)) return parsed;

                RespLog.Warn(Module + ".ResolveBindIp",
                    "bind-ip '" + config.BindIp + "' is not a valid address; falling back to the client-server IP.");
            }

            // Same interface the protobuf client listener uses, so RESP is reachable wherever
            // NCache clients already are - and not on every interface by accident.
            return fallbackBindIp ?? IPAddress.Any;
        }

        private void Bind(ref int ErrorCode)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(new IPEndPoint(_bindIp, _config.Port));

                int backlog = _config.MaxConnections > 0
                    ? _config.MaxConnections
                    : ServiceConfiguration.MaxPendingConnections;

                socket.Listen(backlog);

                _serverSocket = socket;
                _isListening = true;

                socket.BeginAccept(AcceptCallback, socket);

                StartIdleSweeper();
            }
            catch (SocketException se)
            {
                try { socket.Close(); } catch { }

                if (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    // Loud on purpose: two caches defaulting to 9981 on one machine is the most
                    // likely misconfiguration, and a silent skip would look like the feature is broken.
                    RespLog.Error(Module + ".Bind",
                        "RESP listener NOT started for cache '" + _cacheName + "': port " + _config.Port +
                        " on " + _bindIp + " is already in use. Give each cache on this machine its own " +
                        "<resp-server port=\"...\"/>. The cache host will not start.");

                    ErrorCode = 11;
                    throw new ChannelException("RESP server could not be started on the specified port [" +
                                               _config.Port + "]. The port might be already in use.");
                }
                else
                {
                    RespLog.Error(Module + ".Bind",
                        "RESP listener NOT started for cache '" + _cacheName + "' on " + _bindIp + ":" +
                        _config.Port + ". " + se.SocketErrorCode + " - " + se.Message);
                }
                throw;
            }
            catch (Exception e)
            {
                try { socket.Close(); } catch { }
                RespLog.Error(Module + ".Bind",
                    "RESP listener NOT started for cache '" + _cacheName + "'. " + e);
                throw;
            }

            // No security warning to the Event Viewer on every listener start - deliberately,
            // and the same decision the Enterprise build made when it removed its own. There is
            // no configuration here that could make the line say anything different, so it would
            // be the same sentence on every start of every cache for the life of the install:
            // noise in the log an operator reads for events, which is the surest way to have the
            // real entries ignored. That the port is open and unauthenticated belongs in the
            // documentation, and it is in the README banner.
            LogStarted();
            AttachWhenTheCacheStarts();
        }

        /// <summary>
        /// Records that RESP is enabled for this cache, in the Event Viewer and in the cache's own
        /// log.
        /// <para>
        /// The socket-server log the lines above go to is not where an operator looks for a cache's
        /// state, and the security warning beside this one says something else - that the port is
        /// open - rather than that it opened at all. Without this entry the only evidence a cache
        /// was serving RESP lived in a file nobody reads, which is what made a working listener
        /// look like a failed one.
        /// </para>
        /// <para>
        /// The listener binds before the host is told to start the cache, so the cache log usually
        /// does not exist yet; the entry is then held and written when the cache reports it has
        /// started.
        /// </para>
        /// </summary>
        private void LogStarted()
        {
            string message = "RESP communication has been enabled for cache '" + _cacheName +
                             "'. RESP listener started on " + _bindIp + ":" + _config.Port + ".";

            try
            {
                AppUtil.LogEvent("NCache", message,
                    System.Diagnostics.EventLogEntryType.Information, EventCategories.Information,
                    EventID.GeneralInformation);
            }
            catch { }

            _startedMessage = message;
            TryLogStartedToCache();
        }

        /// <summary>Writes the start entry to the cache log once; false while that log is unavailable.</summary>
        private bool TryLogStartedToCache()
        {
            try
            {
                // Resolving is safe from both callers: at bind time nothing holds the cache
                // server's lock, and OnCacheStarted runs on the thread doing the start, which
                // already holds it. The accept path uses the non-resolving CacheLogger instead.
                var logger = _ownedDispatcher?.ResolveCacheLogger();
                if (logger == null) return false;

                string message = Interlocked.Exchange(ref _startedMessage, null);
                if (message != null) logger.CriticalInfo(Module + ".Start", message);
                return true;

            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attaches this listener to its cache as soon as the cache exists, rather than on the
        /// first command.
        /// <para>
        /// Attaching is what subscribes to that cache instance's <c>CacheStopped</c>, and that
        /// subscription is what gives the RESP port back when the cache stops. While it happened
        /// only as a side effect of a command resolving the cache, a client that connected and sent
        /// nothing but PING left the listener holding the port through the stop - and the next
        /// incarnation of the cache could then fail to bind it.
        /// </para>
        /// <para>
        /// The listener binds before the host is told to start the cache, so there is usually
        /// nothing to attach to yet; the cache's own start event is what says there is.
        /// </para>
        /// </summary>
        private void AttachWhenTheCacheStarts()
        {
            if (_ownedDispatcher == null) return;

            // Stay subscribed while either job is outstanding: attaching, and the start entry the
            // cache log could not take yet.
            if (!_ownedDispatcher.AttachToCache() || _startedMessage != null)
                Alachisoft.NCache.Caching.Cache.OnCacheStarted += OnCacheStarted;
        }

        private void OnCacheStarted(string cacheName)
        {
            if (!string.Equals(cacheName, _cacheName, StringComparison.OrdinalIgnoreCase)) return;

            // Attach first, and whether or not the entry can be written yet. Attaching is what
            // subscribes to this cache instance's CacheStopped, and that subscription is what gives
            // the RESP port back when the cache stops.
            //
            // Resolving here is safe, and is why both jobs wait for this event rather than polling:
            // it runs on the thread that is starting the cache, which already holds the cache
            // server's lock, so the resolve cannot block behind the start it belongs to.
            bool attached = _ownedDispatcher != null && _ownedDispatcher.AttachToCache();

            if (TryLogStartedToCache() && attached)
                Alachisoft.NCache.Caching.Cache.OnCacheStarted -= OnCacheStarted;
        }

#endregion

        #region Accept

        private void AcceptCallback(IAsyncResult result)
        {
            Socket clientSocket = null;

            try
            {
                clientSocket = ((Socket)result.AsyncState).EndAccept(result);
                clientSocket.NoDelay = true;   // RESP is request/response; Nagle only adds latency
            }
            catch (ObjectDisposedException)
            {
                return;   // listener stopped
            }
            catch (Exception e)
            {
                if (_isListening) RespLog.Error(Module + ".AcceptCallback", "EndAccept failed. " + e);
            }
            finally
            {
                // Re-arm before doing any work with the accepted socket, matching
                // ConnectionManager.AcceptCallback, so the backlog keeps draining.
                RearmAccept();
            }

            if (clientSocket == null) return;

            if (!TryAdmit(clientSocket)) return;

            RespConnection connection = null;
            try
            {
                connection = new RespConnection(this, clientSocket, _dispatcher, ReceiveBufferSize);

                lock (_connectionsLock)
                {
                    if (!_isListening)
                    {
                        connection.Dispose();
                        return;
                    }
                    _connections.Add(connection);
                    LogConnectivity(connection, true);
                }
                System.Threading.Interlocked.Increment(ref _connectionsReceived);

                connection.Start();
                PublishConnectedClients();
            }
            catch (Exception e)
            {
                RespLog.Error(Module + ".AcceptCallback", "could not set up the accepted connection. " + e);
                if (connection != null) connection.Dispose();
                else CloseQuietly(clientSocket);
            }
        }

        private void RearmAccept()
        {
            if (!_isListening) return;

            try
            {
                var socket = _serverSocket;
                if (socket != null) socket.BeginAccept(AcceptCallback, socket);
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                if (_isListening) RespLog.Error(Module + ".RearmAccept", "could not re-arm accept. " + e);
            }
        }

        /// <summary>Enforces max-connections, the Redis "maxclients" analogue.</summary>
        private bool TryAdmit(Socket clientSocket)
        {
            int limit = _config.MaxConnections;
            if (limit <= 0) return true;

            int current;
            lock (_connectionsLock) current = _connections.Count;

            if (current < limit) return true;

            System.Threading.Interlocked.Increment(ref _connectionsRejected);

            RespLog.Warn(Module + ".Accept",
                "refused a RESP connection for cache '" + _cacheName + "': max-connections (" +
                limit + ") reached.");

            // Tell the client why before hanging up, rather than dropping the socket silently.
            TrySendRefusal(clientSocket, "-ERR max number of clients reached\r\n");
            return false;
        }

        private static void TrySendRefusal(Socket socket, string message)
        {
            try
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(message);
                socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
            }
            catch { }
            finally
            {
                CloseQuietly(socket);
            }
        }

        private static void CloseQuietly(Socket socket)
        {
            try { socket.Shutdown(SocketShutdown.Both); } catch { }
            try { socket.Close(); } catch { }
        }

        internal void OnConnectionClosed(RespConnection connection)
        {
            lock (_connectionsLock)
            {
                if (!_connections.Remove(connection)) return;
                LogConnectivity(connection, false);
            }

            // Drop the connection's subscriptions. Nothing called this before, so every subscriber
            // that ever disconnected stayed in the pub/sub hub for the life of the cache host: it
            // was counted by PUBLISH and PUBSUB NUMSUB (a channel reused across runs reported 4
            // receivers for 2 live ones), pushes kept being attempted to its closed socket, and the
            // node stayed registered with the engine topic after its last real subscriber left.
            if (_ownedDispatcher != null) _ownedDispatcher.OnConnectionClosed(connection.ConnectionState);

            PublishConnectedClients();
        }

        /// <summary>
        /// Log socket lifecycle independently of native client registration. RESP has no INIT
        /// identity: the connection id and endpoint identify even clients that never authenticate.
        /// Only connections outside subscriber mode produce lifecycle logs and system events.
        /// At accept time every connection is outside subscriber mode; SUBSCRIBE happens later.
        /// Called under the connection lock so connect/disconnect records retain their order.
        /// </summary>
        internal void LogConnectivity(RespConnection connection, bool connected)
        {

            string module = Module + (connected ? ".OnClientConnected" : ".OnClientDisconnected");
            string message = "RESP Connection \"" + connection.ConnectionState.Id + "\" (" +
                connection.RemoteEndPoint + ") has " + (connected ? "connected to" : "disconnected from") +
                " cache '" + _cacheName + "'. # Number of Connections " + ConnectionCount;

            // A listener can accept before the cache is running. Fall back to the socket logger
            // in that window; logging failures must never prevent socket setup or cleanup.
            try
            {
                var logger = _ownedDispatcher?.CacheLogger;

                if (logger != null) logger.CriticalInfo(module, message);
                else RespLog.Critical(module, message);
            }
            catch { }

            if (ServiceConfiguration.LogClientEvents)
            {
                Alachisoft.NCache.Common.AppUtil.LogEvent("NCache", message,
                    System.Diagnostics.EventLogEntryType.Information, EventCategories.Information,
                    connected ? EventID.ClientConnected : EventID.ClientDisconnected);
            }
        }

        #endregion

        #region Stop

        /// <summary>
        /// Closes connections that have been silent past <c>idle-timeout</c>, the Redis
        /// <c>timeout</c> analogue. Does nothing when the setting is 0, which is the default and
        /// means "never close", as in Redis.
        /// <para>
        /// One timer per listener rather than one per connection: a cache host with a thousand idle
        /// clients should not hold a thousand timers, and the listener already owns the connection
        /// set this has to walk.
        /// </para>
        /// <para>
        /// The period is a second, or the timeout itself when that is shorter, so a connection is
        /// closed within about a second of crossing the line rather than at some multiple of it.
        /// </para>
        /// </summary>
        private void StartIdleSweeper()
        {
            int timeout = _config.IdleTimeout;
            if (timeout <= 0)
            {
                RespLog.Detail(Module + ".Idle",
                    "idle-timeout is 0 for cache '" + _cacheName + "'; idle connections are never closed.");
                return;
            }

            int periodMs = Math.Min(1000, timeout * 1000);
            _idleSweeper = new Timer(_ => SweepIdleConnections(), null, periodMs, periodMs);

            RespLog.Critical(Module + ".Idle",
                "cache '" + _cacheName + "': connections idle for more than " + timeout +
                "s will be closed (subscribers are exempt).");
        }

        private void SweepIdleConnections()
        {
            int timeout = _config.IdleTimeout;
            if (timeout <= 0 || !_isListening) return;

            try
            {
                RespConnection[] candidates;
                lock (_connectionsLock)
                {
                    if (_connections.Count == 0) return;
                    candidates = new RespConnection[_connections.Count];
                    _connections.CopyTo(candidates);
                }

                // Disposed outside the lock on purpose: Dispose calls back into
                // OnConnectionClosed, which takes the same lock, and holding it here would
                // deadlock the sweep against the connection it is closing.
                foreach (var connection in candidates)
                {
                    if (connection == null || !connection.IsIdleBeyond(timeout)) continue;

                    RespLog.Detail(Module + ".Idle",
                        "closing " + connection.RemoteEndPoint + ": idle " +
                        connection.IdleSeconds + "s, limit " + timeout + "s.");

                    try { connection.Dispose(); } catch { }
                }
            }
            catch (Exception e)
            {
                // A failed sweep must never take the listener down with it; the next tick retries.
                RespLog.Detail(Module + ".Idle", "sweep failed: " + e.Message);
            }
        }

        /// <summary>
        /// Closes the listening socket, and nothing else.
        /// <para>
        /// This runs on the thread that is stopping the cache, ahead of the rest of the teardown.
        /// Releasing the port is the one part that cannot be deferred: the service treats the cache
        /// as stopped the moment <c>Cache.Dispose</c> returns and may immediately start a
        /// replacement host, and that host binds this same port - resp.ncconf gives a cache one
        /// RESP port for every incarnation, so unlike the management port it cannot be reallocated
        /// when it is found busy. A late release is therefore not a slow start but a failed one.
        /// </para>
        /// <para>
        /// Closing a listening socket does not block, which is what makes it safe here while
        /// disposing the live connections stays off this thread.
        /// </para>
        /// </summary>
        internal void StopAccepting()
        {
            _isListening = false;

            var socket = _serverSocket;
            _serverSocket = null;
            if (socket == null) return;

            try
            {
                // Matches ConnectionManager.StopListening: on Linux the accept does not unblock
                // on Close() alone.
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                        System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    try { socket.Shutdown(SocketShutdown.Both); } catch { }
                }
            }
            catch { }

            try { socket.Close(); } catch { }

            RespLog.Critical(Module + ".StopAccepting",
                "RESP listener for cache '" + _cacheName + "' released " + _bindIp + ":" +
                _config.Port + "; the port is free for the next incarnation of this cache.");
        }

        /// <summary>
        /// Stops listening and drops every live connection. Safe to call more than once, and safe
        /// to call from the cache-stopped path - the cache host calls it again when it shuts down.
        /// </summary>
        public void Stop()
        {
            if (_stats != null)
            {
                // Zeroed on every call, including the host's second one: a stopped listener must
                // not leave its last connection count standing as though it were current. The
                // collector itself belongs to the host's socket server, which releases it.
                try { _stats.ConncetedClients = 0; } catch { }
            }

            // Not "are we still listening": StopAccepting may already have closed the socket on
            // the cache-stopping thread, and the connections and the sweeper would then never be
            // torn down.
            if (Interlocked.Exchange(ref _stopped, 1) != 0) return;

            StopAccepting();

            Alachisoft.NCache.Caching.Cache.OnCacheStarted -= OnCacheStarted;

            var sweeper = _idleSweeper;
            _idleSweeper = null;
            if (sweeper != null) { try { sweeper.Dispose(); } catch { } }

            RespConnection[] live;
            lock (_connectionsLock)
            {
                live = new RespConnection[_connections.Count];
                _connections.CopyTo(live);
            }

            foreach (var connection in live)
            {
                try { connection.Dispose(); } catch { }
            }

            // Close outbound forwarding connections to other nodes as well as inbound clients.
            if (_ownedDispatcher != null)
            {
                try { } catch { }
            }

            RespLog.Critical(Module + ".Stop",
                "RESP listener for cache '" + _cacheName + "' stopped; closed " + live.Length + " connection(s).");
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}
