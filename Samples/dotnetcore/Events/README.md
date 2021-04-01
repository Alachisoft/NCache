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
These events cover Selective events, General events and Custom events.

This sample uses SampleData project as a reference for model class "Product".

### Prerequisites

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- change the cache name
- Event notifications must be enabled in NCache Web Manager 
	- Start NCache Web Manager and create a clustered cache with the name specified in app.config. 
	- Now select the Options' tab in the "Advanced Settings" of cache's details page. 
	- Enable event notifications (item-add, item-remove, item-update)
	- Save changes.
- By default this sample uses 'democache', make sure that cache is running. 

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