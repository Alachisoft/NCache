using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Alachisoft.NCache.SocketServer
{
    public class MetricServerFactory
    {
        private static OSInfo currentOS = OSInfo.Windows;
        private static IMetricServer s_metricServer = null;
        private static object metricServer = null;
#if NETCORE
        private static string path = AppUtil.InstallDir + "bin\\service\\";
#else
        private static string path = AppUtil.InstallDir + "bin\\assembly\\4.0\\";
#endif
        private static string componentsKeyName = "SOFTWARE\\Microsoft\\Active Setup\\Installed Components", componentName, version;
        private static bool isInstalled = false;

        public static IMetricServer GetMetricServer()
        {
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                currentOS = OSInfo.Linux;
#endif
            return s_metricServer;
        }

        private static void GetNETFrameworkVersion()
        {
            RegistryKey componentsKey = Registry.LocalMachine.OpenSubKey(componentsKeyName);
            string[] instComps = componentsKey.GetSubKeyNames();
            foreach (string instComp in instComps)
            {
                RegistryKey key = componentsKey.OpenSubKey(instComp);
                componentName = (string)key.GetValue(null); // Gets the (Default) value from this key
                if (componentName != null && componentName.IndexOf(".NET Framework") >= 0)
                {
                    version = (string)key.GetValue("Version");
                    if (version != null && version.Split(',').Length >= 4)
                    {
                        CheckVersion();
                        if (isInstalled)
                        {
                            break;
                        }
                    }
                }
            }

        }

        private static void CheckVersion()
        {
            Array.ConvertAll(version.Split(','), Double.Parse);
            if (version[0] >= 4 && version[1] >= 7 && version[2] >= 2)
            {
                isInstalled = true;

            }

            else isInstalled = false;

        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var requestedAssembly = new AssemblyName(args.Name);
            try
            {
                // Feel free to resolve any other assemblies, but this will take care of Annotations
                return requestedAssembly.Name == "Alachisoft.NCache.MetricServer"
                    ? Assembly.Load(requestedAssembly.Name)
                    : null;
            }
            catch
            {
            }

            return null;
        }
    }
}

