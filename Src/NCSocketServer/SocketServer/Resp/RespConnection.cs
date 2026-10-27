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
using System.Net.Sockets;
using System.Threading;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// One accepted RESP client socket: receive, parse, dispatch, reply.
    /// <para>
    /// Mirrors the existing socket server's model - <see cref="SocketAsyncEventArgs"/> receives, as
    /// in <c>ConnectionManager.BeginAsyncReceive</c> - rather than inventing a second concurrency
    /// pattern. Commands execute inline on the receive completion, because every supported command
    /// is a single-key cache call and a queue hop would add latency for nothing.
    /// </para>
    /// <para>
    /// Executing inline is also what makes pipelining correct: RESP requires replies in request
    /// order, and a single receive-parse-dispatch chain per connection gives that by construction.
    /// If this is ever moved onto a worker pool, the pool MUST shard by connection.
    /// </para>
    /// </summary>
    internal sealed class RespConnection : IDisposable, IRespPushTarget
    {
        private const string Module = "RespConnection";

        private readonly RespListener _owner;
        private readonly Socket _socket;
        private readonly RespReader _reader = new RespReader();
        private readonly RespWriter _writer = new RespWriter();
        private readonly IRespCommandDispatcher _dispatcher;
        private readonly byte[] _receiveBuffer;
        private readonly string _remote;
        private readonly RespConnectionState _state;

        private SocketAsyncEventArgs _receiveArgs;
        private int _disposed;

        // Command replies are written from the receive thread; pushed pub/sub messages arrive on
        // the poller thread. Both end up in Socket.Send, and a partial send from one interleaved
        // with the other would corrupt the stream, so every write to the socket takes this.
        private readonly object _sendSync = new object();

        // When this connection last read or wrote anything, as UTC ticks. Written from the receive
        // thread and from the pub/sub poller thread, read by the listener's idle sweep, so it moves
        // through Volatile rather than a plain field. Ticks rather than a DateTime because a 64-bit
        // read is atomic on the platforms this runs on and a DateTime's would not be.
        private long _lastActivityTicks;

        internal RespConnection(RespListener owner, Socket socket, IRespCommandDispatcher dispatcher, int receiveBufferSize)
        {
            _owner = owner;
            _socket = socket;
            _dispatcher = dispatcher;
            _receiveBuffer = new byte[receiveBufferSize];

            string remote;
            try { remote = socket.RemoteEndPoint != null ? socket.RemoteEndPoint.ToString() : "?"; }
            catch { remote = "?"; }
            _remote = remote;
            _state = new RespConnectionState(remote);
            MarkActive();
            _state.PushTarget = this;
        }

        /// <summary>This connection's protocol state, for the listener that owns the connection set.</summary>
        internal RespConnectionState ConnectionState
        {
            get { return _state; }
        }

        public string RemoteEndPoint
        {
            get { return _remote; }
        }

        /// <summary>This connection's CLIENT LIST line. Formatted where CLIENT INFO is, so the two agree.</summary>
        internal string Describe()
        {
            try { return RespHandshakeCommands.DescribeClient(_state); }
            catch { return null; }
        }

        internal void Start()
        {
            try
            {
                _receiveArgs = new SocketAsyncEventArgs();
                _receiveArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
                _receiveArgs.Completed += OnReceiveCompleted;

                ReceiveLoop();
            }
            catch (Exception e)
            {
                RespLog.Error(Module + ".Start", "could not start receiving from " + _remote + ". " + e);
                Dispose();
            }
        }

        /// <summary>
        /// Issues receives until one goes asynchronous.
        /// <para>
        /// Looping rather than recursing matters: a fast client that keeps completing receives
        /// synchronously would otherwise grow the stack one frame per command until it overflows.
        /// </para>
        /// </summary>
        private void ReceiveLoop()
        {
            try
            {
                while (Volatile.Read(ref _disposed) == 0)
                {
                    if (_socket.ReceiveAsync(_receiveArgs)) return;   // completion will resume us
                    if (!ProcessReceive(_receiveArgs)) return;        // connection finished
                }
            }
            catch (ObjectDisposedException) { Dispose(); }
            catch (SocketException) { Dispose(); }
            catch (Exception e)
            {
                RespLog.Error(Module + ".ReceiveLoop", _remote + ": " + e);
                Dispose();
            }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (ProcessReceive(args)) ReceiveLoop();
        }

        /// <summary>Returns false when the connection is finished and must not be read again.</summary>
        private bool ProcessReceive(SocketAsyncEventArgs args)
        {
            if (Volatile.Read(ref _disposed) != 0) return false;

            if (args.SocketError != SocketError.Success)
            {
                RespLog.Detail(Module + ".ProcessReceive", _remote + " socket error " + args.SocketError);
                Dispose();
                return false;
            }

            int received = args.BytesTransferred;
            if (received <= 0)
            {
                RespLog.Detail(Module + ".ProcessReceive", _remote + " closed the connection");
                Dispose();
                return false;
            }

            bool keepReading;
            try
            {
                keepReading = HandleBytes(received);
            }
            catch (Exception e)
            {
                RespLog.Error(Module + ".ProcessReceive", _remote + ": " + e);
                Dispose();
                return false;
            }

            if (!keepReading)
            {
                Dispose();
                return false;
            }

            // The buffer is reused for the next receive; reset it in case a previous call shrank it.
            args.SetBuffer(0, _receiveBuffer.Length);
            return true;
        }

        /// <summary>
        /// Feeds received bytes to the parser and drains every complete command out of them.
        /// Returns false when the connection should close.
        /// </summary>
        private bool HandleBytes(int count)
        {
            MarkActive();

            _reader.Feed(_receiveBuffer, 0, count);
            _writer.Reset();

            bool close = false;

            try
            {
                RespRequest request;
                while (_reader.TryReadRequest(out request))
                {
                    if (RespLog.IsDetailedEnabled)
                        RespLog.Detail(Module + ".Command", _remote + " -> " + request.Name);

                    bool wasSubscriber = _state.InSubscriberMode;
                    RespCommandOutcome outcome = _dispatcher.Dispatch(request, _writer, _state);
                    // Subscription changes also change the non-subscriber client gauge, even
                    // when no socket connects or disconnects (including RESET/unsubscribe).
                    if (wasSubscriber != _state.InSubscriberMode)
                    {
                        _owner?.PublishConnectedClients();
                    }

                    if (outcome == RespCommandOutcome.CloseConnection)
                    {
                        close = true;
                        break;
                    }
                }
            }
            catch (RespProtocolException pe)
            {
                // Framing is lost - there is no way to find the start of the next command, so say so
                // and hang up, exactly as a real Redis server does.
                RespLog.Detail(Module + ".Protocol", _remote + " protocol error: " + pe.Message);
                _writer.WriteError("ERR Protocol error: " + pe.Message);
                close = true;
            }

            // One send per received batch, so a pipelined burst costs a single syscall.
            if (!_writer.IsEmpty && !Flush()) return false;

            return !close;
        }

        private bool Flush()
        {
            var buffer = _writer.GetBuffer();

            try
            {
                lock (_sendSync)
                {
                    if (!SendAll(buffer.Array, buffer.Offset, buffer.Count)) return false;
                }
                return true;
            }
            catch (ObjectDisposedException) { return false; }
            catch (SocketException se)
            {
                RespLog.Detail(Module + ".Flush", _remote + " send failed: " + se.SocketErrorCode);
                return false;
            }
            finally
            {
                _writer.Reset();
            }
        }


        /// <summary>
        /// Writes a pre-encoded push frame - a pub/sub message - straight to the socket.
        /// <para>
        /// Called from the pub/sub poller, never from this connection's receive thread, so it takes
        /// the same send lock the reply path uses. A failure here means the peer has gone; it is
        /// reported rather than thrown, because the poller is fanning out to many connections and
        /// one dead client must not stop the others being served.
        /// </para>
        /// </summary>

        private void MarkActive()
        {
            Volatile.Write(ref _lastActivityTicks, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Whether this connection has been silent long enough to be closed, for the listener's
        /// idle sweep.
        /// <para>
        /// A subscriber is never idle in this sense. Verified against real Redis (8.10 with
        /// <c>timeout 5</c>): a plain connection is closed, a SUBSCRIBEd one is not. That is the
        /// only sensible reading - a subscriber legitimately says nothing for hours while it waits
        /// for messages, and closing it would break the one use case that generates no traffic of
        /// its own.
        /// </para>
        /// <para>
        /// Both directions count as activity, again as Redis does: it refreshes lastinteraction when
        /// it reads from a client and when it writes to one, so a connection being fed pushed
        /// messages is not idle either.
        /// </para>
        /// </summary>
        internal bool IsIdleBeyond(int seconds)
        {
            if (seconds <= 0) return false;
            if (Volatile.Read(ref _disposed) != 0) return false;

            var state = _state;
            if (state != null && state.InSubscriberMode) return false;

            long idle = DateTime.UtcNow.Ticks - Volatile.Read(ref _lastActivityTicks);
            return idle >= seconds * TimeSpan.TicksPerSecond;
        }

        /// <summary>How long this connection has been silent, for logging.</summary>
        internal int IdleSeconds
        {
            get
            {
                long idle = DateTime.UtcNow.Ticks - Volatile.Read(ref _lastActivityTicks);
                return idle <= 0 ? 0 : (int)(idle / TimeSpan.TicksPerSecond);
            }
        }

        /// <summary>
        /// Writes a whole buffer to the socket. Callers hold <see cref="_sendSync"/>; this does not
        /// take it, because both callers need the lock to span more than just the write.
        /// </summary>
        private bool SendAll(byte[] buffer, int offset, int count)
        {
            MarkActive();

            while (count > 0)
            {
                int sent = _socket.Send(buffer, offset, count, SocketFlags.None);
                if (sent <= 0) return false;

                offset += sent;
                count -= sent;
            }
            return true;
        }

        public bool Push(byte[] frame)
        {
            if (frame == null || frame.Length == 0) return true;
            if (Volatile.Read(ref _disposed) != 0) return false;

            try
            {
                lock (_sendSync)
                {
                    if (!SendAll(frame, 0, frame.Length)) return false;
                }
                return true;
            }
            catch (ObjectDisposedException) { return false; }
            catch (SocketException se)
            {
                RespLog.Detail(Module + ".Push", _remote + " push failed: " + se.SocketErrorCode);
                return false;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            try { _socket.Shutdown(SocketShutdown.Both); } catch { }
            try { _socket.Close(); } catch { }

            if (_receiveArgs != null)
            {
                try
                {
                    _receiveArgs.Completed -= OnReceiveCompleted;
                    _receiveArgs.Dispose();
                }
                catch { }
                _receiveArgs = null;
            }

            if (_owner != null) _owner.OnConnectionClosed(this);
        }
    }
}
