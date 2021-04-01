using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Statistics;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Config.Dom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{
    internal class HealthMonitor :IDisposable
    {
        CacheRuntimeContext cacheContext = null;
        long eventLoggingTime;
        long cacheLoggingTime;
        Thread monitoringThread;
        int sleepTime = 1000;
        bool running = false;
        IDictionary<object, AlertsObserver> alertDictionary = new Dictionary<object, AlertsObserver>();
        public long EventLoggingTime
        {
            get { return eventLoggingTime; }

        }

        public long CacheLoggingTime
        {
            get { return cacheLoggingTime; }

        }

        public CacheRuntimeContext Context
        {
            get { return cacheContext; }
        }
        public HealthMonitor (CacheRuntimeContext context)
        {
            cacheContext = context;
            InitiazeMonitor();
        }


        void InitiazeMonitor ()
        {
            if (cacheContext.HealthAlerts != null)
            {
                eventLoggingTime = cacheContext.HealthAlerts.EventLoggingInterval;

                if (eventLoggingTime < 60)
                    eventLoggingTime = 60;

                cacheLoggingTime = cacheContext.HealthAlerts.CacheloggingInterval;

                if (cacheLoggingTime < 10)
                    cacheLoggingTime = 10;
            }
            InitializeHealthDictionary(cacheContext.HealthAlerts);
        }
        public void InitializeThread ()
        {
            if (monitoringThread == null)
            {
                monitoringThread = new Thread(new ThreadStart(StartMonitoring));
                monitoringThread.IsBackground = true;
                monitoringThread.Name = "MonitoringThread";
                running = true;
                monitoringThread.Start();
            }
        }

        void InitializeHealthDictionary(HealthAlerts healthAlerts)
        {
            if(healthAlerts!=null)
            {
                foreach (DictionaryEntry data in healthAlerts.Resources)
                {
                    string collectorName = (string)data.Key;

                    if ((collectorName.ToLower().Equals(ResourceName.MIORRORQUEUEACTIVE) || collectorName.ToLower().Equals(ResourceName.BRIDGQQUEUE)) && (cacheContext.CacheImpl != null && !cacheContext.IsClusteredImpl))
                        continue;

                    AlertCollectorsBase collector = GetCollector(collectorName, (ResourceAtribute) data.Value);
                    alertDictionary.Add(collector, new AlertsObserver(this, collector));
                }
            }
        }

        public void WriteCacheLog (string message)
        {
            if (cacheContext != null && cacheContext.NCacheLog != null)
                cacheContext.NCacheLog.CriticalInfo(message);
        }

        public void WriteEventLog(string message,EventLogEntryType type, int eventId)
        {
            AppUtil.LogEvent(message, type, eventId);
        }

        AlertCollectorsBase GetCollector (string collectorName, ResourceAtribute data)
        {
            AlertCollectorsBase collector = null;
            string counterName, name;
            int eventID = 0;

            switch (collectorName)
            {
                case ResourceName.MEMORY:
                    collector = new MemoryUsageCollector(data, cacheContext);
                    break;

                case ResourceName.NETWORK:
                    collector = new NetworkUsageCollector( data, cacheContext);
                    break;

                case ResourceName.CPU:
                    collector = new CPUUsageCollector(data, cacheContext);
                    break;

                case ResourceName.REQUESTPERSEC:
                    counterName = "Requests/sec";
                    name = counterName;
                    eventID = EventID.RequestsAlert;

                    collector = new SocketResourcesCollector (counterName, name, eventID, data, cacheContext);
                    break;

                case ResourceName.CLIENTCONNECTION:
                    counterName = "# Clients";
                    name = "Connected Clients";
                    eventID = EventID.ClientConnectionAlert;

                    collector = new SocketResourcesCollector(counterName, name, eventID, data, cacheContext,true);
                    break;
                    
                case ResourceName.MIORRORQUEUEACTIVE:
                    counterName = "Mirror queue size";
                    name = "Mirror Queue";
                    eventID = EventID.MirrorQueueAlert;

                    collector = new CacheResourcesCollector(counterName, name, eventID, data, cacheContext, true);
                    break;
                case ResourceName.MIORRORQUEUEREPLICA:
                    counterName = "Mirror queue size";
                    name = "Mirror Queue (Replica)";
                    eventID = EventID.MirrorQueueAlert;

                    collector = new CacheResourcesCollector(counterName, name, eventID, data, cacheContext, true);
                    break;
                case ResourceName.WRITEBEHINDQUEUE:

                    counterName = "Write-behind queue count";
                    name = "Write Behind Queue";
                    eventID = EventID.WriteBehindQueueAlert;

                    collector = new CacheResourcesCollector(counterName,name, eventID, data, cacheContext,true);
                    break;

                case ResourceName.AERAGECACHEOPERATIONS:

                    counterName = "Average us/cache operation";
                    name = counterName;
                    eventID = EventID.AverageCacheOperations;

                    collector = new CacheResourcesCollector(counterName, name, eventID, data, cacheContext);
                    break;
            }
            return collector;
        }

        void StartMonitoring ()
        {
            try
            {
                while (running)
                {
                    try
                    {
                        if (alertDictionary != null)
                        {
                            ICollection<AlertsObserver> enumerator = alertDictionary.Values;
                            if (enumerator != null)
                            {
                                foreach (AlertsObserver observer in enumerator)
                                {
                                    observer.MonitorAlerts();
                                }

                            }
                        }
                    }
                    catch (ThreadAbortException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    Thread.Sleep(sleepTime);

                }
            }
            catch
            {

            }
        }

        void StopMonitoring ()
        {
            running = false;
            if (monitoringThread != null && monitoringThread.IsAlive && monitoringThread.ThreadState == System.Threading.ThreadState.Running)
                monitoringThread.Abort();

        }

        public void Dispose()
        {
            StopMonitoring();
            monitoringThread = null;
            alertDictionary.Clear();
            alertDictionary = null;

        }
    }
}
