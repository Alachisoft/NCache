NHIBERNATE SAMPLE

========================================================================
			Ncache Features Used
========================================================================

This sample deals with the following features of NCache.
	i)	NHibernate Integration
	ii)	Database Dependency

========================================================================
				INTRODUCTION
========================================================================

This sample application explains integrating NHibernate (OR mapping 
tool) with NCache to cache the data and enabling database 
dependency in NHibernate applications.

========================================================================
				Description
========================================================================

There are two modules in this sample.

------------------------------------------------------------------------
NHibernator
------------------------------------------------------------------------

Simple Class library that maps the database table with .net objects 
using NHibernate assembly.

------------------------------------------------------------------------
NHibernateTest
------------------------------------------------------------------------

Console Application that creates a Factory object presented in the 
above module to get the data. The objects that are used in these 
operations are cached using NCache. You can see the count of the 
cache items in the stats window of 'myCache' (default cache for this 
sample).

========================================================================
				Requirements
========================================================================

Requirments may differ dependeing upon the database you are using. This
sample is configured to work with Sql Server 2000. To use with other 
databases, you may have to make changes to the App.config file of the 
NHibernateTest module.

------------------------------------------------------------------------
Enabling Notification
------------------------------------------------------------------------

(a) Create a table 'ncache_db_sync' in the database. 
    You can use the following script to create this table. This 
    script is also available in the file 'ncache_db_sync.sql' in 
    the scripts directory.

	CREATE TABLE ncache_db_sync(
	cache_key VARCHAR(256),
	cache_id VARCHAR(256),
	changecount INT DEFAULT(0),
	PRIMARY KEY(cache_key, cache_id) );

(b) Create UPDATE and DELETE triggers, for every table on which 
    notification is required. A sample trigger is given below to 
    use with this sample. This script is also available in the 
    file 'myTrigger.sql' in the scripts directory.

	CREATE TRIGGER myTrigger
	ON dbo.Products
	FOR DELETE, UPDATE
	AS
	UPDATE ncache_db_sync
	SET changecount = changecount + 1
	WHERE cache_key IN 
	(Cast((Select old.ProductID from DELETED old) as VarChar)
	 +':dbo.Products');

(c) Change the connection string in the 'App.config' file for 
    NHibernateTest.

(d) Set dbSync type = "SqlDependency" in the ncache section of the 
    App.config file.


========================================================================
			How To Run The Sample
========================================================================

To run the sample  
	1. 	Make sure the cache (mycache) is registered on your 
		machine and is running. This cache gets registered 
		when you install the NCache.


========================================================================
			settings for NHibernateTest Module
========================================================================

hibernate.connection.connection_string:	The connection string used to 
                                        connect to the database. By 
					default it connects to the 
					Northwind on localhost.

hibernate.cache.provider_class:		the integration provider 
					provided by NCache for 
					NHibernate

These settings for NHibernateTest program can be changed from the 
'NCacheNHibernate.xml' file located at:

<NCache Root>\Samples\clr20\NHibernate\NHibernateTest
			OR
<NCache Root>\Samples\clr11\NHibernate\NHibernateTest

=======================================================================
			NCache Settings
=======================================================================

By default this sample uses local cache named 'myCache'. This cache 
is automatiaclly installed with the NCache. You can change the 
configuration of the sample to use any other cache. Start the 
cache if its not running. For further settings refer to NCache Help.


------------------------------------------------------------------------
The NCache(TM) is a product of Alachisoft(TM), Inc.
Copyright© 2014 Alachisoft, Inc.
All rights reserved.


