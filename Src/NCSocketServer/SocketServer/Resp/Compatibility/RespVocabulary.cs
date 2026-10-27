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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// What a key holds, in Redis's terms. <see cref="None"/> is a key that does not exist, which
    /// is what TYPE reports as "none".
    /// </summary>
    /// <remarks>
    /// Not the engine's EntryType. The two overlap but are not the same set, and naming the engine's
    /// enum here would put a cache type in the signature of every type-aware command - which is the
    /// coupling this layer exists to remove.
    /// </remarks>
    internal enum RespKeyType
    {
        None = 0,
        String,
        List,
        Set,
        Hash,
        Stream,
        SortedSet
    }

    /// <summary>Whether a write may create a key, overwrite one, or either.</summary>
    internal enum RespWriteCondition
    {
        /// <summary>Write regardless. Plain SET.</summary>
        Always = 0,

        /// <summary>Only if the key does not exist. SET NX, SETNX.</summary>
        IfAbsent,

        /// <summary>Only if the key already exists. SET XX.</summary>
        IfPresent
    }

    /// <summary>
    /// The NX/XX/GT/LT guard on EXPIRE and its variants. Deliberately a different enum from
    /// <see cref="RespWriteCondition"/>: these compare the expiry a key already carries, not whether
    /// the key exists, and a single enum serving both reads correctly in neither place.
    /// </summary>
    internal enum RespExpiryCondition
    {
        Always = 0,

        /// <summary>NX - only when the key currently has no expiry.</summary>
        IfNoExpiry,

        /// <summary>XX - only when the key currently has one.</summary>
        IfHasExpiry,

        /// <summary>GT - only when the new deadline is later than the current one.</summary>
        IfGreater,

        /// <summary>LT - only when the new deadline is earlier than the current one.</summary>
        IfLess
    }

    /// <summary>
    /// When a key should expire, expressed absolutely.
    /// <para>
    /// Absolute rather than a duration, on purpose. Every relative Redis spelling - EX, PX, the
    /// EXPIRE seconds argument - is converted to a deadline by the caller, so nothing below this
    /// point has to know which form arrived, and a command's meaning cannot drift with however long
    /// it then took to reach the engine.
    /// </para>
    /// </summary>
    internal struct RespExpiry
    {
        private RespExpiry(DateTime? deadline, bool keep)
        {
            UtcDeadline = deadline;
            KeepExisting = keep;
        }

        /// <summary>The moment the key expires, UTC. Null for a key that does not expire.</summary>
        public DateTime? UtcDeadline { get; }

        /// <summary>KEEPTTL - leave whatever expiry the key already has untouched.</summary>
        public bool KeepExisting { get; }

        /// <summary>
        /// No expiry. Note this is not the same as <see cref="Keep"/>: a plain SET clears an
        /// existing expiry, which is Redis behaviour people are frequently surprised by.
        /// </summary>
        public static RespExpiry None
        {
            get { return new RespExpiry(null, false); }
        }

        /// <summary>SET ... KEEPTTL.</summary>
        public static RespExpiry Keep
        {
            get { return new RespExpiry(null, true); }
        }

        /// <summary>Expire at this UTC moment.</summary>
        public static RespExpiry At(DateTime utcDeadline)
        {
            return new RespExpiry(utcDeadline, false);
        }

        /// <summary>True when this sets a deadline, rather than clearing or preserving one.</summary>
        public bool HasDeadline
        {
            get { return UtcDeadline.HasValue; }
        }
    }

    /// <summary>One field of a hash, or one field of a stream entry.</summary>
    internal struct RespField
    {
        public RespField(byte[] name, byte[] value)
        {
            Name = name;
            Value = value;
        }

        public byte[] Name { get; }
        public byte[] Value { get; }
    }

    /// <summary>
    /// What a SET did.
    /// <para>
    /// Three facts rather than one, because SET can be asked to report any of them: a conditional
    /// SET answers nil when the guard refused the write, and SET ... GET answers whatever was there
    /// before. Collapsing this to a bool would leave SET NX GET unexpressible.
    /// </para>
    /// </summary>
    internal struct RespSetOutcome
    {
        public RespSetOutcome(bool stored, byte[] previousValue, bool keyExisted)
        {
            Stored = stored;
            PreviousValue = previousValue;
            KeyExisted = keyExisted;
        }

        /// <summary>False when an NX or XX guard refused the write.</summary>
        public bool Stored { get; }

        /// <summary>The value before the write, for SET ... GET. Null where the key held nothing.</summary>
        public byte[] PreviousValue { get; }

        /// <summary>Whether the key existed beforehand - what separates a nil value from an absent key.</summary>
        public bool KeyExisted { get; }
    }

    /// <summary>Which end of a list an operation works on.</summary>
    internal enum RespListEnd
    {
        Left = 0,
        Right
    }

    /// <summary>A member of a sorted set, with its score.</summary>
    internal struct RespScoredMember
    {
        public RespScoredMember(byte[] member, double score)
        {
            Member = member;
            Score = score;
        }

        public byte[] Member { get; }
        public double Score { get; }
    }

    /// <summary>A stream entry id - the ms-seq pair Redis prints as <c>1526919030474-55</c>.</summary>
    internal struct RespStreamId
    {
        public RespStreamId(long milliseconds, long sequence)
        {
            Milliseconds = milliseconds;
            Sequence = sequence;
        }

        public long Milliseconds { get; }
        public long Sequence { get; }

        public override string ToString()
        {
            return Milliseconds.ToString(CultureInfo.InvariantCulture) + "-" +
                   Sequence.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>One stream entry: its id, and its fields in insertion order.</summary>
    internal sealed class RespStreamEntryView
    {
        public RespStreamEntryView(RespStreamId id, IList<RespField> fields)
        {
            Id = id;
            Fields = fields;
        }

        public RespStreamId Id { get; }
        public IList<RespField> Fields { get; }
    }

    /// <summary>The entries one stream contributed to an XREAD, with the stream they came from.</summary>
    internal sealed class RespStreamSlice
    {
        public RespStreamSlice(string key, IList<RespStreamEntryView> entries)
        {
            Key = key;
            Entries = entries;
        }

        public string Key { get; }
        public IList<RespStreamEntryView> Entries { get; }
    }
}
