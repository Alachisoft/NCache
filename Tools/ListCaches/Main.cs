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
#define PRINT_URI1

using System;
using System.Data;
using System.Collections;
using System.Globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;


namespace Alachisoft.NCache.Tools.ListCaches
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
                ListCachesTool.Run(args);
			}
			catch(Exception e)
			{
                Console.Error.WriteLine(e);
			}
		}
	}
    public class ListCachesParam : Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string _server = string.Empty;
        private int _port=-1;
        private bool _detailed, _printConf,_xmlSyntax;
        [ArgumentAttribute(@"/s", @"/server")]
        public string Server { get { return _server; } set { _server = value; } }
        [ArgumentAttribute(@"/p", @"/port")]
        public int Port { get { return _port; } set { _port = value; } }
        [ArgumentAttribute(@"/a", @"/detail",false)]
        public bool Detail { get { return _detailed; } set { _detailed = value; } }
        public bool XmlSyntax { get { return _xmlSyntax; } }
        public bool PrintConf { get { return _printConf; } }

    }

	/// <summary>
	/// Summary description for ListCachesTool.
	/// </summary>
	sealed class ListCachesTool
	{
		/// <summary> NCache service controller. </summary>
		static private NCacheRPCService	NCache = new NCacheRPCService("");
		/// <summary> Detailed information flag, specified at the command line. </summary>
		static private bool				detailed;
		/// <summary> Print configuration flag, specified at the command line. </summary>
		static private bool				printConf;
		/// <summary> xml configuration syntax flag, specified at the command line. </summary>
		static private bool				xmlSyntax;
        /// <summary>arguments passed to ListCache </summary>
        static private ListCachesParam cParam = new ListCachesParam();


        static private void PrintCacheInfo(CacheStatistics statistics, string partId, string cacheName, bool isRunning)
        {

            CacheStatistics s = statistics;
            string schemeName = s.ClassName.ToLower(CultureInfo.CurrentCulture);
            string running = isRunning ? "Running" : "Stopped";

            Console.WriteLine("{0,-20} {1,-15} {2,-30} {3,-5}", cacheName, partId, schemeName, running);
        }
        static private void PrintCacheInfo(Cache cache, string partId)
        {

            CacheStatistics s = cache.Statistics;
            string schemeName = s.ClassName.ToLower(CultureInfo.CurrentCulture);
            string running = cache.IsRunning ? "Running" : "Stopped";

            Console.WriteLine("{0,-20} {1,-15} {2,-30} {3,-5}", cache.Name, partId, schemeName, running);
        }
        static private void PrintDetailedCacheInfo(CacheStatistics statistics, string partId, bool printConf, bool xmlSyntax, bool isRunning, string cacheName, String configString)
        {
            CacheStatistics s = statistics;
            long MaxSize = 0;
            string schemeName = s.ClassName.ToLower(CultureInfo.CurrentCulture);
            bool running = isRunning;

            Console.WriteLine("Cache-ID:       {0}", cacheName);

            if (partId != null && partId != string.Empty)
                Console.WriteLine("Partition-ID:   {0}", partId);

            Console.WriteLine("Scheme:         {0}", schemeName);
            Console.WriteLine("Status:         {0}", isRunning ? "Running" : "Stopped");
            if (running)
            {
                if (s is ClusterCacheStatistics)
                {
                    System.Text.StringBuilder nodes = new System.Text.StringBuilder();

                    ClusterCacheStatistics cs = s as ClusterCacheStatistics;
                   
                    Console.WriteLine("Cluster size:   {0}", cs.Nodes.Count);

                    MaxSize = (cs.LocalNode.Statistics.MaxSize / 1024) / 1024;

                    
                    foreach (NodeInfo n in cs.Nodes)
                    {
                        nodes.Append("                ").Append(n.Address).Append("\n");
                    }
                    Console.Write("{0}", nodes.ToString());

                    if (partId != null && partId != string.Empty)
                    {
                        if (cs.SubgroupNodes != null && cs.SubgroupNodes.Contains(partId.ToLower()))
                        {
                            nodes = new System.Text.StringBuilder();
                            ArrayList groupNodes = cs.SubgroupNodes[partId.ToLower()] as ArrayList;
                            Console.WriteLine("Partition size: {0}", groupNodes.Count);
                            
                            foreach (Address address in groupNodes)
                            {
                                nodes.Append("                ").Append(address).Append("\n");
                            }
                        }
                        Console.Write("{0}", nodes.ToString());
                    }
                }
                Console.WriteLine("UpTime:         {0}", s.UpTime);

                if (s.MaxSize != 0)
                    Console.WriteLine("Capacity:       {0} MB", ((s.MaxSize / 1024) / 1024));
                else
                    Console.WriteLine("Capacity:       {0} MB", MaxSize);

                Console.WriteLine("Count:          {0}", s.Count);
            }
            if (printConf)
            {
                try
                {
                    if (xmlSyntax)
                    {
                        PropsConfigReader pr = new PropsConfigReader(configString);
                        Console.WriteLine("Configuration:\n{0}", ConfigReader.ToPropertiesXml(pr.Properties, true));
                    }
                    else
                    {
                        Console.WriteLine("Configuration:\n{0}", configString);
                    }
                }
                catch (ConfigurationException) { }
            }
            Console.WriteLine("");
        }

		static private void PrintDetailedCacheInfo(Cache cache, string partId, bool printConf, bool xmlSyntax)
		{
			CacheStatistics s = cache.Statistics;
            long MaxSize=0;
            string schemeName = s.ClassName.ToLower(CultureInfo.CurrentCulture);
			bool running = cache.IsRunning;

			Console.WriteLine("Cache-ID:       {0}", cache.Name);
            
            if (partId != null && partId != string.Empty)
                Console.WriteLine("Partition-ID:   {0}", partId);

			Console.WriteLine("Scheme:         {0}", schemeName);
			Console.WriteLine("Status:         {0}", cache.IsRunning ? "Running":"Stopped");
			if(running)
			{
				if(s is ClusterCacheStatistics)
				{
					System.Text.StringBuilder nodes = new System.Text.StringBuilder();

					ClusterCacheStatistics cs = s as ClusterCacheStatistics;
                  
					Console.WriteLine("Cluster size:   {0}", cs.Nodes.Count);
                    
                    MaxSize = (cs.LocalNode.Statistics.MaxSize/1024)/1024;

                    
                    foreach (NodeInfo n in cs.Nodes)
                    {
                      nodes.Append("                ").Append(n.Address).Append("\n");
                    }
                    Console.Write("{0}", nodes.ToString());

                    if (partId != null && partId != string.Empty)
                    {
                        if (cs.SubgroupNodes != null && cs.SubgroupNodes.Contains(partId.ToLower()))
                        {
                            nodes = new System.Text.StringBuilder();
                            ArrayList groupNodes = cs.SubgroupNodes[partId.ToLower()] as ArrayList;
                            Console.WriteLine("Partition size: {0}", groupNodes.Count);
                           
                            foreach (Address address in groupNodes)
                            {
                                nodes.Append("                ").Append(address).Append("\n");
                            }
                        }
                        Console.Write("{0}", nodes.ToString());
                    }
				}
				Console.WriteLine("UpTime:         {0}", s.UpTime);

                if(s.MaxSize != 0)
                Console.WriteLine("Capacity:       {0} MB", ((  s.MaxSize / 1024) / 1024));
                else
                Console.WriteLine("Capacity:       {0} MB", MaxSize);

                Console.WriteLine("Count:          {0}", s.Count);
			}
			if(printConf)
			{
				try
				{
					if(xmlSyntax)
					{
						PropsConfigReader pr = new PropsConfigReader(cache.ConfigString);
						Console.WriteLine("Configuration:\n{0}", ConfigReader.ToPropertiesXml(pr.Properties, true));
					}
					else
					{
						Console.WriteLine("Configuration:\n{0}", cache.ConfigString);
					}
				}
				catch(ConfigurationException){}
			}
			Console.WriteLine("");
		}


		/// <summary>
		/// Sets the application level parameters to those specified at the command line.
		/// </summary>
		/// <param name="args">array of command line parameters</param>
        

		/// <summary>
		/// The main entry point for the tool.
		/// </summary>
		static public void Run(string[] args)
		{
			
            object param = new ListCachesParam();
            CommandLineArgumentParser.CommandLineParser(ref param, args);
            cParam = (ListCachesParam)param;
            AssemblyUsage.PrintLogo(cParam.IsLogo);



            if (cParam.IsUsage)
            {
                AssemblyUsage.PrintUsage();
                return;
            }

            if (!cParam.Detail && string.IsNullOrEmpty(cParam.Server) && args.Length != 0 && cParam.Port == -1 && cParam.IsUsage)
            {
                AssemblyUsage.PrintUsage();
                return;
            }
            
            
            if (cParam.Port != -1)
            {
                NCache.Port = cParam.Port;
            }
            if (cParam.Server != null && !cParam.Server.Equals(""))
            {
                NCache.ServerName = cParam.Server;
            }

		    string getBindIp = string.Empty;
		    

			
			try
			{
				ICacheServer m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
			    getBindIp = m.GetBindIP();
                Console.WriteLine("Listing registered caches on server {0}:{1}\n", getBindIp, NCache.Port);

				if(m != null)
				{ 
                   Alachisoft.NCache.Common.Monitoring.ConfiguredCacheInfo[] caches = m.GetAllConfiguredCaches();
                   Alachisoft.NCache.Common.Monitoring.ConfiguredCacheInfo[] partitionedReplicaCaches = m.GetConfiguredPartitionedReplicaCaches();// PartitionedReplicaCaches;

                   if (caches.Length > 0 || partitionedReplicaCaches.Length > 0)
                   {
                       if (!cParam.Detail)
                       {
                           Console.WriteLine("{0,-20} {1,-15} {2,-30} {3,-5}", "Cache-ID", "Partition Id", "Scheme", "Status");
                           Console.WriteLine("{0,-20} {1,-15} {2,-30} {3,-5}", "--------", "------------", "------", "------");
                       }

                       if (caches.Length > 0)
                       {
                           for (int i = 0; i < caches.Length; i++)
                           {
                               Alachisoft.NCache.Common.Monitoring.ConfiguredCacheInfo cacheInfo = caches[i];
                               if (!cParam.Detail)
                               {
                                    PrintCacheInfo(m.GetCacheStatistics2(cacheInfo.CacheId), "N/A", cacheInfo.CacheId, cacheInfo.IsRunning);
                               }
                               else
                               {
                                   PrintDetailedCacheInfo(m.GetCacheStatistics2(cacheInfo.CacheId), null, cParam.PrintConf, cParam.XmlSyntax, cacheInfo.IsRunning, cacheInfo.CacheId, cacheInfo.CachePropString);
                               } 
                           }
                       }

                       if (partitionedReplicaCaches.Length > 0)
                       {
                           IEnumerator ide = partitionedReplicaCaches.GetEnumerator();
                           while (ide.MoveNext())
                           {
                               Hashtable cacheTbl = ide.Current as Hashtable;
                               if (cacheTbl != null)
                               {
                                   IDictionaryEnumerator e = cacheTbl.GetEnumerator();
                                   while (e.MoveNext())
                                   {
                                       string partId = e.Key as string;
                                       Cache cache = (Cache)e.Value;
                                       if (!detailed)
                                       {
                                           PrintCacheInfo(cache, partId);
                                       }
                                       else
                                       {
                                           PrintDetailedCacheInfo(cache, partId, printConf, xmlSyntax);
                                       }
                                   }
                               }
                           }
                       }
                   }
                   else
                   {
                       Console.WriteLine("There are no registered caches on {0}", NCache.ServerName);
                   }
				}
			}
			catch(Exception e)
			{
				Console.Error.WriteLine("Error: {0}", e.Message);
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
