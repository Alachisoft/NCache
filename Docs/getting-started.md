# Getting Started with NCache Open Source

NCache Open Source brings in-memory distributed caching to .NET applications. This guide walks through installing NCache, standing up a Local or Replicated cache, connecting a client, and running your first cache operations.

> **Scope:** NCache Open Source supports **Local** and **Replicated** cache topologies only. The combined size of all caches on a node is capped at **4 GB**, and Replicated caches are limited to a maximum of **3 nodes**.

## Prerequisites

- A supported OS: Windows or Linux.
- [Docker](https://www.docker.com/) if you'd rather run NCache in a container instead of installing it directly.
- .NET SDK, if you plan to build a client application (see Step 6).

## Step 1: Install NCache

You can either install NCache natively or run it via Docker.

### Option A — Native install (Windows)

1. Download the installer from the [NCache Downloads](https://www.alachisoft.com/download-ncache.html) page (registration required).
2. Copy the `.msi` file to a local folder, e.g. `C:\NCache`.
3. Open **Command Prompt as Administrator** and run:

   ```
   msiexec /I "<Setup Path>\ncache.oss.x64.msi"
   ```

4. Follow the setup wizard: accept the license agreement, then enter the installation key and account details you registered with.
5. Click **Install** to finish.

For the full walkthrough, see the [NCache Windows Installation Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/windows-installation.html).

### Option B — Docker

1. Confirm Docker is installed:

   ```bash
   docker version
   ```

2. Pull the [NCache image](https://hub.docker.com/r/alachisoft/ncache):

   ```bash
   docker pull alachisoft/ncache:latest
   ```

   > **Tip:** Pin a specific version tag (e.g. `alachisoft/ncache:x.x.x`) in production instead of `latest`, so upgrades happen intentionally rather than on your next container restart.

3. Create the container using host networking (recommended, to keep cluster communication and ports working correctly):

   ```bash
   docker create --name ncache --network host alachisoft/ncache:latest
   ```

4. Start it:

   ```bash
   docker start ncache
   ```

See the [Docker Installation Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/getting-started-guide-docker.html) for more detail.

## Step 2: Register NCache (Docker only)

Native Windows/Linux installs register automatically during setup. Docker containers need an explicit registration step, as either a **Cache Server** (default) or a **Dev/QA Server** (for evaluation).

Run this from the host terminal:

```bash
docker exec -it ncache /opt/ncache/bin/tools/register-ncacheevaluation \
  -key xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx \
  -firstname John -lastname Smith \
  -email john@yourdomain.com \
  -registeras CacheServer \
  -company your_company_name
```

Swap in your own key, name, email, and company. Details: [NCache Docker Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/getting-started-guide-docker.html).

## Step 3: Create a Cache

Cache definitions live in `config.ncconf`. Open the file at `%NCHOME%\config\config.ncconf` (any text editor works) and add a `<cache-config>` block under `<configuration>`. Each cache needs a unique name.

### Local Cache

A single, non-clustered cache on one node:

```xml
<cache-config cache-name="demoLocalCache" alias="" config-id="e097c2c0-88af-4aa2-8a8a-c6432eeaa3fe" config-version="0" store-type="distributed-cache">
  <cache-settings inproc="False" data-format="Serialized" serialization="Json">
    <logging enable-logs="True" trace-errors="True" trace-debug="False" log-path=""/>
    <performance-counters enable-counters="True" snmp-port="0"/>
    <cleanup interval="15sec"/>
    <storage type="heap" cache-size="1024mb"/>
    <eviction-policy enabled-eviction="True" default-priority="normal" policy="priority" eviction-ratio="5%"/>
    <cache-topology topology="local-cache"/>
    <client-death-detection enable="False" grace-interval="60sec"/>
  </cache-settings>
</cache-config>
```

### Replicated Cache

A Replicated cache clusters multiple nodes so they act as one logical cache. To add a node to an existing single-node cluster:

```xml
<cache-config cache-name="demoCache" alias="" config-id="44fb997a-f6a7-433b-9439-7ef9de9f47c7" config-version="0" store-type="distributed-cache">
  <cache-settings inproc="False" data-format="Serialized" serialization="Json">
    <logging enable-logs="True" trace-errors="True" trace-debug="False" log-path=""/>
    <performance-counters enable-counters="True" snmp-port="0"/>
    <cleanup interval="15sec"/>
    <storage type="heap" cache-size="1024mb"/>
    <eviction-policy enabled-eviction="True" default-priority="normal" policy="priority" eviction-ratio="5%"/>
    <cache-topology topology="replicated">
      <cluster-settings operation-timeout="60sec" stats-repl-interval="300sec">
        <cluster-connection-settings port-range="1" connection-retries="1" connection-retry-interval="2secs" cluster-port="7801"/>
      </cluster-settings>
    </cache-topology>
    <client-death-detection enable="False" grace-interval="60sec"/>
  </cache-settings>
  <cache-deployment deployment-version="0">
    <servers>
      <server-node ip="20.200.20.40"/>
      <server-node ip="20.200.20.39"/>
    </servers>
  </cache-deployment>
</cache-config>
```

Update the `<server-node>` IPs to match your own servers, then:

1. Save `config.ncconf` on node-1.
2. Copy the same `<cache-deployment>` block into `config.ncconf` on node-2.
3. Restart the NCache service on node-2:

   ```powershell
   Restart-Service -Name NCacheSvc
   ```

4. Stop and start the cache on both nodes.
5. Confirm the node joined successfully:

   ```powershell
   Get-Caches -Detail
   ```

**A few gotchas worth knowing:**
- You can run a "cluster" of one node for local testing.
- Every cluster needs a unique `cluster-port`; nodes in the *same* cluster must all share that port, or they won't be able to join each other.
- The same configuration block needs to exist on every server in the cluster.

See [Add Cache Server Node](https://www.alachisoft.com/resources/docs/ncache/admin-guide/add-server-node-in-cluster.html#add-a-node-to-a-2-node-cluster) and [Remove Cache Server Node](https://www.alachisoft.com/resources/docs/ncache/admin-guide/remove-server-node-from-cluster.html#remove-a-node-from-a-cache-cluster) for growing/shrinking clusters beyond this.

## Step 4: Configure a client

Any app connecting to the cache (locally or remotely) needs a matching entry in `client.ncconf`:

```xml
<cache id="demoCache" client-cache-id="" load-balance="false" enable-client-logs="False" log-level="error">
  <server name="20.200.20.40"/>
</cache>
```

- The `id` must be unique within `client.ncconf`.
- List every cache server as its own `<server>` entry, using each server's IP.
- Repeat this for each machine that needs to act as a cache client.

## Step 5: Restart 

Configuration changes only take effect after restarting both the cache and the NCache service. Run PowerShell as Administrator:

```powershell
Restart-Service -Name NCacheSvc
```

Then confirm:
- The `NCacheSvc` service is running.
- The cache instance status shows **Running**.

If a config file has a missing or malformed tag, the service will fail to start — double check `config.ncconf` first if this happens.

## Step 6: Verify and start the Cache

Check that the cache registered correctly:

```powershell
Get-Caches -Detail -Server 20.200.20.40
```

You should see your cache listed with status **Stopped**. If it's missing, recheck the config and make sure the service was restarted.

Start it on every server node:

```powershell
Start-Cache -Name demoCache -Server 20.200.20.40
```

Optionally, put load on the cluster to sanity-check it:

```powershell
Test-Stress -CacheName demoCache
```

## Step 7: Monitor Cluster Health

**Windows Performance Monitor:** open `perfmon`, add counters under the **NCache** category (scoped to `\\<server-ip>` for remote machines), and watch metrics like:

- `Additions/sec` — new items added per second
- `Count` — items currently in the cache
- `Expirations/sec` — items expiring per second
- `Fetches/sec` — reads from the cache
- `Requests/sec` — total commands received
- `Updates/sec` — items updated per second

**SNMP:** NCache ships an `alachisoft.mib` file with counters for cache, bridge, and client metrics, found at:
- Windows: `%NCHOME%\bin\resources`
- Linux: `/opt/ncache/ext/resources/snmp/`

Load it into an SNMP browser (e.g. MIB Browser) to inspect values.

## Step 8: Set Up a .NET Client Project

1. Create a new .NET console application.
2. In Visual Studio: **Tools → NuGet Package Manager → Package Manager Console**.
3. Install the SDK:

   ```powershell
   Install-Package Alachisoft.NCache.OpenSource.SDK
   ```

   This generates `client.ncconf` and `config.ncconf` in your project. If they ever go missing, regenerate them by running `init.ps1` from the Package Manager Console.

## Step 9: Try It Out

### Basic cache operations

```csharp
using Alachisoft.NCache.Client;

// Connect to cache
ICache cache = CacheManager.GetCache("demoCache");

// Create item
var product = new Product(1005, "Laptop", 1500, "Electronics");
var cacheItem = new CacheItem(product);

// Add to cache
cache.Add("1009", cacheItem);
Console.WriteLine("Item 1009 successfully added to demoCache.");

// Dispose cache
cache.Dispose();

[Serializable]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
    public string Category { get; set; }

    public Product(int id, string name, double price, string category)
    {
        Id = id;
        Name = name;
        Price = price;
        Category = category;
    }
}
```

More on this in the [Basic Cache Operations guide](https://www.alachisoft.com/resources/docs/ncache/prog-guide/basic-cache-operations.html).

### Item-level events

Subscribe to changes on a specific key:

```csharp
using Alachisoft.NCache.Runtime.Events;

// Connect to cache
using ICache cache = CacheManager.GetCache("demoCache");

// Add an item first, because the key must already exist
var product = new Product(1005, "Laptop", 1500, "Electronics");
string key = "Product:1005";
cache.Add(key, new CacheItem(product));

// Create callback
var dataNotificationCallback = new CacheDataNotificationCallback(OnCacheDataModification);

// Register item-level notifications for update operation
cache.MessagingService.RegisterCacheNotification(
    key, dataNotificationCallback, EventType.ItemUpdated, EventDataFilter.DataWithMetadata);

// Update item to trigger ItemUpdated event
product.Price = 1750;
cache.Insert(key, new CacheItem(product));

Console.WriteLine("Item-level event notifications registered successfully.");

// Callback method that is invoked when a registered cache item is modified
static void OnCacheDataModification(string key, CacheEventArg args)
{
    switch (args.EventType)
    {
        case EventType.ItemUpdated:
            Console.WriteLine($"Item with key '{key}' has been updated in cache '{args.CacheName}'.");

            if (args.Item != null)
            {
                Product updatedProduct = args.Item.GetValue<Product>();
                Console.WriteLine($"Updated product: {updatedProduct.Name}, Price: {updatedProduct.Price}");
            }
            break;
    }
}
```

More on this in the [Events overview](https://www.alachisoft.com/resources/docs/ncache/prog-guide/events-overview.html).

> **Logs:** After running your app, check `%NCHOME%\log-files` (or your custom install path) for NCache log output — useful for troubleshooting connection or config issues.

## Where to Go Next

- [NCache Installation Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/)
- [NCache Programmer's Guide](https://www.alachisoft.com/resources/docs/ncache/prog-guide/)
- [NCache Command Line / PowerShell Reference](https://www.alachisoft.com/resources/docs/ncache/powershell-ref/)
- [Open Source vs. other editions — feature comparison](https://www.alachisoft.com/ncache/edition-comparison.html)
