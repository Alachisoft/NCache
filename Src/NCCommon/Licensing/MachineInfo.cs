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
#if NETCORE
using Alachisoft.NCache.Licensing.LinuxUtil;
#endif
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Alachisoft.NCache.Common.Licensing
{
    public static class MachineInfo
    {
        static MachineInfo()
        {
            Load();
        }

        private static int _minAllowedCores = 8;
        private static int _minCores = 4;
        private static bool _loaded = false;
        private static int GB = 1024 * 1024;
        public static bool VCPUBasedLicensing { get; set; }
        public static int TotalAvailableCores { get { return LogicalCores; } }
        public static string ComputerName { get; private set; }
        public static int PhysicalCores { get; private set; }
        public static int LogicalCores { get; set; }
        public static int Licenses { get; private set; }
        public static int SocketCount { get; private set; }
        public static string[] MacAddresses { get; private set; }
        public static string StaticMac { get; set; }
        public static bool Error { get; private set; }
        public static bool IgnodeVM { get; set; }
        public static bool CreateLog { get; set; }
        public static string Platform { get; set; }
        public static decimal Memory { get; set; }
        public static long MEMORY_NOT_DEFINED_FOR_DOCKER = 9223372036854771712;
        public static bool IsKubernetes { get; set; }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        private static decimal GetTotalPhysicalMemoryInGB()
        {
            long memory = 0;
            bool returnValue = false;
            try
            {
                returnValue = GetPhysicallyInstalledSystemMemory(out memory);
                //memory in kilobytes, to convert it into GB , divide with 1024*1024
                if (returnValue) return Math.Round(Convert.ToDecimal(memory) / (1024 * 1024), 2);
            }
            catch
            {                
            }
            //Return 4gb  In case of error or GetPhysicallyInstalledSystemMemory() returns false
            return 8;
        }


    


            private static void ParseOutputResult(string outString)
        {
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                String[] rows = outString.Split(';')[0].Split('\n');
                if (rows.Length >= 2)
                {
                    int numberOfCores = rows[0].IndexOf("NumberOfCores");
                    int numberOfLogicalProcessors = rows[0].IndexOf("NumberOfLogicalProcessors");
                    int numberOfSockets = rows[0].IndexOf("SocketDesignation");
                    if (numberOfCores >= 0 && numberOfCores < rows[1].Length)
                    {
                        try
                        {
                            Licenses = int.Parse(rows[1][numberOfCores].ToString());
                        }
                        catch (Exception e)
                        {
                        }
                    }
                    if (numberOfLogicalProcessors >= 0 && numberOfLogicalProcessors < rows[1].Length)
                    {
                        try
                        {
                            PhysicalCores = int.Parse(rows[1][numberOfLogicalProcessors].ToString());
                        }
                        catch (Exception e)
                        {
                        }
                    }

                    if (numberOfSockets > 0)
                    {
                        try
                        {
                            for (int row = 1; row < rows.Length - 1; row++)
                            {
                                if (numberOfSockets < rows[row].Length)
                                {
                                    String socketSubString = rows[row].Substring(numberOfSockets);
                                    if (socketSubString.Substring(0, 3).ToUpper().Equals("CPU") || socketSubString.Substring(0, 3).ToUpper().Equals("PROC"))
                                    {
                                        int currentSocketCount = 1;
                                        String[] tokens = socketSubString.Split(' ');
                                        try
                                        {
                                            currentSocketCount = int.Parse(tokens[1]);
                                        }
                                        catch (Exception e)
                                        {
                                        }

                                        if (SocketCount < currentSocketCount)
                                        {
                                            SocketCount = currentSocketCount;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            //On any exception, considering it a VM
                            SocketCount = 2;
                        }
                    }

                    if (SocketCount < 1)
                        SocketCount = 2;
                }
            }
#if NETCORE
            else
                SocketCount = 2; //If OS not detected, considering it a VM
#endif
        }

        public static int GetTotalAvailableCores(bool vCPUBasedLicensing)
        {
            if (vCPUBasedLicensing)
                return LogicalCores;
            return PhysicalCores;
        }


        private static string ExecuteCommandOnWindows()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wmic.exe",
                    Arguments = "cpu get",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            proc.Start();
            proc.PriorityClass = ProcessPriorityClass.RealTime;
            return proc.StandardOutput.ReadToEnd();
        }

        private static void Load()
        {
            if (_loaded)
                return;

            ComputerName = Environment.MachineName;
            Platform = ".Net";
            try
            {
#if NETCORE
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    PhysicalCores = int.Parse("grep \"cpu cores\" /proc/cpuinfo | uniq | sed -e 's/[^0-9]*//g'".Bash());
                    LogicalCores = Environment.ProcessorCount;
                    SocketCount = int.Parse("grep \"physical id\" /proc/cpuinfo | sort | uniq | wc -l".Bash());
                    long memory = 0;
                    try
                    {
                        memory = long.Parse("cat /sys/fs/cgroup/memory/memory.limit_in_bytes".Bash());
                        // conversion from byets to gigabytes
                        Memory = Math.Round((decimal)memory / (GB * 1024), 2);
                    }
                    catch (Exception)
                    {
                    }
                    if (memory == MEMORY_NOT_DEFINED_FOR_DOCKER || memory == 0)
                    {
                        memory = long.Parse("cat /proc/meminfo | grep MemTotal | grep -o -E '[0-9]+'".Bash().Trim().Split(' ')[0]);
                        // conversion from kilobyets to gigabytes
                        Memory = Math.Round((decimal)memory / GB, 2);
                    }
                }
                else
#endif
                {
                    SocketCount = 2;
                    PhysicalCores = 2;

                    // we need to fallback to node memory/cpu incase user hasn't set the resources for NCache pod
                    if (!IsKubernetes || LogicalCores == 0)
                        LogicalCores = Environment.ProcessorCount;
                    if (!IsKubernetes || Memory == 0)
                        Memory = GetTotalPhysicalMemoryInGB();
                }
            }
            catch (Exception)
            {
                LogicalCores = Environment.ProcessorCount;
                Memory = GetTotalPhysicalMemoryInGB();
            }
            if (TotalAvailableCores < _minAllowedCores)
                PhysicalCores = 2;
            else
                PhysicalCores = GetLogicalProcessors();

            LoadMacAddresses();

            if (!string.IsNullOrEmpty(StaticMac))
            {
                if (StaticMac.Length != 17)
                {
                    MacAddresses[0] = "";
                    MacAddresses[1] = "";
                    MacAddresses[2] = "";
                    MacAddresses[3] = "";
                    Console.WriteLine(" Static mac does not follow data format, please enter Mac as 00-12-34-5H-78-HG");
                    Error = true;
                    return;
                }

                string staticMac = string.Empty;
                //string swapper;
                int tokenNum = 0;
                try
                {
                    var tokens = StaticMac.Split('-');
                    string strToken = tokens[tokenNum];
                    tokenNum++;
                    while (!string.IsNullOrEmpty(strToken))
                    {
                        if (strToken.Length != 2)
                        {
                            MacAddresses[0] = "";
                            MacAddresses[1] = "";
                            MacAddresses[2] = "";
                            MacAddresses[3] = "";
                            Console.WriteLine(" Static mac does not follow data format, please enter Mac as 00-12-34-5H-78-HG");
                            Error = true;
                            return;
                        }
                        staticMac += strToken;
                        strToken = tokens[tokenNum];
                        tokenNum++;
                    }
                }
                catch (Exception)
                {
                    return;
                }
                if ((staticMac != MacAddresses[0]) && (staticMac != MacAddresses[1]) && (staticMac != MacAddresses[2]) && (staticMac != MacAddresses[3]))
                {
                    MacAddresses[3] = MacAddresses[2];
                    MacAddresses[2] = MacAddresses[1];
                    MacAddresses[1] = MacAddresses[0];
                }
                else if (staticMac == MacAddresses[1])
                {
                    MacAddresses[1] = MacAddresses[0];
                }
                else if (staticMac == MacAddresses[2])
                {
                    MacAddresses[2] = MacAddresses[0];
                }
                else if (staticMac == MacAddresses[3])
                {
                    MacAddresses[3] = MacAddresses[0];
                }
                MacAddresses[0] = staticMac;
            }
            _loaded = true;
        }
        public static int GetLogicalProcessors()
        {
            int logicalCount = 0;
            using (var searcher = new ManagementObjectSearcher("select NumberOfCores from Win32_Processor"))
            {
                foreach (var item in searcher.Get())
                {
                    logicalCount += int.Parse(item["NumberOfCores"].ToString());
                }
            }
            return logicalCount;
        }
       
     
       

        private static void LoadMacAddresses()
        {
try
            {            var macAddresses = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                                where nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                                select nic.GetPhysicalAddress().ToString().Replace("-", string.Empty).ToLower());

            MacAddresses = new string[4] { "", "", "", "" };
            int i = 0;
            foreach (var macAddress in macAddresses)
            {
                if (string.IsNullOrEmpty(macAddress))
                    continue;

                MacAddresses[i] = macAddress;
                i++;
                if (i == 4)
                    break;
            }
}
            catch
            {

            }
        }

    }
}
