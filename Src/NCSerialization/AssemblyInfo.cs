//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

#if NETCORE
[assembly: AssemblyTitle("Alachisoft.NCache.Serialization (.NETCore)")]
#else
[assembly: AssemblyTitle("Alachisoft.NCache.Serialization")]
#endif

[assembly: AssemblyProduct("Alachisoft® NCache OpenSource")]
[assembly: AssemblyTrademark("NCache ™ is a registered trademark of Alachisoft.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]

[assembly: AssemblyCopyright("Copyright © 2005-2021 Alachisoft")]
[assembly: AssemblyCulture("")]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion("5.0.0")]

//
// Since we need to support two runtimes which are not fully backward compatible we have
// to use side-by-side assemblies and that mandates a new version policy.
//
// From now on our version will obey the following structure.
//		Major.Minor.RuntimePatch.Private
//
// Major
//		Major version number of product, for example 1
// Minor
//		Minor version number of product, for example 5
// RuntimePatch
//		This is a number of the format RXX, where R is the runtime compaibility number
//		and XX is a two digit patch number. Currently two values for R are defined.
//		
//		R = 1 (.NET 1.0 and 1.1)
//		R = 2 (.NET 2.0)
//		
//		Everytime microsoft ships a .NET version that is not backward compatible to older
//		versions we'll have to redefine R.
// Private
//		Defines the private or developer build numbers. Not to be used for production.
//
//#if VS2005
//[assembly: AssemblyVersion("1.5.200.0")]
//[assembly: AssemblyDescription(".NET 2.0 supported")]
//#else
//[assembly: AssemblyVersion("1.5.100.0")]
//[assembly: AssemblyDescription(".NET 1.0 and 1.1 supported")]
//#endif

//
// In order to sign your assembly you must specify a key to use. Refer to the 
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing. 
//
// Notes: 
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. KeyFile refers to a file which contains
//       a key.
//   (*) If the KeyFile and the KeyName values are both specified, the 
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP, that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key 
//           in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//       When specifying the KeyFile, the location of the KeyFile should be
//       relative to the project output directory which is
//       %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//       located in the project directory, you would specify the AssemblyKeyFile 
//       attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//       documentation for more information on this.
//
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]
#if DEBUG
[assembly: AssemblyKeyFile("..\\..\\Resources\\ncache.snk")]
#else
[assembly: AssemblyKeyFile("..\\..\\Resources\\ncache.snk")]
#endif
[assembly: AssemblyFileVersionAttribute("5.0.5.0")]
[assembly: AssemblyDescriptionAttribute("Compact Serialization File")]
[assembly: AssemblyInformationalVersion("5.0.0")]
