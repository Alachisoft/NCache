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
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.XmlSerialization;
using Alachisoft.NCache.Common.Util;
using UserInfo = Alachisoft.NCache.Licensing.DOM.UserInfo;
#if NETCORE
using System.Xml.Linq;
#endif

namespace Alachisoft.NCache.Licensing.RegistryUtil
{
    public class RegUtil
    {
        private static string NCInfoFileOnLinux = "//usr//lib//ncinfo.xml";
        private static string LicenseInfoFileOnLinux = "//usr//lib//nclicense.xml";
        private static string ForProductInfo = @"SOFTWARE\Alachisoft\NCache";
        private static string ForUserInfo = @"SOFTWARE\Alachisoft\NCache\UserInfo";

        #region -------- TLS -------------
        private static string TlsInfoFileOnLinux = "//config//tls.ncconf";
        private static string ForTLSInfo = @"SOFTWARE\Alachisoft\NCache\TLS";
        #endregion

        private static bool loaded = false;
        public static LicenseProperties LicenseProperties { set; get; }
        public static String WinStub { get { return "bncn.dat"; } }
        public static String LinuxStub { get { return ".bncn.so"; } }

        //IsolatedStorageDirectoryName should never be changed.
        public static String IsolatedStorageDirectoryName { get { return "5b971b45-20e0-475d-8584-fe40b9aa9f8c"; } }

        //ServerIsolatedStorageFileName should be changed in every public release. Use Powershell cmd "[guid]::NewGuid()" to generate a new file name.
        public static String ServerIsolatedStorageFileName { get { return "5f50fd6a-e6a3-463c-b00d-fc6ae5f585cd"; } }

        //ClientIsolatedStorageFileName should be changed in every public release. Use Powershell cmd "[guid]::NewGuid()" to generate a new file name.
        public static String ClientIsolatedStorageFileName { get { return "447e7572-3168-4f1e-b235-40931eedceb4"; } }

        public static int ServerVersionId { get { return 78; } }
        public static int ClientVersionId { get { return 79; } }

        public static int ProductVersion { get { return Alachisoft.NCache.Common.Monitoring.Version.GetFormattedVersion(); } }
        public static int ServerEditionId { get { return 80; } }
        public static int ClientEditionId { get { return 81; } }

        public static string GetInstallType()
        {
            string environmentInfo = LicenseProperties.Product.InstallType;
            return InstallTypeUtil.GetInstallType(environmentInfo);
        }


        public static string GetLicenseInfoPathForLinux()
        {
            return "/usr/lib/" + LicenseInfoFileOnLinux;
        }

        public static string CloudRegistryInfo
        {
            get
            {
#if NETCORE
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    return RegHelper.GetRegValue(ForUserInfo, "cl-id", 0) as string;
                }
#if NETCORE
                else
                {
                    return LoadLicenseInfoOnLinux("cl-id");
                }

#endif
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException();
#if NETCORE
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                    {
                        using (RegistryKey key = Registry.LocalMachine.CreateSubKey(ForUserInfo))
                        {
                            if (key != null)  //must check for null key
                            {
                                key.SetValue("cl-id", value);
                            }
                        }
                    }
            }
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

                LicenseProperties = XMLManager.ReadConfiguration<LicenseProperties>(NCInfoFileOnLinux);

                if (LicenseProperties == null)
                    return;

                if (LicenseProperties.Product != null)
                {

                    LicenseProperties.Product.ProductName = "NCache";
                    LicenseProperties.Product.ScriptDirectory = LoadLicenseInfoOnLinux("ScriptDirectory");
                }

                LicenseProperties.UserInfo.AuthCode = LoadLicenseInfoOnLinux("auth-code");
                LicenseProperties.UserInfo.LicenseKey = LoadLicenseInfoOnLinux("license-key");
                LicenseProperties.UserInfo.CloudUrl = LoadLicenseInfoOnLinux("cloud-url");
            }
            else
            {
                throw new NotImplementedException();
            }
#endif
        }

        public static void Save()
        {

#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                UpdatingRegistryElementsForWindows();
            }

#if NETCORE
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                XMLManager.WriteConfiguration<LicenseProperties>(NCInfoFileOnLinux, LicenseProperties);
                File.WriteAllLines(NCInfoFileOnLinux, File.ReadAllLines(NCInfoFileOnLinux)); //To Remove BOM characters from XML file
                SaveLicenseInfoOnLinux(LicenseProperties.UserInfo.LicenseKey, LicenseProperties.UserInfo.AuthCode,LicenseProperties.UserInfo.CloudId);
            }
            else
            {
                throw new NotImplementedException();
            }

#endif
        }

        public static string GetInstallCode()
        {
            if (LicenseProperties == null || LicenseProperties.Product == null)
                return "";

            return LicenseProperties.Product.InstallCode = LicenseProperties.Product.InstallCode ?? string.Empty;
        }

#if NETCORE
        private static string LoadLicenseInfoOnLinux(string element)
        {
            try
            {
                if (File.Exists(LicenseInfoFileOnLinux))
                {
                    var doc = XDocument.Load(LicenseInfoFileOnLinux);
                    if (doc.Root.Element(element) != null)
                        return doc.Root.Element(element).Value;
                    return "";
                }
            }
            catch { }

            return "";
        }
        public static void SaveLicenseInfoOnLinux(string licenseKey, string authCode, string clid = "")
        { 
			if(string.IsNullOrEmpty(clid))
				clid="";

            if (File.Exists(LicenseInfoFileOnLinux))
            {
                var doc = XDocument.Load(LicenseInfoFileOnLinux);
                var key = doc.Root.Element("license-key");
                var auth = doc.Root.Element("auth-code");
                var cl = doc.Root.Element("cl-id");
				
				if (cl==null)
                {
                    doc.Root.Add(new XElement("cl-id",clid));
                }
                else
                {
                    cl.Value = clid;
                }

                key.Value = licenseKey;
                auth.Value = authCode;

                doc.Save(LicenseInfoFileOnLinux);
            }
            else
            {
                new XDocument(
                    new XElement("license-config",
                    new XElement("license-key", licenseKey),
                    new XElement("auth-code", authCode),
                    new XElement("cl-id", clid))).Save(LicenseInfoFileOnLinux);
                File.WriteAllLines(LicenseInfoFileOnLinux, File.ReadAllLines(LicenseInfoFileOnLinux)); //To Remove BOM characters from XML file
            }
        }

#endif
        private static void LoadingRegistryElementsForWindows()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            if (LicenseProperties == null) { LicenseProperties = new LicenseProperties(); }
            if (LicenseProperties.Product == null) { LicenseProperties.Product = new Product(); }
            if (LicenseProperties.UserInfo == null) { LicenseProperties.UserInfo = new UserInfo(); }

            LicenseProperties.Product.ProductName = "NCache";
            //FOR PRODUCT
            LicenseProperties.Product.DotNetInstallMode = RegHelper.GetRegValue(ForProductInfo, "DotNetInstallMode", 0) as string;
            var installCode = RegHelper.GetRegValue(@ForProductInfo, "InstallCode", 0) as string;
            LicenseProperties.Product.HttpPort = RegHelper.GetRegValue(@ForProductInfo, "Http.Port", 0) as string;
            

            LicenseProperties.UserInfo.LicenseKey = RegHelper.GetRegValue(ForUserInfo, "licenseKey", 0) as string;
            LicenseProperties.UserInfo.DeactivateCode = RegHelper.GetRegValue(ForUserInfo, "DeactivateCode", 0) as string;
            LicenseProperties.UserInfo.AuthCode = RegHelper.GetRegValue(ForUserInfo, "AuthCode", 0) as string;

            LicenseProperties.Product.InstallDir = RegHelper.GetRegValue(ForProductInfo, "InstallDir", 0) as string;
            LicenseProperties.Product.LastReportTime = RegHelper.GetRegValue(ForProductInfo, "LastReportTime", 0) as string;
            LicenseProperties.Product.Platform = RegHelper.GetRegValue(ForProductInfo, "Platform", 0) as string;
            LicenseProperties.Product.TcpPort = RegHelper.GetRegValue(ForProductInfo, "Tcp.Port", 0) as string;
            LicenseProperties.Product.ActVoil = RegHelper.GetRegValue(ForProductInfo, "act-voil", 0) as string;
            LicenseProperties.Product.VoilDate = RegHelper.GetRegValue(ForProductInfo, "voil-date", 0) as string;
            LicenseProperties.Product.InstallType = RegHelper.GetRegValue(ForProductInfo, "InstallType", 0) as string;
            LicenseProperties.Product.ScriptDirectory = RegHelper.GetRegValue(ForProductInfo, "ScriptDirectory", 0) as string;

            LicenseProperties.UserInfo.FirstName = RegHelper.GetRegValue(ForUserInfo, "firstName", 0) as string;
            LicenseProperties.UserInfo.LastName = RegHelper.GetRegValue(ForUserInfo, "lastName", 0) as string;
            LicenseProperties.UserInfo.Company = RegHelper.GetRegValue(ForUserInfo, "company", 0) as string;
            LicenseProperties.UserInfo.Address = RegHelper.GetRegValue(ForUserInfo, "address", 0) as string;
            LicenseProperties.UserInfo.Zip = RegHelper.GetRegValue(ForUserInfo, "zip", 0) as string;
            LicenseProperties.UserInfo.City = RegHelper.GetRegValue(ForUserInfo, "city", 0) as string;
            LicenseProperties.UserInfo.State = RegHelper.GetRegValue(ForUserInfo, "state", 0) as string;
            LicenseProperties.UserInfo.Country = RegHelper.GetRegValue(ForUserInfo, "country", 0) as string;
            LicenseProperties.UserInfo.Phone = RegHelper.GetRegValue(ForUserInfo, "phone", 0) as string;
            LicenseProperties.UserInfo.Email = RegHelper.GetRegValue(ForUserInfo, "email", 0) as string;
            LicenseProperties.UserInfo.ExtCode = RegHelper.GetRegValue(ForUserInfo, "ExtCode", 0) as string;
            LicenseProperties.UserInfo.CloudId = RegHelper.GetRegValue(ForUserInfo, "cl-id", 0) as string;
            LicenseProperties.UserInfo.CloudUrl = RegHelper.GetRegValue(ForUserInfo, "cloud-url", 0) as string;
            LicenseProperties.UserInfo.TrialKey = RegHelper.GetRegValue(ForUserInfo, "trialkey", 0) as string;

          

        }

  

        private static void UpdatingRegistryElementsForWindows()
        {
            if (LicenseProperties == null || LicenseProperties.Product == null || LicenseProperties.UserInfo == null)
            {
                //License Properties does not have required data
                return;
            }

            //For updating registry of Product
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@ForProductInfo))
            {
                if (key != null)  //must check for null key
                {
                    if (!string.IsNullOrEmpty(LicenseProperties.Product.DotNetInstallMode)) key.SetValue("DotNetInstallMode", LicenseProperties.Product.DotNetInstallMode);
                    key.SetValue("Http.Port", LicenseProperties.Product.HttpPort);
                    key.SetValue("InstallCode", LicenseProperties.Product.InstallCode);
                    key.SetValue("InstallDir", LicenseProperties.Product.InstallDir);
                    key.SetValue("LastReportTime", LicenseProperties.Product.LastReportTime ?? string.Empty);
                    if (!string.IsNullOrEmpty(LicenseProperties.Product.Platform)) key.SetValue("Platform", LicenseProperties.Product.Platform);
                    if (!string.IsNullOrEmpty(LicenseProperties.Product.SPVersion)) key.SetValue("SPVersion", LicenseProperties.Product.SPVersion);
                    key.SetValue("Tcp.Port", LicenseProperties.Product.TcpPort);
                    if (string.IsNullOrEmpty(LicenseProperties.Product.ActVoil))
                    {
                        string actVoilValue = RegHelper.GetRegValue(ForProductInfo, "act-voil", 0) as string;
                        if (!string.IsNullOrEmpty(actVoilValue))
                            key.DeleteValue("act-voil");

                        string actVoilDate = RegHelper.GetRegValue(ForProductInfo, "voil-date", 0) as string;
                        if (!string.IsNullOrEmpty(actVoilDate))
                            key.DeleteValue("voil-date");
                    }
                    else
                    {
                        key.SetValue("act-voil", LicenseProperties.Product.ActVoil);

                        if (!string.IsNullOrEmpty(LicenseProperties.Product.VoilDate))
                            key.SetValue("voil-date", LicenseProperties.Product.VoilDate);
                    }

                    if (!string.IsNullOrEmpty(LicenseProperties.Product.InstallType)) key.SetValue("InstallType", LicenseProperties.Product.InstallType);
                }

            }
        

            //For updating registry of User
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(ForUserInfo))
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
                    key.SetValue("cl-id", LicenseProperties.UserInfo.CloudId ?? string.Empty);
                    key.SetValue("cloud-subscription-id", LicenseProperties.UserInfo.CloudSubscriptionID ?? string.Empty);
                    key.SetValue("trialkey", LicenseProperties.UserInfo.TrialKey ?? string.Empty);

                }
            }
        }


        public static void UpdateInstallCodeForWindows(string installcode)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(ForProductInfo))
                {
                    if (key != null)
                    {
                        if (!string.IsNullOrEmpty(installcode)) key.SetValue("InstallCode", installcode);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        public static string ReadInstallType()
        {
            if (!string.IsNullOrEmpty(LicenseProperties.Product.Edition))
            {
                var editionParts = LicenseProperties.Product.Edition.Split('-');
                if (editionParts.Length > 2)
                {
                    return editionParts[3];
                }
            }
            return "";
        }
        public static string ReadInstallCode()
        {
            var installCode = RegHelper.GetRegValue(@ForProductInfo, "InstallCode", 0) as string;
            return installCode;
        }
        public static string GetCompatibleInstallTypeFormatted()
        {
            var installType = GetInstallTypeOrFramework();
            if (!string.IsNullOrEmpty(installType))
                return $"({installType})";

            return installType;
        }

        public static string GetCompatibleInstallType()
        {
            string installationType = GetInstallTypeOrFramework();

            if (string.IsNullOrEmpty(installationType))
                return "";


            switch (installationType)
            {
                case ".NET 6.0":
                    installationType = ".NET Core";
                    break;

                case ".NET 8.0":
                    installationType = ".NET Core";
                    break;

                case ".NET 4.8":
                    installationType = ".NET Framework";
                    break;
                default:
                    installationType = "";
                    break;
            }

            return installationType;
        }

        public static string GetInstallTypeOrFramework()
        {
            string framework = string.Empty;

            if (!string.IsNullOrEmpty(framework))
                return framework;
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
                var licenseInfo = XMLManager.ReadConfiguration<LicenseProperties>(NCInfoFileOnLinux);
                return licenseInfo.Product.Framework;
            }
            catch (FileNotFoundException e)
            {
                return string.Empty;
            }
        }
        #region ------ TLS --------
        static bool GetValueFromRegistryAsBoolean(string key)
        {
            if (TryGetKeyValueFromRegistry(key, out int value))
                return value == 1;

            return false;
        }

        private static bool TryGetKeyValueFromRegistry(string key, out int value)
        {
            return int.TryParse(RegHelper.GetRegValue(RegHelper.SSL_KEY, key, 0) as string, out value);
        }

#if NETCORE
        private static bool IsEmptyFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return true;

            string fileContent = File.ReadAllText(filePath);
            return string.IsNullOrEmpty(fileContent);

        }
        private static void AddTagsInTlsConfigFile(string path)
        {
            if (path == null)
                return;

            XDocument xmlDoc = new XDocument();

            XElement tlsConfigTags = PrepareXmlTagsForTlsConfig();

            xmlDoc.Add(tlsConfigTags);
            xmlDoc.Save(path);

        }

        private static XElement PrepareXmlTagsForTlsConfig()
        {
            XElement rootElement = new XElement("tls-info");

            rootElement.Add(new XElement("enable", false));
            rootElement.Add(new XElement("server-certificate-cn", "server-certificate-cn"));
            rootElement.Add(new XElement("server-certificate-thumbprint", "your-thumbprint"));
            rootElement.Add(new XElement("protocol-version", "protocol-version"));

            return rootElement;
        }

        public static void UpdatingRegistryTLSElementsForLinux(string certificateName, string certificateThumbprint, string clientCertificateName, string clientCertificateThumbprint, bool enable, bool enableClientServerTLS, bool requireClientCert, bool requireServerCert, bool enableServerToServerTLS, bool enableBridgeTLS, bool forserverinstallation, string pfxPath, string pfxPassword, string protocolVersion)
        {
            try
            {
                var pathOfConfig = AppUtil.InstallDir + TlsInfoFileOnLinux;

                if (IsEmptyFile(pathOfConfig))
                    AddTagsInTlsConfigFile(pathOfConfig);

                if (File.Exists(pathOfConfig))
                {
                    var doc = XDocument.Load(pathOfConfig);
                    var certName = doc.Root.Element("server-certificate-cn");
                    certName.Value = certificateName;
                    var thumbPrint = doc.Root.Element("server-certificate-thumbprint");
                    thumbPrint.Value = certificateThumbprint;
                    var register = doc.Root.Element("enable");
                    register.Value = enable.ToString().ToLower();

                    var pfxPathElem = doc.Root.Element("pfx-path");
                    pfxPathElem.Value = pfxPath ?? "";
                    var pfxPasswordElem = doc.Root.Element("pfx-password");
                    pfxPasswordElem.Value = pfxPassword ?? "";

                    var protoclVersion = doc.Root.Element("protocol-version");
                    protoclVersion.Value = protocolVersion ?? "";
                    doc.Save(pathOfConfig);


                }
            }
            catch (Exception)
            {

                throw;
            }

        }
        public static void UnRegisterTLSElementsForLinux()
        {
            try
            {
                var pathOfConfig = AppUtil.InstallDir + TlsInfoFileOnLinux;
                if (File.Exists(pathOfConfig))
                {
                    var doc = XDocument.Load(pathOfConfig);
                    var register = doc.Root.Element("enable");
                    register.Value = "false";
                    doc.Save(pathOfConfig);
                }
            }
            catch (Exception)
            {
                throw;
            }

        }
#endif
        public static void UpdatingRegistryTLSElementsForWindows(string certificateName, string certificateThumbprint, string clientCertificateName, string clientCertificateThumbprint, bool enable, bool enableClientServerTLS, bool requireClientCert, bool requireServerCert, bool enableServerToServerTLS, bool enableBridgeTLS, bool forserverinstallation, string pfxPath, string pfxPassword, string protocolVersion)
        {
            try
            {

                if (LicenseProperties == null || LicenseProperties.Product == null || LicenseProperties.UserInfo == null)
                {
                    //License Properties does not have required data
                    return;
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(ForTLSInfo))
                {
                    if (key != null)
                    {

                        if (!string.IsNullOrEmpty(certificateName)) key.SetValue("ServerCertificateCN", certificateName);

                        if (!string.IsNullOrEmpty(certificateThumbprint))
                            key.SetValue("ServerCertificateThumbprint", certificateThumbprint);


                        key.SetValue("Enable", enable, RegistryValueKind.DWord);

                        key.SetValue("PFXPath", pfxPath ?? "");
                        key.SetValue("PFXPassword", pfxPassword ?? "");

                        key.SetValue("ProtocolVersion", protocolVersion ?? "");

                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public static void UnRegisterTLSElementsForWindows()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(ForTLSInfo))
                {

                    key.SetValue("Enable", false, RegistryValueKind.DWord);

                }
            }
            catch (Exception)
            {
                throw;
            }

        }

#endregion
    }
}
