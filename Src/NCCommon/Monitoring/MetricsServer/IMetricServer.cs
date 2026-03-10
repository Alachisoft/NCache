using Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public interface IMetricServer : IDisposable
    {
      
        #region Publishing 

        #region Publishing - Metadata
        void PublishMetadata(string sessionId, string version, CacheMetaData cacheMeta);

        void PublishMetadata(string sessionId, string version, ClientMetaData clientMeta);

        void PublishMetadata(string sessionId, CounterMetadataCollection counterMeta);

        

        #endregion
        #region Publishing - Data
        PublishCountersDataResult PublishData(string session, CounterDataCollection data);
        
        void PublishData(string session, Common.Monitoring.ClusterHealthData data);
        #endregion
        #endregion
    }

}
