using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.Stats;
using System.Diagnostics;

namespace Alachisoft.NCache.Caching.Statistics
{
    public class PerfmonInterLocked
    {
        public static PerformanceCounterBase Exchange(ref PerformanceCounterBase baseCounter, double value)
        {

            if (baseCounter != null)
            {
                lock (baseCounter)
                {
                    baseCounter.Value = value;
                }
            }
            return baseCounter;
        }

        public static PerformanceCounterBase Increment(ref PerformanceCounterBase baseCounter)
        {
            if (baseCounter != null)
            {
                lock (baseCounter)
                {
                    baseCounter.Increment();
                }
            }
            return baseCounter;
        }

        public static PerformanceCounterBase IncrementBy(ref PerformanceCounterBase baseCounter, double value)
        {
            if (baseCounter != null)
            {
                lock (baseCounter)
                {
                    baseCounter.IncrementBy(value);
                }
            }
            return baseCounter;
        }

        public static PerformanceCounterBase BeginSample(ref PerformanceCounterBase baseCounter, ref UsageStats statistics)
        {
            if (baseCounter != null)
            {
                lock (baseCounter)
                {
                    statistics.BeginSample();
                }
            }
            return baseCounter;
        }

        public static PerformanceCounterBase EndSample(ref PerformanceCounterBase baseCounter, ref UsageStats statistics)
        {
            if (baseCounter != null)
            {
                lock (baseCounter)
                {
                    statistics.EndSample();
                    baseCounter.IncrementBy(statistics.Current * 1000 / Stopwatch.Frequency);
                }
            }
            return baseCounter;
        }
    }
}
