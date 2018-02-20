# PUBSUB Sample

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample program demonstrates how to use the NCache messaging API to publish or subscribe messages. It shows how to create, get or delete a message topic and publish or subscribe messages using it.

This sample has 2 projects:

1) Publisher: It will create a topic, publish some messages on it and then deletes it.
2) Subscriber: It will create or get a topic and then create subscription on it.

### Prerequisites

- Visual Studio 2017 or later.
- .NETCore 2.0 or later.

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- Change the cache name
- By default this sample uses 'myPartitionedCache', make sure that cache is running. 

### Build and Run the Sample
    
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