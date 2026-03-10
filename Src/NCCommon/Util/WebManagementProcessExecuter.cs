//  Copyright (c) 2026 Alachisoft
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
using Alachisoft.NCache.Common.RuntimeEnvironment;
#if NETCORE
using Alachisoft.NCache.Licensing.LinuxUtil;
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
        static private readonly string _PROCESS_NAME = "Alachisoft.NCache.ManagementCenter";
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
        public static void ExecuteProcess()
        {
            if (NCacheRuntimeEnvironment.GetEnvironment.IsWebProcessRunning())
            {
                throw new InvalidOperationException("NCacheManagementCenter is already running.");
            }

            Process process = null;
            try
            {
                InvokeWebProcess(out process);
            }
            catch (Exception exc)
            {
                try
                {
                    if (process != null)
                        process.Kill();
                }
                catch (Exception)
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
            string path = NCacheRuntimeEnvironment.GetEnvironment.WebManagerProcessPath;
            if (!File.Exists(path))
            {
                return ExecutionStatus.NotFound;
            }
            if (!NCacheRuntimeEnvironment.GetEnvironment.IsWebProcessRunning())
            {
                Process process = null;
                try
                {
                    InvokeWebProcess(out process);
                    return ExecutionStatus.Started;
                }
                catch (Exception exc)
                {
                    try
                    {
                        if (process != null)
                            process.Kill();
                    }
                    catch (Exception)
                    {
                    }
                    throw exc;
                }
            }
            else
            {
                return ExecutionStatus.AlreadyRunning;
            }
        }

        /// <summary>
        /// Invokes the Web Manager process
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static void InvokeWebProcess(out Process process)
        {
            if (!File.Exists(NCacheRuntimeEnvironment.GetEnvironment.WebManagerProcessPath))
            {
                throw new DirectoryNotFoundException("Alachisoft.NCache.ManagementCenter not found at the following path: " + NCacheRuntimeEnvironment.GetEnvironment._WEB_DIR);
            }

            process = new Process
            {
                StartInfo = NCacheRuntimeEnvironment.GetEnvironment.GetWebManagerProcessInfo(new RuntimeOption())
            };

            if (!process.Start())
            {
                throw new Exception("Unable to start NCacheManagementCenter.");
            }
        }

        /// <summary>
        /// Method to stop web manager process.
        /// </summary>
        /// <returns>True if successfully stoped.</returns>
        public static bool StopProcess()
        {
            return NCacheRuntimeEnvironment.GetEnvironment.KillWebProcess();
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
