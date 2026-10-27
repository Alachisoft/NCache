# NCache: Distributed Cache for .NET

**100% Native .NET and Extremely Fast**

**No limits on Cluster Size or Cache Size | Redis RESP Protocol Server for Windows**

NCache is an open source distributed cache for .NET applications that is high-performance. NCache runs natively on Windows and can also be deployed on Linux using containers.

NCache Open Source edition is free to use. Its source code is available on GitHub, and optional Professional Support is available for organizations that want commercial assistance.

**Quick links:** [Getting Started](#getting-started) · [Redis RESP Server](#redis-resp-protocol-server-for-windows) · [Documentation](#documentation-and-examples) · [Docker Hub](https://hub.docker.com/r/alachisoft/ncache/tags) · [Professional Support](#professional-support-optional)


## Table of Contents

- [Key Features](#key-features)
  - [Architecture](#architecture)
  - [Caching](#caching)
  - [Pub/Sub Messaging](#pubsub-messaging)
  - [.NET Integrations](#net-integrations)
- [Redis RESP Protocol Server for Windows](#redis-resp-protocol-server-for-windows)
- [Getting Started](#getting-started)
  - [Option 1: Install on Windows](#option-1-install-on-windows)
  - [Option 2: Run as Docker Container](#option-2-run-as-docker-container)
  - [Option 3: Run with Kubernetes](#option-3-run-with-kubernetes)
  - [Create a Cache and Connect](#create-a-cache-and-connect)
- [Use NCache in Your .NET Application](#use-ncache-in-your-net-application)
- [Deployment Options](#deployment-options)
- [Documentation and Examples](#documentation-and-examples)
- [NCache Open Source vs. Enterprise](#ncache-open-source-vs-ncache-enterprise)
- [Professional Support (Optional)](#professional-support-optional)
- [Community and Contributions](#community-and-contributions)
- [License](#license)

## Key Features

### Architecture

- In-memory distributed caching for .NET. Java, Python, and Node.js coming soon.
- Replicated Cache topology for high availability
- Dynamic cluster configuration
- **No limits on cluster size and cache size**

### Caching

- Add, retrieve, update, and remove cached items
- Bulk cache operations
- Absolute and sliding expiration
- Eviction policies
- Item-level event notifications
- Locking and unlocking of cached items

### Pub/Sub Messaging

- Publish and subscribe to messages through NCache
- Enable message-based communication between applications

### .NET Integrations

- .NET client SDK NuGet
- ASP.NET Core caching, sessions, SignalR, Response Cache, and Output Cache
- Legacy ASP.NET caching, session state, view state, SignalR, and output cache
- EF Core Cache with Extension Methods
- NHibernate second-level caching integration
- Redis RESP protocol compatible server on Windows (see below)

> [Edition Comparison](https://www.alachisoft.com/ncache/edition-comparison.html)

## Redis RESP Protocol Server for Windows

**Run Redis clients against NCache RESP server natively on Windows.**

- **RESP compatibility:** Implements Redis RESP protocol String, Pub/Sub, Connection, and Server commands. NCache Enterprise also supports Key, Hash, List, Set, Stream commands.
- **Use with existing Redis clients:** Connect existing Redis clients such as StackExchange.Redis (.NET), Jedis (Java), redis-py (Python), and Node.js Redis.
- **Windows native administration and monitoring:** Use Windows and NCache tools for administration and monitoring.

[NCache RESP Architecture](https://www.alachisoft.com/ncache-resp/ncache-resp-server-architecture.html), [RESP Compatibility Matrix](https://www.alachisoft.com/ncache-resp/ncache-redis-resp-matrix.html)

## Getting Started

[Get Key (free) for NCache Open Source.](https://www.alachisoft.com/download-ncache.html#ncache)

**Activate with Free Key:** Use this free key `XXXXXX` with any of the deployment options below to active NCache open source edition for a free perpetual use.

### Option 1: Install on Windows

**Install directly from a URL without any downloads.**

Deploy NCache Server from an elevated PowerShell terminal:

```powershell
msiexec.exe /i `
  https://download.alachisoft.com/ncache/ncache-oss-5.3.603.msi `
  KEY="XXXXXX" /qn
```

The `/qn` flag runs the Windows installer silently. Once installation completes, configure and start your cache using the supported administration tools.

For other installation choices, see the [Windows Installation Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/command-line-installation.html).

### Option 2: Run as Docker Container

Deploy a running NCache Server as Docker container directly from Docker Hub:

```bash
docker run -d --name ncache --network host -e KEY="XXXXXX" \
  alachisoft/ncache:5.3.603-oss
```

Docker automatically pulls the image if it is not already available locally. The command starts a container named `ncache` in detached mode and uses the host network. **Host networking is required for NCache server deployment**.

See [Docker Hub Tags](https://hub.docker.com/r/alachisoft/ncache/tags), [Docker Installation Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/getting-started-guide-docker.html)

### Option 3: Run with Kubernetes

Use the [Kubernetes YAML files](https://github.com/Alachisoft/NCache-Kubernetes/tree/main/ncache-oss) (`ncache.yaml`, `discoveryservice.yaml`, and `client-gatewayservice.yaml`). Configure your free `KEY` in `ncache.yaml` and use the published Docker image `alachisoft/ncache:5.3.603-oss`.

From the directory containing the YAML files, run:

```bash
kubectl apply -f ncache.yaml
kubectl apply -f discoveryservice.yaml
kubectl apply -f client-gatewayservice.yaml
```

Verify the deployment with `kubectl get pods` and `kubectl get services`.

### Create a Cache and Connect

Once installed:

1. Create a supported cache topology, such as a replicated cache.
2. Start the cache and confirm its status.
3. Configure your application with the cache name and server endpoints.
4. Connect using the native NCache SDK, or use a supported Redis client with the Windows RESP Server.

Follow the [Getting Started Guide](https://www.alachisoft.com/resources/docs/ncache/getting-started/ncache-oss.html) for version-correct commands and configuration examples.

## Use NCache in Your .NET Application

NCache provides a native .NET client SDK for integrating cache operations into your application.

1. Add the NCache client package compatible with your application and server version.
2. Configure the cache connection.
3. Connect and perform basic operations such as adding, retrieving, and removing items.

See the [Programmer's Guide](https://www.alachisoft.com/resources/docs/ncache/prog-guide/) and [NCache Samples](https://github.com/Alachisoft/NCache-Samples) for working code and instructions for supported .NET versions.

For applications already using Redis client libraries on Windows, see the [Redis RESP Protocol Server for Windows](#redis-resp-protocol-server-for-windows) section above.

## Deployment Options

- **Windows:** Install NCache directly on supported Windows hosts.
- **Windows Container Nano Server:** Deploy using the published NCache Nano Server images (coming soon).
- **Linux Docker containers:** Deploy using the published NCache container images.
- **Docker Compose:** Create your own Docker Compose project to deploy multi-node cache cluster on a single VM, mainly for development and testing (not for production).
- **Kubernetes:** Use supported container deployment configurations to run NCache in a Kubernetes environment.
- **Cloud infrastructure:** Deploy on Microsoft Azure, Amazon Web Services (AWS), or Google Cloud, including Kubernetes deployments on Azure Kubernetes Service (AKS), Amazon Elastic Kubernetes Service (EKS), and Google Kubernetes Engine (GKE).

## Documentation and Examples

| Resource | Description |
| --- | --- |
| [Getting Started Guide](https://www.alachisoft.com/resources/docs/ncache/getting-started/ncache-oss.html) | Set up and begin using NCache. |
| [Installation Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/) | Install and deploy NCache. |
| [Admin Guide](https://www.alachisoft.com/resources/docs/ncache/admin-guide/) | Create, configure, and operate caches. |
| [Developer's Guide](https://www.alachisoft.com/resources/docs/ncache/prog-guide/) | Integrate NCache with your application. |
| [NCache Samples](https://github.com/Alachisoft/NCache-Samples) | Explore sample applications and APIs. |
| [NCache Integrations](https://github.com/Alachisoft/NCache-Integrations) | Find integration examples. |
| [Docker Hub](https://hub.docker.com/r/alachisoft/ncache/tags) | Explore official container images and tags. |
| [Release Notes](https://www.alachisoft.com/resources/release-notes/) | Review version-specific improvements and fixes. |

## NCache Open Source vs. Enterprise

**Open Source** provides distributed in-memory caching with no software license fees, including unlimited cluster size and unlimited replicated cache size starting in version 5.3.603.

**Enterprise** is a commercial edition with additional enterprise capabilities and deployment options. Compare their supported caching topologies, management, security, performance, and other features before choosing an edition.

[Edition Comparison](https://www.alachisoft.com/ncache/edition-comparison.html)

## Professional Support (optional)

NCache Open Source edition is free to use without support. If you prefer full paid support, Alachisoft offers an optional Professional Support plan.

| Professional Support | Details |
| --- | --- |
| Phone and email support | During office hours. 2 days response time 
| Monthly price | **$1000/month** |
| Annual price | **$10,000/year** |


[Purchasing terms and coverage](https://www.alachisoft.com/pricing/).

## Community and Contributions

We welcome developer feedback and contributions.

- [Report a bug or request a feature](https://github.com/Alachisoft/NCache/issues)
- [View or submit pull requests](https://github.com/Alachisoft/NCache/pulls)
- [Join GitHub Discussions](https://github.com/Alachisoft/NCache/discussions)
- Explore the source code in this repository.

Before contributing, review any contribution guidelines provided in the repository.

## License

**Source code:** The NCache source code in this repository is made available under the [Apache License 2.0](LICENSE).

**Official distributions:** NCache Open Source edition `.msi` installers, `.tar.gz` packages, and Docker and Windows Container images are free to use under a separate *NCache Open Source software distribution license*, rather than the Apache 2.0 license that applies only to the source code. Official distributions remain subject to their applicable branding and redistribution terms.

Review the applicable license terms accompanying the distribution before installation or redistribution.
