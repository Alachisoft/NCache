using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{
    public class CPUUsageCollector : AlertCollectorsBase
    {
        PerformanceCounter counter = null;
        private CPUUsage _cpuUsage = null;
        int processrs = 0;

        private void InitializeCPUUsageCollector()
        {
            CouneterName = "% Processor Time";
            Name = "CPU Usage";
#if !NETCORE
            int retries = 3;
            do
            {
                try
                {
                    InitializeCounter();
                    break;
                }
                catch (Exception)
                {
                    retries--;
                   
                }
            } while (retries > 0);
           
#elif NETCORE
            InitiliazeCPUNETCORE();
#endif
            processrs = Environment.ProcessorCount;
            if (processrs <= 0)
                processrs = 1;
        }

        internal CPUUsageCollector(ResourceAtribute attribute, CacheRuntimeContext cacheRuntimeContext) : base(attribute, cacheRuntimeContext)
        {
            try
            {
                InitializeCPUUsageCollector();
                SetThresholds();
            }
            catch (Exception ex)
            {

            }
        }

        public CPUUsageCollector()
        {
            InitializeCPUUsageCollector();
        }

        public override int EventId
        {
            get
            {
                return EventID.CPUAlert;
            }
        }

        public override double Collectstats()
        {
            try
            {

#if !NETCORE
                if (counter != null)
                        return (counter.NextValue()/ processrs);
#elif NETCORE

                if (_cpuUsage != null)
                {

                    return _cpuUsage.GetUsage();
                }
#endif

            }
            catch (Exception ex)
            {

            }
            return 0.0;
        }

        public void InitializeCounter()
        {
            try
            {
                string instanceName = GetProcessInstancename();
                counter = new PerformanceCounter("Process", "% Processor Time", instanceName, true);
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Couldn't Initialize CPU Usage Counter : " + ex.ToString(), EventLogEntryType.Error);
            }
        }

        public double InitiliazeCPUNETCORE()
        {
            if (_cpuUsage == null)
            {
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    _cpuUsage = new NetCoreCPUUsage();
                else
                    _cpuUsage = new CPUUsage();
#endif

            }
            return 0.0;
        }


    }
}
