// Copyright (c) 2018 Alachisoft
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
// limitations under the License

using System;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.IO;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.SocketServer;

namespace Alachisoft.NCache.Daemon
{
    class Daemon
    {
        private static Object s_servicehost;

        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
            DaemonOperations(args);
        }

        static void DaemonOperations(string[] args)
        {
            if (args.Length != 0)
            {
                if (args[0].Equals("start"))
                {
                    AppUtil.LogEvent("NCacheSvc", "NCache daemon is starting..... ", EventLogEntryType.Information, EventCategories.Information, EventID.ServiceStart);
                    StartDaemon();
                }
                else if (args[0].Equals("stop"))
                {
                    AppUtil.LogEvent("NCacheSvc", "NCache daemon is stopping..... ", EventLogEntryType.Information, EventCategories.Information, EventID.ServiceStop);
                    StopDaemon();
                    AppUtil.LogEvent("NCacheSvc", "NCache daemon has stopped. ", EventLogEntryType.Information, EventCategories.Information, EventID.ServiceStop);
                }

                else
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "-c \" " + args[0] + "/dotnet " + args[1] + "/bin/service/Alachisoft.NCache.Daemon.dll start " + "\"" };
                    Process proc = new Process() { StartInfo = startInfo, };
                    proc.Start();
                }
            }
        }

        private static void StartDaemon()
        {
            ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
            //Alachisoft.NCache.ServiceHost.ServiceUtil service = new Alachisoft.NCache.ServiceHost.ServiceUtil();
            s_servicehost = new Alachisoft.NCache.SocketServer.ServiceHost();
            Alachisoft.NCache.Common.Util.ServiceConfiguration.Load();
            string[] args = null;
            ((Alachisoft.NCache.SocketServer.ServiceHost)s_servicehost).Start(args);
            AppUtil.LogEvent("NCacheSvc", "NCache daemon has started. ", EventLogEntryType.Information, EventCategories.Information, EventID.ServiceStart);
            _manualResetEvent.WaitOne();
            ((Alachisoft.NCache.SocketServer.ServiceHost)s_servicehost).Stop();
        }

        private static void StopDaemon()
        {
        }

        private static Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location);
                string installDir = directoryInfo.Parent.Parent.FullName;
                return Assembly.LoadFrom(Path.Combine(Path.Combine(installDir, "lib"), new AssemblyName(args.Name).Name + ".dll"));
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
