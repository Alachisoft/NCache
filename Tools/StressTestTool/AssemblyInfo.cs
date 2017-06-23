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


// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: AssemblyTitle("stresstesttool")]
[assembly: AssemblyDescription("StressTestTool Utility for NCache")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]
[assembly: AssemblyProduct("Alachisoft® NCache")]
[assembly: AssemblyCopyright("Copyright© Alachisoft 2017")]
[assembly: AssemblyTrademark("NCache™ is a registered trademark of Alachisoft.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("4.6.0.0")]
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]

namespace Alachisoft.NCache.Tools.StressTestTool
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
            
            string logo = @"Alachisoft (R) NCache Utility StressTool. Version " + Common.ProductVersion.GetVersion() +
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
            string usage = @"DESCRIPTION: StressTestTool.exe allows you to quickly simulate heavy transactional load on a given cache. And, this helps you see how NCache actually performs under stress in your own environment.Please watch NCache performance counters in NCache Manager “statistics” or regular PerfMon.

NOTE: A test-case represents a user session or multiple get and update operations on the same cache key. Use test-case to simulate an ASP.NET or JSP sessions. When all test-case-iterations are used up, a user session becomes idle and left to expire. Each test-case-iteration consists of one or more gets and updates (ASP.NET session simulation would use 1 get and 1 update). And test-case-iteration-delay represents a delay between each iteration and can be used to simulate ASP.NET session behavior where a user clicks on a URL after 15-30 seconds delay.

USAGE: stresstesttool cache-id [option[...]].               

ARGUMENT:
 cache-id 
    Name of the cache.

OPTION:
 /n item-count
    How many total items you want to add. (default: infinite)     

 /i test-case-iterations 
    How many iterations within a test case (default: 20)

 /d test-case-iteration-delay 
    How much delay (in seconds) between each test case iteration (default: 0)

 /g gets-per-iteration 
    How many gets within one iteration of a test case (default: 1)

 /u updates-per-iteration 
    How many updates within one iteration of a test case (default: 1)

 /m item-size 
    Specify in bytes the size of each cache item (default: 1024)     

 /e sliding-expiration 
    Specify in seconds sliding expiration (default: 300; minimum: 15)

 /t thread-count
    How many client threads (default: 1; max: 3)

 /r reporting-interval
    Report after this many total iterations (default: 5000)

 /G /nologo
    Suppresses display of the logo banner       

 /? 
    Displays a detailed help screen.
";

            System.Console.WriteLine(usage);
        }
    }
}
