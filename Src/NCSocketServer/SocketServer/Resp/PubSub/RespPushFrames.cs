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
using System.Text;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp.PubSub
{
    /// <summary>
    /// Pre-encoded RESP frames for the messages a server pushes at a subscriber.
    /// <para>
    /// Built once per message rather than once per recipient: a channel with fifty subscribers
    /// sends the same bytes fifty times, and encoding them fifty times would be pure waste. It also
    /// keeps the encoding off the connection's send path, which holds a lock.
    /// </para>
    /// </summary>
    internal static class RespPushFrames
    {
        /// <summary>*3 message &lt;channel&gt; &lt;payload&gt;</summary>
        public static byte[] Message(string channel, byte[] payload)
        {
            return Message("message", channel, payload);
        }

        /// <summary>*3 smessage &lt;channel&gt; &lt;payload&gt; - what an SSUBSCRIBE subscriber expects.</summary>
        public static byte[] ShardMessage(string channel, byte[] payload)
        {
            return Message("smessage", channel, payload);
        }

        private static byte[] Message(string kind, string channel, byte[] payload)
        {
            var w = new RespWriter();
            w.WriteArrayHeader(3);
            w.WriteBulkString(kind);
            w.WriteBulkString(channel);
            w.WriteBulkString(payload);
            return w.ToArray();
        }

        /// <summary>*4 pmessage &lt;pattern&gt; &lt;channel&gt; &lt;payload&gt;</summary>
        public static byte[] PatternMessage(string pattern, string channel, byte[] payload)
        {
            var w = new RespWriter();
            w.WriteArrayHeader(4);
            w.WriteBulkString("pmessage");
            w.WriteBulkString(pattern);
            w.WriteBulkString(channel);
            w.WriteBulkString(payload);
            return w.ToArray();
        }

        /// <summary>
        /// The acknowledgement Redis sends for each channel named in a SUBSCRIBE or UNSUBSCRIBE.
        /// The trailing count is the connection's total subscription count afterwards, across both
        /// channels and patterns - clients rely on it reaching zero to know they may leave
        /// subscriber mode.
        /// </summary>
        public static void WriteAck(RespWriter writer, string kind, string channel, int total)
        {
            writer.WriteArrayHeader(3);
            writer.WriteBulkString(kind);
            if (channel == null) writer.WriteNull();
            else writer.WriteBulkString(channel);
            writer.WriteInteger(total);
        }
    }
}
