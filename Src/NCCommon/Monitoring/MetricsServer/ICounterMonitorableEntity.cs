using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.Common.Monitoring
{
    public interface ICounterMonitorableEntity:IMonitorableEntity
    {
        CounterMetadataCollection Metadata { get;  }
        IntervalCounterDataCollection Data { get; }
        Publisher PublisherType { get;  }
        bool MergeCounters { get; }
    }
}
