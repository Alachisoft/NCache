using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData;

namespace Alachisoft.NCache.Common.Monitoring
{
    public interface IMetricsTransporter : IDisposable
    {
        bool IsConnected { get; }
        void Connect();
 
        void PublishMetadata(string sessionId, string version, CacheMetaData cacheMeta);

        void PublishMetadata(string sessionId, string version, ClientMetaData clientMeta);

        void PublishMetadata(string sessionId, CounterMetadataCollection counterMeta);

        PublishCountersDataResult PublishData(string session, CounterDataCollection data);

        void PublishData(string session, ClusterHealthData data);
    }
}
