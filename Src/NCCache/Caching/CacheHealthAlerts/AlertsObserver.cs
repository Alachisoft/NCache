using Alachisoft.NCache.Caching.CacheHealthAlerts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{
    public class AlertsObserver
    {
        List<double> valueStorage = null;
        AlertCollectorsBase alertCollector = null;
        string cacheName = null;
        HealthMonitor healthMonitor = null;

        int index = 0;
        DateTime lastMaxIntervalEvent = DateTime.Now;
        DateTime lastMaxIntervalCache = DateTime.Now;
        DateTime lastMinIntervalEvent = DateTime.Now;
        DateTime lastMinIntervalCache = DateTime.Now;
        bool loggedInHighInterval = false;
        bool loggedInLowInterval = false;
        bool initialize = false;

        internal AlertsObserver(HealthMonitor monitor, AlertCollectorsBase collector)
        {
            healthMonitor = monitor;
            if (collector != null)
            {
                alertCollector = collector;
                if (alertCollector!=null)
                    valueStorage = new List<double>(alertCollector.Duration);
            }
            if (healthMonitor!=null && healthMonitor.Context!=null && healthMonitor.Context.CacheImpl!=null)
                cacheName = healthMonitor.Context.CacheImpl.Name;

        }

        internal void MonitorAlerts ()
        {
            double data = alertCollector.Collectstats();

            if (data >= 0)
            {
                if (valueStorage.Count < alertCollector.Duration)
                {
                    valueStorage.Add(data);
                    initialize = true;
                }
                else
                {
                    valueStorage[index] = data;
                    initialize = false;
                }

                index++;
                if (index >= alertCollector.Duration)
                {
                    CheckandRaiseEvent(index, data);
                    index = 0;
                }
                  

            }
        }


        internal string  GetLowEventMesage(double value)
        {
            return  $"Current Value ({value})  of '{ alertCollector.Name}' is less than or equal to specified minimum threshold {alertCollector.MinThreshold} for '{cacheName}'.";
        }

        internal string GetHighEventMesage(double value)
        {
            return $"Current Value ({value})  of '{ alertCollector.Name}' is higher than or equal to specified maximmum threshold {alertCollector.MaxThreshold} for '{cacheName}'.";
        }


        internal string GetLowCacheMesage(double value)
        {
            return $"Current Value ({value})  of '{ alertCollector.Name}' is less than or equal to specified minimum threshold {alertCollector.MinThreshold}.";
        }

        internal string GetHighCacheMesage(double value)
        {
            return $"Current Value ({value})  of '{ alertCollector.Name}' is higher than or equal to specified maximmum threshold {alertCollector.MaxThreshold}.";
        }


        internal void RaiseLowEvent(double value)
        {
            string eventMessage = GetLowEventMesage(value);
            string cacheMessage = GetLowCacheMesage(value);
            if (initialize)
            {
                bool logEvent = alertCollector.LogLowEvent(value);

                if (logEvent) healthMonitor.WriteEventLog(eventMessage, System.Diagnostics.EventLogEntryType.Warning, alertCollector.EventId);
                lastMinIntervalEvent = DateTime.Now;
                if (logEvent) healthMonitor.WriteCacheLog(cacheMessage);
                lastMinIntervalCache = DateTime.Now;
                alertCollector.LastValue = value;
            }
            else
            {
                bool logEvent = false;

                if (DateTime.Now > lastMinIntervalEvent.AddMinutes(healthMonitor.EventLoggingTime))
                {
                    logEvent = alertCollector.LogLowEvent(value);
                    if (logEvent)
                    {
                        healthMonitor.WriteEventLog(eventMessage, System.Diagnostics.EventLogEntryType.Warning, alertCollector.EventId);
                        lastMinIntervalEvent = DateTime.Now;
                    }
                }

                if (DateTime.Now > lastMinIntervalCache.AddMinutes(healthMonitor.CacheLoggingTime))
                {
                    logEvent = alertCollector.LogLowEvent(value);
                    if (logEvent)
                    {
                        healthMonitor.WriteCacheLog(cacheMessage);
                        lastMinIntervalCache = DateTime.Now;
                    }
                }
            }
            
        }

        internal void RaiseHighEvent(double value)
        {
            string eventMessage = GetHighEventMesage(value);
            string cacheMessage = GetHighCacheMesage(value);

            if (initialize)
            {
                healthMonitor.WriteEventLog(eventMessage, System.Diagnostics.EventLogEntryType.Warning, alertCollector.EventId);
                lastMaxIntervalEvent = DateTime.Now;
                healthMonitor.WriteCacheLog(cacheMessage);
                lastMaxIntervalCache = DateTime.Now;
            }
            else
            {

                if (DateTime.Now > lastMaxIntervalEvent.AddMinutes(healthMonitor.EventLoggingTime))
                {
                    healthMonitor.WriteEventLog(eventMessage, System.Diagnostics.EventLogEntryType.Warning, alertCollector.EventId);
                    lastMaxIntervalEvent = DateTime.Now;
                }

                if (DateTime.Now > lastMaxIntervalCache.AddMinutes(healthMonitor.CacheLoggingTime))
                {
                    healthMonitor.WriteCacheLog(cacheMessage);
                    lastMaxIntervalCache = DateTime.Now;
                }
            }

        }

        internal void CheckandRaiseEvent (int index, double data)
        {
            if (alertCollector != null)
            {
              
                if (index >= alertCollector.Duration)
                {
                    var avgValue = valueStorage.OfType<double>().Average();

                    avgValue = Math.Round(avgValue, 3);
                    data = Math.Round(data, 3); // figure should vbe round

                    if (avgValue<= alertCollector.MinThreshold)
                    {
                        RaiseLowEvent(data);
                    }
                    if (avgValue >= alertCollector.MaxThreshold && alertCollector.LogHighEvent())
                    {
                        RaiseHighEvent(data);
                    }

                    if (alertCollector.LastValue != avgValue)
                        alertCollector.LastValue = avgValue;
                }

            }
        }
    }
} 
