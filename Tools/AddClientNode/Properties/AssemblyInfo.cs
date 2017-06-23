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
using Alachisoft.NCache.Management;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: AssemblyTitle("addclientnode")]
[assembly: AssemblyDescription("AddClientNode Utility for NCache")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]
[assembly: AssemblyProduct("Alachisoft® NCache")]
[assembly: AssemblyCopyright("Copyright© Alachisoft 2017")]
[assembly: AssemblyTrademark("NCache™ is a registered trademark of Alachisoft.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("4.6.0.0")]
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]

namespace Alachisoft.NCache.Tools.AddClientNode
{
    /// <summary>
    /// Internal class that helps display assembly usage information.
    /// </summary>
    internal sealed class AssemblyUsage
    {
        /// <summary>
        /// Displays the logo banner
        /// </summary>
        /// <param name="printlogo">specifies whether to display logo or not</param>
        public static void PrintLogo(bool printlogo)
        {
            string logo = @"Alachisoft (R) NCache Utility AddClientNode. Version " + Common.ProductVersion.GetVersion() +
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

            string usage = @"Usage: addclientnode  [option[...]].

 cache-id
    Specifies name of clustered cache to which client node is to be added. 
    Cache must exist on source server.
  
 /s /server 
    Specifies a server name where the NCache service is running and a cache 
    with the specified cache-name is registered. 

 /e /client-node
    Specifies a client node where the NCache service is running.

Optional:

 /p /port
    Specifies a port number for communication with the NCache server.

 /u /update-server-config
    Specifies whether to update the client-nodes sections of server node(s) of
    the specified cluster. The default value is true.(Useful when cluster nodes
    and clients are in different networks)

 /a /acquire-server-mapping
    Specifies whether to fetch the server mapping list from the server node(s).
    The default value is false.(Useful when cluster nodes and clients are in
    different networks)

 /G /nologo
    Suppresses display of the logo banner  

 /?
    Displays a detailed help screen
";

            System.Console.WriteLine(usage);
        }
    }
}
