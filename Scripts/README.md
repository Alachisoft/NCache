# NCache 4.9 Open Source Edition #

This document shows how NCache Open Source 4.9 can be installed and uninstalled with the help of some batch scripts that are provided with the project.

## Table of Contents ##

---

- [Installation Instructions](#installation-instructions)
  - [Setting Up the Environment](#setting-up-the-environment)
  - [Installing NCache Open Source](#installing-ncache-open-source)
- [Uninstallation Instructions](#uninstallation-instructions)
- [Updating Assemblies](#updating-assemblies)
  - [If NCache is Already Installed](#if-ncache-is-already-installed)
  - [If NCache is not Installed](#if-ncache-is-not-installed)

## Installation Instructions ##

---

This section of the document is concerned with providing guidance on how to **set up the environment** and **install** NCache Open Source on your machine.

### Setting Up the Environment ###

- Make sure you have **.NET Framework version >= 4.0** and **.NET Core version >= 2.0** installed in your machine since these technologies are used to build the project files.
- For information on how to install .NET Framework and .NET Core, please visit the following installation guides,
  - [.NET Framework installation guide.](https://docs.microsoft.com/en-us/dotnet/framework/install/guide-for-developers)
  - [.NET Core installation guide.](https://www.microsoft.com/net/learn/get-started/windows)
- Last but not the least, make sure that another installation of NCache is not present on your machine otherwise, the scripts will not install NCache Open Source. For more information on uninstalling NCache please refer to the [uninstalling NCache](#uninstallation-instructions) section.

### Installing NCache Open Source ###

- To install NCache Open Source, please run `install.bat` in elevated mode (right click and choose "Run as administrator"). You can find `install.bat` at "Scripts\Installation-scripts\install.bat".
- The installation script installs NCache binaries and configuration files from the build directory. Build directory contains the assemblies compiled from NCache source code. You can replace these assemblies with newer assemblies compiled with different set of NCache source code. For more details, please see the [Updating Assemblies](#updating-assemblies) section below.
- To change NCache install directory, open `install.bat` in a text editor of your choice and modify `InstallDir`. By default, installation directory is "C:\Program Files\NCache" which is set as an environment variable by the key `%NCHOME%`.

## Uninstallation Instructions ##

---

This section of the document is concerned with providing guidance on how to uninstall NCache on your machine. The following steps can be followed in order to uninstall NCache,

- Just like installation scripts, uninstallation scripts are also available with the project to uninstall NCache.
- To uninstall NCache, please run `uninstall.bat` in elevated mode (right click and choose "Run as administrator"). You can find `uninstall.bat` at "Scripts\Installation-scripts\uninstall.bat".
- Uninstall script removes all NCache configuration files and binaries from installation directory as well as from GAC and Windows Registry. The installation directory of NCache is read by the script itself from `%NCHOME%` (an environment variable set during installation).

> **NOTE**: This method can be used to uninstall any version and edition of NCache available.

## Updating Assemblies ##

---

This section of the document is concerned with updating assemblies for NCache Open Source. The following steps elaborate how that can be done so,

### If NCache is Already Installed ###

If NCache is already installed, you will have to replace relevant files in their respective locations in `%NCHOME%` (an environment variable set during installation). If you have made changes to NCache source code, you'll have to replace the assemblies compiled from this source code. Make sure your activity caters to the following,

- Replace assemblies in `%NCHOME%` under its relevant folder. For example,
  - If `Alachisoft.NCache.Cache.dll` is modified and build with .NET Framework, this file will be replaced with the already existing file in "%NCHOME%\bin\assembly\4.0\" since this change is related to NCache's source code.
  - If `Alachisoft.NCache.Linq.dll` is modified and build with .NET Framework, this file will be replaced with the already existing file in "%NCHOME%\integrations\LINQToNCache\4.0\" since this change is related to an integration project.
- Install assemblies to GAC. For information on how to install assemblies to GAC, please visit [this link](https://docs.microsoft.com/en-us/dotnet/framework/app-domains/how-to-install-an-assembly-into-the-gac).
- Reinstall service (if changes are made in its code). For information on how to install service in Windows, please visit [this link](https://docs.microsoft.com/en-us/dotnet/framework/windows-services/how-to-install-and-uninstall-services).
- Restart service using the following Windows PowerShell command,
  > Make sure to run Windows PowerShell in elevated mode (right click and choose "Run as administrator").
  ```PowerShell
  Restart-Service -Name NCacheSvc
  ```

### If NCache is not Installed ##

If NCache is not installed, that illustrates that the installation package is being updated. For that, when the module is built, a Post-Build script in the module will replace the assembly of the module in the relevant build directory on its own.
