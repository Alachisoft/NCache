# GETTING STARTED GUIDE

Here are some basic steps you should follow to ensure a smooth installation, configuration, and use of NCache. We'll discuss following important steps to quickly get started with NCache.

### Table of contents

* [Introduction](#introduction)
* [Launch PowerShell Core](#launch-powershell-core)
* [Configure Firewall](#configure-firewall)
* [Create Cache](#create-cache)
* [Add as a Remote Client](#add-as-a-remote-client)
* [Simulate Cache Usage](#simulate-cache-usage)
* [Monitor Caches](#monitor-caches)
* [Documentation](#documentation)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

NCache provides Remote-Client installation for Linux based on .NET Core. This installs the remote client libraries along with client cache. Install this on all your web/app servers from where you want to remotely access the cache servers. Do not install it on cache servers.  

After the installation is complete, **NCache Install Directory** by default is **/opt/ncache**, unless specified differently during installation.


### Launch PowerShell Core

**To manage caches, PowerShell Core 6.0 needs to be installed on your Linux machine**

PowerShell Core 6.0 can be installed from:  https://github.com/PowerShell/PowerShell/blob/master/docs/installation/linux.md

	1. Open Terminal on your Linux machine.
	2. Type command `pwsh` and press Enter.
	3. Now import NCache PowerShell module in this instance by typing the command (replace `$NCHOME` with NCache install directory path):

```batchfile
Import-Module $NCHOME/bin/ncacheps
```

### Configure Firewall

NCache consists of distributed components including cache client, cache server and tools. These components communicate with each other over TCP sockets. In secure environment where Firewall is configured, you need to allow communication on following set of TCP ports in Firewall settings. 

	-	Client/Server Port : 9800 (Default TCP port for cache client and server communication)
	-	Management Port: 8250 (Default TCP port for PowerShell cmdlets and NCache service)
	-	Cluster ports : Cluster-port that you define in cache cluster configuration. It is recommended to use a consecutive range of ports, for e.g. 7800 onward, and configure Firewall to allow communication on this port range.


### Create Cache
NCache Open Source and Community editions allow you to configure the following caches:

	•	Local Cache (meaning a standalone cache)
	•	Partitioned Cache
	•	Replicated Cache

For Partitioned and Replicated Caches you need NCache server installation which is available for Windows only.

To create a clustered cache on Windows installation, please follow NCache Admin Guide's configure caches http://www.alachisoft.com/resources/docs/ncache-com/admin-guide/configure-caches.html


### Add as a Remote Client

	To configure this machine as a cache client:
	1.	Open *$NCHOME/config/client.ncconf* file.
	2.	Copy/paste the entire `<cache>` section from the example below to the `<configuration>` section.

```xml
		<cache id="demoCache" load-balance="True" enable-client-logs="False" log-level="error">
			<server name="20.200.20.220"/>
		</cache>
```
	3.	Make sure your cache id is unique within *client.ncconf* which contains multiple cache connections.
	4.	Replace the `cache-id` specified in the `<cache>` tag to the cache with which you want to register as a client node.
	5.	Replace the IP address specified in the name property of `<server>` tag to your cache server's IP address. Mention all the cache server IP addresses here as separate `<server>` tags.

### Simulate Cache Usage
	You can quickly run a Stress Test Tool that comes with NCache installation to verify that cache clients can make calls to cache servers. To start this test, please type the following command in PowerShell Core (replace 'demoCache' with the cache you created):

```batchfile
		Test-Stress –CacheName demoCache
```
	This command starts making cache calls to the cache servers.

### Monitor Caches

	To monitor caches, NCache provides monitoring through PerfMon on Windows. Please refer to the following admin guide to see how to monitor NCache:
	http://www.alachisoft.com/resources/docs/ncache-com/admin-guide/monitor-ncache.html


### Documentation
The complete online documentation for NCache is available at:
http://www.alachisoft.com/resources/docs/ncache-com/admin-guide/
http://www.alachisoft.com/resources/docs/ncache-com/getting-started-guide/
http://www.alachisoft.com/resources/docs/ncache-com/install-guide/


### Technical Support

Alachisoft © provides various sources of technical support.

- Please refer to http://www.alachisoft.com/support.html to select a support resource you find suitable for your issue.
- To request additional features in the future, or if you notice any discrepancy regarding this document, please drop an email to [support@alachisoft.com](mailto:support@alachisoft.com).

### Copyrights

 © Copyright 2005-2018 Alachisoft
