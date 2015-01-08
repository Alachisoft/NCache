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

namespace Alachisoft.NCache.Web.Caching.Statistics
{
	/// <summary>
	/// Summary description for PerfInstaller.
	/// </summary>
	[RunInstaller(true)]
	public class PerfInstaller : System.Configuration.Install.Installer
	{
        private System.Diagnostics.PerformanceCounterInstaller pcClientInstaller;
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


		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{

            this.pcClientInstaller = new System.Diagnostics.PerformanceCounterInstaller();
            this.pcClientInstaller.CategoryName = "NCache Client";
            this.pcClientInstaller.Counters.AddRange(new System.Diagnostics.CounterCreationData[] {
            new System.Diagnostics.CounterCreationData("Fetches/sec", "Number of Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
			new System.Diagnostics.CounterCreationData("Additions/sec", "Number of Add operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
			new System.Diagnostics.CounterCreationData("Updates/sec", "Number of Insert operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
			new System.Diagnostics.CounterCreationData("Deletes/sec", "Number of Remove operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
			new System.Diagnostics.CounterCreationData("Average µs/fetch", "Average time in microseconds, taken to complete one fetch operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average µs/fetch base", "Base counter for average µs/fetch", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average µs/add", "Average time in microseconds, taken to complete one add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average µs/add base", "Base counter for average µs/add", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average µs/insert", "Average time in microseconds, taken to complete one insert operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average µs/insert base", "Base counter for average µs/insert", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average µs/remove", "Average time in microseconds, taken to complete one remove operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average µs/remove base", "Base counter for average µsec/remove", System.Diagnostics.PerformanceCounterType.AverageBase),                                                                                       
            new System.Diagnostics.CounterCreationData("Request queue size","Total number of requests from all clients on a single machine waiting for response from cache server",System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Average µs/event"," Average time taken in single event processing on the client.",System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average µs/event base"," Average time taken in single event proccesing on the clients.",System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Events Processed/sec","Number of events processed per sec on client.",System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Events Triggered/sec","Number of events triggered and received by client per second.",System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average µs/serialization","Average time in microseconds, taken to serialize/deserialize one object.",System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average µs/serialization base","Base counter for Average µs/serialization.",System.Diagnostics.PerformanceCounterType.AverageBase)});

            // 
			// PerfInstaller
			// 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] { this.pcClientInstaller });
		}
		#endregion
	}
}
