// Copyright (c) 2017 Alachisoft
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
using System.Data;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Xml.Schema;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;

using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System.Configuration;
using Alachisoft.NCache.Config.Dom;


namespace Alachisoft.NCache.Tools.StartCache
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
				StartCacheTool.Run(args);
			}
			catch(Exception e)
			{
                Console.Error.WriteLine(e); 
			}
		}
	}

    public class StartCacheToolParam:Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string _cacheId = "";
        private ArrayList caches = new ArrayList();
        private string _server = string.Empty;
        private int port = -1;

        public ArrayList CacheIdArray
        {
            get { return caches; }
            set { caches = value; }
        }

        [ArgumentAttribute(@"", @"")]
        public string CacheId
        {
            get { return _cacheId; }
            set { caches.Add(value); }
        }


        [ArgumentAttribute(@"/s", @"/server")]
        public  string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        [ArgumentAttribute(@"/p", @"/port")]
        public  int Port
        {
            get { return port; }
            set { port = value; }
        }
    }

	/// <summary>
	/// Summary description for StartCacheTool.
	/// </summary>
	sealed class StartCacheTool
	{
		/// <summary> NCache service controller. </summary>
        static private NCacheRPCService NCache;
        static private StartCacheToolParam cParam = new StartCacheToolParam();
		/// <summary> Cache ids specified at the command line. </summary>
		static private ArrayList s_cacheId = new ArrayList();



        /// <summary>
		/// Sets the application level parameters to those specified at the command line.
		/// </summary>
		/// <param name="args">array of command line parameters</param>
		static private bool ApplyParameters(string[] args)
		{

            if (cParam.Server != null && cParam.Server != string.Empty)
                NCache.ServerName = cParam.Server;
            else
            {
                NCache.ServerName=System.Environment.MachineName; 
            }
            NCache.Port = cParam.Port;
           
			if(cParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort:CacheConfigManager.HttpPort;
            s_cacheId = cParam.CacheIdArray;
		
			if(s_cacheId.Count == 0)
			{
				Console.Error.WriteLine("Error: cache name not specified.");
				return false;
            }
            AssemblyUsage.PrintLogo(cParam.IsLogo);
            return true;
		}


		/// <summary>
		/// The main entry point for the tool.
		/// </summary>
		static public void Run(string[] args)
		{
            NCache = new NCacheRPCService("");
            string cacheIp = string.Empty;

            try
            {
                object param = new StartCacheToolParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (StartCacheToolParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }
                
                if (!ApplyParameters(args)) return;
                ICacheServer m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                CacheServerConfig config = null;
                cacheIp = m.GetClusterIP();
                if (m != null)
                {
                  
                    foreach (string cache in s_cacheId)
                    {
                        try
                        {
                            config = m.GetCacheConfiguration((string)cache);
                            if (config != null && config.InProc)
                            {
                                throw new Exception("InProc caches cannot be started explicitly.");
                            }
                           
                            Console.WriteLine("\nStarting cache '{0}' on server {1}:{2}.", cache, cacheIp, NCache.Port);
                            m.StartCache(cache, string.Empty);

                            Console.WriteLine("'{0}' successfully started on server {1}:{2}.\n", cache, cacheIp,
                                NCache.Port);
                        }

                        catch (Exception e)
                        {
                            Console.Error.WriteLine("Failed to start '{0}' on server {1}.", cache,
                                cacheIp);
                            Console.Error.WriteLine();
                            Console.Error.WriteLine(e.ToString() + "\n");
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                Console.Error.WriteLine("Error : {0}", "NCache service could not be contacted on server.");
                Console.Error.WriteLine();
                Console.Error.WriteLine(ex.ToString());
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
