using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;

namespace Alachisoft.NCache.Caching.Statistics
{
    internal interface ICustomCountersInstaller
    {
        CounterCreationData[] CounterData { get; set; }
    }
}