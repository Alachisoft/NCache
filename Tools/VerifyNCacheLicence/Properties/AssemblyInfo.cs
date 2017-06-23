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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("VerifyNCacheLicence")]

[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]

[assembly: AssemblyProduct("VerifyNCacheLicence")]

[assembly: AssemblyCopyright("Copyright © Alachisoft 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("27a6f82b-84c2-499d-9b83-e214c8549c23")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("4.6.0.0")]
[assembly: AssemblyFileVersion("4.6.0.0")]

namespace Alachisoft.NCache.Tools.VerifyNCacheLicence
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

             string product="NCache";
             
            string logo = @"Alachisoft (R) "+ product +" Utility Verify License. Version " +Common.ProductVersion.GetVersion() +
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

        static public void PrintUsage()
        {

             string product="NCache";

            string usage = @"Description: This tool verify the" + product +"License. For registered version it will display the " +
                        "registration details. In evaluation mode it will display the remaining day if evaluation is still valid else " +
                        "give the expiration message." +
                        "\n\nUsage:"+

               "verifyncachelicense"

               +" [option[...]]." +
                        "\n\nOption:" +
                        "\n /G /nologo" +
                        "\n    Suppresses the startup banner and copyright message." +
                        "\n\n /?" +
                        "\n    Displays a detailed help screen\n";

            System.Console.WriteLine(usage);
        }
    }
}
