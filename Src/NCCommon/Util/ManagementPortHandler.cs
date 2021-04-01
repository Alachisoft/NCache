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
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
#if !NETCORE
using System.Management;
#endif
using System.Diagnostics;
using Alachisoft.NCache.Tools.Common;
using System.Collections;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Reflection;
using Alachisoft.NCache.Common.Configuration;
#if NETCORE
using Alachisoft.NCache.Licensing.NetCore.LinuxUtil;
#endif

namespace Alachisoft.NCache.Common.Util
{
    public class ManagementPortHandler
    {
        static int startPort;
        static int endPort = ServiceConfiguration.ManagementPortUpper;
        static Hashtable runningcaches = null;
#if !NETCORE
        static Process[] processes = Process.GetProcessesByName("Alachisoft.NCache.CacheHost");
#elif NETCORE
        static Process[] processes = Process.GetProcessesByName("dotnet");
#endif

        public static Hashtable RunningCaches
        {
            get { return ManagementPortHandler.runningcaches; }
            set { ManagementPortHandler.runningcaches = value; }
        }

        static public int GenerateManagementPort(ArrayList occupiedPorts)
        {
            TcpListener ss = null;
            // Reset the start port everytime to make sure no ports are left unused.
            startPort = ServiceConfiguration.ManagementPortLower;
            if (startPort <= 0 || endPort <= 0)
#if !NETCORE && !NETCOREAPP2_0
                throw new ManagementException("Port range is not defined in Alachisoft.NCache.Service.exe.config located at 'NCache Installation'/bin/service");
#elif NETCORE
                throw new Exception("Port range is not defined in Alachisoft.NCache.Service.exe.config located at 'NCache Installation'/bin/service"); 
#endif
            while (true)
            {
                try
                {
                    if (startPort <= endPort)
                    {
                        ss = new TcpListener(IPAddress.Any, startPort);
                        ss.Start();
                    }
                    if (!occupiedPorts.Contains(startPort))
                        break;
                    else
                    {
                        IncrementStartPort(ss);
                    }
                }
                catch (Exception ex)
                {
                    IncrementStartPort(ss);
                }

            }
            if (ss != null)
            {
                try
                {
                    ss.Stop();
                }
                catch (Exception e)
                {
                }
            }
            return startPort;
        }



        static void IncrementStartPort(TcpListener ss)
        {
            startPort++;
            if (ss != null)
            {
                try
                {
                    ss.Stop();
                }
                catch (Exception e)
                {
                }
            }
        }



        static public Hashtable DiscoverCachesViaWMI()
        {
            ServiceController sc = new ServiceController("Winmgmt");
            runningcaches = new Hashtable();
            if (sc.Status == ServiceControllerStatus.Running)
            {
                try
                {
                    foreach (Process process in processes)
                    {
                        string cmdLine = GetCommandLine(process);
                        string[] tmp = cmdLine.Split(' ');
                        if (tmp != null && tmp.Length > 0)
                        {
                            object param = new CacheHostParam();
                            SeperateHostArgumentParser.CommandLineParser(ref param, tmp);
                            CacheHostParam cParam = (CacheHostParam)param;
                            CacheHostInfo cacheInfo = null;
                            if (!runningcaches.ContainsKey(cParam.CacheName.ToLower()))
                            {
                                cacheInfo = new CacheHostInfo();
                                cacheInfo.ProcessId = process.Id;
                                cacheInfo.ManagementPort = cParam.ManagementPort;
                                runningcaches.Add(cParam.CacheName.ToLower(), cacheInfo);
                            }
                            else
                            {
                                cacheInfo = runningcaches[cParam.CacheName.ToLower()] as CacheHostInfo;
                                cacheInfo.ProcessId = process.Id;
                                cacheInfo.ManagementPort = cParam.ManagementPort;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return runningcaches;
                }
                return runningcaches;
            }
            else
                return runningcaches;
        }

        static public List<ProcessInfo> DiscoverCachesViaNetStat()
        {
            // Use netstats to get the caches
            Process p = new Process();
            List<ProcessInfo> processInfoList = new List<ProcessInfo>();
            runningcaches = new Hashtable();
            try
            {

                p.StartInfo = new ProcessStartInfo("netstat.exe", "-a -n -o -p TCP");

                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.Verb = "runas";

                p.Start();

                string content = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                string exitStatus = p.ExitCode.ToString();

                if (exitStatus != "0")
                {
                    // something bad happened.
                }

                string[] rows = Regex.Split(content, "\r\n");

                foreach (string row in rows)
                {
                    string[] tokens = Regex.Split(row, "\\s+");
                    if (tokens.Length > 4 && (tokens[4].Equals("LISTENING")))
                    {
#if !NETCORE
                        if ((LookupProcess(Convert.ToInt32(tokens[5]))).Equals("Alachisoft.NCache.CacheHost"))
#elif NETCORE
                        if ((LookupProcess(Convert.ToInt32(tokens[5]))).Equals("dotnet"))
#endif
                        {
                            string localAddress = Regex.Replace(tokens[2], @"\[(.*?)\]", "1.1.1.1");

                            try
                            {
                                int port = Int32.Parse(localAddress.Split(':')[1]);
                                if (port >= ServiceConfiguration.ManagementPortLower && port <= endPort)
                                {
                                    ProcessInfo info = new ProcessInfo();
                                    info.protocol = localAddress.Contains("1.1.1.1") ? String.Format("{0}v6", tokens[1]) : String.Format("{0}v4", tokens[1]);
                                    info.port_number = port;
                                    info.process_name = LookupProcess(Convert.ToInt32(tokens[5]));
                                    info.pid = Convert.ToInt32(tokens[5]);
                                    processInfoList.Add(info);
                                    if (processInfoList.Count == processes.Length)
                                        break;
                                }
                            }
                            catch (Exception ex)
                            { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }


            return processInfoList;
        }

        public static string LookupProcess(int pid)
        {
            string procName;
            try { procName = Process.GetProcessById(pid).ProcessName; }
            catch (Exception) { procName = "-"; }
            return procName;
        }

        private static string GetCommandLine(Process process)
        {
            StringBuilder commandLine = new StringBuilder(process.MainModule.FileName);

            commandLine.Append(" ");
#if !NETCORE
            EnumerationOptions options = new EnumerationOptions();
            options.Timeout = new TimeSpan(0, 0, 5);

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("", "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id, options))
                {
                    foreach (ManagementObject @object in searcher.Get())
                        try
                        {
                            commandLine.Append(@object["CommandLine"]);
                            commandLine.Append(" ");
                            return commandLine.ToString();
                        }
                        catch (Exception ex)
                        {
                            AppUtil.LogEvent(ex.Message, EventLogEntryType.Error);
                        }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
#elif NETCORE
            //TODO: ALACHISOFT (System.Management has some issues)
            throw new NotImplementedException();
#endif

            return null;
        }

        public static Hashtable DiscoverCachesViaPGrep()
        {
            runningcaches = new Hashtable();
#if NETCORE
            string result = "pgrep -af dotnet.*Alachisoft.NCache.CacheHost.dll".Bash();
            foreach (string line in result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                if (!String.IsNullOrEmpty(line))
                {
                    string[] tokens = Regex.Split(line, "\\s+");
                    string cachename = "";
                    int cacheport = 0;
                    int pid = 0;
                    if (int.TryParse(tokens[0], out pid))
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (tokens[i] == "/i")
                                cachename = tokens[i + 1];
                            else if (tokens[i] == "/p")
                                int.TryParse(tokens[i + 1], out cacheport);
                        }

                        CacheHostInfo info = new CacheHostInfo();
                        info.ProcessId = pid;
                        info.ManagementPort = cacheport;
                        runningcaches.Add(cachename, info);
                        if (runningcaches.Count == processes.Length)
                            break;
                    }

                }
            }
#endif
            return runningcaches;
            
        }
    }
}
