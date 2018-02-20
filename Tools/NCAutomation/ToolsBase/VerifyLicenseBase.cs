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
// limitations under the License.

using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common;
using System;
using System.Management.Automation;
using System.IO;
using System.Reflection;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Get, "NCacheVersion")]
    public class VerifyLicenseBase : VerifyLicenseParameters, IConfiguration
    {
        private string TOOLNAME = "VerifyLicense Tool";
        bool isClient = false;
        public void InitializeCommandLinePrameters(string[] args)
        {

        }

        public bool ValidateParameters()
        {

            return false;
        }

        public void VerifyLicense()
        {
            try
            {
                ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);

                PrintUserInfo(OutputProvider);
                OutputProvider.WriteLine("\n");

                OutputProvider.WriteLine("Edition Installed: NCache 4.9 OpenSource Edition.\n");
                OutputProvider.WriteLine("Licensed to use FREE of cost. Use As-is without support.\n");
            }
            catch (Exception ex)
            {

                OutputProvider.WriteLine(ex.ToString());
                return;
            }
        }

        private static void PrintUserInfo(IOutputConsole OutputProvider )
        {

            string USER_KEY = RegHelper.ROOT_KEY + @"\UserInfo";
            string firstName = (string)RegHelper.GetRegValue(USER_KEY, "firstname", 0);
            string lastName = (string)RegHelper.GetRegValue(USER_KEY, "lastname", 0);
            string company = (string)RegHelper.GetRegValue(USER_KEY, "company", 0);
            string email = (string)RegHelper.GetRegValue(USER_KEY, "email", 0);

            OutputProvider.WriteLine("This product is registered to \nUser\t:\t" + firstName + " " + lastName + "\nEmail\t:\t" + email + "\nCompany\t:\t" + company);

        }

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
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
#if NETCORE
        private static System.Reflection.Assembly GetAssembly(object sender, ResolveEventArgs args)
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
                string installDir = directoryInfo.Parent.FullName; //linux install directory
                directoryInfo = Directory.GetParent(installDir); //go back one directory
                installDir = directoryInfo.FullName;
                final = Path.Combine(installDir, "lib");
            }
            return System.Reflection.Assembly.LoadFrom(Path.Combine(final, new AssemblyName(args.Name).Name + ".dll"));
        }
#endif
    }
}
