// Copyright (c) 2017 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Diagnostics;

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
            try {
                
                process = new Process();
                process.StartInfo = new ProcessStartInfo();
                process.StartInfo.Arguments = command;
                process.StartInfo.WorkingDirectory=".";
                process.StartInfo.WindowStyle=ProcessWindowStyle.Hidden;
                string part1 = Path.Combine(AppUtil.InstallDir, "bin");
                string part2 = Path.Combine(part1, "service");
                string part3 = Path.Combine(part2, "Alachisoft.NCache.CacheHost.exe");
                process.StartInfo.FileName = part3;                                  
                if (!process.Start())
                {
                    throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Unable to start Cache Process");
                }

            } catch (Exception exp) {
                process.Kill();
                throw exp;
            }
            return process;
        }

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
