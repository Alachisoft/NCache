using Alachisoft.NCache.Licensing.LinuxUtil;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Alachisoft.NCache.Common.RuntimeEnvironment
{
    internal class JavaRuntimeEnvironment: NCacheRuntimeEnvironment
    {
        private const string SERVICE = "Alachisoft.NCache.Daemon";
        private const string LoaderSERVICE = "Alachisoft.NCache.LoaderDaemon";
        private const string CACHEHOSTNAME = "Alachisoft.NCache.CacheHost";
        private const string WEBEXUEUTABLENAME = "Alachisoft.NCache.ManagementCenter";

        public override string NCacheServicePath { get { return Path.Combine(_SERVICE_DIR, ServiceName); } }

        internal override string ServiceName { get { return SERVICE; } }
        internal override string LoaderServiceName { get { return LoaderSERVICE; } }
        internal override string CacheHostName { get { return CACHEHOSTNAME; } }
        internal override string WebExecutableName { get { return WEBEXUEUTABLENAME; } }
        internal override string WebProcessName { get { return WEBEXUEUTABLENAME; } }
        public override string CacheHostProcessPath { get { return Path.Combine(_SERVICE_DIR, CacheHostName); } }


        public override ProcessStartInfo GetNCacheServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return new ProcessStartInfo()
            {
                FileName =  NCacheServicePath,
                Arguments = runtimeOption.Command
            };

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
                path = Path.Combine(path, new AssemblyName(runtimeOption.Name).Name + ".dll");
            }
            return Assembly.LoadFrom(Path.Combine(path, new AssemblyName(runtimeOption.Name).Name + ".dll"));

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
                    FileName = CacheHostProcessPath,
                    Arguments = runtimeOption.Command
                }
            };
            return process;
        }

        public override ProcessStartInfo GetWebManagerProcessInfo(RuntimeOption runtimeOption)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = WebManagerProcessPath,
                WorkingDirectory = _WEB_DIR,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal
            };
            return startInfo;
        }

        public override System.Configuration.Configuration LoadConfiguration()
        {
            var ExeConfigFilename = NCacheServicePath + ".dll.config";
            if (File.Exists(ExeConfigFilename))
            {
                var fileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = ExeConfigFilename
                };

                return ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            }
            return null;
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
            string result = "pgrep -af dotnet.*" + WebExecutableName.Bash();
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
    }
}