// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Management;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: AssemblyTitle("removenode")]
[assembly: AssemblyDescription("RemoveNode Utility for NCache")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]
[assembly: AssemblyProduct("Alachisoft® NCache Open Source")]
[assembly: AssemblyCopyright("Copyright © 2015 Alachisoft")]
[assembly: AssemblyTrademark("NCache ™ is a registered trademark of Alachisoft.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("4.4.0.0")]
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]

namespace Alachisoft.NCache.Tools.RemoveNode
{
	/// <summary>
	/// Internal class that helps display assembly usage information.
	/// </summary>
	/// 
    internal sealed class AssemblyUsage
	{
		/// <summary>
		/// Displays the logo banner
		/// </summary>
		/// <param name="printlogo">specifies whether to print the logo or not</param>
		public static void PrintLogo(bool printlogo)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			string logo = @"Alachisoft (R) NCache Utility RemoveNode. Version " + assembly.GetName().Version +
				@"
Copyright (C) Alachisoft 2015. All rights reserved.";

			if (printlogo)
			{
				System.Console.WriteLine(logo);
				System.Console.WriteLine();
			}
		}

		/// <summary>
		/// Displays assembly usage information.
		/// </summary>
		
		static public void PrintUsage()
		{

            string usage = @"Usage: removenode cache-id(s) [option[...]].

Argument:
 cache-id
    Specifies id of cach registered on the server. 
    The cache with this id is unregistered on the server.

Option:
 /s server-name
    Specifies a server name where the NCache service is running. The default 
    is the local machine

 /p port
    Specifies the port if the server channel is not using the default port. 
    The default is " + CacheConfigManager.HttpPort + @" for http and " + CacheConfigManager.NCacheTcpPort + @" for tcp channels
 
 /G /nologo
    Suppresses display of the logo banner 

 /?
    Displays a detailed help screen 
";

			System.Console.WriteLine(usage);
		}
	}
}


