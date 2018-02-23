# NCache 4.9 Open Source Edition for Linux #

This document shows how tar.gz for NCache Open Source 4.9 can be created the help of some batch scripts that are provided.

### Setting Up the Environment ###

- Make sure you have **.NET Core version >= 2.0**  and **7Zip**  installed in your machine since these technologies are used to build the project files and make the tar.gz.
- For information on how to install .NET Core and 7Zip, please visit the following installation guides,
  - [.NET Core installation guide.](https://www.microsoft.com/net/learn/get-started/windows)
  - [7Zip installation guide.](http://www.7-zip.org/download.html)

  
## tar.gz Creation Instructions ##

- Run 'build-all-netcore.bat' placed at "\Scripts\NetCore\build-scripts\" to compile all required assmebly files.
- Run 'create-targz.bat' placed at "\Scripts\NetCore\Installation-scripts\" to create the tar.gz, it will be placed at 
"\Scripts\NetCore\" with the name of 'ncache-opensource-4.9-dotnetclient.tar.gz'



