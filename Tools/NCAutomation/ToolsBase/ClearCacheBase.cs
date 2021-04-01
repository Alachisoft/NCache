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
using Alachisoft.NCache.Tools.Common;
using System;
using System.Reflection;
using System.IO;
using System.Management.Automation;
using System.Collections.Generic;
using Alachisoft.NCache.Client;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Clear, "Cache")]
    public class ClearCacheBase : ClearCacheParameters, IConfiguration
    {
        const string ContentCacheGroup = "{03FAE478-9E87-47b8-B481-0832CDBF500D}";
        private string TOOLNAME = "ClearCache Tool";
        private bool isPowershell = false;

        public void ClearCacheTool()
        {
            try
            {

                if (!ValidateParameters()) return;
                ClearCache(Name, ForceClear);

            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine("Error: " + ex.Message);
                OutputProvider.WriteErrorLine(ex.ToString());
            }
        }


        public void ClearCache(string cacheId, bool forceClear)
        {
            ICache cache = null;

            try
            {
                CacheConnectionOptions cacheParams = new CacheConnectionOptions();

                cache = CacheManager.GetCache(cacheId.ToLower(), cacheParams);


                if (!ForceClear)
                {
                    long count = cache.Count;
                    OutputProvider.WriteLine("");
                    OutputProvider.WriteLine("\"" + cacheId + "\" cache currently has " + count + " items. ");
                    OutputProvider.WriteLine("Do you really want to clear it (Y or N)? ");
                    string response = string.Empty;
                    if (isPowershell)
                    {
                        ICollection<PSObject> resp = this.InvokeCommand.InvokeScript("Read-Host");

                        foreach (PSObject r in resp)
                        {
                            response = r.ToString();
                        }
                    }
                    else
                    {
                        response = Console.ReadLine();
                    }

                    if (response != "Y" && response != "y")
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
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Automation.Util.AssemblyResolver.GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Clear-Cache Cmdlet";
                isPowershell = true;
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
    }
}
