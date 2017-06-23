# NHIBERNATE

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Copyrights](#copyrights)

### Introduction

This sample program demonstrates how to use the NHibernate integration for NCache. 
This sample application explains integrating NHibernate (OR mapping tool) with NCache to cache the data .
There are two modules in this sample.
- NHibernator
	- Simple Class library that maps the database table with .net objects using NHibernate assembly.
- NHibernateTest
	- Console Application that creates a Factory object presented in the above module to get the data. The objects that are used in these operations are cached using NCache. You can see the count of the cache items in the stats window of 'myPartitionedCache' (default cache for this sample).

This sample uses SampleData project as a reference for model class "Customer".

### Prerequisites

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
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

### Copyrights

[C] Copyright 2017 Alachisoft 