using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Alachisoft.NCache.Automation.Util
{
    public class AssemblyResolver
    {
        public static System.Reflection.Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                string final = "";
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                    string bin = directoryInfo.Parent.Parent.FullName; //bin folder
                    final = System.IO.Path.Combine(bin, "service"); /// from where you neeed the assemblies
                }
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                    string installDir = directoryInfo.Parent.Parent.Parent.FullName; //linux install directory
                    final = System.IO.Path.Combine(installDir, "lib");
                }
                return System.Reflection.Assembly.LoadFrom(System.IO.Path.Combine(final, new AssemblyName(args.Name).Name + ".dll"));
            }
            catch
            {
                return null;
            }
        }
    }
}
