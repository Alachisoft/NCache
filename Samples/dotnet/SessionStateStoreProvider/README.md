# GUESS GAME with SESSION STORE PROVIDER

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Copyrights](#copyrights)

### Introduction

It is a simple ASP.NET application integrated with NCache Session Store Provider and provides a simple guessgame. 
User is asked to guess a number between 1 and 100. 
The guesses made by the user are stored in the cache and are displayed on the same web page.

### Prerequisites

Before the sample application is executed make sure that:

- IIS 5.0 or later is required
- NCache 3.0 or later is required
- Deploy SessionStateStoreProvider in IIS as an application.
- web.config have been changed according to the configurations. 
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