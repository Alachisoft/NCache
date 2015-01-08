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
using System.Collections;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;



namespace Alachisoft.NCache.Tools.StopCache
{
    /// <summary>
    /// Main application class
    /// </summary>
    internal class Application
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                StopCacheTool.Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public class StopCacheToolParam : Alachisoft.NCache.Tools.Common.CommandLineParamsBase
        {
            private string _server = string.Empty;

            private string cacheId = "";
            private int port = -1;
            private ArrayList caches = new ArrayList();

            public StopCacheToolParam()
            {

            }

            public ArrayList CacheIdArray
            {
                get { return caches; }
                set { caches = value; }
            }

            [ArgumentAttribute("", "")]
            public string CacheId
            {
                get { return cacheId; }
                set { caches.Add(value); }
            }

            [ArgumentAttribute(@"/s", @"/server")]
            public string Server
            {
                get { return _server; }
                set { _server = value; }
            }

            [ArgumentAttribute(@"/p", @"/port")]
            public int Port
            {
                get { return port; }
                set { port = value; }
            }
        }


        /// <summary>
        /// Summary description for StopCacheTool.
        /// </summary>
        private sealed class StopCacheTool
        {
            /// <summary> NCache service controller. </summary>
            private static NCacheRPCService NCache = new NCacheRPCService("");

            /// <summary> Cache ids specified at the command line. </summary>
            private static ArrayList s_cacheId = new ArrayList();

            private static StopCacheToolParam cParam = new StopCacheToolParam();


            /// <summary>
            /// Sets the application level parameters to those specified at the command line.
            /// </summary>
            /// <param name="args">array of command line parameters</param>
            private static bool ApplyParameters(string[] args)
            {

                NCache.Port = cParam.Port;
                NCache.ServerName = cParam.Server;

                if (cParam.Port == -1)
                    NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                s_cacheId = cParam.CacheIdArray;

                if (s_cacheId.Count == 0)
                {
                    Console.Error.WriteLine("Error: cache name not specified");
                    return false;
                }
                AssemblyUsage.PrintLogo(cParam.IsLogo);
                return true;
            }

            /// <summary>
            /// The main entry point for the tool.
            /// </summary>
            public static void Run(string[] args)
            {
                try
                {
                    object param = new StopCacheToolParam();
                    CommandLineArgumentParser.CommandLineParser(ref param, args);
                    cParam = (StopCacheToolParam) param;
                    if (cParam.IsUsage)
                    {
                        AssemblyUsage.PrintLogo(cParam.IsLogo);
                        AssemblyUsage.PrintUsage();
                        return;
                    }

                    if (!ApplyParameters(args)) return;

                    ICacheServer m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    string getBindIp = string.Empty;
                    if (m != null)
                    {
                        foreach (string cache in s_cacheId)
                        {
                            try
                            {
                                getBindIp = m.GetBindIP();
                                Console.WriteLine("\nStopping cache '{0}' on server {1}:{2}.", cache, getBindIp,
                                    NCache.Port);

                                m.StopCache(cache);
                                Console.WriteLine("'{0}' successfully stopped on server {1}:{2}.\n", cache, getBindIp,
                                    NCache.Port);
                            }
                               
                            catch (Exception e)
                            {
                                Console.Error.WriteLine("Failed to stop '{0}'. Error: {1} ", cache, e.Message);
                                Console.Error.WriteLine();
                                Console.Error.WriteLine(e.ToString());
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error : {0}", e.Message);

                    Console.Error.WriteLine();
                    Console.Error.WriteLine(e.ToString());
                }
                finally
                {
                    NCache.Dispose();
                }
            }
        }
    }
}
