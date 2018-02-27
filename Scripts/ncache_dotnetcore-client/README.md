# Installing NCache Open Source 4.9 Remote Cache Client #

This document serves as a reference guide on how NCache Open Source 4.9 Remote Cache Client can be compiled and installed on your machine provided that the environment on your machine contains the necessary tools.

## Table of Contents ##

---

- [Overview](#overview)
- [Setting Up the Environment](#setting-up-the-environment)
- [Building NCache](#building-ncache)
- [Install Instructions](#install-instructions)

## Overview ##

---

NCache Open Source Remote Cache Client is installed by building the source code and creating a tar.gz of the binaries obtained from the build on a Windows machine. This tar.gz file is copied to your Linux machine and extracted. From the extract, an install shell script is run to carry out the installation of NCache.

## Setting Up the Environment ##

---

In order to build and use NCache as remote cache client, you **must** have **.NET Core version >= 2.0** and **7Zip** installed on your machine. For information on how to install the required technologies, please visit the following guides,

- [.NET Core installation guide.](https://www.microsoft.com/net/learn/get-started/windows)
- [7Zip installation binaries.](http://www.7-zip.org/download.html)

> It is to be understood that another installation of NCache should not be present on your machine otherwise, the scripts will not install NCache Open Source Remote Cache Client. For more information on uninstalling NCache Remote Cache Client please refer to the [uninstall instructions here](../README.md#uninstall-instructions).

## Building NCache ##

---

The source code for NCache Open Source 4.9 Remote Cache Client can be divided into the following divisions,

- NCache Remote Cache Client and Tools
- NCache Integrations

In order to get to using them, these divisions are to be compiled and installed on your machine. For that, follow the following steps:

- Build the source code (source, tools and integrations) by running the `build.bat` script present in this directory.

## Install Instructions ##

---

NCache Open Source Remote Cache Client can be installed in Linux Operating System environment only. The following steps demonstrate how this can be brought to action:

- Once the source code has been compiled successfully, create the tar.gz file by running `create-targz.bat` (present in the current directory) in **elevated mode** (right click and choose "Run as administrator").
- The `create-targz` script creates the archive using NCache binaries from the build directory. Build directory contains the assemblies compiled from NCache source code. You can replace these assemblies with newer assemblies compiled with different set of NCache source code. For more details, please see the [Updating Assemblies section here](../README.md#updating-assemblies).
- Once the archive is created, you can install NCache remote cache client from it on your Linux machine by first extacting it in your Linux machine and then running the `install` shell script provided with the extract. For that simply run the following command in your terminal,

  ```bash
  ./install
  ```
