http://splinter.com.au/using-the-new-odpnet-to-access-oracle-from-c

http://www.oracle.com/technetwork/topics/dotnet/index-085163.html

Download x86 Oracle Data Access

http://www.oracle.com/technetwork/database/windows/downloads/index-101290.html


Download x64 Oracle Data Access

http://www.oracle.com/technetwork/topics/dotnet/utilsoft-086879.html


In case of x86:

1. Extract the "ODTwithODAC112021.zip" file

2. Goto ODTwithODAC112021\stage\Components\oracle.ntoledb.odp_net_2\11.2.0.2.0\1\DataFiles\
   this folder contains the jar files; and these jar files internally contains the *.dll we require.

3. Extract the filegroup2.jar and filegroup4.jar files using any compression tools like 7zip or rar etc
	From "filegroup2.jar" you will get the Oracle.DataAccess.dll for .net 4x (Oracle.DataAccess.dll version: : 4.112.2.0)
	and From "filegroup17.jar" you will get the Oracle.DataAccess.dll for .net 2x (Oracle.DataAccess.dll version: 2.112.2.0)

4. Goto ODTwithODAC112021\stage\Components\oracle.rdbms.rsf.ic\11.2.0.2.0\1\DataFiles\
	Extract filegroup2.jar and goto filegroup2\bin\ 
	From here you will get oci.dll.dbl and rename it  to oci.dll (just remove the .dbl)

5. Goto ODTwithODAC112021\stage\Components\oracle.rdbms.ic\11.2.0.2.0\1\DataFiles\
	Extract filegroup4.jar and then filegroup4\instantclient\light get the oraociicus11.dll


6. Goto ODTwithODAC112021\stage\Components\oracle.ntoledb.odp_net_2\11.2.0.2.0\1\DataFiles\

	Extract filegroup16.jar and then from filegroup16\bin get the OraOps11w.dll

7. Goto ODTwithODAC112021\stage\Components\oracle.ldap.rsf.ic\11.2.0.2.0\1\DataFiles\
	Extract filegroup1.jar and from filegroup1\bin\  get the orannzsbb11.dll

8. Goto ODTwithODAC112021\stage\Components\oracle.rdbms.rsf.ic\11.2.0.2.0\1\DataFiles\

	Extract filegroup3.jar and then from filegroup3\bin get the oraocci11.dll

9. Goto ODTwithODAC112021\stage\Components\oracle.rdbms.rsf.ic\11.2.0.2.0\1\DataFiles\
	Extract filegroup2.jar and then from filegroup2\bin get the ociw32.dll.dbl and rename to ociw32.dll (simply remove the .dbl)



In case of x64:

1. Extract the "ODAC112021Xcopy_x64.zip" file

2. Goto ODAC112021Xcopy_x64\odp.net4\odp.net\bin\4\
	From here you will get the Oracle.DataAccess.dll for .net 4.x (Oracle.DataAccess.dll version: : 4.112.2.0)

3. Goto ODAC112021Xcopy_x64\odp.net20\odp.net\bin\2.x\
	From here you will get the Oracle.DataAccess.dll for .net 2.x  (Oracle.DataAccess.dll version: 2.112.2.0)

4. Goto ODAC112021Xcopy_x64\instantclient_11_2\
	Get the oci.dll; ociw32.dll; orannzsbb11.dll; oraocci11.dll



Now to run the application:

	1. Create the project
	2. Add the reference to Oracle.DataAccess.dll (Note: Oracle have different version for 4.0 and 2.0 frameworks)
	3. and Place the rest of dll in the debug/release folder