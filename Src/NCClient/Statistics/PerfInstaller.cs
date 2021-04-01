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
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections;
using System.ComponentModel;
#if !NETCORE
using System.Configuration.Install;
#endif

namespace Alachisoft.NCache.Client.Caching.Statistics
{
    /// <summary>
    /// Summary description for PerfInstaller.
    /// </summary>
#if !NETCORE
    [RunInstaller(true)]
    public class PerfInstaller : System.Configuration.Install.Installer
    {
        private System.Diagnostics.PerformanceCounterInstaller pcClientInstaller;
#elif NETCORE
        public class PerfInstaller
        {
#endif
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        /// <summary>
        /// Installs perfmon counters related to NCache Client 
        /// </summary>
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
#endif

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            var categoryName = "NCache Client";
#if !NETCORE
            this.pcClientInstaller = new System.Diagnostics.PerformanceCounterInstaller();
            this.pcClientInstaller.CategoryName = categoryName;
            System.Diagnostics.CounterCreationData[] counterCreationData = new System.Diagnostics.CounterCreationData[]
#elif NETCORE
            System.Diagnostics.CounterCreationDataCollection counterCreationData = new System.Diagnostics.CounterCreationDataCollection()
#endif
 {

            new System.Diagnostics.CounterCreationData("Fetches/sec", "Number of Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Additions/sec", "Number of Add operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Updates/sec", "Number of Insert operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Deletes/sec", "Number of Remove operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Read Operations/sec", "Number of Read operations per second", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Write Operations/sec", "Number of Write operations per second", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average us/fetch", "Average time in microseconds (us), taken to complete one fetch operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/fetch base", "Base counter for average microseconds (us)/fetch", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/add", "Average time in microseconds (us), taken to complete one add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/add base", "Base counter for average microseconds (us)/add", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/insert", "Average time in microseconds, taken to complete one insert operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/insert base", "Base counter for average microseconds (us) F/insert", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/remove", "Average time in microseconds (us), taken to complete one remove operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/remove base", "Base counter for average microseconds (us)/remove", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Request queue size","Total number of requests from all clients on a single machine waiting for response from cache server",System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Average Item Size","Average size of the item added to/fetched from the cache by the client. Average size is calculated before compression/after decompression is applied.",System.Diagnostics.PerformanceCounterType.AverageCount64),
            new System.Diagnostics.CounterCreationData("Average Item Size base","Base counter for average size of the item added to the cache by the client. Average size is calculated before compression is applied.",System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/event","Average time in microseconds (us), taken in single event processing on the client.",System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/event base"," Average time in microseconds (us), taken in single event proccesing on the clients.",System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Events Proccesed/sec","Number of events processed per sec on client.",System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Events Triggered/sec","Number of events triggered and received by client per second.",System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average us/serialization","Average time in microseconds (us), taken to serialize one object.",System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/serialization base","Base counter for Average microseconds (us)/serialization.",System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/deserialization","Average time in microseconds (us), taken to deserialize one object.",System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/deserialization base","Base counter for Average microseconds (us)/deserialization.",System.Diagnostics.PerformanceCounterType.AverageBase),
            
            //Bulk Counters

            new System.Diagnostics.CounterCreationData("Average us/addbulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/addbulk base", "Base counter for Average microseconds (us)/addbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/fetchbulk", "Average time in microseconds (us), taken to complete bulk get operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/fetchbulk base", "Base counter for Average microseconds (us)/fetchbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/insertbulk", "Average time in microseconds (us), taken to complete bulk insert operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/insertbulk base", "Base counter for Average microseconds (us)/insertbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/removebulk", "Average time in microseconds (us), taken to complete bulk remove operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/removebulk base", "Base counter for Average microseconds (us)/removebulk", System.Diagnostics.PerformanceCounterType.AverageBase),

            new System.Diagnostics.CounterCreationData(CounterNames.AvgPublishMessage, "Average time in microseconds, taken to complete publish messages operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData(CounterNames.AvgPublishMessageBase, "Base counter for Average us/publish messages", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData(CounterNames.MessagePublishPerSec, "Number of messages published per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData(CounterNames.MessageDeliveryPerSec, "Number of messages delivered to subsribers per second.", System.Diagnostics.PerformanceCounterType.SampleCounter)

            };
#if !NETCORE
            this.pcClientInstaller.Counters.AddRange(counterCreationData);
            // 
            // PerfInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] { this.pcClientInstaller });
#elif NETCORE
            if (!System.Diagnostics.PerformanceCounterCategory.Exists(categoryName))
                System.Diagnostics.PerformanceCounterCategory.Create(categoryName, "Visit Documentation", System.Diagnostics.PerformanceCounterCategoryType.MultiInstance, counterCreationData);
#endif
        }
        #endregion
    }
}