# NHIBERNATE

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample program demonstrates how to use the NHibernate integration for NCache and Database Dependency. 
This sample application explains integrating NHibernate (OR mapping tool) with NCache to cache the data and enabling database 
dependency in NHibernate applications.
There are two modules in this sample.
- NHibernator
	- Simple Class library that maps the database table with .net objects using NHibernate assembly.
- NHibernateTest
	- Console Application that creates a Factory object presented in the above module to get the data. The objects that are used in these 
operations are cached using NCache. You can see the count of the cache items in the stats window of 'myPartitionedCache' (default cache for this sample).

This sample uses SampleData project as a reference for model class "Customer".

### Prerequisites

Before the sample application is executed make sure that:

- Requirements may differ depending upon the database you are using. This sample is configured to work with Sql Server 2000. To use with other 
databases, you may have to make changes to the App.config file of the NHibernateTest module.
	
	(Steps 1 and 2 are only required in case of OLEDB dependency)
	1) Create a table 'ncache_db_sync' in the database. 
    You can use the following script to create this table. This 
    script is also available in the file 'ncache_db_sync.sql' in 
    the scripts directory.
	``` 
    CREATE TABLE ncache_db_sync(
	cache_key VARCHAR(256),
	cache_id VARCHAR(256),
	changecount INT DEFAULT(0),
	PRIMARY KEY(cache_key, cache_id) );
    ```
	2) Create UPDATE and DELETE triggers, for every table on which 
    notification is required. A sample trigger is given below to 
    use with this sample. This script is also available in the 
    file 'myTrigger.sql' in the scripts directory.
	```
    CREATE TRIGGER myTrigger
	ON dbo.Products
	FOR DELETE, UPDATE
	AS
	UPDATE ncache_db_sync
	SET changecount = changecount + 1
	WHERE cache_key IN 
	(Cast((Select old.ProductID from DELETED old) as VarChar)
	 +':dbo.Products');
     ```
	3) Change the connection string in the 'App.config' file for 
    NHibernateTest.
	    
    4) Settings for NHibernateTest Module
    	- hibernate.connection.connection_string:	The connection string used to connect to the database. By default it connects to the Northwind on localhost.
		- hibernate.cache.provider_class: the integration provider provided by NCache for NHibernate
		- These settings for NHibernateTest program can be changed from the  'NCacheNHibernate.xml' file located at:
```
<NCache Root>\Samples\dotnet\NHibernate\NHibernateTest
```
- NCacheNHibernate.xml have been changed according to the configurations. 
	- change the cache name
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