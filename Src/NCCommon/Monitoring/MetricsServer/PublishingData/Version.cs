using Alachisoft.NCache.Common.RuntimeEnvironment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    struct ProductVersionType
    {
        public static int Major, Minor, Build, Revision;
    }
    public class Version
    {
        private const string ASSEMBLY_FILE = "Alachisoft.NCache.Cache.dll";

        /// <summary>
        /// Reads the product version from the installed Alachisoft.NCache.Cache.dll and returns it
        /// in display form, e.g. "5.3.603 RC1".
        /// </summary>
        public static string GetVersion()
        {
            string path = "";
            System.Reflection.Assembly assembly;
            try
            {
                path = Path.Combine(AppUtil.InstallDir, "bin", "assembly", "4.0");// if framework installation

                if (!File.Exists(Path.Combine(path, ASSEMBLY_FILE)))
                    path = Path.Combine(AppUtil.InstallDir, "bin", "service"); // if netcore installation

                if (!File.Exists(Path.Combine(path, ASSEMBLY_FILE)))
                    path = Path.Combine(AppUtil.InstallDir, "lib"); // if netcore Linux

                if (!File.Exists(Path.Combine(path, ASSEMBLY_FILE)))
                    throw new FileNotFoundException("Could not find the assembly", path);

                assembly = System.Reflection.Assembly.LoadFrom(Path.Combine(path, ASSEMBLY_FILE));
            }
            catch (Exception)
            {
                // Every NCache assembly carries the same version stamp, so when the cache
                // assembly cannot be located (client-only or NuGet installations) report
                // the version of this assembly rather than failing.
                assembly = Assembly.GetExecutingAssembly();
            }

            return GetVersion(ReadVersionString(assembly));
        }

        /// <summary>
        /// Converts a raw version string into its display form.
        /// Accepts "5.3.603-rc1", "5.3.603" and legacy four-part "5.3.6.0".
        /// The numeric part is kept as-is and a pre-release label, if present, is appended
        /// in upper case: "5.3.603-rc1" becomes "5.3.603 RC1".
        /// </summary>
        public static string GetVersion(string version)
        {
            try
            {
                string numeric, preRelease;
                if (!TryParseVersion(version, out numeric, out preRelease))
                    return version;

                if (string.IsNullOrEmpty(preRelease))
                    return numeric;

                return string.Format("{0} {1}", numeric, preRelease.ToUpperInvariant());
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

        /// <summary>
        /// Returns the numeric components of the executing assembly's version concatenated into an
        /// integer, e.g. "5.3.603" gives 53603. This value is sent to the server as the client
        /// version, which compares it against 5000 to detect 5.x and later clients.
        /// The revision is appended only when it is actually present in the version string.
        /// </summary>
        public static int GetFormattedVersion(bool withRevision = false)
        {
            System.Reflection.Assembly assembly = Assembly.GetExecutingAssembly();
            string numeric, preRelease;
            TryParseVersion(ReadVersionString(assembly), out numeric, out preRelease);

            StringBuilder productVersion = new StringBuilder();
            productVersion.Append(ProductVersionType.Major);
            productVersion.Append(ProductVersionType.Minor);
            productVersion.Append(ProductVersionType.Build);

            if (withRevision && numeric != null && numeric.Split('.').Length > 3)
                productVersion.Append(ProductVersionType.Revision);

            return Int32.Parse(productVersion.ToString());
        }


        public static string GetVersionForNuGet()
        {
            string version = null;
            System.Reflection.Assembly assembly = null;
            try
            {
                var path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

#if NETCORE
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !File.Exists(Path.Combine(path, ASSEMBLY_FILE)))
                {
                    // On Linux the service runs from /bin/service while the cache assembly lives in /lib.
                    path = path.Replace("/bin/service", "/lib");
                }
#endif
                if (!File.Exists(Path.Combine(path, ASSEMBLY_FILE)))
                    throw new FileNotFoundException("Could not find the assembly", path);

                assembly = System.Reflection.Assembly.LoadFrom(Path.Combine(path, ASSEMBLY_FILE));
                version = ReadVersionString(assembly);
                return GetVersion(version);
            }
            catch (Exception)
            {
                // Same reasoning as GetVersion(): every NCache assembly carries the same stamp,
                // so fall back to this assembly instead of returning an empty or partial string.
                return GetVersion(ReadVersionString(Assembly.GetExecutingAssembly()));
            }
        }

        /// <summary>
        /// Reads the most specific version string an assembly carries. AssemblyInformationalVersion
        /// is preferred because it is the only attribute that carries the pre-release label
        /// (e.g. "5.3.603-rc1"); AssemblyFileVersion is used when it is absent.
        /// </summary>
        public static string ReadVersionString(System.Reflection.Assembly assembly)
        {
            try
            {
                var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (informational != null && !string.IsNullOrWhiteSpace(informational.InformationalVersion))
                    return informational.InformationalVersion.Trim();
            }
            catch (Exception)
            {
            }

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        /// <summary>
        /// Splits a version string into its numeric part and optional pre-release label, and
        /// populates ProductVersionType. Missing components are treated as zero, so both
        /// three-part ("5.3.603") and four-part ("5.3.6.0") strings are accepted.
        /// </summary>
        private static bool TryParseVersion(string version, out string numeric, out string preRelease)
        {
            numeric = null;
            preRelease = null;
            ProductVersionType.Major = ProductVersionType.Minor = ProductVersionType.Build = ProductVersionType.Revision = 0;

            if (string.IsNullOrWhiteSpace(version))
                return false;

            version = version.Trim();

            // Build metadata ("+abc123") is never part of the displayed version. It follows the
            // pre-release label when both are present ("5.3.603-rc1+abc123"), so drop it first.
            int plus = version.IndexOf('+');
            if (plus >= 0)
                version = version.Substring(0, plus);

            // "5.3.603-rc1" -> numeric "5.3.603", preRelease "rc1"
            int dash = version.IndexOf('-');
            if (dash >= 0)
            {
                numeric = version.Substring(0, dash);
                preRelease = version.Substring(dash + 1);
            }
            else
            {
                numeric = version;
            }

            string[] components = numeric.Split('.');
            if (components.Length == 0)
                return false;

            if (components.Length > 0) int.TryParse(components[0], out ProductVersionType.Major);
            if (components.Length > 1) int.TryParse(components[1], out ProductVersionType.Minor);
            if (components.Length > 2) int.TryParse(components[2], out ProductVersionType.Build);
            if (components.Length > 3) int.TryParse(components[3], out ProductVersionType.Revision);

            return true;
        }
    }
}
