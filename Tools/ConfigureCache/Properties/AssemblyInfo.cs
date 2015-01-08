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
[assembly: AssemblyTitle("createcache")]
[assembly: AssemblyDescription("CreateCache Utility for NCache")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]
[assembly: AssemblyProduct("Alachisoft® NCache")]
[assembly: AssemblyCopyright("Copyright© 2015 Alachisoft")]
[assembly: AssemblyTrademark("NCache™ is a registered trademark of Alachisoft.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("4.4.0.0")]
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]

namespace Alachisoft.NCache.Tools.CreateCache
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
            string logo = @"Alachisoft (R) NCache Utility CreateCache. Version " + assembly.GetName().Version +
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

            string usage = @"Usage: createcache [option[...]].

 cache-id
    Specifies the id/name of the cache for which cache will be registered. 

 /s /server
    Specifies the NCache server names/ips where Cache should be configured, 
    seperated by commas e.g. 120.168.98.10,120.168.98.9

For Advance Case: In this case all configuration related settings will be taken from specified configuration file.

 /S cache-size
    Specifies the size(MB) of the cache to be created, default size if 1024 MB. 

 /T path
    Specifies the path of the cache source config which will be configured. 

For topology other than local you have to give topology and cluster port

 /t /topology 
    Specifies the topology in case of clustered cache. Possible values are
    i.     local 
    ii.    partitioned
    iii.   replicated

 /I /inproc
    Specifies the isolationlevel for local cache.

 /C /cluster-port 
    Specifies the port of the server, at which server listens. 

Optional:

For Simple case:

 /y /evict-policy 
    Specifies the eviction policy for cache items. Cached items will be 
    cleaned from the cache according to the specified policy if the cache
    reaches its limit. Possible values are
    i.   Priority  
    
 /o /ratio 
    Specifies the eviction ratio(Percentage) for cache items. Cached items will
    be cleaned from the cache according to the specified ratio if the cache
    reaches its limit. Default value is 5 (percent)
  
 /i /interval 
    Specifies the time interval(seconds) after which cache cleanup is called.
    Default clean-interval is 15 (seconds)

 /d /def-priority 
    Specifies the default priority in case of priority based eviction policy is
    selected.
    Possible values are
    i.     high
    ii.    above-normal
    iii.   normal (default)
    iv.    below-normal
    v.     low

For Both cases:

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
