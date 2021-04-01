using System;
using System.Collections;
using System.ComponentModel;
#if !NETCORE
using System.Configuration.Install;
#endif

namespace Alachisoft.NCache.SocketServer.Statistics
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
            var categoryName = "NCache Server";

#if !NETCORE
            this.pcInstaller = new System.Diagnostics.PerformanceCounterInstaller();
			// 
			// pcInstaller
			// 
			this.pcInstaller.CategoryName = categoryName;
            System.Diagnostics.CounterCreationData[] counterCreationData = new System.Diagnostics.CounterCreationData[] 
#elif NETCORE
            var counterCreationData = new System.Diagnostics.CounterCreationDataCollection()
#endif
            {

                                                                                                new System.Diagnostics.CounterCreationData("Requests/sec", "Number of requests (meaning cache commands like add, get, insert, remove etc.) being processed from all clients by this cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Bytes sent/sec", "Bytes being sent from cache server to all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
                                                                                                new System.Diagnostics.CounterCreationData("Bytes received/sec", "Bytes being received by cache server from all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64)

            };

#if !NETCORE
            this.pcInstaller.Counters.AddRange(counterCreationData);
			// 
			// PerfInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] { this.pcInstaller});
#elif NETCORE
            if (!System.Diagnostics.PerformanceCounterCategory.Exists(categoryName))
                System.Diagnostics.PerformanceCounterCategory.Create(categoryName, "Visit Documentation", counterCreationData);
#endif

        }
        #endregion
    }
}
