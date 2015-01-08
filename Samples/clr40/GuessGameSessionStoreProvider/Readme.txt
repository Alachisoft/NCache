GUESS GAME with SESSION STORE PROVIDER SAMPLE
---------------------------------------------


DESCRIPTION
-----------
It is a simple ASP.NET application integrated with NCache Session Store Provider and provides a simple guessgame. User is asked to guess a number between 1 and 100. The guesses made by the user are stored in the cache and are displayed on the same web page.

HOW TO RUN THE SAMPLE
--------------------- 
	1.	Deploy GuessGameSessionStoreProvider in IIS as an application.
	2.	By default this sample uses 'mypartitionedreplica' to store the session information. After successful 				installation of NCache, this cache is also registered on your machine.
	3.	Start 'mypartitionedreplica' using the startcache utility as mentioned below. See NCache help for more 		information.
		
		Startcache mypartitionedreplica /s localhost
	
	4.	When you use this sample you can see the stats of the 'mypartitionedreplica' in the perfom under "NCache " counters

REQUIRMENTS
-----------
IIS 5.0 or later
NCache 3.0 or later

Note
----
For NCache Developer Edition replace 'mypartitionedreplica' with 'myCache' or any other local-cache.