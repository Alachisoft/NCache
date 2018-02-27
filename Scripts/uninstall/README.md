# Uninstall Scripts #

This document serves as a reference guide on how NCache Open Source 4.9 Cache Server can be uninstalled on your machine. For uninstall, simply run the `uninstall.bat` file in **elevated mode** (right click and choose "Run as administrator") present in this folder. Uninstall script removes all NCache configuration files and binaries from installation directory as well as from GAC and Windows Registry using some utilities present in the *uninstall-scripts* folder within this folder. The installation directory of NCache is read by the script itself from `NCHOME` (an environment variable set during installation).

> **NOTE**: This method can be used to uninstall any version and edition of NCache available for Windows.