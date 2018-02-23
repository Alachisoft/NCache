# BACKING SOURCE

### Table of contents

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Build and Run the sample](#build-and-run-the-sample)
* [Additional Resources](#additional-resources)
* [Technical Support](#technical-support)
* [Copyrights](#copyrights)

### Introduction

- This sample explains how NCache keeps the cached items synchronized with the database, and how NCache silently loads the expired items from the datasource using its 'ReadThru' feature and similarly, silently updates items using its 'WriteThru' feature.

- This sample deals with the following features of NCache.
	1) ReadThru
	2) WriteThru
	3) ReSync Expired Items

- This sample uses SampleData project as a reference for model class `Customer`.

### Prerequisites

Before the sample application is executed make sure that:

- `app.config` has been changed according to the following configurations, 
	- Change the cache name

- By default this sample uses 'myPartitionedCache', make sure that the cache is running.

- Northwind database must be configured in Sql Server and hosted.

- Before running this sample make sure that backing source is enabled and the following providers are registered,
	- For Read Thru
		1) SqlReadThruProvider
		2) SqliteReadThruProvider
	- For Write Thru
		1) SqlWriteThruProvider
		2) SqliteWriteThruProvider		

- To enable and register backing source follow these steps,
	1) Make sure Cache is stopped before making any configuration changes. Stop the Cache using Stop-Cache powershell cmdlet.
	2) To add Read-Thru providers in a cache set the enable read-thru tag true like `<read-thru enable-read-thru="True">`.
	3) To add Write-Thru providers in a cache set the enable write-thru tag true like `<write-thru enable-write-thru="True">`.
	4) Provide the other configurations under the `<provider>` tag in `config.ncconf`.
    5) Once changes are made on all server nodes, restart the NCache service and start Cache using Start-Cache powershell cmdlet.

	The following example adds the providers to the cache. Please update the connection string before providing this configuration. For Sqlite database, provide the absolute path of the database file as replacement for `ABSOLUTE_PATH_TO_SQLITE_DB_FILE` in the following configurations. For this sample, a Northwind Sqlite database has already been provided at the following location `%NCHOME%\samples\dotnet\BackingSource\Data\Northwind.sl3` so this path can also be used.
    > If the mentioned path is to be used, make sure that `%NCHOME%` is replaced with the actual path for the install directory (of NCache) otherwise, the path will not be valid.

```xml
      <backing-source>
        <read-thru enable-read-thru="True">
          <provider provider-name="sqlreadthruprovider" assembly-name="BackingSource.Providers.Sql, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" class-name="Alachisoft.NCache.Samples.Providers.SqlReadThruProvider" full-name="BackingSource.Providers.Sql.dll" default-provider="True">
            <parameters name="connstring" value="Data Source=localhost;Database=northwind;User Id=admin;password=xxxxxxxx;"/>
          </provider>
          <provider provider-name="sqlitereadthruprovider" assembly-name="BackingSource.Providers.Sqlite, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" class-name="Alachisoft.NCache.Samples.Providers.SqliteReadThruProvider" full-name="BackingSource.Providers.Sqlite.dll" default-provider="False">
            <parameters name="connstring" value="Data Source=ABSOLUTE_PATH_TO_SQLITE_DB_FILE;FailIfMissing=True;"/>
          </provider>
        </read-thru>
        <write-thru enable-write-thru="True">
          <provider provider-name="sqlwritethruprovider" assembly-name="BackingSource.Providers.Sql, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" class-name="Alachisoft.NCache.Samples.Providers.SqlWriteThruProvider" full-name="BackingSource.Providers.Sql.dll" default-provider="True">
            <parameters name="connstring" value="Data Source=localhost;Database=northwind;User Id=admin;password=xxxxxxxx;"/>
          </provider>
          <provider provider-name="sqlitewritethruprovider" assembly-name="BackingSource.Providers.Sqlite, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" class-name="Alachisoft.NCache.Samples.Providers.SqliteWriteThruProvider" full-name="BackingSource.Providers.Sqlite.dll" default-provider="False">
            <parameters name="connstring" value="Data Source=ABSOLUTE_PATH_TO_SQLITE_DB_FILE;FailIfMissing=True;"/>
          </provider>
          <write-behind mode="non-batch" failed-operations-queue-limit="5000" failed-operations-eviction-ratio="5%" throttling-rate-per-sec="500"/>
        </write-thru>
      </backing-source>
```

- Provide the assembly names and classes to use and name these providers as described above. Also specify connection string as 'connstring' parameter for northwind database.

- To deploy Provider for cache,
	- Make sure that NCache service and Cache is not running.	
	- Make 'deploy' folder in NCache directory if not already present.
	- Make directory with cache name in the deploy folder.
	- Copy and paste all the DLLs to that folder.
	- Once changes are made on all server nodes, restart the NCache service and start Cache using `Start-Cache` powershell cmdlet.

- DLLs needed to be deployed are as follows,
	1) BackingSource.Providers.Sql.dll
    2) BackingSource.Providers.Sqlite.dll
    3) System.Data.SQLite.dll
    4) SampleData.dll

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