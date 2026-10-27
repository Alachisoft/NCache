# NCache RESP (Redis protocol) listener

A second TCP listener inside each NCache cache host that speaks the Redis Serialization Protocol,
so stock Redis clients (`redis-cli`, StackExchange.Redis, redis-py, …) can talk to NCache directly.

It runs **alongside** the existing protobuf listener in the same process and drives the **same**
cache engine, so a key written by one protocol is visible to the other.

> ## ⚠️ THERE IS NO SECURITY ON THIS PORT
>
> **There is no TLS.** The port is plaintext, always: every command and every value is readable by
> anything on the path. Put it behind a TLS-terminating proxy if the traffic must cross a network
> you do not trust.
>
> **There is no authentication.** `AUTH` is not part of NCache Open Source: it is refused as an
> unsupported command, and so is the `AUTH` clause of `HELLO`. Anything that can reach the port has
> full read/write access to the entire cache — every key written by every NCache client — and
> `FLUSHALL` is one command away.
>
> Both of those are stated in the listener's own log on every start, so the state is never a guess.
> What reaches the port is therefore the only control there is: RESP is off unless `resp.ncconf`
> names the cache, and the listener binds `NCacheServer.BindToClientServerIP` rather than every
> interface. Keep it on a trusted network.
>
> There is no per-user identity either, in any edition: no users and no per-key permissions, so
> `ACL` is refused rather than stubbed, and NCache's own Active Directory security does not apply
> to this port.
>
> A client configured with a password will fail at `AUTH` rather than connect and believe it
> authenticated. That is deliberate: this listener used to answer `+OK` to any `AUTH`, which is the
> most dangerous shape of all, because it looks secure from the outside.

---

## 1. Enabling it

Two switches, both must be on.

**a. Per cache** — add a `<resp-server>` element inside `<cache-settings>` in `config.ncconf`
(`C:\Program Files\NCache\config\config.ncconf` on Windows), as a sibling of `<pipelining>`:

```xml
<cache-config cache-name="demoCache" ...>
  <cache-settings inproc="False" ...>
    <pipelining enabled="True" batch-interval="250usec"/>
    <resp-server enable="true" port="6379" bind-ip="" max-connections="1000" idle-timeout="0"/>
    ...
  </cache-settings>
</cache-config>
```

| attribute | default | meaning |
|---|---|---|
| `enable` | `false` | Off by default; upgrading an existing deployment never opens a port on its own. |
| `port` | `6379` | Must be unique per cache on a machine. A collision is logged and RESP stays off for that cache — the cache still starts normally. |
| `bind-ip` | `""` | Empty inherits `NCacheServer.BindToClientServerIP`. |
| `max-connections` | `1000` | Redis `maxclients` analogue. Further clients get `-ERR max number of clients reached`. |
| `idle-timeout` | `0` | Seconds; `0` disables, like Redis `timeout 0`. |

A cache with no `<resp-server>` element is treated as disabled, so existing config files keep
working untouched. The Dom is `Src/NCCache/Config/Dom/RespServer.cs`.

**b. Per machine** — `NCacheServer.EnableRespServer` in
`bin\service\Alachisoft.NCache.Service.dll.config`. It defaults to `true`, and exists so an
operator can kill every RESP listener on a box without editing each cache's config:

```xml
<add key="NCacheServer.EnableRespServer" value="true"/>
```

**c. Restart the cache** so the host process re-reads its config:

```powershell
Import-Module 'C:\Program Files\NCache\bin\tools\ncacheps'   # Windows PowerShell 5.1 only
Stop-Cache  demoCache -Server <server-ip>
Start-Cache demoCache -Server <server-ip>
```

Confirm the port is up:

```powershell
Get-NetTCPConnection -State Listen -LocalPort 6379
```

### Environment variables

Everything below is read from the process environment rather than from `config.ncconf`, because
each one has to be identical on every node and a per-cache setting invites them to drift apart.

| variable | effect |
|---|---|
| `NCACHE_KEY_HASH=crc16` | Redis-compatible CRC16 slot placement, and the switch that turns the `CLUSTER` family on. **A cluster-wide contract enforced nowhere**: every node and every client must agree, and the cache must start empty. A mismatch misroutes silently — there is no handshake to catch it. See `Src/NCCommon/Util/RedisKeyHash.cs`. |
| `NCACHE_RESP_ANNOUNCE` | Overrides the endpoints advertised by `CLUSTER SLOTS`/`NODES`, for NAT and container port mapping. |
| `NCACHE_RESP_FORWARD_DISABLED=1` | Turns off RESP-layer forwarding; every command is served by whichever node the client happened to hit. |
| `NCACHE_RESP_FORWARD_SELFTEST=1` | Forces the forwarding path so it can be exercised on one node. Never set in production. |

---

## 2. Pointing a client at it

### redis-cli

```
redis-cli -h <server-ip> -p 6379
> PING
PONG
> SET user:1 alice
OK
> GET user:1
"alice"
> TTL user:1
(integer) -1
> SET session:1 token EX 60
OK
> TTL session:1
(integer) 60
> RPUSH jobs a b c
(integer) 3
> TYPE jobs
list
```

Plain `telnet` works too — inline commands (`PING\r\n`) are supported.

### StackExchange.Redis

```csharp
var options = new ConfigurationOptions
{
    EndPoints = { { "20.200.20.20", 6379 } },
    AbortOnConnectFail = false,

    // RESP2 only. The library will negotiate RESP3 if you let it, and HELLO 3 is refused
    // with NOPROTO - pin it rather than rely on the fallback.
    Protocol = RedisProtocol.Resp2,
};

using var redis = ConnectionMultiplexer.Connect(options);
IDatabase db = redis.GetDatabase();

db.StringSet("user:1", "alice");
string value = db.StringGet("user:1");
db.StringSet("session:1", "token", TimeSpan.FromSeconds(60));
```

Earlier revisions of this file told you to disable `CLUSTER`, `SUBSCRIBE`, `PSUBSCRIBE` and
`PUBLISH` through a `CommandMap`. **Do not** — all four are implemented, and suppressing them
client-side only hides working functionality. Cluster mode additionally needs
`NCACHE_KEY_HASH=crc16`; without it the server truthfully reports `standalone` and the library will
not attempt slot routing.

### redis-py

```python
import redis
r = redis.Redis(host="20.200.20.20", port=6379, decode_responses=True, protocol=2)
r.set("user:1", "alice")
print(r.get("user:1"))
```

---

## 3. Supported commands

The `switch` in `RespCommandDispatcher.Dispatch` is the authoritative list. This table tracks it.

| family | commands |
|---|---|
| **Strings / CRUD** | `GET` `SET` (`EX` `PX` `EXAT` `PXAT` `NX` `XX` `KEEPTTL` `GET`) `SETNX` `SETEX` `PSETEX` `GETSET` `GETDEL` `GETEX` `DEL` `UNLINK` `EXISTS` `TOUCH` `MGET` `MSET` `TYPE` |
| **Flush** | `FLUSHDB` `FLUSHALL` (both accept `ASYNC`/`SYNC`) — see the warning below |
| **String shape** | `STRLEN` `APPEND` `GETRANGE` `SUBSTR` `SETRANGE` |
| **Counters** | `INCR` `DECR` `INCRBY` `DECRBY` |
| **Expiration** | `EXPIRE` `PEXPIRE` `EXPIREAT` `PEXPIREAT` `TTL` `PTTL` `PERSIST` `EXPIRETIME` `PEXPIRETIME` |
| **Hashes** | `HSET` `HMSET` `HGET` `HMGET` `HDEL` `HEXISTS` `HSETNX` `HLEN` `HSTRLEN` `HGETALL` `HKEYS` `HVALS` |
| **Lists** | `LPUSH` `RPUSH` `LPUSHX` `RPUSHX` `LPOP` `RPOP` `LLEN` `LINDEX` `LSET` `LRANGE` |
| **Sets** | `SADD` `SREM` `SISMEMBER` `SCARD` `SMEMBERS` `SPOP` `SRANDMEMBER` |
| **Streams** | `XADD` `XLEN` `XRANGE` `XREVRANGE` `XDEL` `XTRIM` `XREAD` `XGROUP` `XREADGROUP` `XACK` `XINFO` `XPENDING` `XCLAIM` |
| **Pub/sub** | `SUBSCRIBE` `UNSUBSCRIBE` `PSUBSCRIBE` `PUNSUBSCRIBE` `PUBLISH` `PUBSUB` |
| **Sharded pub/sub** | `SSUBSCRIBE` `SUNSUBSCRIBE` `SPUBLISH` — these differ from plain pub/sub only in how Redis routes them between nodes, and ours is already cluster-wide, so they are the same three handlers. |
| **Connection / handshake** | `PING` `ECHO` `TIME` `QUIT` `HELLO` `SELECT` `RESET` `CLIENT` `COMMAND` `CONFIG` `INFO` |
| **Cluster** | `CLUSTER KEYSLOT` `MYID` `INFO` `SLOTS` `SHARDS` `NODES` |

`CLIENT` covers `SETNAME` `GETNAME` `ID` `SETINFO` `INFO`, and accepts `NO-EVICT` `NO-TOUCH` `REPLY`
as no-ops because they only tune Redis-internal behaviour.

`CLIENT PAUSE` is explicitly unsupported: this listener cannot pause command execution across
client connections. It returns an error and leaves the connection open.

`CLUSTER COUNTKEYSINSLOT` is explicitly unsupported: NCache tracks keys per bucket, which spans
multiple Redis slots, and does not provide per-slot key counts. It returns an error instead of zero.

### What is refused, and why

`RespUnsupported.cs` holds this table. The rule for being in it is strict: **only commands that need
an engine capability NCache does not have.** Anything expressible over what the engine already
provides is implemented instead — a command that half-works, or that says "unsupported" when it
could have worked, is worse than either honest answer.

| the engine would need | refused commands |
|---|---|
| a sorted/scored collection | the whole `Z*` family — `ZADD` `ZRANGE` `ZSCORE` `ZINCRBY` `ZPOPMIN`/`MAX` `ZUNION`/`ZINTER`/`ZDIFF` … |
| a queued multi-key batch with rollback; NCache locking is single-key | `MULTI` `EXEC` `DISCARD` `WATCH` `UNWATCH` |
| an atomic multi-key add-if-absent operation; NCache bulk Add can partially succeed | `MSETNX` |
| an embedded script interpreter | `EVAL` `EVALSHA` `SCRIPT` `FUNCTION` `FCALL` |
| bit-level addressing inside a stored value | `SETBIT` `GETBIT` `BITCOUNT` `BITPOS` `BITOP` `BITFIELD` |
| a probabilistic cardinality structure | `PFADD` `PFCOUNT` `PFMERGE` |
| geospatial indexing, which builds on sorted sets | `GEOADD` `GEOPOS` `GEODIST` `GEOHASH` `GEOSEARCH` `GEORADIUS` |
| a way to park a request inside the engine and wake it on a write | `BLPOP` `BRPOP` `BLMOVE` `BRPOPLPUSH` `BLMPOP` |
| an atomic move between two keys | `RPOPLPUSH` `LMOVE` `LMPOP` `SMOVE` `COPY` `MOVE` `MIGRATE` |
| binary-safe value comparison inside a collection — the engine compares elements through a JSON parse, so remove-by-value is not reliable | `LREM` `LPOS` `LINSERT` `LTRIM` |
| server-side set algebra | `SUNION` `SINTER` `SDIFF` (+`STORE`) `SINTERCARD` `SMISMEMBER` |
| Redis-compatible collection sorting with `BY`/`GET` lookups and `STORE` support | `SORT` |
| a resumable cursor over the keyspace or a collection | `SCAN` `HSCAN` `SSCAN` `KEYS` `RANDOMKEY` `DBSIZE` |
| atomic increment of a single field inside a collection | `HINCRBY` `HINCRBYFLOAT` |
| a floating-point counter; the NCache counter type is integer-only | `INCRBYFLOAT` |
| random selection over a dictionary | `HRANDFIELD` |
| a serialisable key representation for export and import | `DUMP` `RESTORE` `OBJECT` `RENAME` `RENAMENX` `LCS` |
| per-consumer registry state this build does not keep | `XAUTOCLAIM` `XSETID` |
| persistence and replication controls; NCache manages both itself | `SAVE` `BGSAVE` `LASTSAVE` `REPLICAOF` `FAILOVER` `WAIT` |
| Redis replication role details; NCache manages replication through its own topology | `ROLE` |
| server administration that would bypass NCache's own management surface | `SHUTDOWN` `DEBUG` `MONITOR` `SLOWLOG` `LATENCY` `MEMORY` |
| an access-control list | `ACL` |

### ⚠️ `FLUSHDB` / `FLUSHALL` empty the whole cluster

Both map onto `Cache.Clear` — the same engine call the protobuf `ClearCommand` makes, with the same
flag and the same `RaiseCQNotification` context, so continuous queries and cache-cleared events fire
exactly as they do for a protobuf client.

Two differences from Redis, both worth knowing before anyone types one:

- **They are cluster-wide, not node-local.** `PartitionedServerCache.Clear` calls `Clustered_Clear`
  whenever more than one server is in the cluster. In Redis, `FLUSHALL` sent to one node empties
  only that node and a client repeats it against every master. Here a single call wipes everything.
- **`FLUSHDB` and `FLUSHALL` are the same operation.** An NCache cache is one keyspace and `SELECT`
  accepts nothing but `0`, so "this database" and "all databases" name the same thing.

`ASYNC` and `SYNC` are accepted and both behave synchronously. In Redis the flag only chooses
whether memory is reclaimed on a background thread — the keyspace is empty immediately either way.
NCache's `ClearAsync` is genuinely fire-and-forget, so using it for `ASYNC` would let a client see
keys after its flush returned `OK`, which is the one thing Redis guarantees will not happen.

Every flush is logged at critical level with the cache name. **Remember that there is no
authentication on this port**: anything that can reach it can empty the entire cache in one
command.

### Three shapes of refusal

A client can act differently on each, so they are kept distinct. Nothing is ever silently ignored,
and nothing ever drops the connection.

```
> ZADD leaderboard 1 alice
(error) ERR unsupported command 'ZADD': not supported by NCache - requires a sorted/scored
        collection, which the NCache engine does not provide

> BOGUS a b
(error) ERR unknown command 'BOGUS', with args beginning with: 'a', 'b'

> CLUSTER SLOTS                     # with NCACHE_KEY_HASH unset
(error) ERR This instance has cluster support disabled: the RESP listener only advertises a
        cluster when keys are distributed by CRC16 (set NCACHE_KEY_HASH=crc16 on every node)
```

The third is a *configuration state*, not a missing capability — turn CRC16 on and the same command
works. That is why it does not come out of the capability table.

---

## 4. Interop with regular NCache clients

Values written over RESP are stored as raw bytes with NCache's `BinaryData` flag, which is what
makes them readable from the typed client — **as `byte[]`**:

```csharp
// written by a RESP client as SET user:1 alice
byte[] raw = cache.Get<byte[]>("user:1");
string value = Encoding.UTF8.GetString(raw);       // "alice"

cache.Get<string>("user:1");                        // throws InvalidCastException
```

The reverse works when the NCache client stores `byte[]`, which uses the same representation:

```csharp
cache.Insert("user:2", new CacheItem(Encoding.UTF8.GetBytes("bob")));
// redis-cli> GET user:2   ->   "bob"
```

**A value written by a typed client as a normal object is *not* readable as text over RESP.** NCache
serializes on the client side, so `cache.Insert("k", "hello")` stores a CompactBinaryFormatter
payload; `GET k` returns that whole blob, not `hello`. This is inherent to NCache's design, not a
listener limitation — the server never sees the original object. Use `byte[]` on both sides when you
need cross-protocol values.

The collection commands map onto NCache's own distributed data types, so a Redis hash is an NCache
`DistributedDictionary`, a Redis list is a `DistributedList`, a Redis set is a `DistributedHashSet`,
and a key `INCR` created is a `DistributedCounter`. `TYPE` reports the Redis name for each —
`hash`, `list`, `set`, `stream` — and a counter reports `string`, because Redis has no counter type:
there, `INCR` and `GET` address one and the same string, and so they do here.

---

## 5. Known differences from a real Redis

The ones most likely to bite:

- **`GETEX key [EX seconds | PX milliseconds | EXAT seconds | PXAT milliseconds | PERSIST]`**
  returns the value and replaces or removes its expiry. With no option it leaves expiry unchanged;
  a past positive absolute timestamp returns the value and deletes the key immediately. Replacing
  expiry copies and reinserts the entry, preserving metadata and counter type, because NCache's
  add-expiration API combines deadlines rather than replacing them. The key lease is best effort,
  so a concurrent writer can still be lost; this is not an atomic read-and-expiry update.
- **`LPUSHX` / `RPUSHX`** append/prepend only to existing lists, returning `0` for a missing key.
  They accept multiple values, retain the existing TTL and use the same best-effort key lease as
  `LPUSH` / `RPUSH`. `HSTRLEN` returns the field value's byte length, or `0` for a missing key/field.

- **Expired keys stay readable** until NCache's cleanup sweep — up to `<cleanup interval>`, 15s by
  default. `GET` will serve a value after its TTL has passed, and `EXISTS` reports `1` while `TTL`
  reports `0`. Confirmed identical on the protobuf path, so this is NCache behaviour, not a RESP
  artefact. It breaks rate limiters, session timeouts and lock leases.
- **No atomicity** for `PERSIST`, `SET XX`, `SET KEEPTTL` and `GETSET` — each is two engine calls
  with no lock, so a concurrent writer to the same key can be lost. `GET`, `SET`, `SETNX`, `DEL`,
  `EXISTS`, `GETDEL`, `MGET`, `MSET`, `TTL` are single calls and unaffected; `APPEND`, `SETRANGE`
  and the read-modify-write collection commands take a key lease instead.
- **`INCR` on a key `SET` as a string is not atomic.** Redis has no counter type, so a numeric
  string has to be adopted into an NCache counter before the delta can land, and that is a delete
  followed by a create. Two clients incrementing a freshly `SET` key can both read the same seed.
  It only ever happens on the first `INCR` after a `SET` — every later one goes through the engine's
  atomic counter.
- **`PERSIST` and `SET KEEPTTL` drop entry metadata** — group, tags and `queryInfo` are lost, so a
  key touched by them disappears from NCache query results.
- **`SRANDMEMBER key -N`** returns `N` elements with repeats, as Redis does, but draws them from the
  distinct members the engine returned rather than independently from the whole set. The count is
  right; the distribution is not Redis's.
- **One keyspace** — `SELECT 0` only; a non-zero index is refused. An NCache cache has no numbered
  databases.
- **RESP2 only** — `HELLO 3` is refused with `NOPROTO`.
- **No authentication and no identity** — see the banner. `AUTH` and `ACL` are both refused.
- **`COMMAND` and `CONFIG GET` are deliberately thin.** `COMMAND` answers an empty array (and
  `COMMAND COUNT` zero); `CONFIG GET` answers an empty map, which StackExchange.Redis treats as
  "not configured". `CONFIG SET` is refused outright — cache configuration must never be mutable
  from this port.
- **`INFO` reports nine sections, and omits the rest rather than faking them.** Present, with real
  numbers: `server` `clients` `memory` `persistence` `stats` `replication` `cpu` `cluster`
  `keyspace`. Section selection works as in Redis — `INFO`, `INFO <section>`, several at once, and
  `all`/`everything`; an unrecognised name returns empty.

  Absent, because nothing truthful backs them: `commandstats`, `latencystats` and `errorstats` need
  per-command instrumentation that does not exist yet; `keysizes` would have to walk the keyspace,
  and key enumeration is refused here; `hotkeys` needs per-key access counts NCache does not track
  (and is not a real Redis section anyway); `sentinel` only exists in sentinel mode; `modules`
  could never list anything. Asking for one of those returns empty — indistinguishable from a
  misspelt section name. Real Redis does print an empty `# Modules` and `# Sentinel`; this does not.

  `# Persistence` is the one section built from constants rather than measurements, and they are
  facts: NCache never loads a dump, never forks a background save and has no AOF, so `loading:0`
  and `aof_enabled:0` are permanent. The section exists because clients read `loading:` on connect.
- **`used_cpu_*_main_thread` is near zero and does not mean what it does in Redis.** There it is the
  single event-loop thread, where all the work happens. NCache serves RESP commands across the
  thread pool, so what is reported is the process's first-started thread, which does almost none of
  that work. It is literally the main thread; it is not a measure of server load. Use `used_cpu_sys`
  and `used_cpu_user`. The `_children` fields are permanently zero, which is correct — Redis counts
  CPU burnt by processes forked for `BGSAVE`/AOF rewrite, and NCache forks none.
- **`db0:keys=` counts keys but reports `expires=0`.** NCache cannot separate keys carrying a TTL
  from those without one short of walking the keyspace. The key count is real; the expiry breakdown
  is not tracked. An empty cache omits the line entirely, as Redis does.

---

## 6. Cluster, slots and forwarding

Off unless `NCACHE_KEY_HASH=crc16` is set on every node. With it, NCache places keys by the same
CRC16-mod-16384 arithmetic Redis uses, which is what lets a cluster-aware client compute a slot and
have the server agree. `RedisKeyHash` hooks into `AppUtil.GetHashCode`, so every bucket-resolution
site in the engine goes through it and the RESP layer can never disagree with the engine about
ownership.

Two mechanisms then keep a client's request and the data in the same place:

- **Redirection.** `OperationContextFieldName.CallerRoutesBySlot` makes a partitioned node decline a
  key it does not own with `SlotNotOwnedException`, naming the owner, instead of silently fetching
  it over the cluster channel. The listener turns that into a Redis `MOVED`. It fires only when the
  caller opts in *and* the cache distributes by CRC16 — native NCache clients never opt in, because
  their distribution map can be briefly stale during a topology change.
- **Forwarding.** For clients that are not cluster-aware, `RespForwarder` hands the whole command to
  the owning node over a pooled peer connection and relays the reply, so any node can answer for any
  key. `NCACHE_RESP_FORWARD_DISABLED=1` turns this off.

`Cluster/RespClusterRouter` resolves ownership through the engine's own
`DistributionManager.SelectNode` rather than reimplementing the key hash.

### The dispatch order is load-bearing

`RespCommandDispatcher.Dispatch` runs five gates before the command table, and the order is the
whole of the security here. Do not reorder it; the comments in the method name the attack each step
closes.

1. **No authentication gate** — there is none in this build, because there is no `AUTH`. The
   gate that stood here had to come first, ahead of everything: with the peer handshake checked
   first, any client could send `NCRESPFORWARD`, be marked a trusted peer and then run every
   command without knowing the password; with forwarding checked first, an unauthenticated client
   could have its command carried to the owning node — which *did* authenticate — and get the
   answer back. Both were real, which is why the requirement is recorded here: anything put back in
   this position has to satisfy it.
2. **Peer handshake (`NCRESPFORWARD`)** — marks the connection as forwarded traffic, whose commands
   are served locally or fail, never forwarded onward. That is what stops a forwarding loop.
3. **`RespForwarder.TryForward`** — hands the command to the owner and relays the reply.
4. **Subscriber-mode gate** — Redis narrows the command set to almost nothing once a connection
   holds a subscription, and clients rely on it: a `GET` on a subscribed connection must be refused,
   not answered on a link the client is treating as a one-way message stream.
5. **The command table.**

---

## 7. Testing

**Codec unit tests** — NUnit, no cache required. They *link* the codec sources rather than
referencing the assembly, which is only possible because `Protocol/` has no NCache dependency. The
CRC16 compatibility tests do reference a built `NCCommon`, so build that first:

```powershell
dotnet build Src\NCCommon\NCCommon.Net.csproj -c Debug
dotnet test Testing\RespProtocol\RespProtocol.Tests\RespProtocol.Tests.csproj
```

**Conformance suite** — `Testing/NCacheRespProtocol`, xUnit v3 on .NET 10 over StackExchange.Redis,
346 tests covering every command family. It is written to documented Redis behaviour and is verified
green against real Redis, so a failure means this server differs. A command the server *refuses* is
recorded as a warning and a skip rather than a failure, and written to `unsupported-commands.log`
beside the test assembly — which doubles as a compatibility report. Run it from its own directory,
since its `global.json` opts into Microsoft.Testing.Platform and `dotnet test` fails without that on
the .NET 10 SDK:

```powershell
cd Testing\NCacheRespProtocol
dotnet test                                   # target from redis.settings.json
$env:REDIS_HOST="20.200.20.34"; dotnet test   # or override
```

**Targeted apps** — plain executables, each needing a running cache with RESP enabled. Defaults
assume a local two-node cluster on `127.0.0.1:6381,6382`:

```powershell
dotnet run --project Testing\RespProtocol\RespDataTypes.Test -- --endpoints 127.0.0.1:6381,127.0.0.1:6382 [--only <case>]
dotnet run --project Testing\RespProtocol\RespPubSub.Test   -- --endpoints ... [--timeout <ms>]
dotnet run --project Testing\RespProtocol\RespStreams.Test  -- --endpoints ... [--only <case>]
dotnet run --project Testing\RespProtocol\RespOwnership.Test
dotnet run --project Testing\RespProtocol\RespTls.Test      -- <endpoints> --thumbprint <hex>
dotnet run --project Testing\RespProtocol\RespBench         -- --endpoints ... --mode <m> --ops N --concurrency N [--json]
```

`RespClient.Sample` is a minimal worked example rather than a test.
`Testing/RespProtocol/Resp.Test.slnx` opens all of them.

Run any failing case against real Redis before believing it is a server bug — the apps reference no
NCache assembly precisely so they can be pointed at Redis as the control.

---

## 8. How it fits together

RESP socket connections and disconnections are recorded at `CriticalInfo` level in the cache
log, with the connection ID (`CLIENT ID`), remote endpoint, cache name and current RESP connection
count (including subscriber connections). Lifecycle logs and system events are emitted only when
`InSubscriberMode` is false at the time of the event. All newly accepted sockets initially satisfy
this condition; subscribing later does not remove their earlier connection record.
Before the cache is available, records fall back to the socket-server logger. These are
transport lifecycle records, written before any command is dispatched, and include inbound forwarding
connections. Each socket is counted separately. Normal closure, failures and listener shutdown
use the same disconnect path. `NCacheServer.LogClientEvents` additionally enables Information
events 1004/1005 through NCache's existing system event logger and its event-level filtering.
These records do not register RESP sockets as native NCache clients.

```
Alachisoft.NCache.CacheHost.exe        (one process per cache)
├── SocketServer (HostClient)          protobuf; fed by the service via TransferConnection
├── SocketServer (Management)          management port
└── RespListener                       binds the cache's RESP port directly
        └── RespConnection*            one per client socket: receive → parse → dispatch → one flush
                └── RespCommandDispatcher
                        ├── RespCacheGateway              ─┐
                        ├── DataTypes/RespDataTypeGateway  ├─ the only files that reach
                        ├── PubSub/RespPubSubHub           │  Alachisoft.NCache.Caching.Cache
                        └── Streams/RespStreamStore       ─┘
```

A cache host serves exactly one cache, so the port identifies the cache: RESP needs no equivalent of
the protobuf `INIT` handshake and no cache name on the wire. `RespListener.TryStart` never throws and
returns `null` when RESP is disabled, so a problem here can never stop a cache serving protobuf
clients.

The listener binds before the cache exists, so it attaches to the cache on the cache's own start
event rather than on the first command — attaching is what subscribes to that instance's
`CacheStopped`, and a client that only ever sends `PING` must not be the reason the port is still
held at the stop. When the cache does stop, the listening socket is closed **inline** on the
cache-stopping thread (`StopAccepting`) and only the live connections are torn down on a queued
work item: the service treats the cache as stopped the moment `Cache.Dispose` returns and may start
a replacement host immediately, and that host binds this same port. From the stop onwards commands
answer `-LOADING` rather than reaching a cache that reports `IsRunning` while its parts are already
gone.

| file | role |
|---|---|
| **`Protocol/`** | **No NCache dependency at all — that is what lets its tests run without a cache. Keep it that way.** |
| `Protocol/RespReader.cs` | incremental request parser — inline and multibulk, resumable across TCP segments, pipelining, size limits |
| `Protocol/RespWriter.cs` | reply serializer; strips CR/LF from simple strings and errors so a message can never break framing |
| `Protocol/RespRequest.cs` | one parsed command; arguments stay `byte[]` because RESP is binary |
| `Protocol/RespLimits.cs` | the size and count ceilings the parser enforces |
| `RespListener.cs` | bind, accept, connection cap, shutdown, config |
| `RespConnection.cs` | per-socket receive → parse → dispatch → single flush |
| `RespCommandDispatcher.cs` | the gates above, then the command table |
| `RespCacheGateway.cs` | string CRUD and expiration against the engine |
| `RespStringExtras.cs` | `STRLEN` `APPEND` `GETRANGE` `SETRANGE` `EXPIRETIME` |
| `RespHandshakeCommands.cs` | `HELLO`, `CLIENT`, `COMMAND`, `CONFIG`, `SELECT`, `RESET` |
| `RespInfo.cs` | the `INFO` section table and per-section builders |
| `RespUnsupported.cs` | what is refused, and the engine capability each one would need |
| `RespErrorMapper.cs` | NCache exception → RESP error, using real Redis error prefixes |
| `RespConnectionState.cs`, `RespLog.cs` | per-connection state; logging |
| **`DataTypes/`** | hashes, lists, sets, counters |
| `DataTypes/RespDataTypeCommands.cs` | the command handlers |
| `DataTypes/RespDataTypeGateway.cs` | the engine calls behind them |
| `DataTypes/RespKeyLock.cs` | a lease on the data key around read-modify-write sequences |
| `DataTypes/RespValueCodec.cs` | element tagging, so binary values survive the engine's JSON element model |
| **`PubSub/`** | `RespPubSubHub` (cluster-wide fan-out), `RespPubSubCommands`, `RespPushFrames`, `RespGlob` |
| **`Streams/`** | `RespStreamStore` (list + counter + hash, tagged into one slot), `RespStreamCommands`, `RespStreamEntry` |
| **`Cluster/`** | `RespClusterRouter` (ownership via `DistributionManager`), `RespClusterCommands`, `RespForwarder`, `RespPeerPool`, `RespSlotMap`, `RespAnnounce`, `RespReplyFramer` |

Class-level `///` comments in this layer carry the *reasoning* — why a design was rejected, what an
earlier version got wrong. They are the fastest way in; read them before the code.
