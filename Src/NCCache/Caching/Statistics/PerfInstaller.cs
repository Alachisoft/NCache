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
using System.ComponentModel;
using System.Configuration.Install;

namespace Alachisoft.NCache.Caching.Statistics
{
    /// <summary>
    /// Summary description for PerfInstaller.
    /// </summary>
    [RunInstaller(true)]
    public class PerfInstaller : System.Configuration.Install.Installer
    {
        private System.Diagnostics.PerformanceCounterInstaller pcInstaller;
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

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }


        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pcInstaller = new System.Diagnostics.PerformanceCounterInstaller();
            // 
            // pcInstaller
            //
            this.pcInstaller.CategoryName = "NCache";
            this.pcInstaller.Counters.AddRange(new System.Diagnostics.CounterCreationData[] {
																								new System.Diagnostics.CounterCreationData("Count", "Number of items in the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("CacheLastAccessCount", "Number of items which are older then the access interval specified in the service config file.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Fetches/sec", "Number of Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
																								new System.Diagnostics.CounterCreationData("Additions/sec", "Number of Add operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
		                                                                                        new System.Diagnostics.CounterCreationData("Updates/sec", "Number of Insert operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
																								new System.Diagnostics.CounterCreationData("Deletes/sec", "Number of Remove operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/fetch", "Average time in microseconds, taken to complete one fetch operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/fetch base", "Base counter for average µsec/fetch", System.Diagnostics.PerformanceCounterType.AverageBase),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/add", "Average time in microseconds, taken to complete one add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/add base", "Base counter for average µsec/add", System.Diagnostics.PerformanceCounterType.AverageBase),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/insert", "Average time in microseconds, taken to complete one insert operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/insert base", "Base counter for average µsec/insert", System.Diagnostics.PerformanceCounterType.AverageBase),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/remove", "Average time in microseconds, taken to complete one remove operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/remove base", "Base counter for average µsec/remove", System.Diagnostics.PerformanceCounterType.AverageBase),                                                                                       
                                                                                                
																								new System.Diagnostics.CounterCreationData("Expirations/sec", "Number of items being expired currently per second", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("Evictions/sec", "Number of items evicted per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
																								new System.Diagnostics.CounterCreationData("Hits/sec", "Number of successful Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
				                                                                                new System.Diagnostics.CounterCreationData("Hits ratio/sec (%)", "Ratio of number of successful Get operations per second and total number of Get operations per second ", System.Diagnostics.PerformanceCounterType.SampleFraction),
                                                                                                new System.Diagnostics.CounterCreationData("Hits ratio/sec base", "Base counter for Hits ratio/sec", System.Diagnostics.PerformanceCounterType.SampleBase),
                                                                                                new System.Diagnostics.CounterCreationData("Misses/sec", "Number of failed Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),  
                                                                                                
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/cache operation", "Average time in microseconds, taken to complete one cache-operation.", System.Diagnostics.PerformanceCounterType.AverageCount64),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/cache operation base", "Base counter for average µs/cache-operation", System.Diagnostics.PerformanceCounterType.AverageBase),
                                                                                                new System.Diagnostics.CounterCreationData("Requests/sec", "Number of requests received (meaning cache commands like add, get, insert, remove etc.) from all clients to this cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Responses/sec", "Number of responses sent (meaning cache response for commands like add, get, insert, remove etc.) to all clients by this cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Client Requests/sec", "Number of requests sent by all clients to the cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Client Responses/sec", "Number of responses received by all clients from the cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Client bytes sent/sec", "Bytes being sent from cache server to all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Client bytes received/sec", "Bytes being received by cache server from all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Event Queue Count", "Number of items in event queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("General Notifications Queue Size", "Number of general notification in queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Response Queue Count", "Number of items in response queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Response Queue Size", "Size of response queue specified in bytes.", System.Diagnostics.PerformanceCounterType.NumberOfItems64)
#if !CLIENT
                                                                                                ,
                                                                                                new System.Diagnostics.CounterCreationData("Bytes sent/sec", "Number of bytes sent per second to other nodes of the cluster.", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("Bytes received/sec", "Number of bytes received per second from other nodes of the cluster.", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("Socket send time (msec)", "Time in milli seconds it took for the last message to be sent over the socket.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Socket send size (bytes)", "How much data was sent in the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("NaglingMsgCount", "Time in milli seconds for which a sequenced messages waits before it is processed.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Socket recv time (msec)", "Time in milli seconds it took to receive the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Socket recv size (bytes)", "How much data was received in the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("TcpUpQueueCount", "Number of items in TCP up-queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("TcpDownQueueCount", "Number of items in TCP down-queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("BcastQueueCount", "Number of items in BCast queue waiting to be processed on sequence.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("McastQueueCount", "Number of items in MCast queue waiting to be processed on sequence.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Data balance/sec", "Number of items this node is either reading from other nodes or sending to other nodes during a Data Load Balancing mode.", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("Cluster ops/sec", "Number of clustered operations performed per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("Response sent/sec", "Number of responses sent to other nodes in cluster per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("State transfer/sec", "Number of items this node is either reading from other nodes or sending to other nodes during a state transfer mode.", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("Mirror queue size", "Number of items in the Mirror queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Sliding Index queue size", "Number of items in the Sliding-Index queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),                                                                                              
                                                                                                new System.Diagnostics.CounterCreationData("Cache Size", "Size of the cache in bytes including store and all indices.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Query Index Size", "Size of query indices defined on cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Expiration Index Size", "Size of expiration indices defined on cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
                                                                                                new System.Diagnostics.CounterCreationData("Eviction Index Size", "Size of eviction indices define on cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),                                                                                                
                                                                                                new System.Diagnostics.CounterCreationData("Queries/sec", "Number of queries per sec on cache.", System.Diagnostics.PerformanceCounterType.SampleCounter),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/Query Execution", "Average time query take while executing.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
                                                                                                new System.Diagnostics.CounterCreationData("Average µs/Query Execution base", "Average time query take while executing.", System.Diagnostics.PerformanceCounterType.AverageBase),
                                                                                                new System.Diagnostics.CounterCreationData("Average Query Size", "Average Number of items returned by queries.", System.Diagnostics.PerformanceCounterType.AverageCount64),
                                                                                                new System.Diagnostics.CounterCreationData("Average Query Size base", "Average Number of items returned by queries.", System.Diagnostics.PerformanceCounterType.AverageBase)
#endif
            
            });

            // PerfInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.pcInstaller});

        }
        #endregion
    }
}
