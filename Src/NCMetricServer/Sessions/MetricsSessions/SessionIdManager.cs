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

namespace Alachisoft.NCache.MetricServer.Sessions.MetricsSessions
{
    /// <summary>
    /// Derives the session key from what the publisher sent. A cache host publishes under
    /// "&lt;guid&gt;:NCache" (":replica" appended for the replica half); a client under
    /// "&lt;guid&gt;:NCacheClient" plus its instance name, which is its bind IP.
    /// </summary>
    public class SessionIdManager
    {
        private const string replica = "replica";

        public string GetSessionId(string sessionId, CounterMetadataCollection counterMeta)
        {
            return GetSessionId(sessionId, counterMeta.Category, counterMeta.FromReplica, counterMeta.InstanceName);
        }

        public string GetSessionId(string sessionId, CounterDataCollection counterdata)
        {
            return GetSessionId(sessionId, counterdata.Category, counterdata.FromReplica, counterdata.InstanceName);
        }

        public string GetSessionId(string sessionId, CacheMetaData cacheMeta)
        {
            return cacheMeta.FromReplica ? $"{sessionId}:{replica}" : sessionId;
        }

        public string GetSessionId(string sessionId, ClientMetaData client)
        {
            return $"{sessionId}:{client.BindIP}".ToLower();
        }

        private static string GetSessionId(string sessionId, Publisher category, bool fromReplica, string instanceName)
        {
            switch (category)
            {
                case Publisher.NCacheClient:
                    return $"{sessionId}:{instanceName}".ToLower();
                default:
                    return fromReplica ? $"{sessionId}:{replica}" : sessionId;
            }
        }
    }
}
