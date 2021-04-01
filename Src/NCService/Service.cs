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
using System.Data;
using System.Diagnostics;
//using System.Diagnostics.Eventing.Reader;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Configuration;
using System.Reflection;
using System.Security.Permissions;
using System.Security;
using System.Net;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.SocketServer;
using System.Runtime.InteropServices;
using System.Threading;
using Alachisoft.NCache.Common;
using System.Timers;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Service
{

    class Service : System.ServiceProcess.ServiceBase
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components;
        private static ServiceHost _serviceHost;
        public Service()
        {
			
            InitializeComponent();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppUtil.LogEvent("NCache", ((Exception)e.ExceptionObject).ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.UnhandledException);
        }

        // The main entry point for the process
        static void Main(string[] args)
        {

            _serviceHost = new ServiceHost();
            _serviceHost.InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.ServiceProcess.ServiceBase[] ServicesToRun;
            // More than one user Service may run within the same process. To add
            // another service to this process, change the following line to
            // create a second service object. For example,
            //
            //   ServicesToRun = new System.ServiceProcess.ServiceBase[] {new Service1(), new MySecondUserService()};
            //
            ServicesToRun = new System.ServiceProcess.ServiceBase[] { new Service() };
            System.ServiceProcess.ServiceBase.Run(ServicesToRun);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (components != null)
                    {
                        components.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// in case of reactivation function to log the event
        /// 
        /// </summary>     
        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            _serviceHost.Start();
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
        {
            _serviceHost.Stop();
        }


        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // ServiceHost
            // 

            ServiceName = "NCacheSvc";
           
        }
    }
}
