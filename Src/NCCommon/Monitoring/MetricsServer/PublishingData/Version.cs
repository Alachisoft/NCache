using Alachisoft.NCache.Common.RuntimeEnvironment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    struct ProductVersionType
    {
        public static int Major, Minor, Build, Revision;
    }
    public class Version
    {
        public static string GetVersion()
        {
            string version;
            string assemblyFile = "Alachisoft.NCache.Cache.dll";
            string path = "";
            System.Reflection.Assembly assembly;
            try
            {
                path = Path.Combine(AppUtil.InstallDir, "bin", "assembly", "4.0");// if framework installation

                if (!File.Exists(Path.Combine(path, assemblyFile)))
                    path = Path.Combine(AppUtil.InstallDir, "bin", "service"); // if netcore installation

                if (!File.Exists(Path.Combine(path, assemblyFile)))
                    path = Path.Combine(AppUtil.InstallDir, "lib"); // if netcore Linux

                if (!File.Exists(Path.Combine(path, assemblyFile)))
                    throw new FileNotFoundException("Could not find the assembly", path);

                assembly = System.Reflection.Assembly.LoadFrom(Path.Combine(path, assemblyFile));
            }
            catch (Exception ex)
            {
                assembly = System.Reflection.Assembly.LoadFrom(Path.Combine(path, assemblyFile));

            }

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            version = fvi.FileVersion;
            try
            {
                string[] components = version.Split('.');
                int.TryParse(components[0], out ProductVersionType.Major);
                int.TryParse(components[1], out ProductVersionType.Minor);
                int.TryParse(components[2], out ProductVersionType.Build);
                int.TryParse(components[3], out ProductVersionType.Revision);


                string productVersion = string.Format("{0}.{1}", ProductVersionType.Major, ProductVersionType.Minor);
                string servicePack = string.Format("{0}{1}", ProductVersionType.Build > 0 ? "SP" : string.Empty, ProductVersionType.Build > 0 ? ProductVersionType.Build.ToString() : string.Empty);
                string privatePatch = string.Format("{0}{1}", ProductVersionType.Revision > 0 ? "PR" : string.Empty, ProductVersionType.Revision > 0 ? ProductVersionType.Revision.ToString() : string.Empty);

                version = (string.Format("{0} {1} {2}", productVersion, servicePack, privatePatch)).TrimEnd();
                return version;

            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Error occured while reading product info: " + ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
                try
                {
                    string spVersion = (string)Alachisoft.NCache.Common.RegHelper.GetRegValue(Alachisoft.NCache.Common.RegHelper.ROOT_KEY, "SPVersion", 0);
                    return (string.Format("{0} {1} ", version, spVersion));

                }
                catch
                {
                    return null;
                }

            }


        }

        public static int GetFormattedVersion(bool withRevision = false)
        {
            System.Reflection.Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string productVersion = null;
            string version = fvi.FileVersion;
            string[] components = version.Split('.');
            int.TryParse(components[0], out ProductVersionType.Major);
            int.TryParse(components[1], out ProductVersionType.Minor);
            int.TryParse(components[2], out ProductVersionType.Build);
            if (!withRevision)
            {
                int.TryParse(components[3], out ProductVersionType.Revision);
                productVersion = string.Format("{0}{1}{2}", ProductVersionType.Major, ProductVersionType.Minor, ProductVersionType.Build);
            }
            else
            {
                productVersion = string.Format("{0}{1}{2}{3}", ProductVersionType.Major, ProductVersionType.Minor, ProductVersionType.Build, ProductVersionType.Revision);
            }
            version = (string.Format("{0}", productVersion)).TrimEnd();
            int prodVersion = Int32.Parse(version);
            return prodVersion;
        }

        public static string GetVersionForNuGet()
        {
            string version = null;
            string assemblyFile = "Alachisoft.NCache.Cache.dll";
            System.Reflection.Assembly assembly = null;
            try
            {
                var path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

                if (!File.Exists(Path.Combine(path, assemblyFile)))
                    throw new FileNotFoundException("Could not find the assembly", path);

                assembly = System.Reflection.Assembly.LoadFrom(Path.Combine(path, assemblyFile));



                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                version = fvi.FileVersion;

                string[] components = version.Split('.');
                int.TryParse(components[0], out ProductVersionType.Major);
                int.TryParse(components[1], out ProductVersionType.Minor);
                int.TryParse(components[2], out ProductVersionType.Build);
                int.TryParse(components[3], out ProductVersionType.Revision);

                string productVersion = string.Format("{0}.{1}", ProductVersionType.Major, ProductVersionType.Minor);
                string servicePack = string.Format("{0}{1}", ProductVersionType.Build > 0 ? "SP" : string.Empty, ProductVersionType.Build > 0 ? ProductVersionType.Build.ToString() : string.Empty);
                string privatePatch = string.Format("{0}{1}", ProductVersionType.Revision > 0 ? "PR" : string.Empty, ProductVersionType.Revision > 0 ? ProductVersionType.Revision.ToString() : string.Empty);

                version = (string.Format("{0} {1} {2}", productVersion, servicePack, privatePatch)).TrimEnd();
                return version;

            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Error occured while reading product info: " + ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
                try
                {
                    string spVersion = (string)Alachisoft.NCache.Common.RegHelper.GetRegValue(Alachisoft.NCache.Common.RegHelper.ROOT_KEY, "SPVersion", 0);
                    return (string.Format("{0} {1} ", version, spVersion));

                }
                catch
                {
                    return null;
                }

            }


        }
    }
}
