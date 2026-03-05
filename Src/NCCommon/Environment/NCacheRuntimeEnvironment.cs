
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
#if NETCORE
using Alachisoft.NCache.Licensing.RegistryUtil;
#endif

namespace Alachisoft.NCache.Common.RuntimeEnvironment
{
    public class NCacheRuntimeEnvironment
    {
        internal readonly Platform platform = Platform.WindowsFrameWork;

        internal static NCacheRuntimeEnvironment _intance = null;

        public readonly string _WEB_DIR     = null;
        internal readonly string _SERVICE_DIR = null;
        internal readonly string _Deploy_PATH = null;
        internal readonly string _PUBLISH_FOLDER = null;
        internal readonly string _LIB_DIR = Path.Combine("lib");
        internal readonly string _ASSEMBLY_DIR = Path.Combine("assembly");
        internal readonly string _ASSEMBLY_NETCORE_20 = Path.Combine("assembly", "netcore20");
        internal readonly string _LINUX_SHELL_NAME = "/bin/bash";
        internal readonly string _LINUX_PROCESS_NAME = "dotnet";
        internal readonly string _DOTNET_CORE_EXE = "dotnet.exe";
        internal readonly string _DOTNET_CORE_JAVA_EXE = "Alachisoft.NCache.CacheHost.exe";
        internal readonly string _STRESSTEST_PATH = null;
        internal string _LUCENE_INDEXPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ncache", "lucene-index");

        internal NCacheRuntimeEnvironment()
        {
            _WEB_DIR = Path.Combine(AppUtil.InstallDir, "bin", "tools", "web");
            _SERVICE_DIR = Path.Combine(AppUtil.InstallDir, "bin", "service");
            _PUBLISH_FOLDER = Path.Combine(AppUtil.InstallDir, "bin", "published");
            _Deploy_PATH = Path.Combine(AppUtil.InstallDir, "bin", "deploy");
            _STRESSTEST_PATH = Path.Combine(AppUtil.InstallDir, "bin", "tools");


#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platform = Platform.WindowsDotNetCore;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {

                platform = Platform.LinuxDotNetCore;
            }
#else
            platform = Platform.WindowsFrameWork;
#endif
        }

        public virtual string NActivatePath { get { return Path.Combine(AppUtil.InstallDir, "bin", "NActivate"); } }

        //Load Assembly path
        //File Path Executable (Services, bridge, loader, webmanager,  CacheProcess)
        //File reading (config files / stub files/ service config)

        public static NCacheRuntimeEnvironment GetEnvironment
        {
            get
            {
                if (_intance == null)
                    _intance = new NCacheRuntimeEnvironment();

                return _intance.NCacheRuntimeEnvironmentIntance;
            }
        }

        private NCacheRuntimeEnvironment NCacheRuntimeEnvironmentIntance {
            get
            {
                switch (platform)
                {
                    case Platform.WindowsFrameWork:
                        return new FrameworkRuntimeEnvironment();
                    case Platform.WindowsDotNetCore:
                        return new NetCoreWindowRuntimeEnvironment();
                    case Platform.LinuxDotNetCore:
                        return new NetCoreLinuxRuntimeEnvironment();
                    case Platform.LinuxJavaClient:
                        return new JavaRuntimeEnvironment();
                    default:
                        return new FrameworkRuntimeEnvironment();
                }
            }
        }

        internal enum Platform
        {
            LinuxDotNetCore,
            LinuxJavaClient,
            WindowsDotNetCore,
            WindowsFrameWork
        }

        public virtual string LuceneIndexPath { get { return _LUCENE_INDEXPATH; } }
        public virtual string NCacheServicePath { get { return Path.Combine(_SERVICE_DIR, ServiceName); } }

        public string CloudServicePath { get { return Path.Combine(_SERVICE_DIR, CloudServiceName); } }

        public string BridgeServicePath { get { return Path.Combine(_SERVICE_DIR, BridgeServiceName); } }
      
        public string LoaderServicePath { get { return Path.Combine(_SERVICE_DIR, LoaderServiceName); } }

        public virtual string CacheHostProcessPath { get { return Path.Combine(_SERVICE_DIR, CacheHostName); } }

        public virtual string WebManagerProcessPath { get { return Path.Combine(_WEB_DIR, WebExecutableName); } }

        internal virtual string ServiceName { get; }

        internal virtual string LoaderServiceName { get; }

        internal virtual string BridgeServiceName { get; }

        internal virtual string CloudServiceName { get; }

        internal virtual string CacheHostName { get; }

        internal virtual string WebExecutableName { get; }

        internal virtual string WebProcessName { get; }
        internal virtual string StressTestName { get; }

        public virtual ProcessStartInfo GetNCacheServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return null;
        }

        public virtual ProcessStartInfo GetBridgeServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return null;
        }

        public virtual ProcessStartInfo GetLoaderServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return null;
        }

        public virtual ProcessStartInfo GetCloudMonitoringServiceProcessInfo(RuntimeOption runtimeOption)
        {
            return null;
        }

        public virtual ProcessStartInfo GetWebManagerProcessInfo(RuntimeOption runtimeOption)
        {
            return null;
        }

        public virtual Process GetCacheHostProcess(RuntimeOption runtimeOption)
        {
            return null;
        }

        public virtual Assembly LoadAssembly(RuntimeOption runtimeOption)
        {
            return null;
        }

        public virtual bool IsWebProcessRunning()
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains(WebProcessName))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool KillWebProcess()
        {
            Process[] processes = Process.GetProcessesByName(WebProcessName);
            if (processes.Length != 0)
            { 
                foreach (var process in processes)
                {
                    process.Kill();
                }
                return true;
            }
            return false;
        }

        public virtual System.Configuration.Configuration LoadConfiguration()
        {
            if (File.Exists(NCacheServicePath))
            {
                return ConfigurationManager.OpenExeConfiguration(NCacheServicePath);
            }
            return null;
        }



        public System.Configuration.Configuration LoadLoaderConfiguration()
        {
            if (File.Exists(LoaderServicePath))
            {
                return ConfigurationManager.OpenExeConfiguration(LoaderServicePath);
            }
            return null;
        }

        public virtual Process GetStressTestProcess(RuntimeOption runtimeOption)
        {
            return null;
        }
    }
}
