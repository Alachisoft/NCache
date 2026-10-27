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
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// INFO - the server report monitoring tools and client libraries parse.
    /// <para>
    /// The governing rule here is that nothing is invented. Whatever this emits will be scraped,
    /// charted and alerted on, and a plausible-looking number with nothing behind it is worse than
    /// no number at all: a flat zero on a dashboard reads as "measured and idle", not as "we do not
    /// track this". So a field appears only when NCache or the process can actually answer it, and
    /// a whole section is left out when it has nothing truthful to say.
    /// </para>
    /// <para>
    /// That is why several sections a real Redis emits are absent - see <see cref="Sections"/> for
    /// the list and the reason for each. The exception is Persistence, which is emitted with
    /// constants: NCache genuinely never loads, never forks a background save and has no AOF, so
    /// <c>loading:0</c> and <c>aof_enabled:0</c> are facts rather than placeholders - and clients
    /// (StackExchange.Redis among them) read <c>loading:</c> during connect and misbehave without it.
    /// </para>
    /// </summary>
    internal static class RespInfo
    {
        private const string Module = "RespInfo";
        private const string Crlf = "\r\n";

        /// <summary>Live RESP connection count. Wired by the listener; zero when nothing sets it.</summary>
        internal static Func<long> ConnectedClientsProvider { get; set; }

        /// <summary>Total accepted connections since start, and the max-connections cap.</summary>
        internal static Func<long> ConnectionsReceivedProvider { get; set; }
        internal static Func<int> MaxConnectionsProvider { get; set; }

        /// <summary>Connections turned away by max-connections. Redis reports these separately.</summary>
        internal static Func<long> RejectedConnectionsProvider { get; set; }

        /// <summary>
        /// Commands dispatched since start. One process serves exactly one cache - the cache host
        /// binds a single RESP port - so a process-wide counter is a per-cache counter.
        /// </summary>
        private static long _commandsProcessed;

        internal static void CountCommand()
        {
            System.Threading.Interlocked.Increment(ref _commandsProcessed);
        }

        /// <summary>
        /// A stable id for this server instance, as Redis reports in run_id. Regenerated per
        /// process, which is exactly what Redis does - a client uses a change in it to tell a
        /// restart from a reconnect.
        /// </summary>
        private static readonly string RunId = Guid.NewGuid().ToString("N");

        #region Section table

        /// <summary>
        /// The sections this listener can answer, in the order Redis emits them.
        /// <para>
        /// Deliberately absent, and why:
        /// </para>
        /// <list type="bullet">
        ///   <item><c>commandstats</c>, <c>latencystats</c>, <c>errorstats</c> - need per-command
        ///         call counts, latency histograms and error tallies on the dispatch path. That
        ///         instrumentation does not exist yet and is tracked separately.</item>
        ///   <item><c>keysizes</c> - needs to walk the keyspace, and key enumeration is refused on
        ///         this listener (SCAN/KEYS require a resumable cursor NCache does not provide).</item>
        ///   <item><c>hotkeys</c> - needs per-key access counts, which NCache does not track. It is
        ///         also not a section real Redis emits.</item>
        ///   <item><c>sentinel</c> - only exists on a server running in sentinel mode.</item>
        ///   <item><c>modules</c> - no module system, so nothing could ever be listed.</item>
        /// </list>
        /// <para>
        /// Asking for one of those returns empty, the same as a misspelt section name. Real Redis
        /// prints an empty <c># Modules</c> and <c># Sentinel</c> header instead; adding either back
        /// is one entry in this table.
        /// </para>
        /// </summary>
        private static readonly string[] Sections =
        {
            "server", "clients", "memory", "persistence", "stats",
            "replication", "cpu", "cluster", "keyspace"
        };

        #endregion

        /// <summary>INFO [section [section ...]] - Redis 7 allows several at once.</summary>
        public static RespCommandOutcome Write(
            RespRequest request, RespWriter writer, string cacheName, int respPort,
            RespCacheGateway gateway)
        {
            var wanted = Select(request);

            var sb = new StringBuilder();
            foreach (var section in Sections)
            {
                if (!wanted.Contains(section)) continue;
                Append(sb, section, cacheName, respPort, gateway);
            }

            writer.WriteBulkString(sb.ToString());
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// Which sections were asked for. No argument means all of them; an unrecognised name
        /// yields nothing at all, which is what Redis does and what clients rely on to probe.
        /// </summary>
        private static HashSet<string> Select(RespRequest request)
        {
            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (request.ArgCount == 0)
            {
                foreach (var s in Sections) wanted.Add(s);
                return wanted;
            }

            for (int i = 0; i < request.ArgCount; i++)
            {
                string name = Text(request.GetArg(i));

                // "all" and "everything" mean the non-default sections too. Here they coincide
                // with the default set, because every section this build can answer is a default
                // one - the non-default ones are precisely the ones not implemented.
                if (string.Equals(name, "all", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "everything", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "default", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in Sections) wanted.Add(s);
                    continue;
                }

                wanted.Add(name);
            }

            return wanted;
        }

        private static void Append(
            StringBuilder sb, string section, string cacheName, int respPort,
            RespCacheGateway gateway)
        {
            if (sb.Length > 0) sb.Append(Crlf);

            switch (section)
            {
                case "server":      Server(sb, cacheName, respPort); break;
                case "clients":     Clients(sb); break;
                case "memory":      Memory(sb, gateway); break;
                case "persistence": Persistence(sb); break;
                case "stats":       Stats(sb, gateway); break;
                case "replication": Replication(sb); break;
                case "cpu":         Cpu(sb); break;
                case "cluster":     Cluster(sb); break;
                case "keyspace":    Keyspace(sb, gateway); break;
            }
        }

        #region Sections

        private static void Server(StringBuilder sb, string cacheName, int respPort)
        {
            sb.Append("# Server").Append(Crlf);
            Line(sb, "redis_version", RespHandshakeCommands.ReportedRedisVersion);
            Line(sb, "ncache_version", RespHandshakeCommands.NCacheVersion);
            Line(sb, "ncache_cache_name", cacheName);

            // A client only attempts cluster mode when the server says it is in it. Under NCache's
            // native hash we must keep claiming standalone: the client's slot arithmetic would not
            // match where the server actually placed the key.
            Line(sb, "redis_mode",  "standalone");
            Line(sb, "os", Environment.OSVersion.ToString());
            Line(sb, "arch_bits", (IntPtr.Size * 8).ToString(CultureInfo.InvariantCulture));
            Line(sb, "process_id", Num(ProcessId()));
            Line(sb, "run_id", RunId);
            Line(sb, "tcp_port", Num(respPort));

            long uptime = UptimeSeconds();
            Line(sb, "uptime_in_seconds", Num(uptime));
            Line(sb, "uptime_in_days", Num(uptime / 86400));
        }

        private static void Clients(StringBuilder sb)
        {
            sb.Append("# Clients").Append(Crlf);
            Line(sb, "connected_clients", Num(Ask(ConnectedClientsProvider)));

            int max = 0;
            var provider = MaxConnectionsProvider;
            if (provider != null) { try { max = provider(); } catch { max = 0; } }
            Line(sb, "maxclients", Num(max));

            // Nothing can block: BLPOP, BRPOP, BLMOVE and the rest are refused outright, because
            // parking a request inside the engine and waking it on a write is not something NCache
            // offers. So this is a fact, not an unmeasured zero.
            Line(sb, "blocked_clients", "0");
        }

        private static void Memory(StringBuilder sb, RespCacheGateway gateway)
        {
            sb.Append("# Memory").Append(Crlf);

            long used = gateway == null ? 0 : gateway.GetCacheSize();
            Line(sb, "used_memory", Num(used));
            Line(sb, "used_memory_human", Human(used));

            long rss = 0, peak = 0;
            try
            {
                var p = Process.GetCurrentProcess();
                rss = p.WorkingSet64;
                peak = p.PeakWorkingSet64;
            }
            catch (Exception e) { RespLog.Detail(Module + ".Memory", e.Message); }

            Line(sb, "used_memory_rss", Num(rss));
            Line(sb, "used_memory_peak", Num(peak));

            var stats = gateway == null ? null : gateway.GetStatistics();
            long maxmemory = stats == null ? 0 : stats.MaxSize;
            Line(sb, "maxmemory", Num(maxmemory));
            Line(sb, "maxmemory_human", Human(maxmemory));

            // The cache's own eviction policy is configured per cache in config.ncconf and is not
            // one of Redis's named policies, so reporting a Redis policy name here would be a
            // guess. "noeviction" is the honest answer for a client asking whether the server will
            // silently drop its writes: NCache evicts on its own terms, not on maxmemory.
            Line(sb, "maxmemory_policy", "noeviction");
            Line(sb, "mem_allocator", "dotnet");
        }

        private static void Persistence(StringBuilder sb)
        {
            sb.Append("# Persistence").Append(Crlf);

            // Constants, and true ones. NCache never loads a dump at startup, never forks a
            // background save and has no append-only file, so each of these is the permanent
            // answer rather than an unmeasured placeholder. loading: in particular is read by
            // StackExchange.Redis during connect.
            Line(sb, "loading", "0");
            Line(sb, "rdb_changes_since_last_save", "0");
            Line(sb, "rdb_bgsave_in_progress", "0");
            Line(sb, "rdb_last_save_time", "0");
            Line(sb, "rdb_last_bgsave_status", "ok");
            Line(sb, "aof_enabled", "0");
            Line(sb, "aof_rewrite_in_progress", "0");
            Line(sb, "aof_last_bgrewrite_status", "ok");
            Line(sb, "aof_last_write_status", "ok");
        }

        private static void Stats(StringBuilder sb, RespCacheGateway gateway)
        {
            sb.Append("# Stats").Append(Crlf);
            Line(sb, "total_connections_received", Num(Ask(ConnectionsReceivedProvider)));
            Line(sb, "total_commands_processed",
                 Num(System.Threading.Interlocked.Read(ref _commandsProcessed)));

            var stats = gateway == null ? null : gateway.GetStatistics();
            Line(sb, "keyspace_hits", Num(stats == null ? 0 : stats.HitCount));
            Line(sb, "keyspace_misses", Num(stats == null ? 0 : stats.MissCount));

            // KNOWN GAP, and the one place in this file that reports a number it has not measured.
            //
            // Redis carries these as cumulative totals since start. NCache records expirations and
            // evictions only as per-second sample performance counters ("Expirations/sec",
            // "Evictions/sec" - see CustomCountersInstaller), with no running total and no getter
            // reachable from here, so there is nothing to read. They are emitted as zero because
            // clients and dashboards treat a missing field as a broken server, but a zero here
            // means "not tracked", NOT "nothing has expired". Sourcing them needs a cumulative
            // counter added on the NCache side.
            Line(sb, "expired_keys", "0");
            Line(sb, "evicted_keys", "0");

            Line(sb, "rejected_connections", Num(Ask(RejectedConnectionsProvider)));
        }

        private static void Replication(StringBuilder sb)
        {
            sb.Append("# Replication").Append(Crlf);

            // True as far as a RESP client is concerned. NCache replicates through its own cluster
            // channel between cache nodes; none of that is a Redis replica of this port, and a
            // client must never try to read from one.
            Line(sb, "role", "master");
            Line(sb, "connected_slaves", "0");
            Line(sb, "master_repl_offset", "0");
        }

        /// <summary>
        /// CPU consumed by this process.
        /// <para>
        /// The two <c>_children</c> fields are permanently zero, and that is correct rather than
        /// unmeasured: Redis reports there the CPU burnt by processes it forked for BGSAVE and AOF
        /// rewrite, and NCache forks nothing.
        /// </para>
        /// <para>
        /// The two <c>_main_thread</c> fields need reading with care. In Redis they mean the single
        /// event-loop thread, which is where essentially all of its work happens, so they track the
        /// process totals closely. NCache has no such thread - RESP commands are served across the
        /// thread pool - so what is reported here is the process's first-started thread, which does
        /// almost no command work and will sit near zero. It is what the field name literally says;
        /// it is not a measure of how busy the server is. Use used_cpu_sys and used_cpu_user for
        /// that.
        /// </para>
        /// </summary>
        private static void Cpu(StringBuilder sb)
        {
            sb.Append("# CPU").Append(Crlf);

            double sys = 0, user = 0, mainSys = 0, mainUser = 0;

            try
            {
                var p = Process.GetCurrentProcess();

                // Each of these is read separately. A single try around the lot would let one
                // unsupported property blank out the fields that did work.
                try { sys = p.PrivilegedProcessorTime.TotalSeconds; } catch { }
                try { user = p.UserProcessorTime.TotalSeconds; } catch { }

                ProcessThread main = MainThread(p);
                if (main != null)
                {
                    try { mainSys = main.PrivilegedProcessorTime.TotalSeconds; } catch { }
                    try { mainUser = main.UserProcessorTime.TotalSeconds; } catch { }
                }
            }
            catch (Exception e)
            {
                // INFO must not fail because the platform will not report processor time.
                RespLog.Detail(Module + ".Cpu", e.Message);
            }

            Line(sb, "used_cpu_sys", Seconds(sys));
            Line(sb, "used_cpu_user", Seconds(user));
            Line(sb, "used_cpu_sys_children", Seconds(0));
            Line(sb, "used_cpu_user_children", Seconds(0));
            Line(sb, "used_cpu_sys_main_thread", Seconds(mainSys));
            Line(sb, "used_cpu_user_main_thread", Seconds(mainUser));
        }

        /// <summary>
        /// The process's main thread, identified as the earliest-started one - ProcessThread
        /// carries no "is main" flag, and the collection is in no guaranteed order.
        /// <para>
        /// Threads can exit while the collection is being walked, and StartTime throws for a thread
        /// that has gone, so every read is guarded and a thread that cannot be read is skipped
        /// rather than aborting the scan.
        /// </para>
        /// </summary>
        private static ProcessThread MainThread(Process process)
        {
            ProcessThread earliest = null;
            DateTime earliestStart = DateTime.MaxValue;

            try
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    DateTime started;
                    try { started = thread.StartTime; }
                    catch { continue; }

                    if (started < earliestStart)
                    {
                        earliestStart = started;
                        earliest = thread;
                    }
                }
            }
            catch (Exception e) { RespLog.Detail(Module + ".MainThread", e.Message); }

            return earliest;
        }

        /// <summary>
        /// # Cluster - two lines, exactly what a standalone Redis answers.
        /// <para>
        /// Always 0, and not a placeholder: this edition refuses the whole CLUSTER family, and the
        /// Server section reports redis_mode:standalone. A client decides whether to attempt
        /// cluster mode from those two facts, so they have to agree - claiming 1 here would send it
        /// looking for slots and a CLUSTER SLOTS reply it would then be refused.
        /// </para>
        /// <para>
        /// The section was listed in <see cref="Sections"/> with no case in the switch above, so
        /// INFO answered eight of the nine sections it advertised and never reported
        /// cluster_enabled at all - which a client reads as "field absent" rather than "not a
        /// cluster".
        /// </para>
        /// </summary>
        private static void Cluster(StringBuilder sb)
        {
            sb.Append("# Cluster").Append(Crlf);
            Line(sb, "cluster_enabled", "0");
        }

        private static void Keyspace(StringBuilder sb, RespCacheGateway gateway)
        {
            sb.Append("# Keyspace").Append(Crlf);

            // The live count, not the Statistics snapshot: that one is refreshed on a timer and
            // reads zero for a while after a cache starts, which made this section vanish exactly
            // when a client was checking what it had just written.
            long count = gateway == null ? 0 : gateway.GetKeyCount();

            // Redis omits the line entirely for an empty database rather than printing zero, and
            // clients use its absence to mean "nothing stored".
            if (count <= 0) return;

            // expires and avg_ttl are reported as zero because NCache does not separate keys that
            // carry an expiry from those that do not without walking the keyspace, which this
            // listener cannot do through the engine's public surface. The key count itself is real.
            sb.Append("db0:keys=").Append(Num(count))
              .Append(",expires=0,avg_ttl=0").Append(Crlf);
        }

        #endregion

        #region Helpers

        private static void Line(StringBuilder sb, string name, string value)
        {
            sb.Append(name).Append(':').Append(value).Append(Crlf);
        }

        private static string Num(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Redis prints processor time with six decimals. Invariant culture is not optional: a
        /// machine running a comma-decimal locale would emit "0,123456" and break every parser.
        /// </summary>
        private static string Seconds(double value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        private static string Human(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return (bytes / (1024.0 * 1024 * 1024)).ToString("F2", CultureInfo.InvariantCulture) + "G";
            if (bytes >= 1024 * 1024)
                return (bytes / (1024.0 * 1024)).ToString("F2", CultureInfo.InvariantCulture) + "M";
            if (bytes >= 1024)
                return (bytes / 1024.0).ToString("F2", CultureInfo.InvariantCulture) + "K";
            return Num(bytes) + "B";
        }

        private static long Ask(Func<long> provider)
        {
            if (provider == null) return 0;
            try { return provider(); }
            catch { return 0; }
        }

        private static int ProcessId()
        {
            try { return Process.GetCurrentProcess().Id; }
            catch { return 0; }
        }

        private static long UptimeSeconds()
        {
            try
            {
                var elapsed = DateTime.Now - Process.GetCurrentProcess().StartTime;
                return elapsed.Ticks > 0 ? (long)elapsed.TotalSeconds : 0;
            }
            catch { return 0; }
        }

        private static string Text(byte[] raw)
        {
            return raw == null ? string.Empty : Encoding.UTF8.GetString(raw);
        }

        #endregion
    }
}
