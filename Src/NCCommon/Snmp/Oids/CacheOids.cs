//  Copyright (c) 2026 Alachisoft
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


using System.Collections.Generic;

namespace Alachisoft.NCache.Common.Snmp.Oids
{
    public class CacheOids : ParentOids
    {
        private static IDictionary<string, string> _counterMappings = null;
        #region ________________________Cache Counters__________________________
        public const string CacheIndex = CacheEntry + ".1";
        public const string CacheName = CacheEntry + ".2";
        public const string ConfigId = CacheEntry + ".3";
        public const string ClientServerIP = CacheEntry + ".4";
        public const string IsReplica = CacheEntry + ".5";
        public const string AdditionsPerSec = CacheEntry + ".6";
        public const string AverageMsecPerAddbulk = CacheEntry + ".7";
        public const string AverageMsecPerBridgeOperation = CacheEntry + ".8";
        public const string AverageMsecPerCacheOperation = CacheEntry + ".9";
        public const string AverageMsecPerFetch = CacheEntry + ".10";
        public const string AverageMsecPerFetchbulk = CacheEntry + ".11";
        public const string AverageMsecPerInsert = CacheEntry + ".12";
        public const string AverageMsecPerInsertbulk = CacheEntry + ".13";
        public const string AverageMsecPerRemove = CacheEntry + ".14";
        public const string AverageMsecPerRemovebulk = CacheEntry + ".15";
        public const string CacheSize = CacheEntry + ".16";
        public const string Count = CacheEntry + ".17";
        public const string DictionaryCount = CacheEntry + ".18";
        public const string CounterCount = CacheEntry + ".19";
        public const string CounterSize = CacheEntry + ".20";
        public const string DeletesPerSec = CacheEntry + ".21";
        public const string MessageStoreSize = CacheEntry + ".22";
        public const string MessagesCount = CacheEntry + ".23";
        public const string ExpirationIndexSize = CacheEntry + ".24";
        public const string ExpirationsPerSec = CacheEntry + ".25";
        public const string FetchesPerSec = CacheEntry + ".26";
        public const string RequestsPerSec = CacheEntry + ".27";
        public const string UpdatesPerSec = CacheEntry + ".28";
        public const string AverageMsecPerQueryExecution = CacheEntry + ".29";
        public const string AverageMsecPerDataSourceUpdate = CacheEntry + ".30";
        public const string AverageMsecPerDataSourceWrite = CacheEntry + ".31";
        public const string NumberOfClients = CacheEntry + ".32";
        public const string AverageQuerySize = CacheEntry + ".33";
        public const string AverageMsecPerReadThru = CacheEntry + ".34";
        public const string AverageMsecPerWriteBehind = CacheEntry + ".35";
        public const string AverageMsecPerWriteThru = CacheEntry + ".36";
        public const string BcastQueueCount = CacheEntry + ".37";
        public const string BridgeOperationsReceived = CacheEntry + ".38";
        public const string BridgeOperationsReceivedPerSec = CacheEntry + ".39";
        public const string BridgeOperationsSent = CacheEntry + ".40";
        public const string BridgeOperationsSentPerSec = CacheEntry + ".41";
        public const string BridgeQueueCount = CacheEntry + ".42";
        public const string BridgeQueueSize = CacheEntry + ".43";
        public const string BytesReceivedPerSec = CacheEntry + ".44";
        public const string BytesSentPerSec = CacheEntry + ".45";
        public const string CacheLastAccessCount = CacheEntry + ".46";
        public const string ClientBytesReceivedPerSec = CacheEntry + ".47";
        public const string ClientBytesSentPerSec = CacheEntry + ".48";
        public const string ClientRequestsPerSec = CacheEntry + ".49";
        public const string ClientResponsesPerSec = CacheEntry + ".50";
        public const string ClusterOpsPerSec = CacheEntry + ".51";
        public const string ClusteredOpsRecvPerSec = CacheEntry + ".52";
        public const string ClusteredOpsSentPerSec = CacheEntry + ".53";
        public const string DataBalancePerSec = CacheEntry + ".54";
        public const string DataSourceFailedOperationsPerSec = CacheEntry + ".55";
        public const string DataSourceUpdatesPerSec = CacheEntry + ".56";
        public const string DictionarySize = CacheEntry + ".57";
        public const string DispatchEnterPerSec = CacheEntry + ".58";
        public const string EventQueueCount = CacheEntry + ".59";
        public const string EvictionIndexSize = CacheEntry + ".60";
        public const string EvictionsPerSec = CacheEntry + ".61";
        public const string GeneralNotificationsQueueSize = CacheEntry + ".62";
        public const string GroupIndexSize = CacheEntry + ".63";
        public const string HitsRatioPerSec = CacheEntry + ".64";
        public const string HitsPerSec = CacheEntry + ".65";
        public const string ListCount = CacheEntry + ".66";
        public const string ListSize = CacheEntry + ".67";
        public const string LoggedRequestCount = CacheEntry + ".68";
        public const string MapReduceCombinedPerSec = CacheEntry + ".69";
        public const string MapReduceMappedPerSec = CacheEntry + ".70";
        public const string MapReduceReducedPerSec = CacheEntry + ".71";
        public const string MapReduceRunningTasks = CacheEntry + ".72";
        public const string MapReduceWaitingTasks = CacheEntry + ".73";
        public const string McastQueueCount = CacheEntry + ".74";
        public const string MessagesDeliveredPerSec = CacheEntry + ".75";
        public const string MessagesExpiredPerSec = CacheEntry + ".76";
        public const string MessagesPublishedPerSec = CacheEntry + ".77";
        public const string MirrorQueueSize = CacheEntry + ".78";
        public const string MissesPerSec = CacheEntry + ".79";
        public const string NaglingMsgCount = CacheEntry + ".80";
        public const string QueriesPerSec = CacheEntry + ".81";
        public const string QueryIndexSize = CacheEntry + ".82";
        public const string QueueCount = CacheEntry + ".83";
        public const string QueueSize = CacheEntry + ".84";
        public const string ReadersCount = CacheEntry + ".85";
        public const string ReadThruPerSec = CacheEntry + ".86";
        public const string RequestLogLedgerSize = CacheEntry + ".87";
        public const string RequestsLoggedPerSec = CacheEntry + ".88";
        public const string ResponseQueueCount = CacheEntry + ".89";
        public const string ResponseQueueSize = CacheEntry + ".90";
        public const string ResponseSentPerSec = CacheEntry + ".91";
        public const string ResponsesPerSec = CacheEntry + ".92";
        public const string SetCount = CacheEntry + ".93";
        public const string SetSize = CacheEntry + ".94";
        public const string SlidingIndexqueuesize = CacheEntry + ".95";
        public const string SocketRecvSize = CacheEntry + ".96";
        public const string SocketRecvtime = CacheEntry + ".97";
        public const string SocketSendSize = CacheEntry + ".98";
        public const string SocketSendTime = CacheEntry + ".99";
        public const string StateTransferPCache = CacheEntry + ".100";
        public const string TcpDownEnterPerSec = CacheEntry + ".101";
        public const string TcpDownQueueCount = CacheEntry + ".102";
        public const string TcpUpQueueCount = CacheEntry + ".103";
        public const string TopicsCount = CacheEntry + ".104";
        public const string WriteBehindBatchCount = CacheEntry + ".105";
        public const string WriteBehindEvictionsPerSec = CacheEntry + ".106";
        public const string WriteBehindFailureRetrycount = CacheEntry + ".107";
        public const string WriteBehindQueueCount = CacheEntry + ".108";
        public const string WriteBehindPerSec = CacheEntry + ".109";
        public const string WriteThruPerSec = CacheEntry + ".110";
        public const string TotalCacheSize = CacheEntry + ".111";
        public const string ConfiguredServersCount = CacheEntry + ".112";
        public const string AverageMsecPerAdd = CacheEntry + ".113";
        #endregion

        #region ______________Persistence Cache Counters_______________________
        public const string PersistenceQueueCount = CacheEntry + ".114";
        public const string PersistenceQueueSize = CacheEntry + ".115";
        public const string PersistenceReadOpPerSec = CacheEntry + ".116";
        public const string PersistenceWriteOpPerSec = CacheEntry + ".117";
        public const string PersistenceReadOpAvgTime = CacheEntry + ".118";
        public const string PersistenceWriteOpAvgTime = CacheEntry + ".119";
        public const string PersistenceLastReadTime = CacheEntry + ".120";
        public const string PersistenceLastWriteTime = CacheEntry + ".121";
        #endregion

        public static IDictionary<string, string> GetCacheOidMappings()
        {
            if (_counterMappings == null)
                PopulateCounterMappings();
            return _counterMappings;
        }

        private static void PopulateCounterMappings()
        {
            _counterMappings = new Dictionary<string, string>();
            _counterMappings.Add("metaData1", CacheName);
            _counterMappings.Add("metaData2", ConfigId);
            _counterMappings.Add("metaData3", ClientServerIP);
            _counterMappings.Add("metaData4", IsReplica);
            _counterMappings.Add("ncache_additions_per_sec", AdditionsPerSec);
            _counterMappings.Add("ncache_average_us_per_addbulk", AverageMsecPerAddbulk);
            _counterMappings.Add("ncache_average_us_per_cache_operation", AverageMsecPerCacheOperation);
            _counterMappings.Add("ncache_average_us_per_fetch", AverageMsecPerFetch);
            _counterMappings.Add("ncache_average_us_per_fetchbulk", AverageMsecPerFetchbulk);
            _counterMappings.Add("ncache_average_us_per_insert", AverageMsecPerInsert);
            _counterMappings.Add("ncache_average_us_per_insertbulk", AverageMsecPerInsertbulk);
            _counterMappings.Add("ncache_average_us_per_remove", AverageMsecPerRemove);
            _counterMappings.Add("ncache_average_us_per_removebulk", AverageMsecPerRemovebulk);
            _counterMappings.Add("ncache_cache_size", CacheSize);
            _counterMappings.Add("ncache_count", Count);
            _counterMappings.Add("ncache_deletes_per_sec", DeletesPerSec);
            _counterMappings.Add("ncache_message_store_size", MessageStoreSize);
            _counterMappings.Add("ncache_messages_count", MessagesCount);
            _counterMappings.Add("ncache_expiration_index_size", ExpirationIndexSize);
            _counterMappings.Add("ncache_expirations_per_sec", ExpirationsPerSec);
            _counterMappings.Add("ncache_fetches_per_sec", FetchesPerSec);
            _counterMappings.Add("ncache_requests_per_sec", RequestsPerSec);
            _counterMappings.Add("ncache_updates_per_sec", UpdatesPerSec);
            _counterMappings.Add("ncache_clients", NumberOfClients);
            _counterMappings.Add("ncache_bcast_queue_count", BcastQueueCount);
            _counterMappings.Add("ncache_bytes_received_per_sec", BytesReceivedPerSec);
            _counterMappings.Add("ncache_bytes_sent_per_sec", BytesSentPerSec);
            _counterMappings.Add("ncache_cache_last_access_count", CacheLastAccessCount);
            _counterMappings.Add("ncache_client_bytes_received_per_sec", ClientBytesReceivedPerSec);
            _counterMappings.Add("ncache_client_bytes_sent_per_sec", ClientBytesSentPerSec);
            _counterMappings.Add("ncache_client_requests_per_sec", ClientRequestsPerSec);
            _counterMappings.Add("ncache_client_responses_per_sec", ClientResponsesPerSec);
            _counterMappings.Add("ncache_cluster_ops_per_sec", ClusterOpsPerSec);
            _counterMappings.Add("ncache_clustered_opsrecv_per_sec", ClusteredOpsRecvPerSec);
            _counterMappings.Add("ncache_clustered_opssent_per_sec", ClusteredOpsSentPerSec);
            _counterMappings.Add("ncache_dispatch_enter_per_sec", DispatchEnterPerSec);
            _counterMappings.Add("ncache_event_queue_count", EventQueueCount);
            _counterMappings.Add("ncache_eviction_index_size", EvictionIndexSize);
            _counterMappings.Add("ncache_evictions_per_sec", EvictionsPerSec);
            _counterMappings.Add("ncache_general_notifications_queue_size", GeneralNotificationsQueueSize);
            _counterMappings.Add("ncache_hits_ratio_per_sec_percent", HitsRatioPerSec);
            _counterMappings.Add("ncache_hits_per_sec", HitsPerSec);
            _counterMappings.Add("ncache_logged_request_count", LoggedRequestCount);
            _counterMappings.Add("ncache_mcast_queue_count", McastQueueCount);
            _counterMappings.Add("ncache_messages_delivered_per_sec", MessagesDeliveredPerSec);
            _counterMappings.Add("ncache_messages_expired_per_sec", MessagesExpiredPerSec);
            _counterMappings.Add("ncache_messages_published_per_sec", MessagesPublishedPerSec);
            _counterMappings.Add("ncache_mirror_queue_size", MirrorQueueSize);
            _counterMappings.Add("ncache_misses_per_sec", MissesPerSec);
            _counterMappings.Add("ncache_nagling_msg_count", NaglingMsgCount);
            _counterMappings.Add("ncache_request_log_ledger_size", RequestLogLedgerSize);
            _counterMappings.Add("ncache_requests_logged_per_sec", RequestsLoggedPerSec);
            _counterMappings.Add("ncache_response_queue_count", ResponseQueueCount);
            _counterMappings.Add("ncache_response_queue_size", ResponseQueueSize);
            _counterMappings.Add("ncache_response_sent_per_sec", ResponseSentPerSec);
            _counterMappings.Add("ncache_responses_per_sec", ResponsesPerSec);
            _counterMappings.Add("ncache_sliding_index_queue_size", SlidingIndexqueuesize);
            _counterMappings.Add("ncache_socket_recv_size_bytes", SocketRecvSize);
            _counterMappings.Add("ncache_socket_recv_time_msec", SocketRecvtime);
            _counterMappings.Add("ncache_socket_send_size_bytes", SocketSendSize);
            _counterMappings.Add("ncache_socket_send_time_msec", SocketSendTime);
            _counterMappings.Add("ncache_state_transfer_per_sec", StateTransferPCache);
            _counterMappings.Add("ncache_tcpdown_enter_per_sec", TcpDownEnterPerSec);
            _counterMappings.Add("ncache_tcp_down_queue_count", TcpDownQueueCount);
            _counterMappings.Add("ncache_tcp_up_queue_count", TcpUpQueueCount);
            _counterMappings.Add("ncache_topics_count", TopicsCount);
            _counterMappings.Add("ncache_total_cache_size", TotalCacheSize);
            _counterMappings.Add("ncache_configured_servers_count", ConfiguredServersCount);
            _counterMappings.Add("ncache_average_us_per_add", AverageMsecPerAdd);
        }
    }
}