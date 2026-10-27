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
using System.Text;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp.PubSub
{
    /// <summary>
    /// SUBSCRIBE, UNSUBSCRIBE, PSUBSCRIBE, PUNSUBSCRIBE, PUBLISH and PUBSUB.
    /// <para>
    /// The subscribe family answers one acknowledgement per channel named, in order, each carrying
    /// the connection's running subscription total. Clients depend on that shape: StackExchange.Redis
    /// counts the acknowledgements to decide when a subscribe has completed, and watches the total
    /// fall to zero to know it may leave subscriber mode.
    /// </para>
    /// </summary>
    internal static class RespPubSubCommands
    {
        /// <summary>
        /// While a connection holds any subscription, Redis rejects everything except these.
        /// RESET and QUIT are here because they are how a client gets out.
        /// </summary>
        private static readonly HashSet<string> AllowedInSubscriberMode =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SUBSCRIBE", "UNSUBSCRIBE", "PSUBSCRIBE", "PUNSUBSCRIBE",
                "SSUBSCRIBE", "SUNSUBSCRIBE",
                "PING", "QUIT", "RESET", "HELLO"
            };

        public static bool IsAllowedInSubscriberMode(string command)
        {
            return command != null && AllowedInSubscriberMode.Contains(command);
        }

        /// <summary>
        /// SUBSCRIBE, or SSUBSCRIBE when <paramref name="sharded"/>. NCache messaging is already
        /// cluster-wide, so Redis's reason for sharded pub/sub does not arise here - but it is
        /// still a separate namespace, with its own reply kinds and its own channel count, because
        /// a client keeps the two in separate tables and matches each incoming frame against the
        /// one it expects. An acknowledgement of the wrong kind leaves StackExchange.Redis with no
        /// handler for the channel, and every message to it is dropped.
        /// </summary>
        public static RespCommandOutcome Subscribe(
            RespRequest request, RespWriter writer, RespConnectionState state, RespPubSubHub hub,
            bool sharded = false)
        {
            if (request.ArgCount < 1) return WrongArgs(writer, sharded ? "ssubscribe" : "subscribe");

            for (int i = 0; i < request.ArgCount; i++)
            {
                string channel = Text(request.GetArg(i));
                if (!IsUsableChannel(channel)) return BadChannel(writer, channel);

                if (sharded)
                {
                    hub.Subscribe(state, RespPubSubHub.ShardTopic(channel));
                    RespPushFrames.WriteAck(writer, "ssubscribe", channel, ShardCount(state));
                }
                else
                {
                    hub.Subscribe(state, channel);
                    RespPushFrames.WriteAck(writer, "subscribe", channel, PlainCount(state));
                }
            }
            return RespCommandOutcome.Continue;
        }

        public static RespCommandOutcome PSubscribe(
            RespRequest request, RespWriter writer, RespConnectionState state, RespPubSubHub hub)
        {
            if (request.ArgCount < 1) return WrongArgs(writer, "psubscribe");

            for (int i = 0; i < request.ArgCount; i++)
            {
                string pattern = Text(request.GetArg(i));
                hub.PSubscribe(state, pattern);
                RespPushFrames.WriteAck(writer, "psubscribe", pattern, PlainCount(state));
            }
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// UNSUBSCRIBE with no arguments drops every channel. Redis still answers one frame per
        /// channel dropped, and exactly one frame carrying a null channel when there were none -
        /// a client that gets no reply at all will hang waiting for it.
        /// </summary>
        public static RespCommandOutcome Unsubscribe(
            RespRequest request, RespWriter writer, RespConnectionState state, RespPubSubHub hub,
            bool sharded = false)
        {
            string kind = sharded ? "sunsubscribe" : "unsubscribe";
            List<string> channels = Targets(request, state, patterns: false, sharded: sharded);

            if (channels.Count == 0)
            {
                RespPushFrames.WriteAck(writer, kind, null, sharded ? ShardCount(state) : PlainCount(state));
                return RespCommandOutcome.Continue;
            }

            foreach (var channel in channels)
            {
                if (sharded)
                {
                    hub.Unsubscribe(state, RespPubSubHub.ShardTopic(channel));
                    RespPushFrames.WriteAck(writer, kind, channel, ShardCount(state));
                }
                else
                {
                    hub.Unsubscribe(state, channel);
                    RespPushFrames.WriteAck(writer, kind, channel, PlainCount(state));
                }
            }
            return RespCommandOutcome.Continue;
        }

        public static RespCommandOutcome PUnsubscribe(
            RespRequest request, RespWriter writer, RespConnectionState state, RespPubSubHub hub)
        {
            List<string> patterns = Targets(request, state, patterns: true, sharded: false);

            if (patterns.Count == 0)
            {
                RespPushFrames.WriteAck(writer, "punsubscribe", null, PlainCount(state));
                return RespCommandOutcome.Continue;
            }

            foreach (var pattern in patterns)
            {
                hub.PUnsubscribe(state, pattern);
                RespPushFrames.WriteAck(writer, "punsubscribe", pattern, PlainCount(state));
            }
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// The engine derives a topic name with Split(TopicSeperator)[1], so a channel containing
        /// that separator would be silently truncated to a different topic - published to one name
        /// and delivered under another. Refusing is the honest answer; silently renaming a client's
        /// channel is not.
        /// </summary>
        internal static bool IsUsableChannel(string channel)
        {
            // The shard prefix is reserved: a plain channel carrying it would read as a shard one.
            return !string.IsNullOrEmpty(channel) && channel.IndexOf('>') < 0
                   && !RespPubSubHub.IsShardTopic(channel);
        }

        private static RespCommandOutcome BadChannel(RespWriter writer, string channel)
        {
            // Two reasons, named separately: '>' would be truncated by the engine's topic parsing,
            // and the shard prefix is reserved. Telling a client only about '>' when it tripped on
            // the prefix sends it looking for a character that is not there.
            string why = RespPubSubHub.IsShardTopic(channel)
                ? "may not begin with '" + RespPubSubHub.ShardPrefix + "', which is reserved for sharded channels"
                : "may not be empty or contain '>'";

            writer.WriteError("ERR channel name " + why + " (got '" + channel + "')");
            return RespCommandOutcome.Continue;
        }

        /// <summary>PUBLISH, or SPUBLISH when <paramref name="sharded"/> - which reaches shard subscribers only.</summary>
        public static RespCommandOutcome Publish(
            RespRequest request, RespWriter writer, RespPubSubHub hub, bool sharded = false)
        {
            if (request.ArgCount != 2) return WrongArgs(writer, sharded ? "spublish" : "publish");

            string channel = Text(request.GetArg(0));
            if (!IsUsableChannel(channel)) return BadChannel(writer, channel);
            long receivers = hub.Publish(
                sharded ? RespPubSubHub.ShardTopic(channel) : channel, request.GetArg(1));

            // Redis returns how many subscribers received it. Ours is the count on this node -
            // the honest number without a synchronous round trip to every other node.
            writer.WriteInteger(receivers);
            return RespCommandOutcome.Continue;
        }

        /// <summary>PUBSUB CHANNELS [pattern] | NUMSUB [channel ...] | NUMPAT | SHARDCHANNELS | SHARDNUMSUB</summary>
        public static RespCommandOutcome PubSub(
            RespRequest request, RespWriter writer, RespPubSubHub hub)
        {
            if (request.ArgCount < 1) return WrongArgs(writer, "pubsub");

            if (request.ArgEquals(0, "CHANNELS") || request.ArgEquals(0, "SHARDCHANNELS"))
            {
                string pattern = request.ArgCount > 1 ? Text(request.GetArg(1)) : null;
                var channels = hub.ActiveChannels(pattern, sharded: request.ArgEquals(0, "SHARDCHANNELS"));

                writer.WriteArrayHeader(channels.Count);
                foreach (var c in channels) writer.WriteBulkString(c);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "NUMSUB") || request.ArgEquals(0, "SHARDNUMSUB"))
            {
                bool sharded = request.ArgEquals(0, "SHARDNUMSUB");
                int n = request.ArgCount - 1;
                writer.WriteArrayHeader(n * 2);
                for (int i = 1; i < request.ArgCount; i++)
                {
                    string channel = Text(request.GetArg(i));
                    writer.WriteBulkString(channel);
                    writer.WriteInteger(hub.SubscriberCount(
                        sharded ? RespPubSubHub.ShardTopic(channel) : channel));
                }
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "NUMPAT"))
            {
                writer.WriteInteger(hub.PatternCount);
                return RespCommandOutcome.Continue;
            }

            writer.WriteError("ERR Unknown PUBSUB subcommand or wrong number of arguments");
            return RespCommandOutcome.Continue;
        }

        #region Helpers

        /// <summary>
        /// The channels or patterns an unsubscribe applies to: those named, or - when none are
        /// named - everything the connection currently holds. Snapshotted under the lock, because
        /// unsubscribing mutates the very set being walked.
        /// </summary>
        private static List<string> Targets(RespRequest request, RespConnectionState state, bool patterns,
                                            bool sharded)
        {
            var list = new List<string>();

            if (request.ArgCount > 0)
            {
                for (int i = 0; i < request.ArgCount; i++) list.Add(Text(request.GetArg(i)));
                return list;
            }

            lock (state.SubSync)
            {
                if (patterns) { list.AddRange(state.Patterns); return list; }

                // Bare names, of this kind only: UNSUBSCRIBE with no arguments leaves shard
                // channels alone, and SUNSUBSCRIBE leaves plain ones.
                foreach (var channel in state.Channels)
                {
                    if (RespPubSubHub.IsShardTopic(channel) != sharded) continue;
                    list.Add(sharded ? RespPubSubHub.ShardName(channel) : channel);
                }
            }
            return list;
        }

        /// <summary>Shard channels held - the count an ssubscribe/sunsubscribe acknowledgement carries.</summary>
        private static int ShardCount(RespConnectionState state)
        {
            lock (state.SubSync)
            {
                int n = 0;
                foreach (var channel in state.Channels)
                    if (RespPubSubHub.IsShardTopic(channel)) n++;
                return n;
            }
        }

        /// <summary>Plain channels and patterns held - the count the other acknowledgements carry.</summary>
        private static int PlainCount(RespConnectionState state)
        {
            lock (state.SubSync) { return state.SubscriptionCount - ShardCount(state); }
        }

        private static RespCommandOutcome WrongArgs(RespWriter writer, string command)
        {
            writer.WriteError("ERR wrong number of arguments for '" + command + "' command");
            return RespCommandOutcome.Continue;
        }

        private static string Text(byte[] value)
        {
            return value == null ? string.Empty : Encoding.UTF8.GetString(value);
        }

        #endregion
    }
}
