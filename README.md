# NCache: Highly Scalable Distributed Cache for .NET

NCache is an extremely fast and scalable Open Source distributed cache for .NET applications. Use NCache for database caching, ASP.NET Session State storage, ASP.NET View State Caching, and much more.

NCache is used by hundreds of companies all over the world in mission critical applications. See more details about [NCache](http://www.alachisoft.com/) at Alachisoft.

## Features

- Publish/Subscribe (Pub/Sub) with Topic
- Cache CRUD operations
- Bulk CRUD operations
- Lock/Unlock cached items
- Item level event notifications
- Eviction
- Absolute and sliding expirations
- ASP.NET Session State Provider
- ASP.NET View State Cache
- NHibernate Second Level Cache provider
- NuGet Packages
- Runs in Microsoft Azure, Amazon, and any other Cloud platform
- SQL Searching
- Distributed Data Structures
- Live Cluster Configuration
- WAN Replication

## Getting Started
You can set up NCache either by installing it directly as a cache server (default), remote client, Dev/QA or by running it inside a Docker container. 

### Option 1: Windows Server Installation
1.	**Download and run the MSI Installer**: Download the installer from [Alachisoft](https://www.alachisoft.com/download-ncache.html) and execute the following in command prompt.

```bash
msiexec.exe /I "C:\NCacheSetupPath\ncache.oss.x64.msi"
```

2.	**Run the Setup Wizard**: Launch the installer and follow the on-screen instructions. Select [Cache Server](https://www.alachisoft.com/resources/docs/ncache/install-guide/windows-installation.html#cache-server) when prompted for the installation type. Please see [Windows Server Installation docs](https://www.alachisoft.com/resources/docs/ncache/install-guide/windows-installation.html#install-ncache) for step-by-step information on these steps. 

3. **Start the NCache Service**: Ensure the NCache service is running. 

For more details on Getting started with NCache Open Source, please see [Getting Started](https://www.alachisoft.com/resources/docs/ncache/getting-started/ncache-oss.html) docs. 

### Option 2: Docker Installation 

NCache provides official images on [Docker Hub](https://hub.docker.com/r/alachisoft/ncache/tags).

1.	**Pull the NCache Docker Image**

```bash
docker pull alachisoft/ncache:latest-oss
```

2.	**Create  NCache Container (using host network mode)**

```bash
docker create --name ncache --network host alachisoft/ncache:latest-oss
```

3.	**Register Cache Server (Register from the host terminal)**

```bash
docker exec -it ncache /opt/ncache/bin/tools/register-ncacheevaluation -key xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx -registeras CacheServer 
```

For more details on NCache Docker, please see [Docker docs](https://www.alachisoft.com/resources/docs/ncache/install-guide/getting-started-guide-docker.html).


### [Installation Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/)
Step-by-step guide about how to install NCache for Open Source edition, and to configure your environment accordingly.

### [Admin Guide](https://www.alachisoft.com/resources/docs/ncache/admin-guide/)
Perform administrative tasks on caches using configuration files for NCache Open Source edition.

### [Programmers Guide](https://www.alachisoft.com/resources/docs/ncache/prog-guide/)
Use various features of NCache in .NET to develop high performance and scalable applications.

### [Dockers Guide](https://www.alachisoft.com/resources/docs/ncache/install-guide/docker-overview.html)
Docker image and Dockerfile with NCache environment to allow seamless building of NCache applications and managing cache clusters.

### [Edition Comparison](https://www.alachisoft.com/ncache/edition-comparison.html)
Compare various editions available. Enterprise Edition vs Open Source Edition.

### [NCache samples](https://github.com/Alachisoft/NCache-Samples)
Demonstration of NCache features using sample applications.

### [NCache Integrations](https://github.com/Alachisoft/NCache-Integrations)
A collection of official integrations that enable the use of NCache with popular libraries and frameworks. 

## License
NCache Open Source is released under the Apache License, Version 2.0
See more details at https://www.alachisoft.com/download-ncache.html
