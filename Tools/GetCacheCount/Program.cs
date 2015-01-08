// Copyright (c) 2015 Alachisoft
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


using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Web.Caching;
using System.Globalization;

namespace Alachisoft.NCache.Tools.GetCacheCount
{
    /// <summary>
    /// Main application class
    /// </summary>
    class Application
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                GetCacheCountTool.Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
    /// <summary>
    /// Summary description for GetCacheCountTool.
    /// </summary>
    sealed class GetCacheCountTool
    {        
        static public void Run(string[] args)
        {
            ToolArgs toolArgs;
            if (!ApplyParameters(args,out toolArgs)) return;
            
            try
            {
                Console.WriteLine(" ");
                using (Cache cache = NCache.Web.Caching.NCache.InitializeCache(toolArgs.Cache, GetInitParams(ref toolArgs))) 
                {
                    Console.WriteLine("Cache item count: {0}", cache.Count);
                    cache.Dispose();
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to Initialize cache '{0}' .",toolArgs.Cache);
                Console.Error.WriteLine("Error :- " + e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.ToString());
            }
        }
      	
        static private bool ApplyParameters(string[] args, out ToolArgs toolArgs)
        {
            bool usage = false;
            bool printLogo = true;
            bool portspecified = false;
            toolArgs = new ToolArgs();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower(CultureInfo.InvariantCulture))
                {
                    case @"/?":
                        usage = true;
                        break;

                    case @"/G":
                    case @"/nologo":
                        printLogo = false;
                        break;

                    case @"/s":
                    case @"/server-name":
                        if (i + 1 < args.Length)
                            toolArgs.Server = args[++i];
                        break;

                    case @"/p":
                        if (i + 1 < args.Length)
                        {
                            try
                            {
                                toolArgs.Port = Int32.Parse(args[++i], null);
                                portspecified = true;
                            }
                            catch (FormatException) { }
                            catch (OverflowException) { }
                        }
                        break;


                    default:
                        if (toolArgs.Cache == null || toolArgs.Cache == string.Empty)
                            toolArgs.Cache = args[i];
                        break;
                }
            }
            if (!portspecified) toolArgs.Port = 0;

            AssemblyUsage.PrintLogo(printLogo);
            if (usage)
            {
                if (!printLogo)
                    Console.WriteLine();
                AssemblyUsage.PrintUsage();
                return false;
            }

            if (toolArgs.Cache == null || toolArgs.Cache == string.Empty)
            {
                Console.Error.WriteLine("Error: Cache name not specified.");
                return false;
            }
            return true;
        }

        private static CacheInitParams GetInitParams(ref ToolArgs toolArgs)
        {
            CacheInitParams initParams = new CacheInitParams();
            string ip = "";
            if (string.IsNullOrEmpty(toolArgs.Server))
            { return initParams; }
            else
            {
                int port = toolArgs.Port != 0 ? toolArgs.Port : 9800;
                CacheServerInfo[] cacheServerInfos = new CacheServerInfo[1];
                cacheServerInfos[0] = new CacheServerInfo(ip, port);
                initParams.ServerList = cacheServerInfos;
                return initParams;
            }
        }
    }
}
