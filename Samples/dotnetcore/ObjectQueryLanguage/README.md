# OBJECT QUERY LANGUAGE

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample program demonstrates how to use the Object Query Language in NCache. 
This sample provides you with example of OQL. Following Query is implemented in this sample:
1> 'SELECT Alachisoft.NCache.Sample.Data.Product Where this.Id = ?'

This sample uses SampleData project as a reference for model class "Product".

### Prerequisites

- Visual Studio 2017 or later.
- .NETCore 2.0 or later.

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- change the cache name
- To use this sample, you must first specify the indexes of the objects you want to query in the cache.
	- Make sure Cache is stopped before making any configuration changes. Stop the cache using Stop-Cache powershell cmdlet.
	- Specify the query indexes through 'config.ncconf' by adding the '<query-indexes>' and  '<query-class>' tag under the '<cache-settings>' tag. 
	
	The following example adds attributes of class, *Product*, as index: 
      <query-indexes>
        <query-class id="Alachisoft.NCache.Sample.Data.Product" name="Alachisoft.NCache.Sample.Data.Product">
          <query-attributes id="Id" name="Id" data-type="System.Int32"/>
          <query-attributes id="UnitPrice" name="UnitPrice" data-type="System.Decimal"/>
          <query-attributes id="Category" name="Category" data-type="System.String"/>
          <query-attributes id="QuantityPerUnit" name="QuantityPerUnit" data-type="System.String"/>
          <query-attributes id="UnitsAvailable" name="UnitsAvailable" data-type="System.Int32"/>
          <query-attributes id="Name" name="Name" data-type="System.String"/>
        </query-class>
      </query-indexes>

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