//  Copyright (c) 2026 Alachisoft
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using Alachisoft.NCache.Licensing.DOM;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.XmlSerialization;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Licensing.RegistryUtil
{
    /// <summary>
    /// Reads how NCache is installed: the registry on Windows, ncinfo.xml on Linux. Licensing used to
    /// live here as well (license key and auth code from nclicense.xml, the registered user, the
    /// activation stub names, install code and evaluation-violation flags); the OpenSource edition has
    /// no licensing, so all of that is gone and only the installation details remain.
    /// </summary>
    public class RegUtil
    {
        private static string NCInfoFileOnLinux = "//usr//lib//ncinfo.xml";
        private static string ForProductInfo = @"SOFTWARE\Alachisoft\NCache";

        public static InstallProperties InstallProperties { set; get; }

        public static string GetInstallType()
        {
            string environmentInfo = InstallProperties.Product.InstallType;
            return InstallTypeUtil.GetInstallType(environmentInfo);
        }

        public static void LoadRegistry()
        {
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                LoadingRegistryElementsForWindows();
            }
#if NETCORE
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!File.Exists(NCInfoFileOnLinux))
                    return;

                InstallProperties = XMLManager.ReadConfiguration<InstallProperties>(NCInfoFileOnLinux);

                if (InstallProperties != null && InstallProperties.Product != null)
                    InstallProperties.Product.ProductName = "NCache";
            }
            else
            {
                throw new NotImplementedException();
            }
#endif
        }

        private static void LoadingRegistryElementsForWindows()
        {
            if (InstallProperties == null) { InstallProperties = new InstallProperties(); }
            if (InstallProperties.Product == null) { InstallProperties.Product = new Product(); }

            InstallProperties.Product.ProductName = "NCache";
            InstallProperties.Product.DotNetInstallMode = RegHelper.GetRegValue(ForProductInfo, "DotNetInstallMode", 0) as string;
            InstallProperties.Product.HttpPort = RegHelper.GetRegValue(ForProductInfo, "Http.Port", 0) as string;
            InstallProperties.Product.InstallDir = RegHelper.GetRegValue(ForProductInfo, "InstallDir", 0) as string;
            InstallProperties.Product.Platform = RegHelper.GetRegValue(ForProductInfo, "Platform", 0) as string;
            InstallProperties.Product.TcpPort = RegHelper.GetRegValue(ForProductInfo, "Tcp.Port", 0) as string;
            InstallProperties.Product.InstallType = RegHelper.GetRegValue(ForProductInfo, "InstallType", 0) as string;
            InstallProperties.Product.ScriptDirectory = RegHelper.GetRegValue(ForProductInfo, "ScriptDirectory", 0) as string;
        }

        public static string GetCompatibleInstallTypeFormatted()
        {
            var installType = GetInstallTypeOrFramework();
            if (!string.IsNullOrEmpty(installType))
                return $"({installType})";

            return installType;
        }

        public static string GetInstallTypeOrFramework()
        {
            string framework = string.Empty;
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                framework = (string)RegHelper.GetRegValue(RegHelper.ROOT_KEY, "Framework", 0);

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                framework = GetFrameworkLinux();
#else
            framework = (string)RegHelper.GetRegValue(RegHelper.ROOT_KEY, "Framework", 0);
#endif

            return framework;
        }

        private static string GetFrameworkLinux()
        {
            try
            {
                var installInfo = XMLManager.ReadConfiguration<InstallProperties>(NCInfoFileOnLinux);
                return installInfo.Product.Framework;
            }
            catch (FileNotFoundException)
            {
                return string.Empty;
            }
        }
    }
}
