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
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common;
/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
namespace Alachisoft.NCache.Config.NewDom
{
	/// 
	/// <summary>
	/// @author numan_hanif
	/// </summary>
	public class DomHelper
	{
		public static Alachisoft.NCache.Config.Dom.CacheServerConfig convertToOldDom(Alachisoft.NCache.Config.NewDom.CacheServerConfig newDom)
		{
			Alachisoft.NCache.Config.Dom.CacheServerConfig oldDom = null;
	   try
	   {
		   if (newDom != null)
		   {
				oldDom = new Alachisoft.NCache.Config.Dom.CacheServerConfig();

				 if (newDom.CacheSettings != null)
				 {
				oldDom.Name = newDom.CacheSettings.Name;
				oldDom.InProc = newDom.CacheSettings.InProc;
				oldDom.ConfigID = newDom.ConfigID;
				oldDom.LastModified = newDom.CacheSettings.LastModified;

				 if (newDom.CacheSettings.Log != null)
				 {
					oldDom.Log = newDom.CacheSettings.Log;
				 }
				 else
				 {
					 oldDom.Log = new Alachisoft.NCache.Config.Dom.Log();
				 }

				if (newDom.CacheSettings.PerfCounters != null)
				{
					oldDom.PerfCounters = newDom.CacheSettings.PerfCounters;
				}
				else
				{
                    oldDom.PerfCounters = new Alachisoft.NCache.Config.Dom.PerfCounters();
				}

				if (newDom.CacheSettings.QueryIndices != null)
				{
					oldDom.QueryIndices = newDom.CacheSettings.QueryIndices;
				}



				if (newDom.CacheSettings.Cleanup != null)
				{
					oldDom.Cleanup = newDom.CacheSettings.Cleanup;
				}
				else
				{
                    oldDom.Cleanup = new Alachisoft.NCache.Config.Dom.Cleanup();
				}

				if (newDom.CacheSettings.Storage != null)
				{
					oldDom.Storage = newDom.CacheSettings.Storage;
				}
				else
				{
                    oldDom.Storage = new Alachisoft.NCache.Config.Dom.Storage();
				}

				if (newDom.CacheSettings.EvictionPolicy != null)
				{
					oldDom.EvictionPolicy = newDom.CacheSettings.EvictionPolicy;
				}
				else
				{
                    oldDom.EvictionPolicy = new Alachisoft.NCache.Config.Dom.EvictionPolicy();
				}

				if (newDom.CacheSettings.CacheTopology != null)
				{


				oldDom.CacheType = newDom.CacheSettings.CacheType;
				}

				if (oldDom.CacheType.Equals("clustered-cache"))
				{

					if (newDom.CacheDeployment != null)
					{
					 if (oldDom.Cluster == null)
					 {
					   oldDom.Cluster = new Alachisoft.NCache.Config.Dom.Cluster();
					 }
					  string topology = newDom.CacheSettings.CacheTopology.Topology;
			if (topology != null)
			{
				topology = topology.ToLower();

				if (topology.Equals("partitioned"))
				{
				topology = "partitioned-server";
				}

				else if (topology.Equals("replicated"))
				{
				topology = "replicated-server";
				}
				else if (topology.Equals("local"))
				{
				topology = "local-cache";
				}

				
			}

					oldDom.Cluster.Topology = topology;
					oldDom.Cluster.OpTimeout = newDom.CacheSettings.CacheTopology.ClusterSettings.OpTimeout;
					oldDom.Cluster.StatsRepInterval = newDom.CacheSettings.CacheTopology.ClusterSettings.StatsRepInterval;
					oldDom.Cluster.UseHeartbeat = newDom.CacheSettings.CacheTopology.ClusterSettings.UseHeartbeat;

					if (oldDom.Cluster.Channel == null)
					{
						oldDom.Cluster.Channel = new Alachisoft.NCache.Config.Dom.Channel();
					}

					oldDom.Cluster.Channel.TcpPort = newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.TcpPort;
					oldDom.Cluster.Channel.PortRange = newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.PortRange;
					oldDom.Cluster.Channel.ConnectionRetries = newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.ConnectionRetries;
					oldDom.Cluster.Channel.ConnectionRetryInterval = newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.ConnectionRetryInterval;
					oldDom.Cluster.Channel.InitialHosts = createInitialHosts(newDom.CacheDeployment.Servers.NodesList,newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.TcpPort);
					oldDom.Cluster.Channel.NumInitHosts = newDom.CacheDeployment.Servers.NodesList.Count;

					if (newDom.CacheDeployment.ClientNodes != null)
					{
						if (oldDom.ClientNodes == null)
						{
                            oldDom.ClientNodes = new Alachisoft.NCache.Config.Dom.ClientNodes();
						}

						oldDom.ClientNodes = newDom.CacheDeployment.ClientNodes;
					}

				  }

				}

				 if (newDom.CacheSettings.AutoLoadBalancing != null)
				 {
					oldDom.AutoLoadBalancing = newDom.CacheSettings.AutoLoadBalancing;
				 }
				oldDom.IsRunning = newDom.IsRunning;
				oldDom.IsRegistered = newDom.IsRegistered;
				oldDom.IsExpired = newDom.IsExpired;
	        }
		  }
	    }
		catch (Exception ex)
		{
		   throw new Exception("DomHelper.convertToOldDom" + ex.Message);
		}
			return oldDom;

		}

		public static Alachisoft.NCache.Config.NewDom.CacheServerConfig convertToNewDom(Alachisoft.NCache.Config.Dom.CacheServerConfig oldDom)
		{
			Alachisoft.NCache.Config.NewDom.CacheServerConfig newDom = null;
			try
			{
				if (oldDom != null)
				{
				newDom = new CacheServerConfig();


				if (newDom.CacheSettings == null)
				{
					newDom.CacheSettings = new CacheServerConfigSetting();
				}
				newDom.CacheSettings.Name = oldDom.Name;
				newDom.CacheSettings.InProc = oldDom.InProc;
				newDom.ConfigID = oldDom.ConfigID;
				newDom.CacheSettings.LastModified = oldDom.LastModified;

				if (oldDom.Log != null)
				{
					newDom.CacheSettings.Log = oldDom.Log;
				}
				else
				{
                    newDom.CacheSettings.Log = new Alachisoft.NCache.Config.Dom.Log();
				}

				if (oldDom.PerfCounters != null)
				{
					newDom.CacheSettings.PerfCounters = oldDom.PerfCounters;
				}
				else
				{
                    newDom.CacheSettings.PerfCounters = new Alachisoft.NCache.Config.Dom.PerfCounters();
				}

				if (oldDom.QueryIndices != null)
				{
					newDom.CacheSettings.QueryIndices = oldDom.QueryIndices;
				}



				 if (oldDom.Cleanup != null)
				 {
					newDom.CacheSettings.Cleanup = oldDom.Cleanup;
				 }
				 else
				 {
                     newDom.CacheSettings.Cleanup = new Alachisoft.NCache.Config.Dom.Cleanup();
				 }

				if (oldDom.Storage != null)
				{
					newDom.CacheSettings.Storage = oldDom.Storage;
				}
				else
				{
                    newDom.CacheSettings.Storage = new Alachisoft.NCache.Config.Dom.Storage();
				}

				if (oldDom.EvictionPolicy != null)
				{
					newDom.CacheSettings.EvictionPolicy = oldDom.EvictionPolicy;
				}
				else
				{
					newDom.CacheSettings.EvictionPolicy = oldDom.EvictionPolicy;
				}

				if (newDom.CacheSettings.CacheTopology == null)
				{

					newDom.CacheSettings.CacheTopology = new CacheTopology();
				}

				newDom.CacheSettings.CacheType = oldDom.CacheType;
				if (oldDom.Cluster != null)
				{
				string topology = oldDom.Cluster.Topology;
			if (topology != null)
			{
				topology = topology.ToLower();

				
				if (topology.Equals("partitioned-server"))
				{
				topology = "partitioned";
				}

				else if (topology.Equals("replicated-server"))
				{
				topology = "replicated";
				}
                else if (topology.Equals("local-cache"))
				{
				topology = "local-cache";
				}

				
			}
				newDom.CacheSettings.CacheTopology.Topology = topology;
				if (oldDom.CacheType.Equals("clustered-cache"))
				{
					if (newDom.CacheDeployment == null)
					{
						newDom.CacheDeployment = new CacheDeployment();
					}

					 if (newDom.CacheSettings.CacheTopology.ClusterSettings == null)
					 {
						newDom.CacheSettings.CacheTopology.ClusterSettings = new Cluster();
					 }
					newDom.CacheSettings.CacheTopology.ClusterSettings.OpTimeout = oldDom.Cluster.OpTimeout;
					newDom.CacheSettings.CacheTopology.ClusterSettings.StatsRepInterval = oldDom.Cluster.StatsRepInterval;
					newDom.CacheSettings.CacheTopology.ClusterSettings.UseHeartbeat = oldDom.Cluster.UseHeartbeat;

					if (newDom.CacheSettings.CacheTopology.ClusterSettings.Channel == null)
					{
						newDom.CacheSettings.CacheTopology.ClusterSettings.Channel = new Channel();
					}

					if (oldDom.Cluster.Channel != null)
					{
						newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.TcpPort = oldDom.Cluster.Channel.TcpPort;
						newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.PortRange = oldDom.Cluster.Channel.PortRange;
						newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.ConnectionRetries = oldDom.Cluster.Channel.ConnectionRetries;
						newDom.CacheSettings.CacheTopology.ClusterSettings.Channel.ConnectionRetryInterval = oldDom.Cluster.Channel.ConnectionRetryInterval;
					}
					if (newDom.CacheDeployment.Servers == null)
					{
						newDom.CacheDeployment.Servers = new ServersNodes();
					}
#if !CLIENT
					newDom.CacheDeployment.Servers.NodesList = createServers(oldDom.Cluster.Channel.InitialHosts);
#endif
                    if (oldDom.ClientNodes != null)
					{
						if (newDom.CacheDeployment.ClientNodes == null)
						{
                            newDom.CacheDeployment.ClientNodes = new Alachisoft.NCache.Config.Dom.ClientNodes();
						}

						newDom.CacheDeployment.ClientNodes = oldDom.ClientNodes;
					}

				}
				}
				else
				{
					if (oldDom.CacheType != null)
					{
						
						if (oldDom.CacheType.Equals("local-cache"))
						{
							newDom.CacheSettings.CacheTopology.Topology = oldDom.CacheType;
						}

						newDom.CacheSettings.CacheTopology.ClusterSettings = null;
					}
				}

			  if (oldDom.AutoLoadBalancing != null)
			  {
				newDom.CacheSettings.AutoLoadBalancing = oldDom.AutoLoadBalancing;
			  }
			  
				newDom.IsRunning = oldDom.IsRunning;
				newDom.IsRegistered = oldDom.IsRegistered;
				newDom.IsExpired = oldDom.IsExpired;
				}
			}
			catch (Exception ex)
			{
				 throw new Exception("DomHelper.convertToNewDom" + ex.Message);
			}
			return newDom;
		}

#if !CLIENT
		 private static ArrayList createServers(string l)
		 {
			Global.Tokenizer tok = new Global.Tokenizer(l, ",");
			string t;
			Address addr;
			int port;
			ArrayList retval = new ArrayList();
			Hashtable hosts = new Hashtable();
			ServerNode node;
	//C# TO JAVA CONVERTER TODO TASK: There is no preprocessor in Java:
			int j = 0;
			while (tok.HasMoreTokens())
			{
				try
				{
					t = tok.NextToken();
	//C# TO JAVA CONVERTER TODO TASK: There is no preprocessor in Java:
					string host = t.Substring(0, (t.IndexOf((char) '[')) - (0));
					host = host.Trim();
					port = Convert.ToInt32(t.Substring(t.IndexOf((char) '[') + 1, (t.IndexOf((char) ']')) - (t.IndexOf((char) '[') + 1)));
					node = new ServerNode(host);
					

					retval.Add(node);
	//C# TO JAVA CONVERTER TODO TASK: There is no preprocessor in Java:
					j++;

				}
                catch (FormatException ex)
				{
                    throw ex;
				}
				catch (Exception ex)
				{
                    throw ex;
				}
			}

			return retval;

		 }
#endif
        private static string createInitialHosts(ArrayList nodes, int port)
		{
			string initialhost = "";
			 try
			 {
				for (int index = 0; index < nodes.Count; index++)
				{
					ServerNode node = (ServerNode) nodes[index];
					initialhost = initialhost + node.IP.ToString() + "[" + port + "]";
					if (nodes.Count > 1 && index != nodes.Count - 1)
					{
					  initialhost = initialhost + ",";
					}
				}
			 }
			catch (Exception)
			{
			}

			 return initialhost;
		}
	}

}
