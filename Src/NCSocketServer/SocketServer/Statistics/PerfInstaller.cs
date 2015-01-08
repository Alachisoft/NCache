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

namespace Alachisoft.NCache.SocketServer.Statistics
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
			this.pcInstaller = new System.Diagnostics.PerformanceCounterInstaller();
			// 
			// pcInstaller
			// 
			this.pcInstaller.CategoryName = "NCache Server";
			this.pcInstaller.Counters.AddRange(new System.Diagnostics.CounterCreationData[] {
                                                                                                new System.Diagnostics.CounterCreationData("Requests/sec", "Number of requests (meaning cache commands like add, get, insert, remove etc.) being processed from all clients by this cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Bytes sent/sec", "Bytes being sent from cache server to all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Bytes received/sec", "Bytes being received by cache server from all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64)

            });
			// 
			// PerfInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
																					  this.pcInstaller});

		}
		#endregion
	}
}
