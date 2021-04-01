using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsLifecycle.Invoke, "MonitorServer")]
    public class MonitorServerBase :MonitorServerParameters , IConfiguration
    {
        private NCacheRPCService NCache = new NCacheRPCService("");
        private ArrayList s_cacheId = new ArrayList();
        private string TOOLNAME = "MonitorServer Tool";
        public bool ValidateParameters ()
        {

            try
            {
                NCache.Port = Port;
            }
            catch (FormatException) { }
            catch (OverflowException) { }

            if (Port > 1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;

            bool validAction = false;

            if (!string.IsNullOrEmpty(Action))
            {
                if (Action.ToLower() == "start")
                    validAction = true;

                if (Action.ToLower() == "stop")
                    validAction = true;
            }

            if (IsUsage || !validAction)
            {
               
                return false;
            }
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }

        public void MonitorServer()
        {

            try
            {
                if (!ValidateParameters()) return;

                if (!Server.Equals(string.Empty))
                {
                    NCache.ServerName = Server;
                }

                if (Port == -1)
                {
                    NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                }
                else
                {
                    NCache.Port = Port;
                }


                ICacheServer m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                if (m != null)
                {

                    try
                    {
                        if (Action.ToLower() == "start")
                        {
                            OutputProvider.WriteLine("Starting monitoring on server {0}:{1}.", NCache.ServerName, NCache.Port);
                            m.StartMonitoringActivity();
                        }
                        if (Action.ToLower() == "stop")
                        {
                            OutputProvider.WriteLine("Stopping monitoring on server {0}:{1}.", NCache.ServerName, NCache.Port);
                            m.StopMonitoringActivity();
                            m.PublishActivity();
                        }
                    }
                    catch (SecurityException e)
                    {
                        OutputProvider.WriteErrorLine("Failed to '{0}' monitoring . Error: {1} ", Action.ToLower(), e.Message);
                    }

                    catch (Exception e)
                    {
                        OutputProvider.WriteErrorLine("Failed to '{0}' monitoring. Error: {1} ", Action.ToLower(), e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);
                
            }
            finally
            {
                NCache.Dispose();
            }
        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);

        }
#if NETCORE
        private static System.Reflection.Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            string final = "";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string bin = directoryInfo.Parent.Parent.FullName; //bin folder
                final = System.IO.Path.Combine(bin, "service"); /// from where you neeed the assemblies
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string installDir = directoryInfo.Parent.FullName; //linux install directory
                directoryInfo = Directory.GetParent(installDir); //go back one directory
                installDir = directoryInfo.FullName;
                final = System.IO.Path.Combine(installDir, "lib");
            }
            return System.Reflection.Assembly.LoadFrom(System.IO.Path.Combine(final, new AssemblyName(args.Name).Name + ".dll"));
        }
#endif

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Invoke-MonitorServer Cmdlet";
                MonitorServer();
            }
            catch (System.Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

        protected override void ProcessRecord()
        {
            try { }
            catch { }

        }

    }
}
