// Copyright (c) 2017 Alachisoft
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
[assembly: AssemblyTitle("addtestdata")]
[assembly: AssemblyDescription("AddTestData Utility for NCache")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]
[assembly: AssemblyProduct("Alachisoft® NCache Open Source")]
[assembly: AssemblyCopyright("Copyright © Alachisoft 2017")]
[assembly: AssemblyTrademark("NCache ™ is a registered trademark of Alachisoft.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("4.6.0.0")]
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]

namespace Alachisoft.NCache.Tools.AddTestData
{
    /// <summary>
    /// Internal class that helps display assembly usage information.
    /// </summary>
    internal sealed class AssemblyUsage
    {
        /// <summary>
        /// Displays the logo banner
        /// </summary>
        public static void PrintLogo(bool printlogo)
        {
            
            string logo = @"Alachisoft (R) NCache Utility AddTestData. Version " + Common.ProductVersion.GetVersion() +
                @"
Copyright (C) Alachisoft 2017. All rights reserved.";

            if (printlogo)
            {
                System.Console.WriteLine(logo);
                System.Console.WriteLine();
            }
        }

        /// <summary>
        /// Displays assembly usage information.
        /// </summary>
        /// <param name="printlogo">speicfies whether to print logo or not.</param>
        static public void PrintUsage()
        {
            string usage = @"Description: addtestdata adds some test data to the cache
to confirm that the cache is functioning properly. The items added to 
the cache expire after 1 minute. 

Usage: addtestdata cache-id [option[...]].

Argument:
 cache-id
    Specifies the id of the cache. 

Option:
 /c item-count
    Number of items to be added to the cache. By default 10 items are added 
    to the cache.    

 /S size
    Size in bytes of each item to be added to the cache. By default items of 1k
    (1024 bytes) are added to the cache.

 /e absolute-expiration 
    Specify in seconds, absolute expiration (default: 300; minimum: 15)
  
 /G /nologo
    Suppresses display of the logo banner. 

 /?
    Displays a detailed help screen. 
";
            System.Console.WriteLine(usage);
        }
    }
}


