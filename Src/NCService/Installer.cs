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
using System.Configuration.Install;
using System.Diagnostics;
using Microsoft.Win32;

namespace Alachisoft.NCache.Service
{
	/// <summary>
	/// Summary description for ProjectInstaller.
	/// </summary>
	[RunInstaller(true)]
	public class Installer : System.Configuration.Install.Installer
	{
		private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
		private System.ServiceProcess.ServiceInstaller serviceInstaller;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components;

		/// <summary>
		/// Constructor
		/// </summary>
		public Installer()
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
			try
			{
				if( disposing )
				{
					if(components != null)
					{
						components.Dispose();
					}
					if(serviceProcessInstaller != null)
					{
						serviceProcessInstaller.Dispose();
					}
					if(serviceInstaller != null)
					{
						serviceInstaller.Dispose();
					}
				}
			}
			finally
			{
				base.Dispose( disposing );
			}
		}


		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// serviceProcessInstaller
			// 
			this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.serviceProcessInstaller.Password = null;
			this.serviceProcessInstaller.Username = null;
			// 
			// serviceInstaller
			// 

            this.serviceInstaller.DisplayName = "NCache";
            
            this.serviceInstaller.ServiceName = "NCacheSvc";

            this.serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
																					  this.serviceProcessInstaller,
																					  this.serviceInstaller});

		}
		#endregion

		/// <summary>
		/// Installs the service by writing service application information to the registry. 
		/// This method is meant to be used by installation tools, which process the 
		/// appropriate methods automatically.
		/// </summary>
		/// <param name="stateSaver">An IDictionary that contains the context information 
		/// associated with the installation.</param>
		public override void Install(IDictionary stateSaver)
		{
			//HKEY_LOCAL_MACHINE\Services\CurrentControlSet
			base.Install(stateSaver);
			try
			{
				RegistryKey services = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Services");
				RegistryKey service = services.OpenSubKey(serviceInstaller.ServiceName, true);
                service.SetValue("Description", "Provides out-proc caching and clustering. Allows local and remote management of NCache configuration.");

			}
			catch(Exception e)
			{
				Console.Error.WriteLine("Installation Error:\n" + e.ToString());
			}
		}
		
		/// <summary>
		/// Uninstalls the service by removing information about it from the registry.
		/// </summary>
		/// <param name="savedState">An IDictionary that contains the context information associated 
		/// with the installation.</param>
		public override void Uninstall(IDictionary savedState)
		{
			try
			{
                KillAlachisoftProcesses();
                RegistryKey services = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Services");
				RegistryKey service = services.OpenSubKey(serviceInstaller.ServiceName, true);
				if(service != null)
				{
					service.DeleteValue("Description");
				}
				//services.DeleteSubKeyTree(serviceInstaller.ServiceName);
				//Delete any keys you created during installation (or that your service created)
				//service.DeleteSubKeyTree("Parameters");
			}
			catch(Exception e)
			{
				Console.Error.WriteLine("Uninstallation Error:\n" + e.ToString());
			}
			finally
			{
				base.Uninstall(savedState);
			}
		}

        private void KillAlachisoftProcesses()
        {
            try
            {

                Process[] nCacheSvcProc = Process.GetProcessesByName("Alachisoft.NCache.Service");
                Process[] nCacheBridgeSvcProc = Process.GetProcessesByName("Alachisoft.NCache.BridgeService");

                if (nCacheSvcProc != null && nCacheSvcProc.Length > 0)
                    (nCacheSvcProc[0]).Kill();

                if (nCacheBridgeSvcProc != null && nCacheBridgeSvcProc.Length > 0)
                    (nCacheBridgeSvcProc[0]).Kill();

                nCacheSvcProc = Process.GetProcessesByName("Alachisoft.NCache.Service.exe");

                nCacheBridgeSvcProc = Process.GetProcessesByName("Alachisoft.NCache.BridgeService.exe");

            }
            catch (Exception)
            {
            }
        }
    }
}
