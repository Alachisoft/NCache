//  Copyright (c) 2021 Alachisoft
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
using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common;

#if NETCORE
using Alachisoft.NCache.Licensing.NetCore.RegistryUtil;
using Alachisoft.NCache.Licensing.NetCore.DOM;

#endif
using Alachisoft.NCache.Licensing;
using System;
using System.Diagnostics;
using System.Management.Automation;
using System.IO;
using System.Reflection;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Management.Management;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Tools.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;
using static Alachisoft.NCache.Licensing.LicenseManager;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Get, "NCacheVersion")]
    public class VerifyLicenseBase : VerifyLicenseParameters, IConfiguration
    {
        private string TOOLNAME = "VerifyLicense Tool";
        NCacheRPCService NCache;

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

        public bool ValidateParameters()
        {

            return false;
        }

        public void VerifyLicense()
        {
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            string ipAddress = "this machine";
            ServerLicenseInfo serverLicenseInfo;

            try
            {
                if (string.IsNullOrEmpty(Server))
                {
                    serverLicenseInfo = new ServerLicenseInfo();
                }
                else
                {
                    NCache = new NCacheRPCService("");
                    NCache.Port = Port;
                    NCache.ServerName = Server;
                    ipAddress = Server;
                    ICacheServer nCacheServer = null;
                    nCacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    if (nCacheServer != null)
                    {
                        serverLicenseInfo = nCacheServer.GetServerLicenseInfo();
                    }
                    else
                    {
                        serverLicenseInfo = new ServerLicenseInfo();
                    }
                }

                OutputProvider.WriteLine("This product is registered to ");
                OutputProvider.WriteLine("User:        " + serverLicenseInfo._registeredName);
                OutputProvider.WriteLine("Email:       " + serverLicenseInfo._email);
                OutputProvider.WriteLine("Company:     " + serverLicenseInfo._companyName);
                OutputProvider.WriteLine("Edition:     " + "NCache OpenSource ");

                if (LicenseManager.LicenseMode(null) == LicenseManager.LicenseType.UnRegistered)
                {
                    OutputProvider.WriteLine("\nThe machine does not have a valid registration information. Please register this machine with a FREE installation key.You can get free installation key from http://www.alachisoft.com/activate/RequestKey.php?Edition=NC-OSS-50-4x&Version=5.0&Source=Register-NCache");

                    OutputProvider.WriteLine("\nIf you are using this machine as NCache client, then you don't need to register NCache on this machine. Only cache server machines are required to be registered");
                }
                else
                {
                    OutputProvider.WriteLine("");
                    OutputProvider.WriteLine("Licensed to use FREE of cost. Use As-is without support.");
                }
            }
            catch (Exception ex)
            {
                OutputProvider.WriteLine(ex.ToString());
                return;
            }

            OutputProvider.WriteLine("\n");
        }

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Automation.Util.AssemblyResolver.GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Get-NCacheVersion Cmdlet";
                VerifyLicense();
            }
            catch (System.Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

        protected override void ProcessRecord()
        {
            try { }
            catch { }

        }

    }
}
