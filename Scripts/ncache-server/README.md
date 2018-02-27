# Installing NCache Open Source 4.9 Cache Server #

This document serves as a reference guide on how NCache Open Source 4.9 Cache Server can be compiled and installed on your machine provided that the environment on your machine contains the necessary tools.

## Table of Contents ##

---

- [Setting Up the Environment](#setting-up-the-environment)
- [Building NCache](#building-ncache)
- [Install Instructions](#install-instructions)

## Setting Up the Environment ##

---

In order to build and use NCache as cache server, you **must** have **.NET Framework version >= 4.0** installed on your machine. For information on how to install .NET Framework, please visit the [.NET Framework installation guide](https://docs.microsoft.com/en-us/dotnet/framework/install/guide-for-developers).

> It is to be understood that another installation of NCache should not be present on your machine otherwise, the scripts will not install NCache Open Source Cache Server. For more information on uninstalling NCache Cache Server please refer to the [uninstall instructions here](../README.md#uninstall-instructions).

## Building NCache ##

---

The source code for NCache Open Source 4.9 Cache Server can be divided into the following divisions,

- NCache Server and Tools
- NCache Integrations

In order to get to using them, these divisions are to be compiled and installed on your machine. For that, follow the following steps:

- To build the source code (source, tools and integrations) you need to run the `build.bat` script present in this directory.
- NCRegistry project has unmanage code for registry management for NCache related values in the registry. This project can not be compiled without Visual Studio SDK and IDE. Therefore, for ease of use, the `ncregister.dll` is placed in the *Resources* folder at root.
- If the user wants to install NCache with an updated `NCRegistry` project, (s)he will have to build `NCRegistry` project and then replace the updated `ncregistry.dll` in the *Resources* folder since the setup picks-up `ncregistry.dll` from there.

## Install Instructions ##

---

NCache Open Source Cache Server can be installed in Windows Operating System environment only. The following steps demonstrate how this can be brought to action:

- To install NCache Open Source Cache Server, first build the source code by running `build.bat` (present in this directory) in **elevated mode** (right click and choose "Run as administrator").
- Once the source code has been compiled successfully, run `install.bat` (also present in this directory) in **elevated mode** (right click and choose "Run as administrator") to install NCache on your machine.
- The installation script installs NCache binaries and configuration files from the build directory. Build directory contains the assemblies compiled from NCache source code. You can replace these assemblies with newer assemblies compiled with different set of NCache source code. For more details, please see the [Updating Assemblies section here](../README.md#updating-assemblies).
- To change NCache install directory, open `install.bat` in a text editor of your choice and modify `InstallDir`. By default, installation directory is "C:\Program Files\NCache\\" which is set as an environment variable by the key `NCHOME`.
