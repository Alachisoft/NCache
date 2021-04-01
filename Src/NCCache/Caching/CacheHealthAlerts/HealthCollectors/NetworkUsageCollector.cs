using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{
    public class NetworkUsageCollector : AlertCollectorsBase
    {
        PerformanceCounter currentBandwidth = null;
        PerformanceCounter totalBytes = null;
        bool initialized = false;

        internal NetworkUsageCollector(ResourceAtribute attribute, CacheRuntimeContext cacheRuntimeContext) : base(attribute, cacheRuntimeContext)
        {
            CouneterName = "Current Bandwidth";
            Name = "Network Usage";
            SetThresholds();
        }

        public override int EventId
        {
            get
            {
                return EventID.NetworkAlert;
            }
        }

        public override double Collectstats()
        {
            double data = 0.0;
            try
            {
                if (!ServiceConfiguration.PublishCountersToCacheHost)
                {
                    if (initialized)
                    {
                        if (currentBandwidth != null)
                        {
                            if (totalBytes != null)
                            {
                                double bytes = totalBytes.NextValue();
                                double bandwidth = currentBandwidth.NextValue();
                                if (bandwidth > 0)
                                    data = ((bytes * 8) / (bandwidth)) * 100;
                            }
                            return data;
                        }
                    }
                    else
                        InitializeCounter();
                }
                else
                {
                    if (Context != null && Context.PerfStatsColl != null)
                        return (Context.PerfStatsColl.GetCounterValue(CouneterName)) / (1024);
                }
            }
            catch (Exception e)
            {

            }
            return data;
        }

        public string GetInstanceName()
        {
            List<CacheNodeStatistics> statistics = null;

            if (Context != null && Context.CacheImpl!=null)
            {
                statistics = Context.CacheImpl.GetCacheNodeStatistics();
                if (statistics != null && statistics.Count > 0)
                {
                    CacheNodeStatistics stats = statistics[0];
                    if (stats.Status == CacheNodeStatus.Running || stats.Status == CacheNodeStatus.InStateTransfer)
                    {
                        return GetNICForIP(stats.Node.Address.IpAddress.ToString());
                    }
                }
                return null;

            }
            return null; 
        
        }
        public string GetNICForIP(string ip)
        {
            if (ip == null) return null;
            string nic = null;
            try
            {

                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("Select * from Win32_NetworkAdapterConfiguration WHERE IPEnabled=True");

                foreach (ManagementObject mo in searcher.Get())
                {
                    string[] ipAddresses = mo.GetPropertyValue("IPAddress") as string[];

                    foreach (string ipAddress in ipAddresses)
                    {
                        if ((string.Compare(ipAddress, ip, true) == 0))
                        {
                            nic = (string)mo.GetPropertyValue("Description");
                            break;
                        }
                    }
                }

            }
            catch (Exception)
            {

            }
            initialized = true;
            if (!string.IsNullOrEmpty(nic))
            {
                nic = nic.Replace('/', '_');        
                nic = nic.Replace('#', '_');        
            }

            return nic;
        }

        void InitializeCounter ()
        {
            try
            {
                if (!initialized)
                {
                    string instanceName = GetInstanceName();
                  
                    currentBandwidth = new PerformanceCounter("Network Adapter", CouneterName, instanceName, true);
                    totalBytes = new PerformanceCounter("Network Adapter", "Bytes Total/Sec", instanceName, true);
                }
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Couldn't Initialize Network Usage Counter : " + ex.ToString(), EventLogEntryType.Error);
            }
        }

    }
}
