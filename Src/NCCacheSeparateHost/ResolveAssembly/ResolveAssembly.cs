using System;
using System.Reflection;
using System.IO;

namespace ResolveAssembly
{
    public static class ResolveAssembly
    {
        public static void Run()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
        }
        private static Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location);
                string installDir = directoryInfo.Parent.Parent.FullName;
                return Assembly.LoadFrom(Path.Combine(Path.Combine(installDir, "lib"), new AssemblyName(args.Name).Name + ".dll"));
            }catch(Exception ex)
            {
                throw ex;
            }

        }
    }
}
