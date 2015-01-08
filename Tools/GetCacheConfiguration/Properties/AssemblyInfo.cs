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


// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: AssemblyTitle("getcacheconfiguration")]
[assembly: AssemblyDescription("GetCacheConfiguration Utility for NCache")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]
[assembly: AssemblyProduct("Alachisoft® NCache")]
[assembly: AssemblyCopyright("Copyright© 2015 Alachisoft")]
[assembly: AssemblyTrademark("NCache™ is a registered trademark of Alachisoft.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("4.4.0.0")]
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]

namespace Alachisoft.NCache.Tools.GetCacheConfiguration
{
    /// <summary>
    /// Internal class that helps display assembly usage information.
    /// </summary>
    internal sealed class AssemblyUsage
    {
        /// <summary>
        /// Displays logo banner
        /// </summary>
        /// <param name="printlogo">Specifies whether to print logo or not</param>
        public static void PrintLogo(bool printlogo)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string logo = @"Alachisoft (R) NCache Utility GetCacheConfiguration. Version " + assembly.GetName().Version +
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

            string usage = @"Usage: getcacheconfiguration [option[...]].

 cache-id
    Specifies the id/name of the cache for which cache configuration will be
    generated. 

 /s /server
    Specifies the NCache server name/ip.

Optional:

 /T /path
    Specifies the path where config will be generated. 

 /p /port
    Specifies the port on which NCache server is listening.

 /G /nologo
    Suppresses display of the logo banner  

 /?
    Displays a detailed help screen 
";

            System.Console.WriteLine(usage);
        }
    }
}
