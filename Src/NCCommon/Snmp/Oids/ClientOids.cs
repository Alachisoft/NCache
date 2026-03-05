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
    public class ClientOids : ParentOids
    {
        private static IDictionary<string, string> _counterMappings = null;

        public const string ClientIndex = ClientEntry + ".1";
        public const string CacheName = ClientEntry + ".2";
        public const string CacheConfigId = ClientEntry + ".3";
        public const string BindIP = ClientEntry + ".4";
        public const string FetchesPerSec = ClientEntry + ".5";
        public const string AdditionsPerSec = ClientEntry + ".6";
        public const string UpdatesPerSec = ClientEntry + ".7";
        public const string DeletesPerSec = ClientEntry + ".8";
        public const string ReadOperationsPerSec = ClientEntry + ".9";
        public const string WriteOperationsPerSec = ClientEntry + ".10";
        public const string AverageMSecPerFetch = ClientEntry + ".11";
        public const string AverageMSecPerAdd = ClientEntry + ".12";
        public const string AverageMSecPerInsert = ClientEntry + ".13";
        public const string AverageMSecPerRemove = ClientEntry + ".14";
        public const string RequestQueueSize = ClientEntry + ".15";
        public const string CompressionsPerSec = ClientEntry + ".16";
        public const string AverageItemSize = ClientEntry + ".17";
        public const string AverageMSecPerEvent = ClientEntry + ".18";
        public const string EventsProcessedPerSec = ClientEntry + ".19";
        public const string EventsTriggeredPerSec = ClientEntry + ".20";
        public const string AverageMSecPerCompression = ClientEntry + ".21";
        public const string AverageMSecPerDecompression = ClientEntry + ".22";
        public const string AverageMSecPerEncryption = ClientEntry + ".23";
        public const string AverageMSecPerDecryption = ClientEntry + ".24";
        public const string AverageCompressedItemSize = ClientEntry + ".25";
        public const string AverageMSecPerSerialization = ClientEntry + ".26";
        public const string AverageMSecPerDeserialization = ClientEntry + ".27";
        public const string AverageMSecPerAddbulk = ClientEntry + ".28";
        public const string AverageMSecPerFetchbulk = ClientEntry + ".29";
        public const string AverageMSecPerInsertbulk = ClientEntry + ".30";
        public const string AverageMSecPerRemovebulk = ClientEntry + ".31";
        public const string NumberOfLastSyncPollUpdates = ClientEntry + ".32";
        public const string NumberOfLastSyncPollRemoves = ClientEntry + ".33";
        public const string AverageMSecPerPublishedMessages = ClientEntry + ".34";
        public const string MessagesPublishedPerSec = ClientEntry + ".35";
        public const string MessagesDeliveredPerSec = ClientEntry + ".36";

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
            _counterMappings.Add("metaData2", CacheConfigId);
            _counterMappings.Add("metaData3", BindIP);
            _counterMappings.Add("ncache_client_fetches_per_sec", FetchesPerSec);
            _counterMappings.Add("ncache_client_additions_per_sec", AdditionsPerSec);
            _counterMappings.Add("ncache_client_updates_per_sec", UpdatesPerSec);
            _counterMappings.Add("ncache_client_deletes_per_sec", DeletesPerSec);
            _counterMappings.Add("ncache_client_average_us_per_fetch", AverageMSecPerFetch);
            _counterMappings.Add("ncache_client_average_us_per_add", AverageMSecPerAdd);
            _counterMappings.Add("ncache_client_average_us_per_insert", AverageMSecPerInsert);
            _counterMappings.Add("ncache_client_average_us_per_remove", AverageMSecPerRemove);
            _counterMappings.Add("ncache_client_request_queue_size", RequestQueueSize);
            _counterMappings.Add("ncache_client_average_item_size", AverageItemSize);
            _counterMappings.Add("ncache_client_average_us_per_event", AverageMSecPerEvent);
            _counterMappings.Add("ncache_client_events_processed_per_sec", EventsProcessedPerSec);
            _counterMappings.Add("ncache_client_events_triggered_per_sec", EventsTriggeredPerSec);
            _counterMappings.Add("ncache_client_average_us_per_serialization", AverageMSecPerSerialization);
            _counterMappings.Add("ncache_client_average_us_per_deserialization", AverageMSecPerDeserialization);
            _counterMappings.Add("ncache_client_average_us_per_addbulk", AverageMSecPerAddbulk);
            _counterMappings.Add("ncache_client_average_us_per_fetchbulk", AverageMSecPerFetchbulk);
            _counterMappings.Add("ncache_client_average_us_per_insertbulk", AverageMSecPerInsertbulk);
            _counterMappings.Add("ncache_client_average_us_per_removebulk", AverageMSecPerRemovebulk);
            _counterMappings.Add("ncache_client_of_last_sync_poll_updates", NumberOfLastSyncPollUpdates);
            _counterMappings.Add("ncache_client_of_last_sync_poll_removes", NumberOfLastSyncPollRemoves);
            _counterMappings.Add("ncache_client_average_us_per_published_messages", AverageMSecPerPublishedMessages);
            _counterMappings.Add("ncache_client_messages_published_per_sec", MessagesPublishedPerSec);
            _counterMappings.Add("ncache_client_messages_delivered_per_sec", MessagesDeliveredPerSec);
        }
    }   
}
