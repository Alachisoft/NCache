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
using System.Globalization;
using System.Text;

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>The RESP type a reply carries. Covers RESP3; RESP2 renders a subset.</summary>
    internal enum RespReplyKind
    {
        /// <summary>Nil. <c>_</c> in RESP3, a null bulk string or null array in RESP2.</summary>
        Nil = 0,

        /// <summary>A simple status, <c>+OK</c>.</summary>
        Status,

        /// <summary>An error. <c>-ERR ...</c> in both.</summary>
        Error,

        /// <summary>A 64-bit integer, <c>:1</c>.</summary>
        Integer,

        /// <summary>A binary-safe string, <c>$5\r\nhello</c>.</summary>
        Bulk,

        /// <summary>An ordered list, <c>*2</c>.</summary>
        Array,

        /// <summary>
        /// Key/value pairs. <c>%2</c> in RESP3; in RESP2 a flat array of alternating entries, which
        /// is what HGETALL and CONFIG GET already send.
        /// </summary>
        Map,

        /// <summary>An unordered collection. <c>~3</c> in RESP3, an array in RESP2.</summary>
        Set,

        /// <summary>
        /// A double. <c>,1.5</c> in RESP3; in RESP2 a bulk string, which is how every sorted-set
        /// score and INCRBYFLOAT result is sent today.
        /// </summary>
        Double,

        /// <summary>A boolean. <c>#t</c> in RESP3, <c>:1</c> or <c>:0</c> in RESP2.</summary>
        Boolean,

        /// <summary>An integer too large for 64 bits. <c>(</c> in RESP3, a bulk string in RESP2.</summary>
        BigNumber,

        /// <summary>
        /// A string with a declared format, <c>=15\r\ntxt:</c>. Used for help text and INFO. A bulk
        /// string in RESP2.
        /// </summary>
        Verbatim,

        /// <summary>
        /// An out-of-band message. <c>&gt;4</c> in RESP3; in RESP2 an ordinary array, which is how
        /// pub/sub delivery works there.
        /// </summary>
        Push
    }

    /// <summary>
    /// A reply, as a value rather than as bytes.
    /// <para>
    /// This is the piece that makes the layering pay for itself, and the reason is worth being
    /// explicit about. Today a handler is handed a <c>RespWriter</c> and writes its own answer, so
    /// every handler decides its own encoding; that is why RESP3 is not a configuration flag but
    /// ninety-nine edits. A handler that instead returns "a map of three fields" has said what it
    /// means, and whether that reaches the client as a RESP3 map or a RESP2 flat array becomes the
    /// encoder's business.
    /// </para>
    /// <para>
    /// So the kinds here are RESP3's, in full, including the four RESP2 has no spelling for -
    /// <see cref="RespReplyKind.Map"/>, <see cref="RespReplyKind.Set"/>,
    /// <see cref="RespReplyKind.Double"/> and <see cref="RespReplyKind.Boolean"/>. RESP2 renders
    /// each of those the way Redis does, which is the whole of what a RESP2 client sees now.
    /// </para>
    /// <para>
    /// Nothing in this type knows how to write itself. There is no reference to the writer here on
    /// purpose: a reply is data, and the encoder that turns it into bytes lives with the protocol,
    /// not with the commands.
    /// </para>
    /// </summary>
    internal sealed class RespReply
    {
        private static readonly RespReply NilReply = new RespReply(RespReplyKind.Nil);
        private static readonly RespReply OkReply = new RespReply(RespReplyKind.Status) { Text = "OK" };
        private static readonly RespReply TrueReply = new RespReply(RespReplyKind.Boolean) { Number = 1 };
        private static readonly RespReply FalseReply = new RespReply(RespReplyKind.Boolean) { Number = 0 };
        private static readonly RespReply EmptyArrayReply =
            new RespReply(RespReplyKind.Array) { Items = new RespReply[0] };

        private RespReply(RespReplyKind kind)
        {
            Kind = kind;
        }

        public RespReplyKind Kind { get; }

        /// <summary>Status text, error text, big-number digits, or a verbatim payload's format.</summary>
        public string Text { get; private set; }

        /// <summary>Integer value, or a boolean as 0/1.</summary>
        public long Number { get; private set; }

        /// <summary>Double value.</summary>
        public double Value { get; private set; }

        /// <summary>Bulk or verbatim payload. Null with <see cref="RespReplyKind.Nil"/> is the nil reply.</summary>
        public byte[] Bytes { get; private set; }

        /// <summary>Elements of an array, set or push.</summary>
        public IList<RespReply> Items { get; private set; }

        /// <summary>Entries of a map, in order - RESP3 maps are ordered on the wire.</summary>
        public IList<KeyValuePair<RespReply, RespReply>> Entries { get; private set; }

        // ---- factories ----

        public static RespReply Nil
        {
            get { return NilReply; }
        }

        public static RespReply Ok
        {
            get { return OkReply; }
        }

        public static RespReply EmptyArray
        {
            get { return EmptyArrayReply; }
        }

        public static RespReply Status(string text)
        {
            return new RespReply(RespReplyKind.Status) { Text = text };
        }

        public static RespReply Error(string text)
        {
            return new RespReply(RespReplyKind.Error) { Text = text };
        }

        public static RespReply Integer(long value)
        {
            return new RespReply(RespReplyKind.Integer) { Number = value };
        }

        public static RespReply Bulk(byte[] value)
        {
            return value == null ? NilReply : new RespReply(RespReplyKind.Bulk) { Bytes = value };
        }

        public static RespReply Bulk(string value)
        {
            return value == null ? NilReply : Bulk(Encoding.UTF8.GetBytes(value));
        }

        public static RespReply Array(IList<RespReply> items)
        {
            return new RespReply(RespReplyKind.Array) { Items = items ?? new RespReply[0] };
        }

        public static RespReply Set(IList<RespReply> items)
        {
            return new RespReply(RespReplyKind.Set) { Items = items ?? new RespReply[0] };
        }

        public static RespReply Push(IList<RespReply> items)
        {
            return new RespReply(RespReplyKind.Push) { Items = items ?? new RespReply[0] };
        }

        public static RespReply Map(IList<KeyValuePair<RespReply, RespReply>> entries)
        {
            return new RespReply(RespReplyKind.Map)
            {
                Entries = entries ?? new List<KeyValuePair<RespReply, RespReply>>()
            };
        }

        public static RespReply Double(double value)
        {
            return new RespReply(RespReplyKind.Double) { Value = value };
        }

        public static RespReply Boolean(bool value)
        {
            return value ? TrueReply : FalseReply;
        }

        public static RespReply BigNumber(string digits)
        {
            return new RespReply(RespReplyKind.BigNumber) { Text = digits };
        }

        /// <param name="format">
        /// The three-character format RESP3 prefixes the payload with - "txt" for plain text, "mkd"
        /// for markdown. Ignored in RESP2, where this is an ordinary bulk string.
        /// </param>
        public static RespReply Verbatim(byte[] payload, string format)
        {
            return new RespReply(RespReplyKind.Verbatim) { Bytes = payload, Text = format ?? "txt" };
        }

        // ---- conveniences, because these shapes recur ----

        /// <summary>An array of bulk strings, with nulls preserved as nil entries.</summary>
        public static RespReply BulkArray(IList<byte[]> values)
        {
            if (values == null) return EmptyArrayReply;

            var items = new RespReply[values.Count];
            for (int i = 0; i < values.Count; i++) items[i] = Bulk(values[i]);
            return Array(items);
        }

        /// <summary>A map built from name/value pairs - a hash, or an INFO section.</summary>
        public static RespReply FieldMap(IList<RespField> fields)
        {
            var entries = new List<KeyValuePair<RespReply, RespReply>>(fields == null ? 0 : fields.Count);
            if (fields != null)
            {
                foreach (RespField field in fields)
                    entries.Add(new KeyValuePair<RespReply, RespReply>(Bulk(field.Name), Bulk(field.Value)));
            }

            return Map(entries);
        }

        /// <summary>
        /// A double as RESP2 renders it: a bulk string. Scores and float counters are sent this way
        /// to a RESP2 client, and <c>R</c> format matches Redis's own formatting of them.
        /// </summary>
        public static string FormatDouble(double value)
        {
            if (double.IsPositiveInfinity(value)) return "inf";
            if (double.IsNegativeInfinity(value)) return "-inf";
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case RespReplyKind.Nil: return "nil";
                case RespReplyKind.Status: return "+" + Text;
                case RespReplyKind.Error: return "-" + Text;
                case RespReplyKind.Integer: return Number.ToString(CultureInfo.InvariantCulture);
                case RespReplyKind.Boolean: return Number != 0 ? "true" : "false";
                case RespReplyKind.Double: return FormatDouble(Value);
                case RespReplyKind.BigNumber: return Text;
                case RespReplyKind.Bulk:
                case RespReplyKind.Verbatim:
                    return Bytes == null ? "nil" : "bulk[" + Bytes.Length + "]";
                case RespReplyKind.Map:
                    return "map[" + (Entries == null ? 0 : Entries.Count) + "]";
                default:
                    return Kind.ToString().ToLowerInvariant() + "[" + (Items == null ? 0 : Items.Count) + "]";
            }
        }
    }
}
