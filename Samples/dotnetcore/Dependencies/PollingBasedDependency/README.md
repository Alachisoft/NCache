# POLLING DEPENDENCY

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

- This sample explains how NCache supports dependent items to be added, and what is the behaviour if dependency has a change.
- Sample adds one item dependent upon a record in the database using Polling Based Dependency.
- Any modification in the database record will result in the invalidation of the cache item and thus the item will be removed from the cache.
- This sample deals with the Polling Based Database dependency feature of NCache.
- This sample uses northwind database. 
	
### Prerequisites

- Visual Studio 2017 or later.
- .NETCore 2.0 or later.

Before the sample application is executed make sure that:

- ncache_db_sync table is created in the database
```
CREATE TABLE ncache_db_sync(
cache_key VARCHAR(256),
cache_id VARCHAR(256),
modified bit DEFAULT(0),
work_in_progress bit Default(0),
PRIMARY KEY(cache_key, cache_id) );
```
- Triggers are created on the dependee tables
```
CREATE TRIGGER myTrigger
ON dbo.Products
FOR DELETE, UPDATE
AS
UPDATE ncache_db_sync
SET modified = 1
FROM ncache_db_sync
INNER JOIN Deleted old ON cache_key = (Cast((old.ProductID) AS VarChar)+ ':dbo.Products' );
```
- app.config have been changed according to the configurations. 
	- Change the cache name 
	- conn-string to connect with database.
	
- By default this sample uses 'myPartitionedCache', make sure that cache is running. 
- To verify that the dependency is working uncomment the required code from AddDependency() method in PollingDependency.cs

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