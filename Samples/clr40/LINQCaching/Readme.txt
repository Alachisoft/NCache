NCACHE LINQ SAMPLE
-------------------
NCACHE FEATURES USED
--------------------
This sample deals with the following feature of NCache.
	i)	LINQ

INTRODUCTION
------------
This sample explains how you can use LINQ Queries with NCache. This sample provides you with 3 examples of LINQ Queries.

DESCRIPTION
-----------
There are two modules in this sample.
	i)	NCacheLINQ.Business (Class library)
		this module some attributes of Products. The Product class and its
		attributes are also implemented in this module.
	ii)	NCacheLINQ (Console application)
		Simple console application that presents you a menu to select the query you want to execute against the cache. This application uses 'myCache' by default. You can specify any other from app.config. 
		Following LINQ Queries are implemented in this sample:
			a)	from product in products where product.ProductID > 10 select product;
			b)	from product in products where product.Category == 4 select product;
			c)	from product in products where product.ProductID < 10 && product.Supplier == 1 select product;

HOW TO RUN THE SAMPLE
---------------------
Before starting the sample follow these steps:
	1.	Build the sample.
	2. 	To use this sample, you must first specify the indexes of the objects you want to query in the cache. By default this sample uses 
		'myCache'. 
	3.	Start NCache Manager and open the 'localcaches' project file from the Recent Projects menu under File menu. 
	4.	Select the 'myCache' icon in the explorer bar from the left pane. Now from the main menu, select the 'Query Indexes' tab.
	5.	Click Add from the Query Indexes tab, you'll see 'Select Indexes' form. Click 'Add...' button to add some classes. 
	6.	Now from the 'Class Browser', browse for the file 'NCacheLINQ.Business.dll'.
	7.	Select the 'Products' class and click 'Add to list' and click Ok.
	8.	Check the class and all of its attributes and click Ok.
	9.	Apply configuration changes and start the cache.
	
You are ready to start the sample application.

NCacheLINQ.Business settings:
------------------------------

	no settings are required for this module.

NCache Settings:
---------------

By default this sample uses localcache named 'myCache'. This cache is automatiaclly installed with the NCache.
You can change the configuration of the sample to use any other cache. Before using any other cache follow the 'How to Run Sample' steps.


REQUIRMENTS
-----------
NCache 3.8 or greater.

