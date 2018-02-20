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
