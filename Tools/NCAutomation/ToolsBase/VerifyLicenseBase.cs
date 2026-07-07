using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common;

#if NETCORE
using Alachisoft.NCache.Licensing.RegistryUtil;
using Alachisoft.NCache.Licensing.DOM;
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
using Alachisoft.NCache.Licensing.RegistryUtil;

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


                
                string[] editionIDPart = null;
                if (!string.IsNullOrEmpty(serverLicenseInfo._editionID))
                    editionIDPart = serverLicenseInfo._editionID.Split('-');

                OutputProvider.WriteLine("This product is registered to ");
                OutputProvider.WriteLine("User:                    " + serverLicenseInfo._registeredName);
                OutputProvider.WriteLine("Email:                   " + serverLicenseInfo._email);
                OutputProvider.WriteLine("Company:                 " + serverLicenseInfo._companyName);

                if (!serverLicenseInfo.HideOperatingSystem)
                    DisplayPlatform(serverLicenseInfo);

                OutputProvider.WriteLine("Edition:                 " + "OpenSource ");
                OutputProvider.WriteLine("");
                OutputProvider.WriteLine("Licensed to use FREE of cost. Use As-is without support.");
                
            }


            catch (NullReferenceException ex)
            {
                OutputProvider.WriteErrorLine("Couldn't find NCache installation on this machine.");
                return;
            }
            catch (Exception ex)
            {
                OutputProvider.WriteLine(ex.ToString());
                return;
            }

            OutputProvider.WriteLine("\n");
        }

       private void DisplayPlatform(ServerLicenseInfo serverLicenseInfo)
        {
            string outputResult = null;
            if (!string.IsNullOrEmpty(serverLicenseInfo.GetOS) || !string.IsNullOrEmpty(serverLicenseInfo.InstallationType))
            {
                outputResult = "Platform:               ";
            }
            outputResult = outputResult + " " + serverLicenseInfo.GetOS;
            outputResult = outputResult + " " + RegUtil.GetInstallTypeOrFramework().ToUpper();
            if (!string.IsNullOrEmpty(outputResult))
                OutputProvider.WriteLine(outputResult);

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
