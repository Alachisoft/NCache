using System.Diagnostics;

namespace Alachisoft.NCache.Caching.Statistics
{
    internal interface IPerfInstaller
    {
        CounterCreationData[] CounterData { get; set; }
    }
}
