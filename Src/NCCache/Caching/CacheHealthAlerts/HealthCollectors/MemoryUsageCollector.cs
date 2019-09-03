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
    public class MemoryUsageCollector : AlertCollectorsBase
    {
        PerformanceCounter counter = null;

        internal MemoryUsageCollector(ResourceAtribute attribute, CacheRuntimeContext cacheRuntimeContext) : base(attribute, cacheRuntimeContext)
        {
            try
            {
                CouneterName = "Private Bytes";
                Name = "Memory Usage";
                SetThresholds();
                if (!ServiceConfiguration.PublishCountersToCacheHost)
                    InitializeCounter();

            }
            catch (Exception ex)
            {

            }
        }

        public override int EventId
        {
            get
            {
                return EventID.MemoryAlert;
            }
        }

        public override double Collectstats()
        {
            double data = 0;
            try
            {
                if (!ServiceConfiguration.PublishCountersToCacheHost)
                {
                    if (counter != null)
                        data = (counter.NextValue()); //mbs
                }
                else
                {
                    if (Context != null && Context.PerfStatsColl != null)
                        data = AppUtil.CurrentProcess.WorkingSet64;
                }
                if (data > 0)
                {
                    long maxSize = Context.CacheImpl.InternalCache.MaxSize * 5;

                    data = (data / maxSize) * 100;

                    return data;
                }
            }
            catch
            {

            }
            return data;

        }

        public void InitializeCounter ()
        {
            try
            {
                string instanceName = GetProcessInstancename();
                counter = new PerformanceCounter("Process", "Working Set - Private", instanceName, true);
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Couldn't Initialize Memory Usage Counter : " + ex.ToString(), EventLogEntryType.Error);
            }
        }

       

    }
}
