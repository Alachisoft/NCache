NCache 4.6 Open Source Edition 

Installation Instructions
---------------------
	- To install NCache Open Source, please run install.bat in elevated mode (right click and choose "run as administrator"). You can find install.bat in ncache-4.4-build/scripts/install.bat
	- Installation script installs NCache binaries and configuration files from ncache-4.4-build/build directory. Build directory contains the assemblies compiled from NCache source code. You can replace these assemblies with newer assemblies compiled with different set of NCache source code. For more details, please see "Update Assemblies" section below.
	- To change NCache install directory, open install.bat in text editor and modify "InstallDir". By default, installation directory is "C:\Program Files\NCache".
	
UnInstallation Instruction
----------------------------
	- Once NCache is installed, uninstallation scripts are also copied to the install directory. 
	- You can run uninstall.bat in elevated mode to uninstall NCache.	
	- Uninstall script removes all NCache configuration files and binaries from installation directory as well as from GAC.
	
Update Assemblies
-------------------
	- If you have made changes to NCache source code, you'll have to replace the assemblies compiled from this source code. Following are more details.
		- If NCache is already installed:
				- Replace assemblies in install directory under its relevant folder. Please note that assemblies compiled with different versions of .NET framework should be placed under their own folders. Currently, NCache assemblies are compiled with either .NET 3.5 or 4.0.
				- Install assemblies to GAC.
				- Reinstall service (if changes are made in its code)
				- Restart service.
			
		- Update installation package:
				- Replace assemblies in the build directory on installation package under their appropriate version of .NET framework.
