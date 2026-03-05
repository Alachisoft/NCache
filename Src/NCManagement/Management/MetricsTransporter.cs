using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Management.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.ServiceControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Management
{

    public class MetricsTransporter : IMetricsTransporter
    {
        ICacheServer _cacheServer;

        public MetricsTransporter()
        {

        }

        public bool IsConnected => _cacheServer != null ? _cacheServer.IsConnected : false;

        public void Connect()
        {
            Initialize();

        }

        public void Dispose()
        {
            if (_cacheServer != null) _cacheServer.Dispose();
        }

        public PublishCountersDataResult PublishData(string session, CounterDataCollection data)
        {
            return (PublishCountersDataResult)_cacheServer.PublishData(session, data);
        }

      

        public void PublishData(string session, ClusterHealthData data)
        {
            throw new NotImplementedException();
        }

        public void PublishMetadata(string sessionId, string version, CacheMetaData cacheMeta)
        {
            _cacheServer.PublishMetadata(sessionId, version, cacheMeta);
        }

       

        public void PublishMetadata(string sessionId, string version, ClientMetaData clientMeta)
        {
            _cacheServer.PublishMetadata(sessionId, version, clientMeta);
        }

        public void PublishMetadata(string sessionId, CounterMetadataCollection counterMeta)
        {
            _cacheServer.PublishMetadata(sessionId, counterMeta);
        }


        private void Initialize()
        {
            CacheService cacheService = new NCacheRPCService(ServiceConfiguration.BindToIP.ToString(), (ServiceConfiguration.ManagementPort));
            _cacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 30));

        }

    }
}
