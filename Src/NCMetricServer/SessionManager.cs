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
using Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.MetricServer.DataModel;
using Alachisoft.NCache.MetricServer.MetricsAgents;
using Alachisoft.NCache.MetricServer.MetricsSessions;
using Alachisoft.NCache.MetricServer.Sessions;
using Alachisoft.NCache.MetricServer.Sessions.MetricsSessions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Alachisoft.NCache.MetricServer
{
    /// <summary>
    /// Keeps one <see cref="MetricsSession"/> per publisher (a cache host, a replica, a client
    /// process). Publishers first send metadata, then counter metadata, then counter data every
    /// second; a background thread pushes the latest values to the agents and drops sessions that
    /// have gone quiet.
    /// </summary>
    public class SessionManager : IDisposable
    {
        private readonly NCacheLogger _logger;
        private Thread _metricsPublisher;
        private bool _isPersistanceEnabled;
        // ServiceConfiguration stores the interval in milliseconds; never spin on a zero value.
        private readonly int _persistanceInterval = Math.Max(ServiceConfiguration.MetricServerPersistanceInterval, 1000);
        private readonly object _clientSessionLock = new object();
        private readonly MetricsAgentManager _metricsAgentManager;

        public SessionIdManager SessionIdManager { get; } = new SessionIdManager();
        public IDictionary<string, MetricsSession> Index { get; set; } = new Dictionary<string, MetricsSession>();

        public SessionManager(NCacheLogger logger)
        {
            _logger = logger;
            _metricsAgentManager = new MetricsAgentManager();
            Intialize();
        }

        public void Intialize()
        {
            if (ServiceConfiguration.EnablePrometheusMonitoring)
            {
                _isPersistanceEnabled = true;
                var prometheusAgent = new PrometheusAgent();
                prometheusAgent.Initialize(_logger);
                prometheusAgent.Start();
                _metricsAgentManager.AddMetricsAgent(prometheusAgent);
            }

            if (_isPersistanceEnabled && _metricsPublisher == null)
            {
                _metricsPublisher = new Thread(DoBackgroundTasks)
                {
                    IsBackground = true,
                    Name = "MetricsPublisher"
                };
                _metricsPublisher.Start();
            }
        }

        #region Get - Sessions

        private ClientSession CreateClientSession(string sessionId, string version, Client clientMeta)
        {
            ClientSession metricsSession;
            lock (_clientSessionLock)
            {
                metricsSession = new ClientSession(sessionId, clientMeta)
                {
                    Version = version
                };
                metricsSession.Initialize(_logger, _metricsAgentManager);
            }
            return metricsSession;
        }

        public MetricsSession GetOrCreateClientSession(string sessionId, string version = null, Client clientMeta = null)
        {
            lock (Index)
            {
                if (!Index.ContainsKey(sessionId))
                {
                    Index[sessionId] = CreateClientSession(sessionId, version, clientMeta);
                }
                return Index[sessionId];
            }
        }

        public MetricsSession GetOrCreateCacheSession(string sessionId, CacheConfig cacheMetadata = null, string version = null)
        {
            lock (Index)
            {
                if (!Index.ContainsKey(sessionId))
                {
                    CacheSession metricsSession = new CacheSession(sessionId, cacheMetadata, version);
                    metricsSession.Initialize(_logger, _metricsAgentManager);
                    Index[sessionId] = metricsSession;
                }
                return Index[sessionId];
            }
        }

        public void ExpireInActiveSessions(List<string> metricsSessions)
        {
            lock (Index)
            {
                try
                {
                    foreach (string sessionId in metricsSessions)
                    {
                        if (Index.TryGetValue(sessionId, out MetricsSession session))
                        {
                            session.Dispose();
                            Index.Remove(sessionId);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("MetricServer SM", $"Unable to Expire Session\n{e}");
                }
            }
        }

        #endregion

        #region Add - Session Data

        public PublishCountersDataResult PublishCountersData(string sessionId, CounterDataCollection data)
        {
            var sessionIdCopy = SessionIdManager.GetSessionId(sessionId, data);
            MetricsSession session = GetSession(sessionIdCopy);
            if (session == null)
                return PublishCountersDataResult.CountersSessionExpired;
            return PublishCountersData(session, data);
        }

        public void PublishCacheMetadata(string sessionId, string version, CacheMetaData cacheMeta)
        {
            var metadata = CacheConfig.FromCacheMetaData(cacheMeta);
            var session = SessionIdManager.GetSessionId(sessionId, cacheMeta);
            var ncacheSession = GetOrCreateCacheSession(session, metadata, version) as CacheSession;
            if (ncacheSession != null)
                ncacheSession.Version = version;
        }

        public void PublishClientMetadata(string sessionId, string version, ClientMetaData clientMeta)
        {
            var metadata = Client.FromClientMetaData(clientMeta);
            metadata.SessionId = sessionId;
            var sessionIdCopy = SessionIdManager.GetSessionId(sessionId, clientMeta);
            var clientSession = GetOrCreateClientSession(sessionIdCopy, version, metadata) as ClientSession;
            if (clientSession != null)
                clientSession.Version = version;
        }

        public void PublishCountersMetadata(string sessionId, CounterMetadataCollection counterMeta)
        {
            var sessionIdCopy = SessionIdManager.GetSessionId(sessionId, counterMeta);
            var metricSession = GetSession(sessionIdCopy);
            if (metricSession != null)
            {
                metricSession.AddCountersMetadata(counterMeta);
                metricSession.CreateCounters();
            }
        }

        private PublishCountersDataResult PublishCountersData(MetricsSession session, CounterDataCollection counterData)
        {
            if (!session.IsMetadataPersisted)
                return PublishCountersDataResult.CountersMetaDataNotPersisted;
            session.AddCountersData(session.SessionId, counterData);
            return PublishCountersDataResult.DataPersistedSuccessfully;
        }

        #endregion

        #region Background Task

        public void DoBackgroundTasks()
        {
            try
            {
                List<string> sessionsToExpire = new List<string>();
                while (_isPersistanceEnabled)
                {
                    try
                    {
                        Thread.Sleep(_persistanceInterval);
                        List<string> sessionKeys;
                        lock (Index)
                        {
                            if (Index.Count == 0)
                                continue;
                            sessionKeys = new List<string>(Index.Keys);
                        }

                        foreach (string sessionKey in sessionKeys)
                        {
                            try
                            {
                                MetricsSession session;
                                bool sessionExists;
                                lock (Index)
                                {
                                    sessionExists = Index.TryGetValue(sessionKey, out session);
                                }
                                if (sessionExists)
                                {
                                    session.Persist();
                                    if (session.IsExpired())
                                        sessionsToExpire.Add(sessionKey);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.Error("SM MetricServer", $" Unable to persist Session: {sessionKey}\n{e}");
                            }
                        }
                        ExpireInActiveSessions(sessionsToExpire);
                        sessionsToExpire.Clear();
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.Error("SM MetricServer Persistor", e.ToString());
                    }
                }
            }
            catch (ThreadInterruptedException) { }
            catch (Exception e)
            {
                _logger.Error("SM MetricServer Persistor", $" Persistor Stopped due to\n{e}");
            }
        }

        #endregion

        public void Dispose()
        {
            try
            {
                _isPersistanceEnabled = false;
                if (_metricsPublisher != null && _metricsPublisher.IsAlive)
                    _metricsPublisher.Interrupt();
                _metricsAgentManager.Dispose();
            }
            catch (Exception)
            {
            }
        }

        private MetricsSession GetSession(string sessionId)
        {
            lock (Index)
            {
                Index.TryGetValue(sessionId, out MetricsSession metricSession);
                return metricSession;
            }
        }
    }
}
