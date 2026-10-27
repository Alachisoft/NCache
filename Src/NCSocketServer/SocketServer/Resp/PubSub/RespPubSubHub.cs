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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Messaging;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp.PubSub
{
    /// <summary>
    /// Redis pub/sub carried on NCache topics.
    /// <para>
    /// Redis pushes a message to every subscribed connection the moment it is published. NCache
    /// delivers by assignment: a subscriber is registered against a topic and then polls for what
    /// has been assigned to it. Bridging the two is this class's whole job, and it is done entirely
    /// in the RESP layer - the engine is driven through its existing public API and is not touched.
    /// </para>
    /// <para>
    /// One subscription per node, not per connection. The node registers itself as a single
    /// subscriber for each channel that any of its clients care about, polls once for all of them,
    /// and fans each message out locally to the connections that asked for it. A per-connection
    /// subscription would multiply the poll cost by the number of clients and deliver the same
    /// message many times over; this way a publish anywhere in the cluster costs one delivery per
    /// node per channel, which is the shape Redis has.
    /// </para>
    /// </summary>
    internal sealed class RespPubSubHub : IDisposable
    {
        private const string Module = "RespPubSubHub";

        /// <summary>How long to wait between polls when the last one found nothing.</summary>
        private const int IdlePollMs = 40;

        /// <summary>How long to wait when the last poll returned messages - drain fast.</summary>
        private const int BusyPollMs = 2;

        /// <summary>
        /// Shard channels live in their own namespace, as in Redis: SSUBSCRIBE x and SUBSCRIBE x
        /// are different channels, a pattern never matches a shard channel, and a shard message is
        /// pushed as "smessage". They ride the same NCache topics as plain channels, under this
        /// prefix, which <see cref="RespPubSubCommands.IsUsableChannel"/> refuses for plain ones so
        /// the two can never collide. Inside the hub and on the connection's channel set a shard
        /// channel is always held prefixed; only the wire sees the bare name.
        /// </summary>
        internal const string ShardPrefix = "__ncresp_shard__:";

        internal static string ShardTopic(string channel) { return ShardPrefix + channel; }

        internal static bool IsShardTopic(string topic)
        {
            return topic != null && topic.StartsWith(ShardPrefix, StringComparison.Ordinal);
        }

        internal static string ShardName(string topic) { return topic.Substring(ShardPrefix.Length); }

        private readonly RespCacheGateway _gateway;
        private readonly string _clientId;

        // channel -> connections subscribed to it. Guarded by _sync.
        private readonly Dictionary<string, HashSet<RespConnectionState>> _channels =
            new Dictionary<string, HashSet<RespConnectionState>>(StringComparer.Ordinal);

        // glob pattern -> connections subscribed to it. Guarded by _sync.
        private readonly Dictionary<string, HashSet<RespConnectionState>> _patterns =
            new Dictionary<string, HashSet<RespConnectionState>>(StringComparer.Ordinal);

        private readonly object _sync = new object();
        private Thread _poller;
        private int _disposed;

        public RespPubSubHub(RespCacheGateway gateway, string cacheName, int respPort)
        {
            _gateway = gateway;

            // One identity per cache host. Every RESP connection on this node shares it, which is
            // what makes a single poll serve all of them.
            _clientId = "resp$" + (cacheName ?? "cache") + "$" + respPort + "$" +
                        System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        #region Subscribe / unsubscribe

        /// <summary>
        /// Subscribes a connection to a channel. Returns the connection's total subscription count,
        /// which is what the SUBSCRIBE reply has to carry.
        /// </summary>
        public int Subscribe(RespConnectionState state, string channel)
        {
            bool firstOnThisNode;

            lock (_sync)
            {
                HashSet<RespConnectionState> set;
                if (!_channels.TryGetValue(channel, out set))
                {
                    set = new HashSet<RespConnectionState>();
                    _channels[channel] = set;
                }
                firstOnThisNode = set.Count == 0;
                set.Add(state);
                state.Channels.Add(channel);
            }

            // Only the first local subscriber needs to register the node with the cluster.
            if (firstOnThisNode) RegisterWithTopic(channel);

            EnsurePoller();
            return state.SubscriptionCount;
        }

        public int Unsubscribe(RespConnectionState state, string channel)
        {
            bool lastOnThisNode = false;

            lock (_sync)
            {
                HashSet<RespConnectionState> set;
                if (_channels.TryGetValue(channel, out set) && set.Remove(state) && set.Count == 0)
                {
                    _channels.Remove(channel);
                    lastOnThisNode = true;
                }
                state.Channels.Remove(channel);
            }

            if (lastOnThisNode) UnregisterFromTopic(channel);
            return state.SubscriptionCount;
        }

        /// <summary>
        /// Subscribes to a glob pattern. Patterns are matched locally against the channel a message
        /// arrived on, so the node still has to be registered for the concrete channels - which it
        /// is, because a pattern subscriber also gets messages for any channel another client here
        /// has subscribed to. A pattern with no matching concrete subscription anywhere sees
        /// nothing, which is a documented limit of this implementation.
        /// </summary>
        public int PSubscribe(RespConnectionState state, string pattern)
        {
            bool firstOnThisNode;

            lock (_sync)
            {
                HashSet<RespConnectionState> set;
                if (!_patterns.TryGetValue(pattern, out set))
                {
                    set = new HashSet<RespConnectionState>();
                    _patterns[pattern] = set;
                }
                firstOnThisNode = set.Count == 0;
                set.Add(state);
                state.Patterns.Add(pattern);
            }

            if (firstOnThisNode) RegisterWithTopic(pattern);

            EnsurePoller();
            return state.SubscriptionCount;
        }

        public int PUnsubscribe(RespConnectionState state, string pattern)
        {
            bool empty = false;

            lock (_sync)
            {
                HashSet<RespConnectionState> set;
                if (_patterns.TryGetValue(pattern, out set) && set.Remove(state) && set.Count == 0)
                {
                    _patterns.Remove(pattern);
                    empty = true;
                }
                state.Patterns.Remove(pattern);
            }

            if (empty) UnregisterFromTopic(pattern);
            return state.SubscriptionCount;
        }

        /// <summary>Drops every subscription a connection holds. Called when it disconnects.</summary>
        public void RemoveConnection(RespConnectionState state)
        {
            if (state == null) return;

            var emptiedChannels = new List<string>();

            lock (_sync)
            {
                foreach (var channel in new List<string>(state.Channels))
                {
                    HashSet<RespConnectionState> set;
                    if (_channels.TryGetValue(channel, out set) && set.Remove(state) && set.Count == 0)
                    {
                        _channels.Remove(channel);
                        emptiedChannels.Add(channel);
                    }
                }
                foreach (var pattern in new List<string>(state.Patterns))
                {
                    HashSet<RespConnectionState> set;
                    if (_patterns.TryGetValue(pattern, out set) && set.Remove(state) && set.Count == 0)
                        _patterns.Remove(pattern);
                }
                state.Channels.Clear();
                state.Patterns.Clear();
            }

            foreach (var channel in emptiedChannels) UnregisterFromTopic(channel);
        }

        #endregion

        #region Publish

        /// <summary>
        /// Publishes to a channel. Returns the number of local subscribers the message was handed
        /// to, which is the best available answer for PUBLISH's reply - the true cluster-wide count
        /// is not knowable without a round trip to every node.
        /// </summary>
        public long Publish(string channel, byte[] payload)
        {
            var cache = _gateway.CacheOrNull;
            if (cache == null) return 0;

            // The topic has to exist before a publish will stick.
            EnsureTopic(cache, channel);

            var meta = new Hashtable
            {
                // The engine does Split(TopicSeperator)[1] on this, so the name has to arrive
                // tagged exactly as the NCache client sends it - a bare channel name throws
                // IndexOutOfRange inside PublishMessage.
                { TopicConstant.TopicName, TopicConstant.TopicTag + channel },
                { TopicConstant.DeliveryOption, ((int)DeliveryOption.All).ToString() },
                { TopicConstant.NotifyOption, "False" }
            };

            long now = DateTime.Now.Ticks;

            cache.PublishMessage(
                Guid.NewGuid().ToString("N"),
                RespCacheGateway.ToUserBinaryObject(cache, payload ?? new byte[0]),
                now,
                // Zero would be read as "already expired" by some paths; give the message a short
                // life so a subscriber that is a poll interval behind still receives it, while
                // nothing accumulates if no one is listening.
                new TimeSpan(0, 0, 30).Ticks,
                meta,
                new BitSet(),
                MessagingContext());

            lock (_sync)
            {
                int n = 0;
                HashSet<RespConnectionState> set;
                if (_channels.TryGetValue(channel, out set)) n += set.Count;
                if (IsShardTopic(channel)) return n;
                foreach (var pair in _patterns)
                    if (RespGlob.IsMatch(pair.Key, channel)) n += pair.Value.Count;
                return n;
            }
        }

        #endregion

        #region Introspection - PUBSUB

        public List<string> ActiveChannels(string pattern)
        {
            return ActiveChannels(pattern, sharded: false);
        }

        /// <summary>Plain channels for PUBSUB CHANNELS, shard channels (bare names) for SHARDCHANNELS.</summary>
        public List<string> ActiveChannels(string pattern, bool sharded)
        {
            lock (_sync)
            {
                var result = new List<string>();
                foreach (var pair in _channels)
                {
                    if (IsShardTopic(pair.Key) != sharded) continue;
                    string name = sharded ? ShardName(pair.Key) : pair.Key;
                    if (pattern == null || RespGlob.IsMatch(pattern, name)) result.Add(name);
                }
                return result;
            }
        }

        public int SubscriberCount(string channel)
        {
            lock (_sync)
            {
                HashSet<RespConnectionState> set;
                return _channels.TryGetValue(channel, out set) ? set.Count : 0;
            }
        }

        public int PatternCount
        {
            get { lock (_sync) { return _patterns.Count; } }
        }

        #endregion

        #region Topic plumbing

        private void EnsureTopic(Alachisoft.NCache.Caching.Cache cache, string channel)
        {
            try
            {
                cache.TopicOpertion(
                    new TopicOperation(channel, TopicOperationType.Create),
                    MessagingContext());
            }
            catch (Exception e)
            {
                RespLog.Detail(Module + ".EnsureTopic", channel + ": " + e.Message);
            }
        }

        private void RegisterWithTopic(string channel)
        {
            RegisterWithTopic(channel, SubscriptionType.Subscriber);
        }

        private void RegisterWithTopic(string channel, SubscriptionType type)
        {
            var cache = _gateway.CacheOrNull;
            if (cache == null) return;

            try
            {
                EnsureTopic(cache, channel);

                var op = new SubscriptionOperation(
                    channel,
                    TopicOperationType.Subscribe,
                    NewSubscription(channel, type));

                cache.TopicOpertion(op, MessagingContext());
                RespLog.Detail(Module + ".Subscribe", "node subscribed to '" + channel + "'");
            }
            catch (Exception e)
            {
                RespLog.Warn(Module + ".Subscribe", "could not subscribe to '" + channel + "'. " + e.Message);
            }
        }

        private void UnregisterFromTopic(string channel)
        {
            UnregisterFromTopic(channel, SubscriptionType.Subscriber);
        }

        private void UnregisterFromTopic(string channel, SubscriptionType type)
        {
            var cache = _gateway.CacheOrNull;
            if (cache == null) return;

            try
            {
                var op = new SubscriptionOperation(
                    channel,
                    TopicOperationType.UnSubscribe,
                    NewSubscription(channel, type));

                cache.TopicOpertion(op, MessagingContext());
                RespLog.Detail(Module + ".Unsubscribe", "node unsubscribed from '" + channel + "'");
            }
            catch (Exception e)
            {
                RespLog.Detail(Module + ".Unsubscribe", channel + ": " + e.Message);
            }
        }

        /// <summary>
        /// A non-durable subscription: messages published while nobody is listening are dropped
        /// rather than queued. That is Redis pub/sub semantics exactly, and it is the reason this
        /// uses NonDurableExclusiveSubscription while the stream commands use the durable policies.
        /// </summary>
        private SubscriptionInfo NewSubscription(string channel, SubscriptionType type)
        {
            return new SubscriptionInfo
            {
                ClientId = _clientId,
                SubscriptionId = channel,
                Type = type,
                SubPolicyType = SubscriptionPolicyType.NonDurableExclusiveSubscription
            };
        }

        #endregion


        /// <summary>
        /// A context for the messaging calls.
        /// <para>
        /// ClientLastViewId matters: without it the cluster layer compares the caller's view of
        /// membership against its own, decides the caller is stale, and refuses every poll with
        /// "Operation could not be completed due to state transfer". The value -5 is the engine's
        /// own "forced view" sentinel, meaning collect from the cluster and return - which is
        /// exactly what a server-side poller wants, and what NCache's own GetMessage command uses.
        /// </para>
        /// </summary>
        private static OperationContext MessagingContext()
        {
            var context = RespCacheGateway.NewOperationContext();
            context.Add(OperationContextFieldName.ClientLastViewId, ForcedViewId);
            return context;
        }

        /// <summary>ClusterCacheBase.forcedViewId - bypasses the client-view staleness check.</summary>
        private const long ForcedViewId = -5;

        #region Poller

        private void EnsurePoller()
        {
            if (_poller != null || Volatile.Read(ref _disposed) != 0) return;

            lock (_sync)
            {
                if (_poller != null) return;
                _poller = new Thread(PollLoop)
                {
                    IsBackground = true,
                    Name = "RespPubSubPoller"
                };
                _poller.Start();
            }
            RespLog.Detail(Module + ".Poller", "started");
        }

        private void PollLoop()
        {
            while (Volatile.Read(ref _disposed) == 0)
            {
                int delivered = 0;
                try { delivered = PollOnce(); }
                catch (Exception e) { RespLog.Detail(Module + ".Poll", e.Message); }

                Thread.Sleep(delivered > 0 ? BusyPollMs : IdlePollMs);
            }
        }

        private int PollOnce()
        {
            bool anySubscribers;
            lock (_sync) { anySubscribers = _channels.Count > 0 || _patterns.Count > 0; }
            if (!anySubscribers) return 0;

            var cache = _gateway.CacheOrNull;
            if (cache == null) return 0;

            var response = cache.GetAssignedMessages(
                new SubscriptionInfo { ClientId = _clientId },
                MessagingContext());

            if (response == null || response.AssignedMessages == null) return 0;

            int delivered = 0;
            var acks = new Dictionary<string, IList<string>>();

            foreach (var pair in response.AssignedMessages)
            {
                string channel = pair.Key;
                if (pair.Value == null) continue;

                // Event topics are NCache's own internal traffic and are not anyone's channel.
                if (TopicConstant.AllEventTopics.Contains(channel)) continue;

                var ids = new List<string>();

                // The engine hands a topic's messages back in storage order, which is not
                // necessarily publish order - a subscriber saw 1, 3, 2. Redis delivers a channel's
                // messages in the order they were published, so each poll's batch is put back into
                // that order by the time Publish stamped on it. A message that only reaches a later
                // poll can still arrive after one published after it.
                var batch = new List<Alachisoft.NCache.Caching.Messaging.Message>();

                // The forced-view poll asks every node and concatenates the answers. A replicated
                // cache keeps every message on every node, so the same message comes back once per
                // node and would be pushed to the subscriber that many times.
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var raw in pair.Value)
                {
                    var queued = raw as Alachisoft.NCache.Caching.Messaging.Message;
                    if (queued == null) continue;
                    if (queued.MessageId != null && !seen.Add(queued.MessageId)) continue;
                    batch.Add(queued);
                }
                StableSortByCreation(batch);

                foreach (var message in batch)
                {
                    if (message.MessageId != null) ids.Add(message.MessageId);

                    byte[] payload = PayloadOf(message);
                    if (payload == null) continue;

                    delivered += Deliver(channel, payload);
                }

                if (ids.Count > 0) acks[channel] = ids;
            }

            if (acks.Count > 0)
            {
                try
                {
                    cache.AcknowledgeMessageReceipt(_clientId, acks, MessagingContext());
                }
                catch (Exception e)
                {
                    RespLog.Detail(Module + ".Ack", e.Message);
                }
            }

            return delivered;
        }

        /// <summary>Insertion sort: batches are a handful of messages, and equal stamps keep their order.</summary>
        private static void StableSortByCreation(List<Alachisoft.NCache.Caching.Messaging.Message> batch)
        {
            for (int i = 1; i < batch.Count; i++)
            {
                var current = batch[i];
                int j = i - 1;
                while (j >= 0 && batch[j].CreationTime > current.CreationTime)
                {
                    batch[j + 1] = batch[j];
                    j--;
                }
                batch[j + 1] = current;
            }
        }

        private static byte[] PayloadOf(Alachisoft.NCache.Caching.Messaging.Message message)
        {
            var binary = message.PayLoad as UserBinaryObject;
            if (binary == null) return null;

            byte[] bytes = binary.GetFullObject();
            // A zero-length payload comes back as null rather than an empty array - the same trap
            // that made empty string values read as missing.
            return bytes ?? new byte[0];
        }

        private static readonly Dictionary<string, HashSet<RespConnectionState>> EmptyPatterns =
            new Dictionary<string, HashSet<RespConnectionState>>();

        /// <summary>Writes the message to every local connection that asked for this channel.</summary>
        private int Deliver(string channel, byte[] payload)
        {
            List<RespConnectionState> direct = null;
            List<KeyValuePair<string, RespConnectionState>> matched = null;
            bool shard = IsShardTopic(channel);

            lock (_sync)
            {
                HashSet<RespConnectionState> set;
                if (_channels.TryGetValue(channel, out set) && set.Count > 0)
                    direct = new List<RespConnectionState>(set);

                // A shard channel is invisible to patterns, as in Redis.
                foreach (var pair in shard ? EmptyPatterns : _patterns)
                {
                    if (!RespGlob.IsMatch(pair.Key, channel)) continue;
                    if (matched == null) matched = new List<KeyValuePair<string, RespConnectionState>>();
                    foreach (var s in pair.Value)
                        matched.Add(new KeyValuePair<string, RespConnectionState>(pair.Key, s));
                }
            }

            int n = 0;

            if (direct != null)
            {
                // The topic name decides the vocabulary, not the connection: everything subscribed
                // to a shard topic got there through SSUBSCRIBE and is waiting for "smessage",
                // which it matches on and without which it drops the message.
                byte[] frame = shard
                    ? RespPushFrames.ShardMessage(ShardName(channel), payload)
                    : RespPushFrames.Message(channel, payload);

                foreach (var state in direct)
                    if (state.Push(frame)) n++;
            }

            if (matched != null)
            {
                foreach (var pair in matched)
                {
                    byte[] frame = RespPushFrames.PatternMessage(pair.Key, channel, payload);
                    if (pair.Value.Push(frame)) n++;
                }
            }

            return n;
        }

        #endregion

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            // Leave the topics registered rather than racing the poller on the way out; a cache
            // restart clears them, and an orphaned non-durable subscription costs nothing.
            var thread = _poller;
            if (thread != null)
            {
                try { thread.Join(200); } catch { }
            }
        }
    }
}
