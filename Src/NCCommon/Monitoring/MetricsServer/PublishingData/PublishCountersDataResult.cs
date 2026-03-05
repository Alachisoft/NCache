using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData
{
    public enum PublishCountersDataResult
    {
        DataPersistedSuccessfully = 0,
        CountersMetaDataNotPersisted = 1,
        CountersSessionExpired = 2,
        MetricServerNotInitialized = 3
    }
}
