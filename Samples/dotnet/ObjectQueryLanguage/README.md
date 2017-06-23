# OBJECT QUERY LANGUAGE

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Copyrights](#copyrights)

### Introduction

This sample program demonstrates how to use the Object Query Language in NCache. 
This sample provides you with 3 examples of OQL. Following Queries are implemented in this sample:
- 'Select Alachisoft.NCache.Sample.Data.Product where this.ProductID > 10'
- 'Select Alachisoft.NCache.Sample.Data.Product where this.ProductID = 71 and this.Category = 4'
- 'Select Alachisoft.NCache.Sample.Data.Product where this.ProductID < 10 and this.Supplier = 1'

This sample uses SampleData project as a reference for model class "Product".

### Prerequisites

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- change the cache name
- To use this sample, you must first specify the indexes of the objects you want to query in the cache.
	- Use AddQueryIndex Tool to create query indexes for sample.Dll and apply configuration to cache. 
	- Provide fully qualified name of "Product" class from "Sample.Dll" for defining indexes.   
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