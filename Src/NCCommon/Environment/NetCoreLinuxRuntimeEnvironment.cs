using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Alachisoft.NCache.Licensing.LinuxUtil;
#if NETCORE
using Alachisoft.NCache.Licensing.RegistryUtil;
#endif
namespace Alachisoft.NCache.Common.RuntimeEnvironment
{
    internal class NetCoreLinuxRuntimeEnvironment : NCacheRuntimeEnvironment
    {
        private const string SERVICE = "Alachisoft.NCache.Daemon.dll";
        private const string CACHEHOSTNAME = "Alachisoft.NCache.CacheHost.dll";
        private const string WEBEXUEUTABLENAME = "Alachisoft.NCache.ManagementCenter.dll";
        private const string NETCORE_STRESSTEST = "teststresstool.dll";

        private const string CloudSERVICE = "Alachisoft.NCache.CloudDaemon.dll";
        private const string CloudSERVICE_JAVA = "Alachisoft.NCache.CloudDaemon";

        private new string _LUCENE_INDEXPATH = Path.Combine(Path.DirectorySeparatorChar.ToString(), "usr", "share", "ncache", "lucene-index");

        public override string LuceneIndexPath { get { return _LUCENE_INDEXPATH; } }

        public override string NActivatePath { get { return Path.Combine(AppUtil.InstallDir, "lib"); } }

        internal override string ServiceName { get { return SERVICE; } }
        internal override string CacheHostName { get { return CACHEHOSTNAME; } }
        internal override string WebExecutableName { get { return WEBEXUEUTABLENAME; } }
        internal override string WebProcessName { get { return WEBEXUEUTABLENAME; } }
        internal override string StressTestName { get { return NETCORE_STRESSTEST; } }

        internal override string CloudServiceName { get { return CloudSERVICE; } }

        public override string NCacheServicePath
        {
            get
            {
                return Path.Combine(_SERVICE_DIR, SERVICE);
            }
        }
        public override ProcessStartInfo GetBridgeServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return new ProcessStartInfo()
            {
                FileName = _LINUX_SHELL_NAME,
                Arguments = "-c \" " + _LINUX_PROCESS_NAME+ " "  + BridgeServicePath +" "+ runtimeOption.Command + "\""
            };

        }

        public override ProcessStartInfo GetNCacheServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return new ProcessStartInfo()
            {
                FileName = _LINUX_SHELL_NAME,
                Arguments = "-c \" " + _LINUX_PROCESS_NAME +" "+ 
                NCacheServicePath + " "+runtimeOption.Command + " " + runtimeOption.Argument + "\""
            };

        }

        public override ProcessStartInfo GetLoaderServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return new ProcessStartInfo()
            {
                FileName = _LINUX_SHELL_NAME,
                Arguments = "-c \" " + _LINUX_PROCESS_NAME +" " + LoaderServicePath + " "+runtimeOption.Command + "\""
            };

        }
        public override Process GetCacheHostProcess(RuntimeOption runtimeOption)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = System.Environment.CurrentDirectory,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    FileName = _LINUX_PROCESS_NAME,
                    Arguments = base.CacheHostProcessPath + " " + runtimeOption.Command
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
                FileName = _LINUX_PROCESS_NAME,
                Arguments = WebManagerProcessPath,
                WorkingDirectory = _WEB_DIR,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal
            };
            return startInfo;
        }
        public override bool IsWebProcessRunning()
        {
            string result = ("pgrep -af dotnet.*" + WebExecutableName).Bash();
            foreach (string line in result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    string[] tokens = Regex.Split(line, "\\s+");
                    if (int.TryParse(tokens[0], out _))
                    {
                        return true;
                    }

                }
            }
            return false;
        }

        public override bool KillWebProcess()
        {
            string result = ("pgrep -af dotnet.*" + WebExecutableName).Bash();
            foreach (string line in result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    string[] tokens = Regex.Split(line, "\\s+");
                    if (int.TryParse(tokens[0], out int pid))
                    {
                        var process = Process.GetProcessById(pid);
                        process?.Kill();
                        return true;
                    }

                }
            }
            return false;
        }

        public override Assembly LoadAssembly(RuntimeOption runtimeOption)
        {
            string path;
            if (runtimeOption.Location.Contains("ncache" + Path.DirectorySeparatorChar + "deploy"))
            {
                path = Path.GetDirectoryName(runtimeOption.Location);
            }
            else
            {
                string location = Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location);
                string installDir = directoryInfo.Parent.Parent.FullName; /// in installdir of linux
                path = Path.Combine(installDir, _LIB_DIR); /// in assembly folder              
            }

            return Assembly.LoadFrom(Path.Combine(path, new AssemblyName(runtimeOption.Name).Name + ".dll"));

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
            process.StartInfo.FileName = _LINUX_PROCESS_NAME;
            process.StartInfo.Arguments = Path.Combine(_STRESSTEST_PATH, NETCORE_STRESSTEST) + " " + runtimeOption.Command;


            if (!process.Start())
            {
                throw new Runtime.Exceptions.ManagementException("Unable to start Test-Stress Process");
            }
            return process;
        }

        public override ProcessStartInfo GetCloudMonitoringServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return new ProcessStartInfo()
            {
                FileName = _LINUX_SHELL_NAME,
#if NETCOREJAVA
                Arguments = "-c \" " + Path.Combine(_SERVICE_DIR, CloudSERVICE_JAVA) + " " + runtimeOption.Command + "\""
#else
                Arguments = "-c \" " + _LINUX_PROCESS_NAME + " " + CloudServicePath + " " + runtimeOption.Command + "\""
#endif
            };
        }
    }
}
