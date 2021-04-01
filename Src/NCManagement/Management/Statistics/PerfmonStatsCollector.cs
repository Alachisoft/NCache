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

using Alachisoft.NCache.Common.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Alachisoft.NCache.Management.Statistics
{
    public class PerfmonStatsCollector : IDisposable
    {
        /// <summary>
        /// contains instances for each cache
        /// key is the cacheId
        /// </summary>
        private static Dictionary<string, PerfmonStatsCollector> Instances = new Dictionary<string, PerfmonStatsCollector>();

        /// <summary>
        /// Contains perfmon instances of active counters
        /// </summary>
        private Dictionary<string, PerformanceCounter> _counters;

        PerfmonStatsCollector()
        {
            _counters = new Dictionary<string, PerformanceCounter>();
        }

        PerfmonStatsCollector(string category, string name, string instance, string machine)
        {
            _counters = new Dictionary<string, PerformanceCounter>();
            var counter = new PerformanceCounter(category, name, instance, machine);
            counter.ReadOnly = true;
            _counters.Add(instance + name, counter);
        }

        public void Dispose()
        {
            
        }

        public static PerfmonStatsCollector GetInstance(string cacheId, string counterName = null, string category = null, string processInstance = null, string machine = null)
        {
            if (!Instances.ContainsKey(processInstance + counterName + category))
            {
                PerfmonStatsCollector perfmonStatsCollector = CreatePerformanceCounterInstance(counterName, category, processInstance, ref machine);
                Instances.Add(processInstance + counterName + category, perfmonStatsCollector);
            }
            return Instances[processInstance + counterName + category];
        }

        private static PerfmonStatsCollector CreatePerformanceCounterInstance(string counterName, string category, string processInstance, ref string machine)
        {
            PerfmonStatsCollector perfmonStatsCollector;
            try
            {
                perfmonStatsCollector = new PerfmonStatsCollector(category, counterName, processInstance, machine);
            }
            catch (Exception e)
            {
                machine = DnsCache.ResolveAddress(machine);
                perfmonStatsCollector = new PerfmonStatsCollector(category, counterName, processInstance, machine);
            }

            return perfmonStatsCollector;
        }

        public static PerfmonStatsCollector GetInstance()
        {
            if (!Instances.ContainsKey("perfmon"))
            {
                Instances.Add("perfmon", new PerfmonStatsCollector());
            }
            return Instances["perfmon"];
        }

        public double GetCounterValue(string processInstance, string counterName)
        {
            try
            {
#if !NETCORE
                System.Threading.Thread.CurrentPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                WindowsImpersonationContext impersonatedUser = WindowsIdentity.GetCurrent().Impersonate();
#endif
                return _counters[processInstance + counterName].NextValue();

            }
            catch (Exception e) { }
            return 0.0;
        }

        public double GetCounterValue(string instanceName, string counterName, string categoryName, string machine = null)
        {
            try
            {
                if (!_counters.ContainsKey(categoryName + ":" + instanceName + ":" + counterName))
                {
                    PerformanceCounter counter = null;
                    try
                    {
                       counter = new PerformanceCounter(categoryName, counterName, instanceName, machine);
                    
                    }
                    catch(Exception)
                    {
                        machine = DnsCache.ResolveAddress(machine);
                        counter = new PerformanceCounter(categoryName, counterName, instanceName, machine);
                    }
                    counter.ReadOnly = true;
                    _counters.Add(categoryName + ":" + instanceName + ":" + counterName, counter);
                }
                return _counters[categoryName + ":" + instanceName + ":" + counterName].NextValue();
            }
            catch (Exception e)
            {
                return -404.0;
            }
        }
    }
}
