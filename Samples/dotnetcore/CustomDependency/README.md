# CUSTOM DEPENDENCY

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample implements the Custom dependency feature of the NCache. In custom dependency, you can implement your 
custom logic which defines when a certain data becomes invalid for cached items. This sample implements Custom 
Dependency and adds items to cache with custom dependency and then updates item in database. Custom depedency 
invalidate data from database and when the dependency expires, the items are removed  from the cache.

### Prerequisites

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- Change the cache name
	- connectionString:	the connection string used to connect to the database. By default it connects to the localhost.
- To deploy Custom Dependency, Build 'CustomDependencyImpl' project then Deploy Providers CustomDependencyImpl.dll on cache.
- To deploy Provider for cache.
	- Make sure Cache is stopped before making any configuration changes. Stop the cache using Stop-Cache powershell cmdlet.
	- Make 'deploy' folder in NCache directory if not already present. 
	- Make directory with cache name in deploy folder in install directory of NCache.
	- Copy and paste all the dlls to that folder
	- Once changes are made on all server nodes, start Cache using Start-Cache powershell cmdlet.
- By default this sample uses 'myPartitionedCache', make sure that cache is running. 

### Build and Run the Sample

- Build 'CustomDependencyUsage' project.
- Run the sample application.

### Additional Resources

##### Documentation
The complete online documentation for NCache is available at:
http://www.alachisoft.com/resources/docs/#ncache

##### Programmers' Guide
The complete programmers guide of NCache is available at:
http://www.alachisoft.com/resources/docs/ncache/ncache-programmers-guide.pdf

### Technical Support

Alachisoft [C] provides various sources of technical support. 

- Please refer to http://www.alachisoft.com/support.html to select a support resource you find suitable for your issue.
- To request additional features in the future, or if you notice any discrepancy regarding this document, please drop an email to [support@alachisoft.com](mailto:support@alachisoft.com).

### Copyrights

[C] Copyright 2018 Alachisoft 