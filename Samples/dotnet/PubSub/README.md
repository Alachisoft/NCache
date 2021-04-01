# PUBSUB SAMPLE

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

This sample program demonstrates how to use the NCache messaging API to publish or subscribe messages. It shows how to create, get or delete a TOPIC and publish or subscribe messages on it.

This sample has 3 projects:

1) Publisher:              It will create two TOPICS, publish some messages on it and then deletes those TOPICS.
2) Non Durable Subscriber: It will create or get a TOPIC and then create subscription on it.
						   It will create pattern based subscriptions on all the topics matching the specified pattern.
3) Durable Subscriber:	   It will create or get a TOPIC and then create durable subscription on it with subscription policy types shared and 
                           exclusive. It will also create pattern based durable subscriptions on all the topics matching the provided pattern.

### Prerequisites

Before the sample application is executed make sure that:

- app.config have been changed according to the configurations. 
	- Change the cache name
- By default this sample uses 'democache', make sure that cache is running. 

### Build and Run the Sample
    
- Build Solution 
- Run the NonDurableSubscriber.
- Run the DurableSubscriber.
- Run the Publisher.


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