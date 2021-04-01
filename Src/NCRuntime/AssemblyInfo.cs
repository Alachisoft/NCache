//  Copyright (c) 2018 Alachisoft
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
using System.Security.Permissions;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
//#if !DEBUG
[assembly: CLSCompliant(true)]
//#endif
//[assembly: FileIOPermission(SecurityAction.RequestMinimum)]
[assembly: ComVisible(false)]
#if NETCORE
[assembly: AssemblyTitle("Alachisoft.NCache.Runtime (.NETCore)")]
#else
[assembly: AssemblyTitle("Alachisoft.NCache.Runtime")]
#endif
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]

[assembly: AssemblyProduct("Alachisoft® NCache OpenSource")]
[assembly: AssemblyCopyright("Copyright © 2005-2021 Alachisoft")]
[assembly: AssemblyTrademark("NCache ™ is a registered trademark of Alachisoft.")]

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
[assembly: AssemblyDescriptionAttribute("Runtime Classes")]
[assembly: AssemblyInformationalVersion("5.0.0")]

[assembly: InternalsVisibleTo("Alachisoft.NCache.Cache,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.Cluster,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.Common,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.Common.Util,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.Management,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.Serialization,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.SessionState,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.SessionStateManagement,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.SessionStoreProvider,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.SocketServer,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.Storage,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.Client,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]
[assembly: InternalsVisibleTo("Alachisoft.NCache.Caching.AutoExpiration,PublicKey=00240000048000009400000006020000002400005253413100040000010001001d52e880bc916ccf859cdcec2e364e066e86136ffef5b9dae06fd02970d1a2b224658f100c17216c40532dfe8f0c274342b20ebfa506067ad95a92891a0381df679be792585190a38e36e117aea93773923bba28bb185ef03ffc5fbec4351ba7fff68bce21d378f30acff1fc1fabdcbf0afc8b6287060fc6fc66bcd75db006bb")]






