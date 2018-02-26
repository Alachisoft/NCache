// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
#if !DEBUG
[assembly: CLSCompliant(true)]
#endif
[assembly: ComVisible(false)]

#if NETCORE
[assembly: AssemblyTitle("Alachisoft.NCache.Storage (.NETCore)")]
#else
[assembly: AssemblyTitle("Alachisoft.NCache.Storage")]
#endif

[assembly: AssemblyProduct("Alachisoft® NCache Open Source")]

[assembly: AssemblyTrademark("NCache ™ is a registered trademark of Alachisoft.")]

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]

[assembly: AssemblyCopyright("Copyright © 2005-2018 Alachisoft")]
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

[assembly: AssemblyVersion("4.9.0")]

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

[assembly: AssemblyFileVersionAttribute("4.9.0.0")]
[assembly: AssemblyDescriptionAttribute("Storage Classes")]
[assembly: AssemblyInformationalVersion("4.9.0")]