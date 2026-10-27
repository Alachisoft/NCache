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
using System.Threading;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// A lock around a read-modify-write sequence, using NCache's own key locking.
    /// <para>
    /// Redis executes each command to completion before starting the next, so a command this layer
    /// builds from several engine calls has to be made to look the same way. APPEND and SETRANGE are
    /// the cases that need it: both read the current value, splice bytes into it and write the whole
    /// value back. Two clients patching different offsets of one key without this would each read
    /// the same starting value and the second write would erase the first client's patch - not a
    /// lost update, which Redis itself allows for concurrent SETs, but a value that silently lacks a
    /// change its author was told had been applied.
    /// </para>
    /// <para>
    /// The lease is taken on the data key itself, and that is safe precisely because the writes here
    /// pass no lock id: NCache sets LockAccessType to IGNORE_LOCK when the id is null, so they
    /// proceed regardless of the lease. The lock therefore coordinates nothing but this layer's own
    /// handlers, which is what is wanted - a Redis client has no notion of a lock and must never be
    /// blocked by one, and an NCache client holding its own lock on the key must not be either.
    /// </para>
    /// <para>
    /// An earlier version leased a separate companion key. It looked tidier and did not work:
    /// NCache will not lock a key that does not exist, so every acquisition failed, spun for the
    /// whole retry budget, and turned a long run of operations into minutes of waiting.
    /// </para>
    /// <para>
    /// Failure to acquire is not an error. Redis has no lock to fail, so refusing a command because
    /// another connection holds a lease would invent a failure mode the client cannot understand or
    /// retry sensibly. After the budget expires the operation proceeds unlocked - no worse than
    /// having no lock at all, which is where this listener stood before - and the contention is
    /// logged. The guarantee is therefore "atomic unless the lease is unavailable", not
    /// unconditional; it is stated that way in the README rather than implied to be stronger.
    /// </para>
    /// <para>
    /// This file is used only by <see cref="RespStringExtras"/>. It sat under the removed DataTypes
    /// folder while the collection commands needed it too, and came back out when those were
    /// dropped: it depends on nothing but <c>Cache.Lock</c> and <c>Cache.Unlock</c>, which this
    /// build has - the same pair the protobuf LockCommand and UnlockCommand use.
    /// </para>
    /// </summary>
    internal sealed class RespKeyLock : IDisposable
    {
        private const string Module = "RespKeyLock";

        /// <summary>How long a lease is held before the engine expires it.</summary>
        private static readonly TimeSpan LeaseTime = TimeSpan.FromSeconds(10);

        /// <summary>How long to keep trying before giving up and proceeding unlocked.</summary>
        private const int AcquireBudgetMs = 750;

        private const int RetryDelayMs = 4;

        private readonly Alachisoft.NCache.Caching.Cache _cache;
        private readonly string _lockKey;
        private readonly object _lockId;

        public bool Acquired { get; }

        private RespKeyLock(Alachisoft.NCache.Caching.Cache cache, string lockKey, object lockId, bool acquired)
        {
            _cache = cache;
            _lockKey = lockKey;
            _lockId = lockId;
            Acquired = acquired;
        }

        /// <summary>
        /// Takes the lease, or returns an unacquired instance after the budget expires. Never throws
        /// and never returns null, so a caller can always write <c>using (RespKeyLock.Acquire(...))</c>.
        /// </summary>
        public static RespKeyLock Acquire(Alachisoft.NCache.Caching.Cache cache, string key)
        {
            if (cache == null) return new RespKeyLock(null, null, null, false);

            int deadline = Environment.TickCount + AcquireBudgetMs;

            while (true)
            {
                try
                {
                    object lockId;
                    DateTime lockDate;

                    if (cache.Lock(key, LeaseTime, out lockId, out lockDate,
                                   RespCacheGateway.NewOperationContext()))
                    {
                        return new RespKeyLock(cache, key, lockId, true);
                    }
                }
                catch (Exception e)
                {
                    // Locking unavailable - a cache that has no such key yet, or a topology that
                    // does not support it. Proceed unlocked rather than failing the command.
                    RespLog.Detail(Module + ".Acquire", key + ": " + e.Message);
                    return new RespKeyLock(null, null, null, false);
                }

                if (Environment.TickCount >= deadline)
                {
                    RespLog.Detail(Module + ".Acquire",
                        "gave up waiting for '" + key + "'; proceeding without the lock");
                    return new RespKeyLock(null, null, null, false);
                }

                Thread.Sleep(RetryDelayMs);
            }
        }

        public void Dispose()
        {
            if (!Acquired || _cache == null) return;

            try
            {
                _cache.Unlock(_lockKey, _lockId, false, RespCacheGateway.NewOperationContext());
            }
            catch (Exception e)
            {
                // The lease expires on its own, so a failed release delays other writers by at
                // most LeaseTime rather than deadlocking them.
                RespLog.Detail(Module + ".Release", _lockKey + ": " + e.Message);
            }
        }
    }
}
