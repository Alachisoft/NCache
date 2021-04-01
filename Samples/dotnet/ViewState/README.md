# View State Caching

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample explains how to use NCache's ViewState Caching. 

### Prerequisites

Before the sample application is executed make sure that:

- `configSection` for view state caching is defined in `web.config`.
    ```xml
	<configSections>
		<!--NCache Register config section first. -->
		<sectionGroup name="ncContentOptimization">
			<section name="settings" type="Alachisoft.NCache.ContentOptimization.Configurations.ContentSettings" allowLocation="true" allowDefinition="Everywhere"/>
        </sectionGroup>
    </configSections>
    ```
- The following configurations are defined in `web.config`.
	```xml
    <ncContentOptimization>
    	<settings enableViewstateCaching="true" enableTrace="false">
        	<cacheSettings cacheName="mycache" connectionRetryInterval="300">
            	<expiration type="None" duration="100"/>
            </cacheSettings>
        </settings>
    </ncContentOptimization>
    ```
    > Please note that these configurations will not be under any other section.
- `web.config` has been changed according to the configurations, 
	- Change the cache name
- The data source for this application has been configured to use `northwind.xml` that is present in 'App_Data' folder (present in the sample's source code). Make sure it exists.
- By default this sample uses 'mycache' so make sure that cache is running. 

### Build and Run the Sample
    
- Run the sample application.

### Additional Resources

##### Documentation
The complete online documentation for NCache is available at:
http://www.alachisoft.com/resources/docs/#ncache

##### Programmers' Guide
The complete programmers guide of NCache is available at:
http://www.alachisoft.com/resources/docs/ncache/prog-guide/

### Technical Support

Alachisoft [C] provides various sources of technical support. 

- Please refer to http://www.alachisoft.com/support.html to select a support resource you find suitable for your issue.
- To request additional features in the future, or if you notice any discrepancy regarding this document, please drop an email to [support@alachisoft.com](mailto:support@alachisoft.com).

### Copyrights

[C] Copyright 2021 Alachisoft 