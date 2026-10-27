//  Copyright (c) 2018 Alachisoft
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using Alachisoft.NCache.Common.Monitoring;
using System;

namespace Alachisoft.NCache.MetricServer
{
    public enum MetricPublisher
    {
        NCache,
        Client
    }
}

namespace Alachisoft.NCache.MetricServer.DataModel
{
    /// <summary>One counter sample as received from a publisher.</summary>
    public class CounterData
    {
        public short CounterId { get; set; }
        public double Data { get; set; }
        public DateTime Timestamp { get; set; }
        public Counter CounterMetadata { get; set; }
    }

    /// <summary>Counter metadata as held by a session.</summary>
    public class Counter : CounterMetadata
    {
        public short Id { get; set; }

        public override string ToString()
        {
            return $"Id: {Id} Name: {Name}";
        }
    }

    /// <summary>Cache metadata flattened for labelling.</summary>
    public class CacheConfig : CacheMetaData
    {
        public string ConfigId { get; set; }
        public string CacheId { get; set; }

        public static CacheConfig FromCacheMetaData(CacheMetaData metaData)
        {
            return new CacheConfig
            {
                CacheId = metaData.Identifier?.CacheId,
                ConfigId = metaData.Identifier?.ConfigId,
                ClusterIP = metaData.ClusterIP,
                ClientServerIP = metaData.ClientServerIP,
                ClientServerPort = metaData.ClientServerPort,
                SessionID = metaData.SessionID,
                Topology = metaData.Topology,
                ConfiguredServers = metaData.ConfiguredServers,
                CacheSize = metaData.CacheSize,
                ConfiguredServersCount = metaData.ConfiguredServersCount,
                FromReplica = metaData.FromReplica,
                InstallationType = metaData.InstallationType
            };
        }
    }

    /// <summary>Client metadata flattened for labelling.</summary>
    public class Client : ClientMetaData
    {
        public string SessionId { get; set; }

        public static Client FromClientMetaData(ClientMetaData clientMeta)
        {
            return new Client
            {
                BindIP = clientMeta.BindIP,
                CacheName = clientMeta.CacheName,
                CacheConfigId = clientMeta.CacheConfigId
            };
        }
    }
}
