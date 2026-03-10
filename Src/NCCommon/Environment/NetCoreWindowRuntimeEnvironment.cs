using Alachisoft.NCache.Common.Util;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;


namespace Alachisoft.NCache.Common.RuntimeEnvironment
{
    internal class NetCoreWindowRuntimeEnvironment : NCacheRuntimeEnvironment
    {
        private const string SERVICE = "Alachisoft.NCache.Service.exe";
        private const string NETCORE_SERVICE_WIN = "Alachisoft.NCache.Service.dll";
        private const string LoaderSERVICE = "Alachisoft.NCache.LoaderService.exe";
        private const string CACHEHOSTNAME = "Alachisoft.NCache.CacheHost.dll";
        private const string WEBEXUEUTABLENAME = "Alachisoft.NCache.ManagementCenter.exe";
        private const string WEBPROCESSNAME = "Alachisoft.NCache.ManagementCenter";
        private const string LOGGINGDIRNAME = "LoggingDir";
        private const string STRESSTEST = "teststresstool.exe";
        public override string NCacheServicePath 
        { 
            get 
            {
                string path = Path.Combine(_SERVICE_DIR, NETCORE_SERVICE_WIN);
                if (!File.Exists(path))
                {
                    var otherPath = Path.Combine(_SERVICE_DIR, SERVICE);
                    if (File.Exists(otherPath))
                        return otherPath;
                }

                return path;
            } 
        }

        internal override string ServiceName { get { return SERVICE; } }
        internal override string LoaderServiceName { get { return LoaderSERVICE; } }
        internal override string CacheHostName { get { return CACHEHOSTNAME; } }
        internal override string WebExecutableName { get { return WEBEXUEUTABLENAME; } }
        internal override string WebProcessName{ get { return WEBPROCESSNAME; } }
        internal override string StressTestName { get { return STRESSTEST; } }

        public override Process GetCacheHostProcess(RuntimeOption runtimeOption)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = ".",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true,
                    FileName = _DOTNET_CORE_JAVA_EXE,
                    Arguments = "\"" + base.CacheHostProcessPath + "\"" + " " + runtimeOption.Command
                }
            };
            // Process is running after this call
#if NETCORE
            process = ProcessCreator.CreateProcess(process.StartInfo.FileName, process.StartInfo.Arguments);
#endif
            if (process != null)
                return process;

            throw new Runtime.Exceptions.ManagementException("Unable to start Cache Process");

        }
        public override ProcessStartInfo GetWebManagerProcessInfo(RuntimeOption runtimeOption)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = WebManagerProcessPath,
                WorkingDirectory = _WEB_DIR,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            return startInfo;
        }

        public override System.Configuration.Configuration LoadConfiguration()
        {

            string path = String.Concat(NCacheServicePath, ".config");

            if (File.Exists(path))
            {
                return ConfigurationManager.OpenExeConfiguration(NCacheServicePath);
            }

            // if check is introduce when framework client with dotnetcore installation read service configuration. 
            string NCacheServicedllPath = Path.Combine(_SERVICE_DIR, SERVICE);
            path = String.Concat(NCacheServicedllPath, ".config");

            if (File.Exists(path))
                return ConfigurationManager.OpenExeConfiguration(NCacheServicedllPath);

            return null;

        }

        public override Process GetStressTestProcess(RuntimeOption runtimeOption)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
            };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.FileName = Path.Combine(_STRESSTEST_PATH, StressTestName);
            process.StartInfo.Arguments = runtimeOption.Command;
            ;

            if (!process.Start())
            {
                throw new Runtime.Exceptions.ManagementException("Unable to start Test-Stress Process");
            }
            return process;
        }
    }
}
