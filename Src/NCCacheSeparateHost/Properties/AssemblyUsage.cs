using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Alachisoft NCache Cache Host Process")]
[assembly: AssemblyDescription("Alachisoft NCache Cache Host Process")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alachisoft")]
[assembly: AssemblyProduct("Alachisoft® NCache Open Source")]
[assembly: AssemblyCopyright("Copyright © 2005-2018 Alachisoft")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("8e3a0c76-ed75-43d7-a280-fdbd23bbf5eb")]

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
[assembly: AssemblyVersion("4.9.0.0")]
[assembly: AssemblyFileVersion("4.9.0.0")]
//[assembly: AssemblyFileVersionAttribute("4.9.0.0")]


namespace Alachisoft.NCache.CacheHost 
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
            string logo = @"Alachisoft (R) NCache Utility CacheSeparateHost. Version " + assembly.GetName().Version +
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

            string usage = @"Usage: CacheSeparateHost [option[...]].

 /i /cachename
    Specifies the id/name of cache.

 /f /configfile 
    Specifies the config file path.

 /p /cacheport 
    Specifies the client port on cache will start.

 /?
    Displays a detailed help screen 
";

            System.Console.WriteLine(usage);
        }
    }
}

