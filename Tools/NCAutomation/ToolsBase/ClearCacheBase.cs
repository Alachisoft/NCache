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
using Alachisoft.NCache.Tools.Common;
using Alachisoft.NCache.Web.Caching;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Clear, "Cache")]
    public class ClearCacheBase : ClearCacheParameters, IConfiguration
    {
        const string ContentCacheGroup = "{03FAE478-9E87-47b8-B481-0832CDBF500D}";
        private string TOOLNAME = "ClearCache Tool";

        public void ClearCacheTool()
        {
            try
            {

                if (!ValidateParameters()) return;
                ClearCache(Name, ForceClear, false);

            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine("Error: " + ex.Message);
                OutputProvider.WriteErrorLine(ex.ToString());
            }
        }


        public void ClearCache(string cacheId, bool forceClear, bool webOnly)
        {
            Cache cache = null;

            try
            {
                CacheInitParams cacheParams = new CacheInitParams();
                

                cache = Web.Caching.NCache.InitializeCache(cacheId.ToLower(), cacheParams);


                if (!ForceClear)
                {
                    long count = cache.Count;
                    OutputProvider.WriteLine("");
                    OutputProvider.WriteLine("\"" + cacheId + "\" cache currently has " + count + " items. ");
                    OutputProvider.WriteLine("Do you really want to clear it (Y or N)? ");
                    ICollection<PSObject> response = this.InvokeCommand.InvokeScript("Read-Host");
                    string resp = string.Empty;
                    foreach (PSObject r in response)
                    {
                        resp = r.ToString();
                    }
                    if (resp != "Y" && resp != "y")
                    {
                        OutputProvider.WriteLine("");
                        OutputProvider.WriteLine("Cache not cleared.");
                        return;
                    }
                }

                cache.Clear();

                OutputProvider.WriteLine("");

              

                    OutputProvider.WriteLine("Cache cleared.");


            }
            catch (Exception e)
            {
                OutputProvider.WriteLine("Error: " + e.Message);
                OutputProvider.WriteErrorLine(e.ToString());
            }
            finally
            {
                if (cache != null)
                    cache.Dispose();
            }

        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

        public bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(Name))
            {
                OutputProvider.WriteErrorLine("\nError: Cache name not specified.");
                return false;
            }
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);

            return true;
        }

        protected override void BeginProcessing()
        {
#if NETCORE
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
#endif
            try
            {
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Clear-Cache Cmdlet";
                ClearCacheTool();
            }
            catch (Exception ex)
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
                final = System.IO.Path.Combine(bin, "service"); /// from where you need the assemblies
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
