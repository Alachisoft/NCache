# Response Caching

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample illustrates the use of a Response Caching using NCache. It uses a partitioned cache named, 'democache',

which is registered on your machine upon the installation of NCache. 

To start or re-register this cache later, you can use NCache Web Manager or NCache command line tools.


### Prerequisites

Requirements:

- Visual Studio 2015 or later.
- .net core  2.2 or later.

Before the sample application is executed make sure that:
- appsettings.json have been changed according to the configurations. 
	- Change the cache name
- By default this sample uses 'democache', make sure that cache is running. 

Or use Option 2:
- Un-comment code found in ConfigureServices method in Startup.cs in Option2 region.
	- Change the cache name
- Comment the code in Option1 region.
- By default 'democache' is used, make sure that cache is running. 

### Build and Run the Sample
    
- Run the sample application.

### Additional Resources

##### Documentation
The complete online documentation for NCache is available at:
http://www.alachisoft.com/resources/docs/#ncache

##### Programmers' Guide
The complete programmers guide of NCache is available at:
http://www.alachisoft.com/resources/docs/ncache/prog-guide/

### Technical Support

Alachisoft [C] provides various sources of technical support. 

- Please refer to http://www.alachisoft.com/support.html to select a support resource you find suitable for your issue.
- To request additional features in the future, or if you notice any discrepancy regarding this document, please drop an email to [support@alachisoft.com](mailto:support@alachisoft.com).

### Copyrights

[C] Copyright 2021 Alachisoft 