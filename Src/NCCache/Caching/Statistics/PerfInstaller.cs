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
using System.Collections;
using System.ComponentModel;
using Alachisoft.NCache.Common.Collections;
#if !NETCORE
using System.Configuration.Install;
#endif
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Statistics
{
    /// <summary>
    /// Summary description for PerfInstaller.
    /// </summary>
#if !NETCORE
	[RunInstaller(true)]
	public class PerfInstaller : System.Configuration.Install.Installer
	{
		private System.Diagnostics.PerformanceCounterInstaller pcInstaller;
#elif NETCORE
    public class PerfInstaller
    {
#endif
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

		public PerfInstaller()
		{
			// This call is required by the Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

#if !NETCORE
        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
#endif


#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{

            var categoryNameNCache = "NCache";
#if !NETCORE
            this.pcInstaller = new System.Diagnostics.PerformanceCounterInstaller();

			
            this.pcInstaller.CategoryName = categoryNameNCache;


            System.Diagnostics.CounterCreationData [] counterCreationData = new System.Diagnostics.CounterCreationData[]
#elif NETCORE
            System.Diagnostics.CounterCreationDataCollection counterCreationData = new System.Diagnostics.CounterCreationDataCollection()
#endif
            {
            new System.Diagnostics.CounterCreationData("# Clients", "Number of connected clients to an instance of cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Count", "Number of items in the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("CacheLastAccessCount", "Number of items which are older then the access interval specified in the service config file.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Fetches/sec", "Number of Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Additions/sec", "Number of Add operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Updates/sec", "Number of Insert operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Deletes/sec", "Number of Remove operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average us/fetch", "Average time in microseconds (us), taken to complete one fetch operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/fetch base", "Base counter for average microseconds(us)/fetch", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/add", "Average time in microseconds (us), taken to complete one add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/add base", "Base counter for average microseconds (us)sec/add", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/insert", "Average time in microseconds (us), taken to complete one insert operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/insert base", "Base counter for average microseconds (us)/insert", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/remove", "Average time in microseconds (us), taken to complete one remove operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/remove base", "Base counter for average microseconds (us)/remove", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/cache operation", "Average time in microseconds (us), taken to complete one cache-operation.", System.Diagnostics.PerformanceCounterType.AverageCount64),
            new System.Diagnostics.CounterCreationData("Average us/cache operation base", "Base counter for average microseconds (us) /cache-operation", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Expirations/sec", "Number of items being expired currently per second", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Evictions/sec", "Number of items evicted per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
#if !(DEVELOPMENT || CLIENT)
            new System.Diagnostics.CounterCreationData("State Transfer/sec", "Number of items this node is either reading from other nodes or sending to other nodes during a state transfer mode.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Mirror queue size", "Number of items in the Mirror queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Sliding Index queue size", "Number of items in the Sliding-Index queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
#endif
            new System.Diagnostics.CounterCreationData("Hits/sec", "Number of successful Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Misses/sec", "Number of failed Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Hits ratio/sec (%)", "Ratio of number of successful Get operations per second and total number of Get operations per second ", System.Diagnostics.PerformanceCounterType.SampleFraction),
            new System.Diagnostics.CounterCreationData("Hits ratio/sec base", "Base counter for Hits ratio/sec", System.Diagnostics.PerformanceCounterType.SampleBase),
#if !(DEVELOPMENT || CLIENT)
            //Moiz: perfmon description task 29-10-13
            //previous description (till 4.1 sp3) for DispatchEnter,TcpDown,Clustered opsent,clusters oprecv, reponse sent (Number of clustered operations sent to other nodes in cluster per second.)
            new System.Diagnostics.CounterCreationData("Data balance/sec", "Number of items this node is either reading from other nodes or sending to other nodes during a Data Load Balancing mode.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Cluster ops/sec", "Number of clustered operations performed per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("TcpdownEnter/sec", "", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Clustered opssent/sec", "Number of clustered operations sent to other nodes in cluster per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Clustered opsrecv/sec", "Number of clustered operations received from other nodes in cluster per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Response sent/sec", "Number of responses sent to other nodes in cluster per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
#endif
            new System.Diagnostics.CounterCreationData("Bytes sent/sec", "Number of bytes sent per second to other nodes of the cluster.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Bytes received/sec", "Number of bytes received per second from other nodes of the cluster.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Requests/sec", "Number of requests received (meaning cache commands like add, get, insert, remove etc.) from all clients to this cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Responses/sec", "Number of responses sent (meaning cache response for commands like add, get, insert, remove etc.) to all clients by this cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Client Requests/sec", "Number of requests sent by all clients to the cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Client Responses/sec", "Number of responses received by all clients from the cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Client bytes sent/sec", "Bytes being sent from cache server to all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Client bytes received/sec", "Bytes being received by cache server from all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("TcpUpQueueCount", "Number of items in TCP up-queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("TcpDownQueueCount", "Number of items in TCP down-queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
#if !(DEVELOPMENT || CLIENT)
            new System.Diagnostics.CounterCreationData("BcastQueueCount", "Number of items in BCast queue waiting to be processed on sequence.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("McastQueueCount", "Number of items in MCast queue waiting to be processed on sequence.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
#endif
            new System.Diagnostics.CounterCreationData("Socket send time (msec)", "Time in milli seconds it took for the last message to be sent over the socket.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Socket send size (bytes)", "How much data was sent in the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Socket recv time (msec)", "Time in milli seconds it took to receive the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Socket recv size (bytes)", "How much data was received in the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),

            new System.Diagnostics.CounterCreationData("Response Queue Count", "Number of items in response queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Event Queue Count", "Number of items in event queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Response Queue Size", "Size of response queue specified in bytes.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),

            new System.Diagnostics.CounterCreationData("Cache Size", "Size of the cache in bytes, including cache store meta info and all other indices.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Expiration Index Size", "Size of expiration in bytes, indices defined on the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Eviction Index Size", "Size of eviction indices in bytes, define on the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            //request logging counters
            new System.Diagnostics.CounterCreationData("Requests Logged/sec", "Average number of requests logged in a second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Request Log Ledger Size", "Total in-memory size of the log ledger, which stores logged requests.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),

            //Bulk Counters
            new System.Diagnostics.CounterCreationData("Average us/addbulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/addbulk base", "Base counter for Average microseconds (us)/addbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/fetchbulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/fetchbulk base", "Base counter for Average microseconds (us)/fetchbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/insertbulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/insertbulk base", "Base counter for Average microseconds (us)/insertbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/removebulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/removebulk base", "Base counter for Average microseconds (us)/removebulk", System.Diagnostics.PerformanceCounterType.AverageBase),

            #region Pub_Sub
            new System.Diagnostics.CounterCreationData(CounterNames.MessageCount, "Number of messages in the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems32),
            new System.Diagnostics.CounterCreationData(CounterNames.TopicCount, "Number of Topics in the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems32),
            new System.Diagnostics.CounterCreationData(CounterNames.MessageStoreSize, "Size of message store in bytes, including message store meta info.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData(CounterNames.MessagePublishPerSec, "Number of messages published per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData(CounterNames.MessageDeliveryPerSec, "Number of messages delivered to subsribers per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData(CounterNames.MessageExpiredPerSec, "Number of messages expired per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),

            #endregion
           
            };

#if !NETCORE
            this.pcInstaller.Counters.AddRange(counterCreationData);
              this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.pcInstaller});
#elif NETCORE
            if (!System.Diagnostics.PerformanceCounterCategory.Exists(categoryNameNCache))
                System.Diagnostics.PerformanceCounterCategory.Create(categoryNameNCache, "Visit Documentation", System.Diagnostics.PerformanceCounterCategoryType.MultiInstance, counterCreationData);
#endif
        }

        #endregion

    }
}
