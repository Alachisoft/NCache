using System;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.IO;

namespace Alachisoft.NCache.Daemon
{
    class NCDaemon
    {
        private static Object s_servicehost;

        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
            Daemon(args);
        }

        static void Daemon(string[] args)
        {
            if (args.Length != 0)
            {
                if (args[0].Equals("start"))
                {
                    Common.AppUtil.LogEvent("NCacheSvc", "NCache daemon is starting..... ", EventLogEntryType.Information, Common.EventCategories.Information, Common.EventID.ServiceStart);
                    StartDaemon();
                }
                else if (args[0].Equals("stop"))
                {
                    Common.AppUtil.LogEvent("NCacheSvc", "NCache daemon is stopping..... ", EventLogEntryType.Information, Common.EventCategories.Information, Common.EventID.ServiceStop);
                    StopDaemon();
                    Common.AppUtil.LogEvent("NCacheSvc", "NCache daemon has stopped. ", EventLogEntryType.Information, Common.EventCategories.Information, Common.EventID.ServiceStop);
                }

                else
                {
                  //  ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "-c \" " + args[0] + "/dotnet " + args[1] + "/bin/service/Alachisoft.NCache.Daemon.dll start " + "\"" };
                    ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "-c \" " + "dotnet " + args[0] + "/bin/service/Alachisoft.NCache.Daemon.dll start " + "\"" };
                    Process proc = new Process() { StartInfo = startInfo, };
                    proc.Start();
                }
            }
        }

        private static void StartDaemon()
        {
            ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
            s_servicehost = new Alachisoft.NCache.SocketServer.ServiceHost();
            Alachisoft.NCache.Common.Util.ServiceConfiguration.Load();
            string[] args = null;
            ((Alachisoft.NCache.SocketServer.ServiceHost)s_servicehost).Start();
            Common.AppUtil.LogEvent("NCacheSvc", "NCache daemon has started. ", EventLogEntryType.Information, Common.EventCategories.Information, Common.EventID.ServiceStart);
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
                AssemblyName asmName=new AssemblyName(args.Name);
                if (asmName.Name.StartsWith("System.IO.FileSystem.resources") ||
                    asmName.Name.StartsWith("System.Runtime.Serialization.Formatters.resources"))
                    return null;

                string location = Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location);
                string installDir = directoryInfo.Parent.Parent.FullName;
                return Assembly.LoadFrom(Path.Combine(Path.Combine(installDir, "lib"), asmName.Name + ".dll"));
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
