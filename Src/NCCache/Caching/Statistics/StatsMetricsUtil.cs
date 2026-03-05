using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.Statistics
{
   public class StatsMetricsUtil
    {
#if NETCORE
        public static CounterMetadataCollection Metadata(Common.Caching.Statistics.CustomCounters.CounterCreationData[] counterMeta, Publisher publisher, Category category)
        {
            try
            {
                List<CounterMetadata> registeredCounters = new List<CounterMetadata>();
                foreach (var counterData in counterMeta)
                {
                    foreach (Counter counter in category.Counters)
                    {
                        if (counter.Name.Equals(counterData.CounterName) && counter.Publish)
                        {
                            if (counterData.IsBaseCounter)
                                continue;

                            registeredCounters.Add(new CounterMetadata
                            {
                                Name = counterData.CounterName,
                                Type = counterData.CounterType,
                                Description = counterData.CounterHelp,
                                Category = publisher
                            });
                        }
                    }
                }
                return new CounterMetadataCollection
                {
                    Counters = registeredCounters,
                };
            }
            catch (Exception ex)
            {
                return new CounterMetadataCollection();
            }
        } 
#endif

        public static CounterMetadataCollection Metadata(System.Diagnostics.CounterCreationData[] counterMeta, Publisher publisher, Category category)
        {
            try
            {
                List<CounterMetadata> registeredCounters = new List<CounterMetadata>();
                foreach (var counterData in counterMeta)
                {
                    foreach (Counter counter in category.Counters)
                    {
                        if (counter.Name.Equals(counterData.CounterName) && counter.Publish)
                        {
                            if (counterData.CounterType == PerformanceCounterType.AverageBase)
                                continue;

                            CounterType type = GetCounterType(counterData);

                            registeredCounters.Add(new CounterMetadata
                            {
                                Name = counterData.CounterName,
                                Type = type,
                                Description = counterData.CounterHelp,
                                Category = publisher
                            });
                        }
                    }
                }
                return new CounterMetadataCollection
                {
                    Counters = registeredCounters,
                };
            }
            catch (Exception ex)
            {
                return new CounterMetadataCollection();
            }
        }

        private static CounterType GetCounterType(System.Diagnostics.CounterCreationData counter)
        {
            switch (counter.CounterType)
            {
                case PerformanceCounterType.AverageCount64:
                case PerformanceCounterType.AverageTimer32:
                    return CounterType.AverageCounter;
                case PerformanceCounterType.NumberOfItems32:
                case PerformanceCounterType.NumberOfItems64:
                case PerformanceCounterType.NumberOfItemsHEX32:
                case PerformanceCounterType.NumberOfItemsHEX64:
                    return CounterType.NumberOfItemCounter;
                case PerformanceCounterType.RateOfCountsPerSecond32:
                case PerformanceCounterType.RateOfCountsPerSecond64:
                    return CounterType.RateOfCounter;
                case PerformanceCounterType.SampleBase:
                case PerformanceCounterType.SampleCounter:
                case PerformanceCounterType.SampleFraction:
                    return CounterType.SampleCounter;
                default:
                    return CounterType.Default;
            }
        }
    }
}
