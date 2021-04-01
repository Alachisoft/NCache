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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Management.Automation;
using System.Text;
using Alachisoft.NCache.Client;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsData.Export, "CacheKeys")]
    public class DumpCacheKeysBase : DumpCacheKeysParameters, IConfiguration
    {
        private string TOOLNAME = "DumpCacheKeys Tool";

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

        public void DumpCacheKeys()
        {
            try
            {
                if (!ValidateParameters()) return;
                CacheConnectionOptions cacheParams = new CacheConnectionOptions();

                ICache cache = CacheManager.GetCache(Name.ToLower(), cacheParams);
                //cache.ExceptionsEnabled = true;

                OutputProvider.WriteLine("Cache count:    " + cache.Count);
                IDictionaryEnumerator keys = (IDictionaryEnumerator)cache.GetEnumerator();

                if (keys != null)
                {
                    long index = 0;
                    bool checkFilter = (KeyFilter != "");
                    KeyFilter = KeyFilter.Trim();
                    while (keys.MoveNext())
                    {
                        if ((KeyCount > 0) && (index >= KeyCount))
                            break;

                        if (checkFilter == true)
                        {
                            string tmpKey = (string)keys.Key;

                            if (tmpKey.Contains(KeyFilter) == true)
                            {
                                OutputProvider.WriteLine(tmpKey);
                            }
                        }
                        else
                        {
                            OutputProvider.WriteLine(keys.Key);
                        }
                        index++;
                    }//end while 
                }//end if 
                cache.Dispose();
            }//end try block                
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error: " + e.Message);
                OutputProvider.WriteErrorLine(e.ToString());


            }
            OutputProvider.WriteLine(Environment.NewLine);
        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);

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
                TOOLNAME = "Export-CacheKeys Cmdlet";
                DumpCacheKeys();
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
