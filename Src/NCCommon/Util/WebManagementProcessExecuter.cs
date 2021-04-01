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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
#if NETCORE
using Alachisoft.NCache.Licensing.NetCore.LinuxUtil;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

#endif

namespace Alachisoft.NCache.Common.Util
{
    /// <summary>
    /// Process Executer class to manage web manager process.
    /// </summary>
    public class WebManagementProcessExecuter
    {
        static string _DEFAULT_URL = "http://127.0.0.0:8251";
        static string _WEB_CONFIG_NAME = "config.json";
        static int _RETRIES = ServiceConfiguration.WebManagerStartRetries;
        static int _RETRY_INTERVAL = 5000;
        static private readonly string _PROCESS_NAME = "Alachisoft.NCache.WebManager";
        static private readonly string _WEB_DIR = Path.Combine(AppUtil.InstallDir, "bin" + Path.DirectorySeparatorChar + "tools" + Path.DirectorySeparatorChar + "web");
        static private string _EXE_PATH = Path.Combine(_WEB_DIR, _PROCESS_NAME + ".exe");
#if NETCORE
        static private readonly string LINUX_PROCESS_NAME = "dotnet";
        static private string LINUX_DLL_PATH = Path.Combine(_WEB_DIR, _PROCESS_NAME + ".dll");
#endif
        /// <summary>
        /// Method to execute web manager process for management commands.
        /// </summary>
        /// <returns>Either URL on which Web manager is running or null if process invokation fails.</returns>
        public static string ExecuteProcess()
        {
            if (IsProcessRunning(_PROCESS_NAME))
            {
                throw new InvalidOperationException("NCacheWebManagementProcess is already running.");
            }

            Process process = null;
            try
            {
                string url = InvokeWebProcess(out process);

                // If unable to ping kill the process
                if (String.IsNullOrEmpty(url))
                {
                    if (process != null)
                    {
                        process.Kill();
                    }
                }

                return url;
            }
            catch (Exception exc)
            {
                try
                {
                    if (process != null)
                        process.Kill();
                }
                catch (Exception e)
                {
                }
                throw exc;
            }
        }

        /// <summary>
        /// Method to auto start web manager process on service startup.
        /// </summary>
        public static ExecutionStatus AutoStartWebManager()
        {

#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _EXE_PATH = LINUX_DLL_PATH;
#endif
            if (!File.Exists(_EXE_PATH))
            {
                return ExecutionStatus.NotFound;
            }
            else if (!IsProcessRunning(_PROCESS_NAME))
            {
                Process process = null;
                string url = null;
                try
                {
                    url = InvokeWebProcess(out process);
						
                    if(!String.IsNullOrEmpty(url))
                    {
                        return ExecutionStatus.Started;
                    }

                    // If unable to ping kill the process
                    if (process != null)
                    {
                        process.Kill();
                    }
                }
                catch (Exception exc)
                {
                    try
                    {
                        if (process != null)
                            process.Kill();
                    }
                    catch (Exception e)
                    {
                    }
                    throw exc;
                }
            }
            else
            {
                return ExecutionStatus.AlreadyRunning;
            }
            return ExecutionStatus.Error;
        }

        /// <summary>
        /// Invokes the Web Manager process
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static string InvokeWebProcess(out Process process)
        {

#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _EXE_PATH = LINUX_DLL_PATH;
#endif
            if (!File.Exists(_EXE_PATH))
            {
                throw new DirectoryNotFoundException(_PROCESS_NAME + " not found at the following path: " + _WEB_DIR);
            }

            process = new Process();
            //--- Configure the process using the StartInfo properties.

#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                process.StartInfo = ProcessConfigLinux();
            else
#endif
            process.StartInfo = ProcessConfigWindows();


            if (!process.Start())
            {
                throw new Exception("Unable to start NCacheWebManagementProcess.");
            }

            Thread.Sleep(2000);

            string url = GetUrlFromConfig(_WEB_DIR);
            int port = new Uri(url).Port;//--- What if url ends with a farward slash (/)

            if (PingUrl("http://localhost:" + port))
            {
                return url;
            }
            else
            {
                return String.Empty;
            }
        }

#if NETCORE
        /// <summary>
        /// Process Info for execution on Linux platform
        /// </summary>
        /// <returns></returns>
        private static ProcessStartInfo ProcessConfigLinux()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = LINUX_PROCESS_NAME;
            startInfo.Arguments = LINUX_DLL_PATH;
            startInfo.WorkingDirectory = _WEB_DIR;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            return startInfo;
        }

#endif
        /// <summary>
        /// Process Info for execution on Windows platform
        /// </summary>
        /// <returns></returns>
        private static ProcessStartInfo ProcessConfigWindows()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = _EXE_PATH;
            startInfo.WorkingDirectory = _WEB_DIR;
            startInfo.UseShellExecute = true;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            return startInfo;
        }
        

        /// <summary>
        /// Method to stop web manager process.
        /// </summary>
        /// <returns>True if successfully stoped.</returns>
        public static bool StopProcess()
        {
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return KillProcessOnLinux();
#endif

            Process[] processes = Process.GetProcessesByName(_PROCESS_NAME);
            if (processes.Length == 0)
            {
                return false;
            }
            else
            {
                foreach (var process in processes)
                {
                    process.Kill();
                }

                return true;
            }
        }

#if NETCORE
        /// <summary>
        /// Kills first dotnet process with arguments Alachisoft.NCache.WebManager.dll 
        /// </summary>
        /// <returns></returns>
        private static bool KillProcessOnLinux()
        {
            int pid = 0;
            if (DiscoverProcessViaPGrep(out pid))
            {
                var process = Process.GetProcessById(pid);
                process?.Kill();
                return true;
            }
            return false;
        }
#endif       
        /// <summary>
        /// Method to determine status of web manager process.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool IsProcessRunning(string name)
        {
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                int pid = 0;
                return DiscoverProcessViaPGrep(out pid);
            }
#endif
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains(name))
                {
                    return true;
                }
            }
            return false;
        }

#if NETCORE
        private static bool DiscoverProcessViaPGrep(out int pid)
        {
            pid = 0;
            var processes = Process.GetProcessesByName("dotnet");
            string result = "pgrep -af dotnet.*Alachisoft.NCache.WebManager.dll".Bash();
            foreach (string line in result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                if (!String.IsNullOrEmpty(line))
                {
                    string[] tokens = Regex.Split(line, "\\s+");
                    if (int.TryParse(tokens[0], out pid))
                    {
                        return true;
                    }

                }
            }
            return false;

        }
#endif

        /// <summary>
        /// Method to read config from web manager folder
        /// </summary>
        /// <param name="workingDirecotry">Path of the web manager folder</param>
        /// <returns></returns>
        private static string GetUrlFromConfig(string workingDirecotry)
        {
            string url = _DEFAULT_URL;
            try
            {
                string configPath = Path.Combine(workingDirecotry, _WEB_CONFIG_NAME);
                if (File.Exists(configPath))
                {
                    using (StreamReader r = new StreamReader(configPath))
                    {
                        string jsonConfig = r.ReadToEnd();
                        dynamic config = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonConfig);
                        url = config.Kestrel.EndPoints.Http.Url.Value;
                    }
                }
            }
            catch (Exception)
            {
            }
            if (url == null)
                return _DEFAULT_URL;

            return url;
        }

        /// <summary>
        /// Method to validate the status of web manager.
        /// </summary>
        /// <param name="url">URL to make ping request on</param>
        /// <returns>Flag if ping response is validated</returns>
        public static bool PingUrl(string url)
        {
            for (int i = 0; i < _RETRIES; i++)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    return (response.StatusCode == HttpStatusCode.OK);
                }
                catch (System.Net.WebException e)
                {
                    Thread.Sleep(_RETRY_INTERVAL);
                    if (i == _RETRIES - 1)
                    {
                        AppUtil.LogEvent(e.ToString(), EventLogEntryType.Error);
                        return false;
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Enum to determine status of web management process exectuter
    /// </summary>
    public enum ExecutionStatus
    {
        Error,
        Started,
        NotFound,
        AlreadyRunning,
    }
}
