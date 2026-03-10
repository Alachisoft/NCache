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
using System.IO;
using System.Diagnostics;
using System.Configuration;
using Alachisoft.NCache.Common.RuntimeEnvironment;

#if NETCORE
using System.Runtime.InteropServices;
#endif

namespace Alachisoft.NCache.Common.Util
{
    public class ProcessExecutor
    {
        
        private string command;
        private const long maxInterval = 10000;

        public ProcessExecutor(string command)
        {
            this.command = command;
        }

        public Process Execute()
        {
            Process process = null;
            try
            {
                RuntimeOption runtimeOption = new RuntimeOption()
                {
                    Command = command
                };
                process = NCacheRuntimeEnvironment.GetEnvironment.GetCacheHostProcess(runtimeOption);

            }
            catch (Exception exp)
            {
                process?.Kill();
                throw exp;
            }
            return process;
        }


        public void ExecuteOnWindows(ProcessStartInfo processStartInfo)
        {
            processStartInfo.FileName = "dotnet.exe";
            string configPath = ConfigurationSettings.AppSettings["CacheHostPath"];
            if (configPath != null)
            {
                processStartInfo.Arguments = "/C " + Path.Combine(configPath, "Alachisoft.NCache.CacheHost.dll")+ " " + command;
            }
            else
            {
                string part1 = Path.Combine(AppUtil.InstallDir, "bin");
                string part2 = Path.Combine(part1, "service");
                string part3 = Path.Combine(part2, "Alachisoft.NCache.CacheHost.dll");
                 part3 = "\""+part3+"\"";
                 processStartInfo.Arguments = part3 + " " + command;
            }
            
        }

#if NETCORE
        public void ExecuteOnLinux(ProcessStartInfo processStartInfo)
        {
            processStartInfo.FileName = "dotnet";
            string configPath = ConfigurationSettings.AppSettings["CacheHostPath"];
            string arguments = "";
            if (configPath != null)
            {
                arguments = Path.Combine(configPath, "Alachisoft.NCache.CacheHost.dll") + " " + command;
            }
            else
            {
                string part1 = Path.Combine(AppUtil.InstallDir, "bin");
                string part2 = Path.Combine(part1, "service");
                string part3 = Path.Combine(part2, "Alachisoft.NCache.CacheHost.dll");
                arguments = part3 + " " + command;
            }
            processStartInfo.Arguments = arguments;
            processStartInfo.UseShellExecute = false;
        }
#endif
        public static void KillProcessByID(int pID)
        {
            try
            {
                Process proc = Process.GetProcessById(pID);
                proc.Kill();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static  void KillProcess(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
