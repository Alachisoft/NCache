# EFCachingProvider

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

- This is a sample application based on NCache's EFCaching provider. NCache's EFCachingProvider implementation allows you to store your entity data to NCache’s Distributed/OutProc cache. This sample cache entity objects in cache. 
	
- It is a console based application. The sample demonstrates how NCache works as a caching provider for entity framework.
	- 'View customers list' command lists all the customers from Northwind database.
	- 'View customer details' gets all the detail of a specific customer. 
	- 'View customer orders' gets the order information of a specific customer. 
	- 'Delete Customer' removes a specific customer from the Northwind database. 
	- 'Add customer' lets you add in a customer in Northwind database.
	- 'Exit' lets you exit the application. 

- EFCaching uses configuration from [InstallDir]\config\efcaching.ncconf. A more detailed information can be found in NCache Documentation.

### Prerequisites

Following are the prerequisites for this sample.

- Northwind is configured in SqlServer. 
- .Net Framework 4.5 is installed
- Entity Framework 6.0

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- Change the cache name 
	- Change connection string for the SQL database in app.config.
- Make sure SQL service is running and server host Northwind sample database.
- Verify that app-id is same as specified in efcaching.config in the configuration folder **[InstallDir]\config**.
- By default this sample uses 'mycache', make sure that cache is running. 

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

[C] Copyright 2017 Alachisoft 