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
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.MetricServer.MetricsAgents;
using System;

namespace Alachisoft.NCache.MetricServer.Sessions
{
    /// <summary>
    /// A publisher session: tracks when it last received data so quiet publishers (a stopped
    /// cache, a closed client) are expired and their metrics removed from the exporter.
    /// </summary>
    public abstract class MetricsSession : CounterSession
    {
        private MetricPublisher _publisher;
        private const int _readExpirationTime = 100; //seconds
        private const int _writeExpirationTime = 30; //seconds
        protected string _sessionId;

        protected int InMemoryInterval { get; set; } = 30;
        public DateTime? LastWriteTime { get; private set; }
        public DateTime? LastReadTime { get; private set; }
        public string Version { get; set; }
        public ILogger Logger { get; set; }
        public MetricPublisher Publisher { get => _publisher; }
        public string SessionId { get => _sessionId; }
        public virtual bool IsMetadataPersisted { get => CountersMetaPersisted; }

        protected MetricsSession(string sessionId, MetricPublisher publisher, string cacheConfigId) : base(cacheConfigId)
        {
            _sessionId = sessionId;
            _publisher = publisher;
            UpdatedPublisher();
        }

        // Session ids are "<guid>:<publisher>[:<instance>]"; the middle token names the publisher.
        protected void UpdatedPublisher()
        {
            string[] sessionToken = _sessionId.Split(':');
            if (sessionToken.Length > 1)
            {
                string pubString = sessionToken[1];
                if (pubString.Equals("NCache"))
                    _publisher = MetricPublisher.NCache;
                else if (pubString.Equals("NCacheClient"))
                    _publisher = MetricPublisher.Client;
            }
        }

        public virtual void Initialize(NCacheLogger logger, MetricsAgentManager metricsAgentManager)
        {
            Logger = logger;
            IntializeCounters(InMemoryInterval, Logger, metricsAgentManager);
        }

        protected void UpdateWriteTime()
        {
            LastWriteTime = DateTime.UtcNow;
        }

        public bool IsWriteActive()
        {
            if (LastWriteTime == null)
                return false;
            return DateTime.UtcNow.Subtract(LastWriteTime.Value).TotalSeconds <= _writeExpirationTime;
        }

        public bool IsReadActive()
        {
            if (LastReadTime == null)
                return false;
            return DateTime.UtcNow.Subtract(LastReadTime.Value).TotalSeconds <= _readExpirationTime;
        }

        public bool IsExpired()
        {
            return !(IsReadActive() || IsWriteActive());
        }

        public virtual void Persist()
        {
            if (IsWriteActive())
                PersistCounters();
        }

        #region Add Counters in Session

        public virtual void AddCountersMetadata(CounterMetadataCollection counterMeta)
        {
            UpdateWriteTime();
            StoreCountersMetadata(counterMeta);
        }

        public virtual void AddCountersData(string sessionId, CounterDataCollection counterData)
        {
            UpdateWriteTime();
            StoreCountersData(sessionId, counterData);
        }

        #endregion
    }
}
