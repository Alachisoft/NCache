# EVENTS

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample program demonstrates how to use the NCache Events and Event Notifications. 
It shows how to initialize the cache instance and Add, Get, Update and Delete object(s) from NCache and receive events.
These events cover Selective events and General events.

This sample uses SampleData project as a reference for model class "Product".

### Prerequisites

- Visual Studio 2017 or later.
- .NETCore 2.0 or later.

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- change the cache name
- Event notifications must be enabled for Cache. Follow these steps to enable notifications
	- Make sure Cache is stopped before making any configuration changes. Stop the cache using Stop-Cache powershell cmdlet.
	- Specify cache-level notifications through 'config.ncconf' by specifying the '<cache-notifications>' tag under the '<cache-settings>' tag:
	- <cache-notifications item-remove="True" item-add="True" item-update="True"/>	
	- Once changes are made on all server nodes, restart the NCache service and start Cache using Start-Cache powershell cmdlet.

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