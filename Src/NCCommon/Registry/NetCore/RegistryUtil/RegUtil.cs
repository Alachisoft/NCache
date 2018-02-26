// Copyright (c) 2018 Alachisoft
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
// limitations under the License

using Alachisoft.NCache.Common.Registry.NetCore.DOM;
using Alachisoft.NCache.Common.XmlSerialization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Alachisoft.NCache.Common.Registry.NetCore.RegistryUtil
{
    public class RegUtil
    {
        private static string LicensePropertiesFile = "NCLicense.xml";
        private static string ForProductInfo = "SOFTWARE\\Alachisoft\\NCache";
        private static string ForUserInfo = "SOFTWARE\\Alachisoft\\NCache\\UserInfo";
        private static bool loaded = false;
        public static LicenseProperties LicenseProperties { set; get; }

        public static string GetRegPathForLinux()
        {
            return "//usr//lib//" + LicensePropertiesFile;
        }

        public static void LoadRegistry()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                LoadingRegistryElementsForWindows();
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var path = GetRegPathForLinux();

                if (loaded || !File.Exists(path))
                    return;

                LicenseProperties = XMLManager.ReadConfiguration<LicenseProperties>(path);
                    return;
                if(LicenseProperties.Product != null) LicenseProperties.Product.ProductName = "NCache";
            }
            else
            {
                throw new NotImplementedException();
            }
            loaded = true;
        }
        public static void Save()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                UpdatingRegistryElementsForWindows();
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                XMLManager.WriteConfiguration<LicenseProperties>(GetRegPathForLinux(), LicenseProperties);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        private static void LoadingRegistryElementsForWindows()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            if (LicenseProperties == null) { LicenseProperties = new LicenseProperties(); }
            if (LicenseProperties.Product == null) { LicenseProperties.Product = new Product(); }
            if (LicenseProperties.UserInfo == null) { LicenseProperties.UserInfo = new UserInfo(); }
            LicenseProperties.Product.ProductName = "NCache";
            //FOR PRODUCT
            using (RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@ForProductInfo, false))
            {
                if (regKey == null) return;
                foreach (var item in regKey.GetValueNames())
                {
                    dictionary.Add(item.ToString(), regKey.GetValue(item.ToString()) as string);
                }
            }

            if (dictionary.ContainsKey("DotNetInstallMode")) LicenseProperties.Product.DotNetInstallMode = dictionary["DotNetInstallMode"];
            if (dictionary.ContainsKey("Http.Port")) LicenseProperties.Product.HttpPort = dictionary["Http.Port"];
            if (dictionary.ContainsKey("InstallCode")) LicenseProperties.Product.InstallCode = dictionary["InstallCode"];
            if (dictionary.ContainsKey("InstallDir")) LicenseProperties.Product.InstallDir = dictionary["InstallDir"];
            if (dictionary.ContainsKey("LastReportTime")) LicenseProperties.Product.LastReportTime = dictionary["LastReportTime"];
            //if (dictionary.ContainsKey("NCacheTcp.Port")) LicenseProperties.Product.NCacheTcpPort = dictionary["NCacheTcp.Port"];
            if (dictionary.ContainsKey("Platform")) LicenseProperties.Product.Platform = dictionary["Platform"];
            if (dictionary.ContainsKey("SPVersion")) LicenseProperties.Product.SPVersion = dictionary["SPVersion"];
            if (dictionary.ContainsKey("Tcp.Port")) LicenseProperties.Product.TcpPort = dictionary["Tcp.Port"];
            if (dictionary.ContainsKey("act-voil")) LicenseProperties.Product.ActVoil = dictionary["act-voil"];

            //FOR USERINFO
            using (RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@ForUserInfo, false))
            {
                if (regKey == null) return;
                foreach (var item in regKey.GetValueNames())
                {
                    dictionary.Add(item.ToString(), regKey.GetValue(item.ToString()) as string);
                }
            }

            if (dictionary.ContainsKey("address")) LicenseProperties.UserInfo.Address = dictionary["address"];
            if (dictionary.ContainsKey("AuthCode")) LicenseProperties.UserInfo.AuthCode = dictionary["AuthCode"];
            if (dictionary.ContainsKey("city")) LicenseProperties.UserInfo.City = dictionary["city"];
            if (dictionary.ContainsKey("company")) LicenseProperties.UserInfo.Company = dictionary["company"];
            if (dictionary.ContainsKey("country")) LicenseProperties.UserInfo.Country = dictionary["country"];
            if (dictionary.ContainsKey("DeactivateCode")) LicenseProperties.UserInfo.DeactivateCode = dictionary["DeactivateCode"];
            if (dictionary.ContainsKey("email")) LicenseProperties.UserInfo.Email = dictionary["email"];
            if (dictionary.ContainsKey("ExtCode")) LicenseProperties.UserInfo.ExtCode = dictionary["ExtCode"] ?? string.Empty;
            if (dictionary.ContainsKey("firstname")) LicenseProperties.UserInfo.FirstName = dictionary["firstname"];
            if (dictionary.ContainsKey("lastname")) LicenseProperties.UserInfo.LastName = dictionary["lastname"];
            if (dictionary.ContainsKey("licenseKey")) LicenseProperties.UserInfo.LicenseKey = dictionary["licenseKey"];
            if (dictionary.ContainsKey("phone")) LicenseProperties.UserInfo.Phone = dictionary["phone"];
            if (dictionary.ContainsKey("state")) LicenseProperties.UserInfo.State = dictionary["state"];
            if (dictionary.ContainsKey("zip")) LicenseProperties.UserInfo.Zip = dictionary["zip"];

        }
        private static void UpdatingRegistryElementsForWindows()
        {
            if (LicenseProperties == null || LicenseProperties.Product == null || LicenseProperties.UserInfo == null)
            {
                //License Properties does not have required data
                return;
            }

            //For updating registry of Product
            using (RegistryKey key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@ForProductInfo))
            {
                if (key != null)  //must check for null key
                {
                    if (!string.IsNullOrEmpty(LicenseProperties.Product.DotNetInstallMode)) key.SetValue("DotNetInstallMode", LicenseProperties.Product.DotNetInstallMode);
                    key.SetValue("Http.Port", LicenseProperties.Product.HttpPort);
                    key.SetValue("InstallCode", LicenseProperties.Product.InstallCode);
                    key.SetValue("InstallDir", LicenseProperties.Product.InstallDir);
                    key.SetValue("LastReportTime", LicenseProperties.Product.LastReportTime ?? string.Empty);
                    //key.SetValue("NCacheTcp.Port", LicenseProperties.Product.NCacheTcpPort);
                    if (!string.IsNullOrEmpty(LicenseProperties.Product.Platform)) key.SetValue("Platform", LicenseProperties.Product.Platform);
                    if (!string.IsNullOrEmpty(LicenseProperties.Product.SPVersion)) key.SetValue("SPVersion", LicenseProperties.Product.SPVersion);
                    key.SetValue("Tcp.Port", LicenseProperties.Product.TcpPort);
                    if (!string.IsNullOrEmpty(LicenseProperties.Product.ActVoil)) key.SetValue("act-voil", LicenseProperties.Product.ActVoil);
                }
            }

            //For updating registry of User
            using (RegistryKey key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(ForUserInfo))
            {
                if (key != null)  //must check for null key
                {
                    key.SetValue("address", LicenseProperties.UserInfo.Address ?? string.Empty);
                    key.SetValue("AuthCode", LicenseProperties.UserInfo.AuthCode ?? string.Empty);
                    key.SetValue("city", LicenseProperties.UserInfo.City ?? string.Empty);
                    key.SetValue("company", LicenseProperties.UserInfo.Company ?? string.Empty);
                    key.SetValue("country", LicenseProperties.UserInfo.Country ?? string.Empty);
                    key.SetValue("DeactivateCode", LicenseProperties.UserInfo.DeactivateCode ?? string.Empty);
                    key.SetValue("email", LicenseProperties.UserInfo.Email ?? string.Empty);
                    key.SetValue("ExtCode", LicenseProperties.UserInfo.ExtCode ?? string.Empty);
                    key.SetValue("firstname", LicenseProperties.UserInfo.FirstName ?? string.Empty);
                    key.SetValue("lastname", LicenseProperties.UserInfo.LastName ?? string.Empty);
                    key.SetValue("licenseKey", LicenseProperties.UserInfo.LicenseKey ?? string.Empty);
                    key.SetValue("phone", LicenseProperties.UserInfo.Phone ?? string.Empty);
                    key.SetValue("state", LicenseProperties.UserInfo.State ?? string.Empty);
                    key.SetValue("zip", LicenseProperties.UserInfo.Zip ?? string.Empty);
                }
            }
        }
    }
}
