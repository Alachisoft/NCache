using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Management
{
 public interface IMetricsMonitor
    {
        bool IsConnected { get; }

        void PublishMetadata(string sessionId, string version, CacheMetaData cacheMeta);
        void PublishMetadata(string sessionId, string version, ClientMetaData cacheMeta);

        void PublishMetadata(string sessionId, CounterMetadataCollection counterMeta);

        int PublishData(string session, CounterDataCollection data);

        

        void PublishData(string session, ClusterHealthData data);
    }
}
