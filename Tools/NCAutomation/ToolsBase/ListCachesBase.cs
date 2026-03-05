using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Get,"Caches")]
    public class ListCachesBase : ListCachesParameters, IConfiguration
    {
        private NCacheRPCService NCache = new NCacheRPCService("");
        private string TOOLNAME = "ListCaches Tool";
        private void PrintCacheInfo(string schema, string storeType, string cacheName, bool isRunning, int pId)
        {            
            string schemeName = schema.ToLower(CultureInfo.CurrentCulture);
            string running = isRunning ? "Running" : "Stopped";
            if (isRunning)
            {
                string processId = pId.ToString();
                string status = string.Concat(running, "(", processId, ")");
                OutputProvider.WriteLine("{0,-25} {1,-37} {2,-25} {3,-15}", cacheName, ToolsUtil.GetStoreDisplayName(storeType, schemeName), schemeName, status);
            }
            else
            {
                OutputProvider.WriteLine("{0,-25} {1,-37} {2,-25} {3,-15}", cacheName, ToolsUtil.GetStoreDisplayName(storeType, schemeName), schemeName, running);
            }
        }

        private void PrintDetailedCacheInfo(CacheStatistics s, long size, string topology, string partId, bool isRunning, string cacheName, string configString, string pid ,string storeType)
        {

            long MaxSize = 0;
            string schemeName = topology;
            bool running = isRunning;

            OutputProvider.WriteLine("Cache-Name:\t\t{0}", cacheName);
            OutputProvider.WriteLine("In-Memory Store Type:\t{0}", ToolsUtil.GetStoreDisplayName(storeType, schemeName));
            OutputProvider.WriteLine("Topology:\t\t{0}", schemeName);
            OutputProvider.WriteLine("Status:\t\t\t{0}", isRunning ? "Running" : "Stopped");
            if (running)
            {
                OutputProvider.WriteLine("Process-ID:\t\t{0}", pid);
                if (s is ClusterCacheStatistics)
                {
                    System.Text.StringBuilder nodes = new System.Text.StringBuilder();

                    ClusterCacheStatistics cs = s as ClusterCacheStatistics;
                   
                    OutputProvider.WriteLine("Cluster-size:           " + cs.Nodes.Count);

                    if (cs.LocalNode != null)
                        MaxSize = (cs.LocalNode.Statistics.MaxSize / 1024) / 1024;

                    foreach (NodeInfo n in cs.Nodes)
                    {

                        if (schemeName.Equals(Common.Enum.CacheTopology.Mirror.ToString()) && n.IsActive)
                            nodes.Append("                        ").Append(n.Address).Append(" (Active)\n");
                        else
                            nodes.Append("                        ").Append(n.Address).Append("\n");
                    }
                    OutputProvider.WriteLine("{0}", nodes.ToString());

                    if (partId != null && partId != string.Empty)
                    {
                        if (cs.SubgroupNodes != null && cs.SubgroupNodes.Contains(partId.ToLower()))
                        {
                            nodes = new System.Text.StringBuilder();
                            ArrayList groupNodes = cs.SubgroupNodes[partId.ToLower()] as ArrayList;
                            OutputProvider.WriteLine("Partition-size: " + groupNodes.Count);
                            foreach (Address address in groupNodes)
                            {
                                nodes.Append("                ").Append(address).Append("\n");
                            }
                        }
                        OutputProvider.WriteLine("{0}", nodes.ToString());
                    }
                }
                OutputProvider.WriteLine("UpTime:                 " + s.UpTime);

                if (s.MaxSize != 0)
                    OutputProvider.WriteLine("Capacity:               " + (size) + " MB");
                else
                    OutputProvider.WriteLine("Capacity:               " + MaxSize + "MB");

                OutputProvider.WriteLine("Count:                  " + s.Count);
            }
            
            OutputProvider.WriteLine("");
        }

        public void ListCaches ()
        {
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            if (Port != -1)
            {
                NCache.Port = Port;
            }
            if (Server != null && !Server.Equals(""))
            {
                NCache.ServerName = Server;
            }

            string getBindIp = string.Empty;


            try
            {
                ICacheServer cacheServer = null;
                try
                {
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception e)
                {
                    OutputProvider.WriteErrorLine("Error: NCache service could not be contacted on server.");
                    return;
                }
                getBindIp = cacheServer.GetBindIP();
                OutputProvider.WriteLine("Listing registered caches on server {0}:{1}\n", getBindIp, NCache.Port);
                if (cacheServer != null)
                {
                    Alachisoft.NCache.Common.Monitoring.ConfiguredCacheInfo[] caches = GetAllConfiguredCaches(cacheServer);


                    if (caches.Length > 0)
                    {
                        if (!Detail)
                        {
                            OutputProvider.WriteLine("{0,-25} {1,-37} {2,-25} {3,-15}", "Cache-Name", "In-Memory Store Type", "Topology", "Status(PID)");
                            OutputProvider.WriteLine("{0,-25} {1,-37} {2,-25} {3,-15}", "----------", "--------------------", "--------", "-----------");
                        }

                        if (caches.Length > 0)
                        {
                            for (int i = 0; i < caches.Length; i++)
                            {
                                Alachisoft.NCache.Common.Monitoring.ConfiguredCacheInfo cacheInfo = caches[i];
                                if (!Detail)
                                {
                                    PrintCacheInfo(cacheInfo.Topology.ToString(), cacheInfo.StoreType, cacheInfo.CacheId, cacheInfo.IsRunning, cacheInfo.ProcessID);
                                }
                                else
                                {

                                    try
                                    {
                                        PrintDetailedCacheInfo(cacheServer.GetCacheStatistics2(cacheInfo.CacheId),
                                        cacheInfo.DataCapacity, cacheInfo.Topology.ToString(), null, cacheInfo.IsRunning, 
                                        cacheInfo.CacheId, cacheInfo.CachePropString, cacheInfo.ProcessID.ToString(), cacheInfo.StoreType);
                                    }
                                    catch (Exception e)
                                    {
                                        if(e.Message != null && e.Message.Contains("No connection could be made because the target machine actively refused it"))
                                        {
                                            PrintDetailedCacheInfo(null, cacheInfo.DataCapacity, cacheInfo.Topology.ToString(), null, 
                                                false, cacheInfo.CacheId, cacheInfo.CachePropString, "0", cacheInfo.StoreType);
                                        }
                                        else
                                        {
                                            OutputProvider.WriteErrorLine("Error: {0}", e.Message);
                                            OutputProvider.WriteErrorLine(e.ToString());
                                        }

                                    }   
                                    
                                }

                            }
                        }

                    }
                    else
                    {
                        if (string.IsNullOrEmpty(NCache.ServerName))
                            NCache.ServerName = getBindIp;
                        if (InMemoryStoreType != null)
                        {
                            OutputProvider.WriteLine("There are no registered caches for {0} on {1}", InMemoryStoreType.ToString(), NCache.ServerName);
                        }
                        OutputProvider.WriteLine("There are no registered caches on {0}", NCache.ServerName);
                    }
                }
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error: {0}", e.Message);
                OutputProvider.WriteErrorLine(e.ToString());
            }
            finally
            {
                NCache.Dispose();
            }

            OutputProvider.WriteLine(Environment.NewLine);
        }

        private ConfiguredCacheInfo[] GetAllConfiguredCaches(ICacheServer cacheServer)
        {
            Alachisoft.NCache.Common.Monitoring.ConfiguredCacheInfo[] caches = cacheServer.GetAllConfiguredCaches();
            List<ConfiguredCacheInfo> cacheInfos = new List<ConfiguredCacheInfo>(caches);

            if (Topology != null)
            {
                foreach (var item in caches)
                {
                    if (!ToolsUtil.CompareTopology(item.Topology, Topology))
                    {
                        cacheInfos.Remove(item);
                    }
                }
            }

            if (InMemoryStoreType != null)
            {
                string storeType = ToolsUtil.GetStore(InMemoryStoreType);
                foreach (var item in caches)
                {
                    if (!item.StoreType.Equals(storeType))
                    {
                        cacheInfos.Remove(item);
                    }
                }
            }

            return cacheInfos.ToArray();
        }
        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);

        }

        protected void Callingfunc()
        {
            OutputProvider = new PowerShellOutputConsole(this);
            TOOLNAME = "Get-Caches Cmdlet";
            ListCaches();
        }

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Automation.Util.AssemblyResolver.GetAssembly);
#endif
                Callingfunc();
                
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

        public bool ValidateParameters()
        {
            return false;
        }
    }
}
