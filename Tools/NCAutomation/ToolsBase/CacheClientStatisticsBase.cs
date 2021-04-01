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
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Get, "CacheClientStatistics")]
    public class CacheClientStatisticsBase : CacheClientStatisticsParameters, IConfiguration
    {
        private string TOOLNAME = "CacheClientStatistics Tool";
        private ArrayList ClientList = new ArrayList();
        private bool _headerRow = false;
        List<Common.Monitoring.PerfmonCounterDetails> _perfmonCounters = new List<Common.Monitoring.PerfmonCounterDetails>();

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
            //Util.ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            if (string.IsNullOrEmpty(CounterNames))
            {
                DoNotShowDefaultCounters = false;
            }
            return true;
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
                TOOLNAME = "CacheClientStatistics Cmdlet";
                CacheClientStatistics();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }
        public ArrayList GetClients(string clients)
        {
            ArrayList clientList = new ArrayList();
            string[] st = clients.Split(new char[] { ',' });
            for (int i = 0; i < st.Length; i++)
            {
                clientList.Add(st[i]);
            }
            return clientList;
        }

        public ArrayList GetCounterNames(string counternames)
        {
            ArrayList counterNames = new ArrayList();
            string[] st = counternames.Split(new char[] { ',' });
            for (int i = 0; i < st.Length; i++)
            {
                counterNames.Add(st[i]);
            }
            return counterNames;
        }
        protected void DisplayTimeStamp()
        {
            string toolName = "Cache Client Statistics";
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt");
            int length = toolName.Length + timeStamp.Length;
            string line = new string('-', length + 4);
            OutputProvider.WriteLine(toolName + " at " + timeStamp);
            OutputProvider.WriteLine(line);
        }

        protected void CacheClientStatistics()
        {
            ArrayList clients = null;
            List<ICacheServer> cacheServers = new List<ICacheServer>();
            bool isServiceRunning = true;

            if (!ValidateParameters())
            {
                return;
            }
            if (Clients != null && !Clients.Equals(""))
            {
                clients = GetClients(Clients);
            }
            else
            {
                clients = new ArrayList();
                NCacheRPCService nCache = new NCacheRPCService("");
                clients.Add(nCache.ServerName);
            }

            foreach (var client in clients)
            {
                NCacheRPCService nCache = new NCacheRPCService(client.ToString());
                try
                {
                    ICacheServer cacheServer = nCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    //NodeStatus status = GetCacheStatistics(_nCache);
                    ClientList.Add(cacheServer.GetBindIP());
                    cacheServers.Add(cacheServer);
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToUpper().Contains("service".ToUpper()))
                    {
                        OutputProvider.WriteErrorLine("NCache Service could not be contacted on server " + client);
                        isServiceRunning = false;
                    }
                    else
                    {
                        cacheServers.Add(null);
                    }
                }
            }
            if (!(Format.Equals("csv", StringComparison.OrdinalIgnoreCase) || Format.Equals("tabular", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Invalid Format type");
            }
            if (Continuous && MaxSamples > 0)
            {
                throw new Exception("The Continuous parameter and the MaxSamples parameter cannot be used in the same command.");
            }
            _perfmonCounters = CreateCounterList();
            if (Continuous && Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                FetchAndDisplayContinousulyCSV(cacheServers);
            }
            else if (Continuous && Format.Equals("tabular", StringComparison.OrdinalIgnoreCase))
            {
                Util.ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
                FetchAndDisplayContinousuly(cacheServers);
            }
            else if (MaxSamples > 0)
            {
                if (Format.Equals("tabular", StringComparison.OrdinalIgnoreCase))
                {
                    ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
                    FetchAndDisplayMax(cacheServers);

                }
                else if (Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                {
                    FetchAndDisplayMaxCSV(cacheServers);
                }
            }

            if (!Continuous && MaxSamples == 0)
            {
                if (isServiceRunning)
                {
                    try
                    {
                        SortedDictionary<string, string[]> CountList = FetchCounters(cacheServers);
                        if (Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                        {
                            DisplayinCSVFormat(CountList);
                        }
                        else if (Format.Equals("tabular", StringComparison.OrdinalIgnoreCase))
                        {
                            Util.ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
                            DisplayTimeStamp();
                            DisplayCounters(CountList);
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
        }
        private void FetchAndDisplayMaxCSV(List<ICacheServer> cacheClients)
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
                    SortedDictionary<string, string[]> CountList = FetchCounters(cacheClients);
                    DisplayinCSVFormat(CountList);
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
        private void FetchAndDisplayContinousulyCSV(List<ICacheServer> cacheClients)
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
                    SortedDictionary<string, string[]> CountList = FetchCounters(cacheClients);
                    DisplayinCSVFormat(CountList);
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
        private void FetchAndDisplayMax(List<ICacheServer> cacheClients)
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
                    SortedDictionary<string, string[]> CountList = FetchCounters(cacheClients);
                    DisplayTimeStamp();
                    DisplayCounters(CountList);
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
        private void FetchAndDisplayContinousuly(List<ICacheServer> cacheClients)
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
                    SortedDictionary<string, string[]> CountList = FetchCounters(cacheClients);
                    DisplayTimeStamp();
                    DisplayCounters(CountList);
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
        protected List<Common.Monitoring.PerfmonCounterDetails> GetDefaultCounters()
        {
            List<string> DefaultCounterList = new List<string>();
            Common.Monitoring.PerfmonCounterDetails item = new Common.Monitoring.PerfmonCounterDetails();
            List<Common.Monitoring.PerfmonCounterDetails> perfmonCounters = new List<Common.Monitoring.PerfmonCounterDetails>();
            DefaultCounterList.Add("Read Operations/sec");
            DefaultCounterList.Add("Write Operations/sec");
            DefaultCounterList.Add("Additions/sec");
            DefaultCounterList.Add("Deletes/sec");
            DefaultCounterList.Add("Fetches/sec");
            DefaultCounterList.Add("Updates/sec");
            DefaultCounterList.Add("Average Item Size");
            DefaultCounterList.Add("Request Queue Size");

            item.Category = "NCache Client";

            item.Value = 0;
            item.Instance = CacheName;
            foreach (string counter in DefaultCounterList)
            {
                item.Counter = counter;
                perfmonCounters.Add(item);
            }
            return perfmonCounters;
        }
        protected List<Common.Monitoring.PerfmonCounterDetails> GetCustomCounters(ArrayList parameters)
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
            item.Category = "NCache Client";
            item.Instance = CacheName;
            item.Value = 0;

            foreach (string counter in customCounterList)
            {
                item.Counter = counter;
                perfmonCounters.Add(item);
            }
            return perfmonCounters;
        }
        protected List<Common.Monitoring.PerfmonCounterDetails> CreateCounterList()
        {
            List<Common.Monitoring.PerfmonCounterDetails> customCounters = new List<Common.Monitoring.PerfmonCounterDetails>();
            List<Common.Monitoring.PerfmonCounterDetails> defaultCounters = new List<Common.Monitoring.PerfmonCounterDetails>();

            if (DoNotShowDefaultCounters)
            {
                return customCounters = GetCustomCounters(GetCounterNames(CounterNames));
            }
            else if (!DoNotShowDefaultCounters && !string.IsNullOrEmpty(CounterNames))
            {
                customCounters = GetCustomCounters(GetCounterNames(CounterNames));
                defaultCounters = GetDefaultCounters();
                List<Common.Monitoring.PerfmonCounterDetails> toRemove = new List<Common.Monitoring.PerfmonCounterDetails>();
                foreach (var counter in customCounters)
                {
                    foreach (var finalCounter in defaultCounters)
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
                defaultCounters.AddRange(customCounters);
            }
            else
            {
                defaultCounters = GetDefaultCounters();
            }
            return defaultCounters;
        }


        SortedDictionary<string, string[]> FetchCounters(List<ICacheServer> cacheClients)
        {
            
            SortedDictionary<string, string[]> counters = new SortedDictionary<string, string[]>();

            for (int i = 0; i < cacheClients.Count; i++)
            {
                if (cacheClients[i] != null)
                {
                    Config.NewDom.CacheServerConfig config = cacheClients[i].GetNewConfiguration(CacheName);
                    _perfmonCounters = cacheClients[i].GetPerfmonValues(_perfmonCounters, CacheName);
                    if (i == 0)
                    {
                        foreach (var counter in _perfmonCounters)
                        {
                            counters.Add(counter.Counter, new string[(cacheClients.Count)]);
                        }
                    }
                    foreach (var counter in _perfmonCounters)
                    {
                        if (counter.Value != -404 && counter.Value != -10)
                        {
                            counters[counter.Counter][i] = string.Format("{0:0.000}", counter.Value);
                        }
                        else
                        {
                            counters[counter.Counter][i] = "--";
                        }
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        foreach (var counter in _perfmonCounters)
                        {
                            counters.Add(counter.Counter, new string[(cacheClients.Count)]);
                        }
                    }
                    foreach (var counter in _perfmonCounters)
                    {
                        counters[counter.Counter][i] = "--";
                    }
                }
            }

            return counters;
        }
        protected void DisplayCounters(SortedDictionary<string, string[]> counters)
        {
            ArrayList arrayList = ClientList;
            if (arrayList.Count == 0)
            {
                throw new Exception("This command requires one or more client IP addresses to fetch counters from.");
            }
            int columns = arrayList.Count;

            var rowCount = counters.Count;

            IDictionary<string, List<string>> map = new Dictionary<string, List<string>>();
            var headers = new string[columns + 1];

            headers[0] = "Counter Name";


            for (int i = 0, j = 0; i < arrayList.Count; i++)
            {
                headers[++j] = (string)arrayList[i];

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

        protected void DisplayinCSVFormat(SortedDictionary<string, string[]> counters)
        {
            StringBuilder sb = new StringBuilder();
            if (!_headerRow)
            {
                DisplayHeaderRow(counters);
            }
            _headerRow = true;
            ArrayList servers = ClientList;
            string counterValue = string.Empty;

            for (int i = 0; i < servers.Count; i++)
            {
                foreach (var counterVal in counters)
                {
                    counterValue = counterValue + ",\"" + counterVal.Value[i] + "\"";
                }
            }
            sb.Append("\"" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\"" + counterValue + "");


            OutputProvider.WriteLine(sb);


        }
        protected void DisplayHeaderRow(SortedDictionary<string, string[]> counters)
        {
            TimeZone timeZone = TimeZone.CurrentTimeZone;
            string name = string.Empty;
            string counterValue = string.Empty;
            StringBuilder sb = new StringBuilder();
            ArrayList servers = ClientList;

            for (int i = 0; i < ClientList.Count; i++)
            {
                foreach (var key in counters.Keys)
                {
                    name = name + ",\"\\\\" + servers[i] + "\\" + "NCache Client" + "(" + CacheName + ")\\" + key + "\"";
                    counterValue = "\"" + counterValue + ",\"";
                }

            }
            TimeSpan timeSpan = new TimeSpan();
            timeSpan = timeZone.GetUtcOffset(DateTime.Now);
            sb.Append("\"(PDH-CSV 4.0)  (" + timeZone.StandardName + ") (" + timeSpan.TotalMinutes * -1 + ")\"" + name + "");
            OutputProvider.WriteLine(sb);
        }

        protected override void ProcessRecord()
        {
            try { }
            catch { }

        }
    }
}
