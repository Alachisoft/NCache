# CONTINUOUS QUERY

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample explains how you can use the NCache Continuous Query. It uses customer instance and stores it to the cache, then queries the cache for the item against the criteria.

This sample uses SampleData project as a reference for model class "Customer".

### Prerequisites

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- Change the cache name

- NCache's Continuous Query uses Object Query Language which requires indexes definition for customer class.
 1) Make sure Cache is stopped before making any configuration changes. Stop the Cache using Stop-Cache powershell cmdlet.
 2) Specify the query indexes through 'config.ncconf' by adding the '<query-indexes>' and  '<query-class>' tag under the '<cache-settings>' tag.
 The following example adds the query indexes for class Customer used in the sample:
    <query-indexes>
        <query-class id="Alachisoft.NCache.Sample.Data.Customer" name="Alachisoft.NCache.Sample.Data.Customer">
          <query-attributes id="ContactName" name="ContactName" data-type="System.String"/>
          <query-attributes id="Country" name="Country" data-type="System.String"/>
          <query-attributes id="ContactNo" name="ContactNo" data-type="System.String"/>
          <query-attributes id="CustomerID" name="CustomerID" data-type="System.String"/>
          <query-attributes id="CompanyName" name="CompanyName" data-type="System.String"/>
          <query-attributes id="Address" name="Address" data-type="System.String"/>
        </query-class>
    </query-indexes>

 3) Once changes are made on all server nodes, restart the NCache service and start Cache using Start-Cache powershell cmdlet.

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