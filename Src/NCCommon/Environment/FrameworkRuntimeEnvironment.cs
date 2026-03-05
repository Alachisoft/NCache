using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Alachisoft.NCache.Common.RuntimeEnvironment
{
    internal class FrameworkRuntimeEnvironment : NCacheRuntimeEnvironment
    {
        private const string SERVICE = "Alachisoft.NCache.Service.exe";
        private const string CACHEHOSTNAME = "Alachisoft.NCache.CacheHost.exe";
        private const string WEBEXUEUTABLENAME = "Alachisoft.NCache.ManagementCenter.exe";
        private const string WEBPROCESSNAME = "Alachisoft.NCache.ManagementCenter";
        private const string LOGGINGDIRNAME = "LoggingDir";
        private const string NETCORE_SERVICE_WIN = "Alachisoft.NCache.Service.dll";
        private const string STRESSTEST = "teststresstool.exe";

        internal override string ServiceName { get { return SERVICE; } }
        internal override string CacheHostName { get { return CACHEHOSTNAME; } }
        internal override string WebExecutableName { get { return WEBEXUEUTABLENAME; } }
        internal override string WebProcessName { get { return WEBPROCESSNAME; } }
        internal override string StressTestName { get { return STRESSTEST; } }

        public override string NCacheServicePath
        {
            get
            {
                return Path.Combine(_SERVICE_DIR, SERVICE);
            }
        }

        public override Process GetCacheHostProcess(RuntimeOption runtimeOption)
        {

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = ".",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true,
                    FileName = CacheHostProcessPath,
                    Arguments = runtimeOption.Command
                }
            };

            if (!process.Start())
            {
                throw new Runtime.Exceptions.ManagementException("Unable to start Cache Process");
            }

            return process;
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
            string NCacheServicedllPath = Path.Combine(_SERVICE_DIR, NETCORE_SERVICE_WIN);
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
