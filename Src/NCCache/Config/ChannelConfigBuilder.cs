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
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Net;
using System.Configuration;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Util;



namespace Alachisoft.NCache.Config
{
	/// <summary>
	///  
	/// </summary>
	internal class ChannelConfigBuilder
	{


		/// <summary>
		/// 
		/// </summary>
		/// <param name="properties"></param>
		/// <returns></returns>
		public static string BuildTCPConfiguration(IDictionary properties,long opTimeout)
		{
			StringBuilder b = new StringBuilder(2048);
			b.Append(BuildTCP(properties["tcp"] as IDictionary)).Append(":");
			b.Append(BuildTCPPING(properties["tcpping"] as IDictionary)).Append(":");
            b.Append(BuildQueue(properties["queue"] as IDictionary)).Append(":");
			b.Append(Buildpbcast_GMS(properties["pbcast.gms"] as IDictionary,false)).Append(":");
			b.Append(BuildTOTAL(properties["total"] as IDictionary,opTimeout)).Append(":");
			b.Append(BuildVIEW_ENFORCER(properties["view-enforcer"] as IDictionary));
			return b.ToString();
		}

		public static string BuildTCPConfiguration(IDictionary properties,long opTimeout,bool isPor)
		{
			StringBuilder b = new StringBuilder(2048);
			b.Append(BuildTCP(properties["tcp"] as IDictionary)).Append(":");
            b.Append(BuildTCPPING(properties["tcpping"] as IDictionary)).Append(":");
            b.Append(BuildQueue(properties["queue"] as IDictionary)).Append(":");
            b.Append(Buildpbcast_GMS(properties["pbcast.gms"] as IDictionary,isPor)).Append(":");
            b.Append(BuildTOTAL(properties["total"] as IDictionary, opTimeout)).Append(":");
            b.Append(BuildVIEW_ENFORCER(properties["view-enforcer"] as IDictionary));

			return b.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="properties"></param>
		/// <returns></returns>
		public static string BuildUDPConfiguration(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(2048);
			b.Append(BuildUDP(properties["udp"] as IDictionary)).Append(":");
			b.Append(BuildPING(properties["ping"] as IDictionary)).Append(":");
			b.Append(BuildMERGEFAST(properties["mergefast"] as IDictionary)).Append(":");
			b.Append(BuildFD_SOCK(properties["fd-sock"] as IDictionary)).Append(":");
			b.Append(BuildVERIFY_SUSPECT(properties["verify-suspect"] as IDictionary)).Append(":");
			b.Append(BuildFRAG(properties["frag"] as IDictionary)).Append(":");
			b.Append(BuildUNICAST(properties["unicast"] as IDictionary)).Append(":");
			b.Append(BuildQueue(properties["queue"] as IDictionary)).Append(":");
			b.Append(Buildpbcast_NAKACK(properties["pbcast.nakack"] as IDictionary)).Append(":");
			b.Append(Buildpbcast_STABLE(properties["pbcast.stable"] as IDictionary)).Append(":");
			b.Append(Buildpbcast_GMS(properties["pbcast.gms"] as IDictionary,false)).Append(":");
			b.Append(BuildTOTAL(properties["total"] as IDictionary,5000)).Append(":");
			b.Append(BuildVIEW_ENFORCER(properties["view-enforcer"] as IDictionary));
			return b.ToString();
		}

		private static string BuildUDP(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(256);
			b.Append("UDP(")
				.Append(ConfigHelper.SafeGetPair(properties,"mcast_addr", "239.0.1.10"))
				.Append(ConfigHelper.SafeGetPair(properties,"mcast_port", 10001))
				.Append(ConfigHelper.SafeGetPair(properties,"bind_addr", null))
				.Append(ConfigHelper.SafeGetPair(properties,"bind_port", null))
				.Append(ConfigHelper.SafeGetPair(properties,"port_range", 256))
				.Append(ConfigHelper.SafeGetPair(properties,"ip_mcast", null))				//"false"
				.Append(ConfigHelper.SafeGetPair(properties,"mcast_send_buf_size", null))	//32000
				.Append(ConfigHelper.SafeGetPair(properties,"mcast_recv_buf_size", null))	//64000
				.Append(ConfigHelper.SafeGetPair(properties,"ucast_send_buf_size", null))	//32000
				.Append(ConfigHelper.SafeGetPair(properties,"ucast_recv_buf_size", null))	//64000
				.Append(ConfigHelper.SafeGetPair(properties,"max_bundle_size", null))		//32000
				.Append(ConfigHelper.SafeGetPair(properties,"max_bundle_timeout", null))	//20
				.Append(ConfigHelper.SafeGetPair(properties,"enable_bundling", null))		//"false"	
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"use_incoming_packet_handler", null))
				.Append(ConfigHelper.SafeGetPair(properties,"use_outgoing_packet_handler", null))
				.Append("ip_ttl=32;")
				.Append(")");
			return b.ToString();
		}
		private static string BuildPING(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(256);
			b.Append("PING(")
				.Append(ConfigHelper.SafeGetPair(properties,"timeout", null))				//2000
				.Append(ConfigHelper.SafeGetPair(properties,"num_initial_members", null))	//2
				.Append(ConfigHelper.SafeGetPair(properties,"port_range", null))			//1
				.Append(ConfigHelper.SafeGetPair(properties,"initial_hosts", null))
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string BuildMERGEFAST(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(16);
			b.Append("MERGEFAST(")
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string BuildFD_SOCK(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(64);
			b.Append("FD_SOCK(")
				.Append(ConfigHelper.SafeGetPair(properties,"get_cache_timeout", null))		//3000
				.Append(ConfigHelper.SafeGetPair(properties,"start_port", null))			//49152
				.Append(ConfigHelper.SafeGetPair(properties,"num_tries", null))				//3
				.Append(ConfigHelper.SafeGetPair(properties,"suspect_msg_interval", null))	//5000
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string BuildVERIFY_SUSPECT(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(32);
			b.Append("VERIFY_SUSPECT(")
				.Append(ConfigHelper.SafeGetPair(properties,"timeout", 1500))
				.Append(ConfigHelper.SafeGetPair(properties,"num_msgs", null))			// null
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}
		private static string BuildQueue(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(32);
			b.Append("QUEUE(")
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string Buildpbcast_NAKACK(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(128);
			b.Append("pbcast.NAKACK(")
				.Append(ConfigHelper.SafeGetPair(properties,"retransmit_timeout", null))		//"600,1200,2400,4800"
				.Append(ConfigHelper.SafeGetPair(properties,"gc_lag", 40))
				.Append(ConfigHelper.SafeGetPair(properties,"max_xmit_size", null))				// 8192
				.Append(ConfigHelper.SafeGetPair(properties,"use_mcast_xmit", null))			// "false"
				.Append(ConfigHelper.SafeGetPair(properties,"discard_delivered_msgs", null))	// "true"
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string BuildUNICAST(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(64);
			b.Append("UNICAST(")
				.Append(ConfigHelper.SafeGetPair(properties,"timeout", null))		// "800,1600,3200,6400"
				.Append(ConfigHelper.SafeGetPair(properties,"window_size", null))	// -1
				.Append(ConfigHelper.SafeGetPair(properties,"min_threshold", null))	// -1
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string Buildpbcast_STABLE(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(256);
			b.Append("pbcast.STABLE(")
				.Append(ConfigHelper.SafeGetPair(properties,"digest_timeout", null))		// 60000
				.Append(ConfigHelper.SafeGetPair(properties,"desired_avg_gossip", null))	// 20000
				.Append(ConfigHelper.SafeGetPair(properties,"stability_delay", null))		// 6000
				.Append(ConfigHelper.SafeGetPair(properties,"max_gossip_runs", null))		// 3
				.Append(ConfigHelper.SafeGetPair(properties,"max_bytes", null))				// 0
				.Append(ConfigHelper.SafeGetPair(properties,"max_suspend_time", null))		// 600000
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string BuildFRAG(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(32);
			b.Append("FRAG(")
				.Append(ConfigHelper.SafeGetPair(properties,"frag_size", null))
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string Buildpbcast_GMS(IDictionary properties,bool isPor)
		{
			StringBuilder b = new StringBuilder(256);
            if (isPor)
            {
                if (properties == null) properties = new Hashtable();
                properties["is_part_replica"]= "true";
            }
			b.Append("pbcast.GMS(")
				.Append(ConfigHelper.SafeGetPair(properties,"shun", null))					// "true"
				.Append(ConfigHelper.SafeGetPair(properties,"join_timeout", null))			// 5000
				.Append(ConfigHelper.SafeGetPair(properties,"join_retry_timeout", null))	// 2000
                .Append(ConfigHelper.SafeGetPair(properties, "join_retry_count", null))	    // 3
				.Append(ConfigHelper.SafeGetPair(properties,"leave_timeout", null))			// 5000
				.Append(ConfigHelper.SafeGetPair(properties,"merge_timeout", null))			// 10000
				.Append(ConfigHelper.SafeGetPair(properties,"digest_timeout", null))		// 5000
				.Append(ConfigHelper.SafeGetPair(properties,"disable_initial_coord", null))	// false
				.Append(ConfigHelper.SafeGetPair(properties,"num_prev_mbrs", null))			// 50
				.Append(ConfigHelper.SafeGetPair(properties,"print_local_addr", null))		// false
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
                .Append(ConfigHelper.SafeGetPair(properties, "is_part_replica", null))
                
                .Append(")");
			return b.ToString();
		}

		private static string BuildTOTAL(IDictionary properties,long opTimeout)
		{
			StringBuilder b = new StringBuilder(8);
			b.Append("TOTAL(")
				.Append(ConfigHelper.SafeGetPair(properties,"timeout", null))				//"600,1200,2400,4800"
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
                .Append(ConfigHelper.SafeGetPair(properties, "op_timeout", opTimeout))
				.Append(")");
			return b.ToString();
		}

		private static string BuildVIEW_ENFORCER(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(16);
			b.Append("VIEW_ENFORCER(")
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");
			return b.ToString();
		}

		private static string BuildTCPPING(IDictionary properties)
		{
			StringBuilder b = new StringBuilder(256);
			b.Append("TCPPING(")
				.Append(ConfigHelper.SafeGetPair(properties,"timeout", null))				//3000
                .Append(ConfigHelper.SafeGetPair(properties,"port_range", null))			//5
				.Append(ConfigHelper.SafeGetPair(properties,"static", null))				//false
				.Append(ConfigHelper.SafeGetPair(properties,"num_initial_members", null))	//2
				.Append(ConfigHelper.SafeGetPair(properties,"initial_hosts", null))
				.Append(ConfigHelper.SafeGetPair(properties,"discovery_addr", "228.8.8.8"))		//228.8.8.8
				.Append(ConfigHelper.SafeGetPair(properties,"discovery_port", 7700))			//7700
				.Append(ConfigHelper.SafeGetPair(properties,"down_thread", null))
				.Append(ConfigHelper.SafeGetPair(properties,"up_thread", null))
				.Append(")");


			return b.ToString();
		}

		private static string BuildTCP(IDictionary properties)
		{

            string bindIP = ServiceConfiguration.BindToIP.ToString();

            StringBuilder b = new StringBuilder(256);
            b.Append("TCP(")
                .Append(ConfigHelper.SafeGetPair(properties, "connection_retries", 0))
                .Append(ConfigHelper.SafeGetPair(properties, "connection_retry_interval", 0))
                .Append(ConfigHelper.SafeGetPair(properties, "bind_addr", bindIP))
				.Append(ConfigHelper.SafeGetPair(properties, "start_port", null))
                .Append(ConfigHelper.SafeGetPair(properties, "port_range", null))
                .Append(ConfigHelper.SafeGetPair(properties, "send_buf_size", null))				//32000
				.Append(ConfigHelper.SafeGetPair(properties, "recv_buf_size", null))				//64000
				.Append(ConfigHelper.SafeGetPair(properties, "reaper_interval", null))			//0
				.Append(ConfigHelper.SafeGetPair(properties, "conn_expire_time", null))			//0
				.Append(ConfigHelper.SafeGetPair(properties, "skip_suspected_members", null))	//true
				.Append(ConfigHelper.SafeGetPair(properties, "down_thread", true))
				.Append(ConfigHelper.SafeGetPair(properties, "up_thread", true))
				.Append(")");
			return b.ToString();
		}
	}
}

