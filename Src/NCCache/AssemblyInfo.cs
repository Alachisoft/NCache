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
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]


#if NETCORE
[assembly: AssemblyTitle("Alachisoft.NCache.Cache (.NETCore)")]
#else
[assembly: AssemblyTitle("Alachisoft.NCache.Cache")]
#endif

[assembly: AssemblyProduct("Alachisoft® NCache Open Source")]


[assembly: AssemblyTrademark("NCache ™ is a registered trademark of Alachisoft.")]


[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]



[assembly: AssemblyCopyright("Copyright © 2005-2018 Alachisoft")]
[assembly: AssemblyCulture("")]

[assembly: InternalsVisibleTo("EladLicenseGenerator,PublicKey=002400000480000094000000060200000024000052534131000400000100010005a3e761ae2217"
+ "0e7f5cc1208e5a2e51fef749c98ee0cc3c94dc1d688fe0324370d327bb3e33248ad603831c8b5b"
+ "7316c451e26b5fcb99ec05884419f7102942e7446a51e0c5812530af21c49330e45baaba4247cb"
+ "07f4807a1d051466040c77d437fb79ffe78a2330d4d5a6830577b98907cba0365ced3f9c4bb91f"
+ "b9520bc9")]
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
[assembly: AssemblyDescriptionAttribute("Cache Core")]
[assembly: AssemblyFileVersionAttribute("4.9.0.0")]
[assembly: AssemblyInformationalVersion("4.9.0")]
