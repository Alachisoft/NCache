using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Management.Management.Statistics
{
    public class StatisticsUtil
    {
        /// <summary>
        /// Gets the exact instance name registered of the Performance counter category "Process". 
        /// </summary>
        /// <param name="cacheId"></param>
        /// <returns></returns>
        public static string GetInstanceName(string cacheId)
        {
            return GetProcessInstanceName(CacheServer.Instance.GetCacheHostProcessID(cacheId));
        }
        
        public static string GetProcessInstanceName(int cacheHostID)
        {
            PerformanceCounterCategory processCategory = new PerformanceCounterCategory("Process");
            string[] instances = GetAllRequiredInstances(processCategory.GetInstanceNames());

            foreach (string instance in instances)
            {
                using (PerformanceCounter processCounter = new PerformanceCounter("Process", "ID Process", instance, true))
                {
                    int processID = (int)processCounter.RawValue;

                    if (processID == cacheHostID)
                    {
                        return instance;
                    }
                }
            }

            return null;
        }

        public static string[] GetAllRequiredInstances(string[] instances)
        {
            return (from instance in instances
                    where instance.Contains("dotnet") || instance.Contains("Alachisoft.NCache.CacheHost")
                    select instance).ToArray();
        }
    }
}
