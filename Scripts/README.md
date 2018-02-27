# NCache 4.9 Open Source Edition #

This document serves as a reference guide on how NCache Open Source 4.9 can be installed or uninstalled on your machine provided that the environment on your machine contains the necessary tools. NCache Open Source is comprised of the following installation options:

- **NCache Cache Server** : For the purpose of using NCache software as both cache server and cache client.
- **NCache Client** : For the purpose of using NCache software as remote cache client only.

In addition to that, NCache Open Source also comes with:

- **NCache Tools** : For using PowerShell (Windows) or PowerShell Core (Linux) cmdlets to get help with cache management.
- **NCache Integrations** : For third party integration tools to demonstrate the use and capabilities of the client side.

## Table of Contents ##

---

- [Setting Up the Environment](#setting-up-the-environment)
- [Install Instructions](#install-instructions)
  - [Installing NCache Open Source Cache Server](#installing-ncache-open-source-cache-server)
  - [Installing NCache Open Source Cache Client](#installing-ncache-open-source-cache-client)
- [Uninstall Instructions](#uninstall-instructions)
  - [Uninstalling NCache Open Source Cache Server](#uninstalling-ncache-open-source-cache-server)
  - [Uninstalling NCache Open Source Remote Cache Client](#uninstalling-ncache-open-source-remote-cache-client)
- [Updating Assemblies](#updating-assemblies)
  - [If NCache is Already Installed](#if-ncache-is-already-installed)
    - [For NCache Open Source Cache Server](#for-ncache-open-source-cache-server)
    - [For NCache Open Source Remote Cache Client](#for-ncache-open-source-remote-cache-client)
  - [If NCache is not Installed](#if-ncache-is-not-installed)

## Setting Up the Environment ##

---

This section of the document is concerned with providing guidance on how to set up the environment for NCache Open Source on your machine. Different installations of NCache require different technologies.

- In order to build and use NCache as cache server, you **must** have **.NET Framework version >= 4.0** installed on your machine.
- In order to build and use NCache as remote cache client, you **must** have **.NET Core version >= 2.0** installed on your machine.
- For information on how to install .NET Framework and .NET Core, please visit the following installation guides,
  - [.NET Framework installation guide.](https://docs.microsoft.com/en-us/dotnet/framework/install/guide-for-developers)
  - [.NET Core installation guide.](https://www.microsoft.com/net/learn/get-started/windows)
- NCache remote cache client is installed from a tar.gz file that is created by scripts provided. These scripts use the software 7Zip to operate. Therefore it is required that this software is also installed.
- Last but not the least, make sure that another installation of NCache is not present on your machine otherwise, the scripts will not install NCache Open Source. For more information on uninstalling NCache please refer to the [uninstalling NCache](#uninstall-instructions) section.

> **NOTE**: It is to be understood that the tools provided for building the source code of NCache are for Windows only. Therefore, the environment building the source code must have Windows Operating System running on it.

## Install Instructions ##

---

This section of the document is concerned with providing guidance on how to install NCache Open Source on your machine.

### Installing NCache Open Source Cache Server ###

NCache Open Source Cache Server can be installed in Windows Operating System environment only. The following steps demonstrate how this can be brought to action:

- To install NCache Open Source Cache Server, first build the source code by running `build.bat` in **elevated mode** (right click and choose "Run as administrator"). The script can be located at *Scripts\ncache-server\build.bat*.
- Once the source code has been compiled successfully, run `install.bat` in **elevated mode** (right click and choose "Run as administrator") to install NCache on your machine. You can find `install.bat` at *Scripts\ncache-server\install.bat*.
- The installation script installs NCache binaries and configuration files from the build directory. Build directory contains the assemblies compiled from NCache source code. You can replace these assemblies with newer assemblies compiled with different set of NCache source code. For more details, please see the [Updating Assemblies](#updating-assemblies) section below.
- To change NCache install directory, open `install.bat` in a text editor of your choice and modify `InstallDir`. By default, installation directory is "C:\Program Files\NCache\\" which is set as an environment variable by the key `NCHOME`.

### Installing NCache Open Source Cache Client ###

NCache Open Source Cache Client can be installed in Linux Operating System environment only. The following steps demonstrate how this can be brought to action:

- To install NCache Open Source Cache Client, first build the source code by running `build.bat` in **elevated mode** (right click and choose "Run as administrator"). The script can be located at *Scripts\ncache-dotnetcore-client\build.bat*.
- Once the source code has been compiled successfully, create the tar.gz file by running `create-targz.bat` in **elevated mode** (right click and choose "Run as administrator"). You can find `create-targz.bat` at *Scripts\ncache-dotnetcore-client\create-targz.bat*.
  > NOTE : The script 'create-targz.bat' requires the software 7Zip in order to create the archive. So make sure it is installed.
- The `create-targz` script creates the archive using NCache binaries from the build directory. Build directory contains the assemblies compiled from NCache source code. You can replace these assemblies with newer assemblies compiled with different set of NCache source code. For more details, please see the [Updating Assemblies](#updating-assemblies) section below.
- Once the archive is created, you can install NCache remote cache client from it on your Linux machine by first extacting it in your Linux machine and then running the `install` shell script provided with the extract. For that simply run the following command in your terminal,

  ```bash
  ./install
  ```

## Uninstall Instructions ##

---

This section of the document is concerned with providing guidance on how to uninstall NCache on your machine. Just like installation scripts, uninstallation scripts are also available with the project to uninstall NCache. They can be brought to use for this purpose.

### Uninstalling NCache Open Source Cache Server ###

NCache Open Source Cache Server can only be installed on Windows Operating System. To uninstall it, simply run the `uninstall.bat` file in **elevated mode** (right click and choose "Run as administrator"). You can find `uninstall.bat` at *Scripts\uninstall\uninstall.bat*. Uninstall script removes all NCache configuration files and binaries from installation directory as well as from GAC and Windows Registry. The installation directory of NCache is read by the script itself from `NCHOME` (an environment variable set during installation).

> **NOTE**: This method can be used to uninstall any version and edition of NCache available for Windows.

### Uninstalling NCache Open Source Remote Cache Client ###

NCache Open Source Remote Cache Client can only be installed on Linux Operating System. To uninstall it, simply run the `uninstall` shell script provided when the tar.gz archive was extracted. The command used is,

```bash
./uninstall
```

## Updating Assemblies ##

---

This section of the document is concerned with updating assemblies for NCache Open Source. The following steps elaborate how that can be done so,

### If NCache is Already Installed ###

If NCache is already installed, you will have to replace relevant files in their respective locations in `NCHOME` (an environment variable set during installation). If you have made changes to NCache source code, you'll have to replace the assemblies compiled from this source code. Make sure your activity caters to the following,

#### For NCache Open Source Cache Server ####

- Replace assemblies in `NCHOME` under its relevant folder. For example,
  - If `Alachisoft.NCache.Cache.dll` is modified and build with .NET Framework, this file will be replaced with the already existing file in "%NCHOME%\bin\assembly\4.0\" since this change is related to NCache's source code.
  - If `Alachisoft.NCache.Linq.dll` is modified and build with .NET Framework, this file will be replaced with the already existing file in "%NCHOME%\integrations\LINQToNCache\4.0\" since this change is related to an integration project.
- Install assemblies to GAC. For information on how to install assemblies to GAC, please visit [this link](https://docs.microsoft.com/en-us/dotnet/framework/app-domains/how-to-install-an-assembly-into-the-gac).
- Reinstall service (if changes are made in its code). For information on how to install service in Windows, please visit [this link](https://docs.microsoft.com/en-us/dotnet/framework/windows-services/how-to-install-and-uninstall-services).
- Restart service using the following Windows PowerShell command,
  > Make sure to run Windows PowerShell in **elevated mode** (right click and choose "Run as administrator").
  ```PowerShell
  Restart-Service -Name NCacheSvc
  ```

#### For NCache Open Source Remote Cache Client ####

- Simply replace the updated assembly with the one already available in the *lib* folder present in the root location of your install directory.
- Restart NCache daemon after that by running the following command from your terminal,
  > Make sure you have **sudo** privileges before running this command.
  ```bash
  systemctl restart ncached
  ```

### If NCache is not Installed ##

If NCache is not installed, that illustrates that the installation package is being updated. For that, when the module is built, a Post-Build script in the module will replace the assembly of the module in the relevant build directory on its own.
