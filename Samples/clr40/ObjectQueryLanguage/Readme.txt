NCACHE QUERY SAMPLE
-------------------
NCACHE FEATURES USED
--------------------
This sample deals with the following feature of NCache.
	i)	Query Indexes

INTRODUCTION
------------
This sample explains how you can use Object Queries with NCache. This sample provides you with 3 examples of Object Queries.

DESCRIPTION
-----------
There are two modules in this sample.
	i)	NCacheQuery.Business (Class library)
		this module connects with the database (northwind.mdb) and loads some attributes of Products table. The Product class and its
		attributes are also implemented in this module.
	ii)	NCacheQuery (Console application)
		Simple console application that presents you a menu to select the query you want to execute against the cache. This 
		application uses 'myCache' by default. 
		Following Queries are implemented in this sample:
			a)	Select NCacheQuerySample.Business.Product where this.ProductID > 10
			b)	Select NCacheQuerySample.Business.Product where this.ProductID = 71 and this.Category = 4
			c)	Select NCacheQuerySample.Business.Product where this.ProductID < 10 and this.Supplier = 1

HOW TO RUN THE SAMPLE
---------------------
Before starting the sample follow these steps:
	1.	Build the sample and Copy 'NCacheQuerySample.Business.dll' file to "NCache ROOT>\bin\service\" folder of NCache.
	2. 	To use this sample, you must first specify the indexes of the objects you want to query in the cache. By default this sample uses 
		'myCache'. 
	3.	Start NCache Manager and open the 'localcaches' project file from the Recent Projects menu under File menu. 
	4.	Select the 'myCache' icon in the explorer bar from the left pane. Now from the main menu, select the 'Query Indexes' tab.
	5.	Click Add from the Query Indexes tab, you'll see 'Select Indexes' form. Click 'Add...' button to add some classes. 
	6.	Now from the 'Class Browser', browse for the file 'NCacheQuerySample.Business.dll'.This is the file that you copied to "NCache ROOT>\bin\service\" folder of NCache in step 1.
	7.	Select the 'Products' class and click 'Add to list' and click Ok.
	8.	Check the class and all of its attributes and click Ok.
	9.	Apply configuration changes and start the cache.
	
You are ready to start the sample application.

NCacheQuery.Business settings:
------------------------------

	no settings are required for this module.

NCache Settings:
---------------

By default this sample uses localcache named 'myCache'. This cache is automatiaclly installed with the NCache.
You can change the configuration of the sample to use any other cache. Before using any other cache follow the 'How to Run Sample' steps.


REQUIRMENTS
-----------
NCache 3.0 or greater.

