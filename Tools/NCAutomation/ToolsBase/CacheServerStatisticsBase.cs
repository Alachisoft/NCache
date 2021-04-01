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
using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Get, "CacheServerStatistics")]
    public class CacheServerStatisticsBase : CacheServerStatisticsParameters, IConfiguration
    {
        private string TOOLNAME = "CacheServerStatistics Tool";
        private ArrayList serverList = new ArrayList();
        private bool _headerRow = false;
        private List<Common.Monitoring.PerfmonCounterDetails> _mainCounters = new List<Common.Monitoring.PerfmonCounterDetails>();
        private List<Common.Monitoring.PerfmonCounterDetails> _replicaCounters = new List<Common.Monitoring.PerfmonCounterDetails>();

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

        public bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(CacheName))
            {
                OutputProvider.WriteErrorLine("\nError: Cache name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(CounterNames))
            {
                DoNotShowDefaultCounters = false;
            }
            return true;
        }

        public void CacheServerStatistics()
        {
            ArrayList servers = null;
            bool isPOR = false;
            bool isRegistered = true;
            List<ICacheServer> cacheServers = new List<ICacheServer>();
            if (!ValidateParameters())
                return;
            if (Servers != null && !Servers.Equals(""))
            {
                servers = GetServers(Servers);
            }
            else
            {
                servers = new ArrayList();
                NCacheRPCService nCache = new NCacheRPCService("");
                servers.Add(nCache.ServerName);
            }
            foreach (var server in servers)
            {
                NCacheRPCService nCache = new NCacheRPCService(server.ToString());
                try
                {
                    ICacheServer cacheServer = nCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    serverList.Add(cacheServer.GetBindIP());
                    NodeStatus status = GetCacheStatistics(nCache);
                    if (status.isRegistered == false)
                    {
                        throw new Exception("The specified cache is not registered on server " + server);
                    }

                    isPOR = status.Topology == Topology.POR;
                    cacheServers.Add(cacheServer);
                    //array of it ICacheServer 
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("The specified cache is not registered"))
                    {
                        OutputProvider.WriteErrorLine(ex.Message);
                        isRegistered = false;
                    }
                    else if (ex.Message.ToUpper().Contains("service".ToUpper()))
                    {
                        OutputProvider.WriteErrorLine("NCache Service could not be contacted on server " + server);
                        isRegistered = false;
                    }
                }
            }
            if (isRegistered)
            {
                _mainCounters = GetAllCounters(false);
                if(isPOR)
                {
                    _replicaCounters = GetAllCounters(true);
                }
                if (!(Format.Equals("csv", StringComparison.OrdinalIgnoreCase) || Format.Equals("tabular", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException("Invalid Format type");
                }
                if (Continuous && MaxSamples > 0)
                {
                    throw new Exception("The Continuous parameter and the MaxSamples parameter cannot be used in the same command.");
                }
                if (Continuous && Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                {

                    FetchAndDisplayContinousulyCSV(cacheServers, isPOR);
                }
                else if (Continuous && Format.Equals("tabular", StringComparison.OrdinalIgnoreCase))
                {
                    ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
                    FetchAndDisplayContinousuly(cacheServers, isPOR);
                }
                else if (MaxSamples > 0)
                {
                    if (Format.Equals("tabular", StringComparison.OrdinalIgnoreCase))
                    {
                        ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
                        FetchAndDisplayMax(cacheServers, isPOR);

                    }
                    else if (Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                    {
                        FetchAndDisplayMaxCSV(cacheServers, isPOR);
                    }
                }
                if (!Continuous && MaxSamples == 0)
                {
                    try
                    {
                        SortedDictionary<string, string[]> CountList = FetchCounters(cacheServers, isPOR);
                        if (Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                        {
                            DisplayinCSVFormat(CountList, isPOR);
                        }
                        else if (Format.Equals("tabular", StringComparison.OrdinalIgnoreCase))
                        {
                            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
                            DisplayTimeStamp();
                            DisplayCounters(CountList, isPOR);
                        }
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        OutputProvider.WriteErrorLine(ex);
                    }
                    catch (Exception ex)
                    {
                        OutputProvider.WriteErrorLine(ex);
                    }
                }
            }
            OutputProvider.WriteLine("\n");
        }

        private void DisplayTimeStamp()
        {
            string toolName = "Cache Server Statistics";
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt");
            int length = toolName.Length + timeStamp.Length;
            string line = new string('-', length + 4);
            OutputProvider.WriteLine(toolName + " at " + timeStamp);
            OutputProvider.WriteLine(line);
        }
        private void FetchAndDisplayContinousulyCSV(List<ICacheServer> cacheServers, bool isPOR)
        {
            try
            {
                if (SampleInterval < 0 || SampleInterval > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("Number must be either non-negative and less than or equal to Int32.MaxValue");
                }
                while (true)
                {
                    Thread.Sleep(SampleInterval * 1000);
                    SortedDictionary<string, string[]> CountList = FetchCounters(cacheServers, isPOR);
                    DisplayinCSVFormat(CountList, isPOR);
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }

        }
        private void FetchAndDisplayMaxCSV(List<ICacheServer> cacheServers, bool isPOR)
        {
            try
            {
                if (SampleInterval < 0 || SampleInterval > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("Number must be either non-negative and less than or equal to Int32.MaxValue");
                }
                for (int i = 0; i < MaxSamples; i++)
                {
                    Thread.Sleep(SampleInterval * 1000);
                    SortedDictionary<string, string[]> CountList = FetchCounters(cacheServers, isPOR);
                    DisplayinCSVFormat(CountList, isPOR);
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }

        }
        private void FetchAndDisplayContinousuly(List<ICacheServer> cacheServers, bool isPOR)
        {
            try
            {
                if (SampleInterval < 0 || SampleInterval > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("Number must be either non-negative and less than or equal to Int32.MaxValue");
                }
                while (true)
                {
                    Thread.Sleep(SampleInterval * 1000);
                    SortedDictionary<string, string[]> CountList = FetchCounters(cacheServers, isPOR);
                    DisplayTimeStamp();
                    DisplayCounters(CountList, isPOR);
                    OutputProvider.WriteLine(" \n\n");
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }

        }
        private void FetchAndDisplayMax(List<ICacheServer> cacheServers, bool isPOR)
        {
            try
            {
                if (SampleInterval < 0 || SampleInterval > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("Number must be either non-negative and less than or equal to Int32.MaxValue");
                }
                for (int i = 0; i < MaxSamples; i++)
                {
                    Thread.Sleep(SampleInterval * 1000);
                    SortedDictionary<string, string[]> CountList = FetchCounters(cacheServers, isPOR);
                    DisplayTimeStamp();
                    DisplayCounters(CountList, isPOR);
                    OutputProvider.WriteLine(" \n\n");
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }

        }
        private NodeStatus GetCacheStatistics(NCacheRPCService nCacheRPCService)
        {
            ICacheServer cacheServer = nCacheRPCService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
            NodeStatus nodeInfo = new NodeStatus();
            nodeInfo.isRegistered = true;

            if (cacheServer.GetCacheConfiguration(CacheName) == null)
            {
                nodeInfo.isRegistered = false;
                return nodeInfo;
            }
            var config = cacheServer.GetCacheConfiguration(CacheName);
            if (config.CacheType.Contains("local"))
            {
                nodeInfo.Topology = Topology.LOCAL;
            }
            else
            {
                if (config.Cluster.Topology.Contains("partitioned-replicas"))
                {
                    nodeInfo.Topology = Topology.POR;
                }
                else
                {
                    nodeInfo.Topology = Topology.OTHER;
                }
            }
            return nodeInfo;
        }

        private List<Common.Monitoring.PerfmonCounterDetails> GetDefaultCounters(bool replica = false)
        {
            List<string> defaultCounterList = new List<string>();
            Common.Monitoring.PerfmonCounterDetails item = new Common.Monitoring.PerfmonCounterDetails();
            List<Common.Monitoring.PerfmonCounterDetails> perfmonCounters = new List<Common.Monitoring.PerfmonCounterDetails>();

            defaultCounterList.Add("Additions/sec");
            defaultCounterList.Add("Cache Size");
            defaultCounterList.Add("Count");
            defaultCounterList.Add("Deletes/sec");
            defaultCounterList.Add("Evictions/sec");
            defaultCounterList.Add("Expirations/sec");
            defaultCounterList.Add("Fetches/sec");
            defaultCounterList.Add("Requests/sec");
            defaultCounterList.Add("Updates/sec");
            defaultCounterList.Add("Cluster ops/sec");
            defaultCounterList.Add("State Transfer/sec");

            item.Category = "NCache";
            if (replica == false)
            {
                item.Instance = CacheName;
            }
            else
            {
                item.Instance = CacheName + "-replica";
            }
            //append replica
            item.Value = 0;

            foreach (string counter in defaultCounterList)
            {
                item.Counter = counter;
                perfmonCounters.Add(item);
            }
            return perfmonCounters;
        }

        private List<Common.Monitoring.PerfmonCounterDetails> GetCustomCounters(bool replica, ArrayList parameters)
        {
            List<string> customCounterList = new List<string>();
            Common.Monitoring.PerfmonCounterDetails item = new Common.Monitoring.PerfmonCounterDetails();
            List<Common.Monitoring.PerfmonCounterDetails> perfmonCounters = new List<Common.Monitoring.PerfmonCounterDetails>();
            foreach (var param in parameters)
            {
                if (!customCounterList.Contains((string)param, StringComparer.OrdinalIgnoreCase))
                {
                    customCounterList.Add(param.ToString());
                }
            }
            item.Category = "NCache";
            if (replica == false)
            {
                item.Instance = CacheName;
            }
            else
            {
                item.Instance = CacheName + "-replica";
            }
            //append replica
            item.Value = 0;

            foreach (string counter in customCounterList)
            {
                item.Counter = counter;
                perfmonCounters.Add(item);
            }
            return perfmonCounters;
            //populate list on params and return it
        }

        private List<Common.Monitoring.PerfmonCounterDetails> GetAllCounters(bool isReplica)
        {
            List<Common.Monitoring.PerfmonCounterDetails> perfmonCounters = new List<Common.Monitoring.PerfmonCounterDetails>();
            if (!DoNotShowDefaultCounters)
            {
                perfmonCounters = GetDefaultCounters(isReplica);
            }

            if (CounterNames != null && !CounterNames.Equals(""))
            {
                //perfmonCounters.AddRange(GetCustomCounters(isReplica, GetCounterNames(CounterNames)));
                var customCounters = GetCustomCounters(isReplica, GetCounterNames(CounterNames));
                List<Common.Monitoring.PerfmonCounterDetails> toRemove = new List<Common.Monitoring.PerfmonCounterDetails>();
                foreach (var counter in customCounters)
                {
                    foreach (var finalCounter in perfmonCounters)
                    {
                        if (counter.Counter.Equals(finalCounter.Counter, StringComparison.OrdinalIgnoreCase))
                        {
                            toRemove.Add(counter);
                        }
                    }
                }
                //perfmonCounters.AddRange(toRemove);
                foreach (var remove in toRemove)
                {
                    customCounters.Remove(remove);
                }
                perfmonCounters.AddRange(customCounters);
            }
            return perfmonCounters;
        }

        private void PopulateCounterValues(List<Common.Monitoring.PerfmonCounterDetails> counters, TopologyCheck topologyCheck, int i, ref SortedDictionary<string, string[]> counterTable)
        {
            int count = 0;
            switch (topologyCheck)
            {
                case TopologyCheck.PoRMain:
                    count = i * 2;
                    break;
                case TopologyCheck.PoRReplica:
                    count = i * 2 + 1;
                    break;
                case TopologyCheck.Other:
                    count = i;
                    break;
                default:
                    count = i;
                    break;
            }

            for (int j = 0; j < counters.Count; j++)
            {
                if (i == 0)
                {
                    if (topologyCheck == TopologyCheck.PoRMain)
                    {
                        counterTable.Add(counters[j].Counter, new string[(serverList.Count) * 2]);
                    }
                    if (topologyCheck == TopologyCheck.Other)
                    {
                        counterTable.Add(counters[j].Counter, new string[(serverList.Count)]);
                    }
                }
                if (counters[j].Value != -404&&counters[j].Value != -10)
                {
                    counterTable[counters[j].Counter][count] = string.Format("{0:0.000}", counters[j].Value);
                }
                else
                {
                    counterTable[counters[j].Counter][count] = "--";
                }
            }
        }

        private SortedDictionary<string, string[]> FetchCounters(List<ICacheServer> cacheServers, bool isPOR)
        {
            SortedDictionary<string, string[]> counterTable = new SortedDictionary<string, string[]>();
            if (isPOR)
            {
                for (int i = 0; i < cacheServers.Count; i++)
                {
                    if (cacheServers[i] != null)
                    {
                        _mainCounters = cacheServers[i].GetPerfmonValues(_mainCounters, CacheName);
                        _replicaCounters = cacheServers[i].GetPerfmonValues(_replicaCounters, CacheName);
                        PopulateCounterValues(_mainCounters, TopologyCheck.PoRMain, i, ref counterTable);
                        PopulateCounterValues(_replicaCounters, TopologyCheck.PoRReplica, i, ref counterTable);
                    }
                }
            }
            else
            {
                for (int i = 0; i < cacheServers.Count; i++)
                {
                    if (cacheServers[i] != null)
                    {
                        _mainCounters = cacheServers[i].GetPerfmonValues(_mainCounters, CacheName);
                        PopulateCounterValues(_mainCounters, TopologyCheck.Other, i, ref counterTable);
                    }
                }
            }
            return counterTable;
        }

        protected void DisplayinCSVFormat(SortedDictionary<string, string[]> counters, bool hasReplica = false)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                if (!_headerRow)
                {
                    DisplayHeaderRow(counters, hasReplica);
                }
                _headerRow = true;
                ArrayList servers = serverList;
                string counterValue = string.Empty;

                if (!hasReplica)
                {
                    for (int i = 0; i < servers.Count; i++)
                    {
                        foreach (var counterVal in counters)
                        {
                            counterValue = counterValue + ",\"" + counterVal.Value[i] + "\"";
                        }
                    }
                    sb.Append("\"" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\"" + counterValue + "");
                }
                else
                {
                    int count = servers.Count * 2;
                    for (int i = 0; i < count; i = i + 2)
                    {
                        foreach (var counterVal in counters)
                        {
                            counterValue = counterValue + ",\"" + counterVal.Value[i] + "\"";
                        }
                        foreach (var counterVal in counters)
                        {
                            counterValue = counterValue + ",\"" + counterVal.Value[i + 1] + "\"";
                        }
                    }
                    sb.Append("\"" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\"" + counterValue + "");
                }
                OutputProvider.WriteLine(sb);
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex.Message);
            }

        }
        private void DisplayHeaderRow(SortedDictionary<string, string[]> counters, bool hasReplica = false)
        {
            TimeZone timeZone = TimeZone.CurrentTimeZone;
            string name = string.Empty;
            string counterValue = string.Empty;
            StringBuilder sb = new StringBuilder();
            ArrayList servers = serverList;
            TimeSpan timeSpan = new TimeSpan();
            if (!hasReplica)
            {
                for (int i = 0; i < serverList.Count; i++)
                {
                    foreach (var key in counters.Keys)
                    {
                        name = name + ",\"\\\\" + servers[i] + "\\" + "NCache" + "(" + CacheName + ")\\" + key + "\"";
                        counterValue = "\"" + counterValue + ",\"";
                    }
                }
            }
            else
            {
                for (int i = 0; i < serverList.Count; i++)
                {
                    foreach (var key in counters.Keys)
                    {
                        name = name + ",\"\\\\" + servers[i] + "\\" + "NCache" + "(" + CacheName + ")\\" + key + "\"";
                        counterValue = "\"" + counterValue + ",\"";
                    }
                    foreach (var key in counters.Keys)
                    {
                        name = name + ",\"\\\\" + servers[i] + "\\" + "NCache" + "(" + CacheName + "-replica" + ")\\" + key + "\"";
                        counterValue = "\"" + counterValue + ",\"";
                    }
                }
            }

            timeSpan = timeZone.GetUtcOffset(DateTime.Now);
            sb.Append("\"(PDH-CSV 4.0)  (" + timeZone.StandardName + ") (" + timeSpan.TotalMinutes * -1 + ")\"" + name + "");
            OutputProvider.WriteLine(sb);
        }

        private void DisplayCounters(SortedDictionary<string, string[]> counters, bool hasReplica = false)
        {
            ArrayList arrayList = serverList;
            int columns = arrayList.Count;
            if (hasReplica)
            {
                columns = columns * 2;
            }

            var rowCount = counters.Count;

            IDictionary<string, List<string>> map = new Dictionary<string, List<string>>();
            var headers = new string[columns + 1];

            headers[0] = "Counter Name";


            for (int i = 0, j = 0; i < arrayList.Count; i++)
            {
                headers[++j] = (string)arrayList[i];

                if (hasReplica)
                {

                    headers[++j] = (string)arrayList[i] + "(Replica)";
                }
            }

            foreach (var header in headers)
            {
                map[header] = new List<string>(rowCount);
            }

            foreach (var val in counters)
            {
                map[headers[0]].Add(val.Key);

                for (var i = 0; i < val.Value.Length; i++)
                {
                    map[headers[i + 1]].Add(val.Value[i]);
                }
            }

            map = AdjustMapForPrinting(map, ref headers, rowCount);

            PrintDataMap(map, headers, rowCount, new string(' ', 5));
        }

        private IDictionary<string, List<string>> AdjustMapForPrinting(IDictionary<string, List<string>> map, ref string[] headers, int rowCount)
        {
            if (map == null || headers == null || map.Count == 0 || headers.Length == 0 || rowCount < 0)
                return map;

            var spacings = new int[headers.Length];

            for (var i = 0; i < headers.Length; i++)
            {
                spacings[i] = headers[i].Length;
            }
            for (var i = 0; i < headers.Length; i++)
            {
                var columnCells = map[headers[i]];

                foreach (var cell in columnCells)
                {
                    if (spacings[i] < cell.Length)
                    {
                        spacings[i] = cell.Length;
                    }
                }
            }
            for (var i = 0; i < headers.Length; i++)
            {
                var columnCells = map[headers[i]];

                for (var j = 0; j < columnCells.Count; j++)
                {
                    if (i > 0)
                    {
                        columnCells[j] = GetRightAlignedCell(columnCells[j], spacings[i]);
                    }
                    else
                    {
                        columnCells[j] = GetLeftAlignedCell(columnCells[j], spacings[i]);
                    }

                }
            }
            for (var i = 0; i < headers.Length; i++)
            {
                headers[i] = GetLeftAlignedCell(headers[i], spacings[i]);
            }
            return map;
        }

        private string GetLeftAlignedCell(string cell, int spacing)
        {
            return $"{cell}{new string(' ', spacing - cell.Length)}";
        }

        private string GetRightAlignedCell(string cell, int spacing)
        {
            return $"{new string(' ', spacing - cell.Length)}{cell}";
        }

        private string GetCenterAlignedCell(string cell, int spacing)
        {
            var halfSpacing = (spacing - cell.Length) / 2;
            halfSpacing = halfSpacing <= 0 ? 1 : halfSpacing;

            return $"{new string(' ', halfSpacing)}{cell}{new string(' ', halfSpacing)}";
        }

        private void PrintDataMap(IDictionary<string, List<string>> map, string[] headers, int rowCount, string constantSpace = "")
        {
            if (map == null || headers == null || map.Count == 0 || headers.Length == 0 || rowCount < 0)
                return;

            var stringBuilder = new StringBuilder(10 * map.Count * rowCount);

            foreach (var header in headers)
            {
                stringBuilder
                        .Append(header)
                        .Append(constantSpace);
            }

            stringBuilder.AppendLine();

            foreach (var header in headers)
            {
                stringBuilder
                        .Append(new string('-', header.Length))
                        .Append(constantSpace);
            }

            stringBuilder.AppendLine();

            for (var i = 0; i < rowCount; i++)
            {
                foreach (var header in headers)
                {
                    stringBuilder
                        .Append(map[header.Trim()][i])
                        .Append(constantSpace);
                }
                stringBuilder.AppendLine();
            }

            OutputProvider.WriteLine(stringBuilder.ToString());
        }

        private ArrayList GetServers(string servers)
        {
            ArrayList serverList = new ArrayList();
            string[] st = servers.Split(new char[] { ',' });
            for (int i = 0; i < st.Length; i++)
            {
                serverList.Add(st[i]);
            }
            return serverList;
        }
        private ArrayList GetCounterNames(string counternames)
        {
            ArrayList serverList = new ArrayList();
            string[] st = counternames.Split(new char[] { ',' });
            for (int i = 0; i < st.Length; i++)
            {
                serverList.Add(st[i]);
            }
            return serverList;
        }
        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Automation.Util.AssemblyResolver.GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "CacheServerStatistics Cmdlet";
                CacheServerStatistics();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

        protected override void ProcessRecord()
        {
            try { }
            catch { }

        }
    }
    internal enum Topology
    {
        LOCAL,
        POR,
        OTHER,
        UNREGISTERED
    }
    internal class NodeStatus
    {
        public Topology Topology { get; set; }
        public Common.Monitoring.CacheNodeStatus Status { get; set; }
        public bool isRegistered { get; set; }
    }

    internal enum TopologyCheck
    {
        PoRMain,
        PoRReplica,
        Other,
    }
}
