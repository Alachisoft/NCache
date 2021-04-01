//  Copyright (c) 2021 Alachisoft
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


namespace Alachisoft.NCache.Common.Snmp.Oids
{
    public class CacheOids : ParentOids
    {
        public const string NodeName = Cache + ".0";
        public const string Count = Cache + ".1";
        public const string CacheLastAccessCount = Cache + ".2";
        public const string AddsPerSec = Cache + ".3";
        public const string HitsPerSec = Cache + ".4";
        public const string InsertsPerSec = Cache + ".5";
        public const string MissPerSec = Cache + ".6";
        public const string GetsPerSec = Cache + ".7";
        public const string DelsPerSec = Cache + ".8";
        public const string mSecPerAdd = Cache + ".9";
        public const string mSecPerInsert = Cache + ".10";
        public const string mSecPerGet = Cache + ".11";
        public const string mSecPerDel = Cache + ".12";
        public const string HitsRatioSec = Cache + ".13";
        public const string ExpiryPerSec = Cache + ".14";
        public const string EvictionPerSec = Cache + ".15";
        public const string StateTransferPerSec = Cache + ".16";
        public const string DataBalPerSec = Cache + ".17";
        public const string mSecPerAddBase = Cache + ".18";
        public const string mSecPerInsertBase = Cache + ".19";
        public const string mSecPerGetBase = Cache + ".20";
        public const string mSecPerDelBase = Cache + ".21";
        public const string HitsRatioSecBase = Cache + ".22";
        public const string MirrorQueueSize = Cache + ".23";
        public const string ReadThruPerSec = Cache + ".24";
        public const string WriteThruPerSec = Cache + ".25";
        public const string CacheSize = Cache + ".26";
        public const string ClusterOpsPerSec = Cache + ".27";
        public const string WriteBehindPerSecond = Cache + ".28";
        public const string WriteBehindQueueCount = Cache + ".29";
        public const string msecPerBulkAdd = Cache + ".30";
        public const string msecPerBulkGet = Cache + ".31";
        public const string msecPerBulkUpdate = Cache + ".32";
        public const string msecPerBulkRemove = Cache + ".33";
        public const string RunningReader = Cache + ".34";
        public const string MessageCount = Cache + ".35";
        public const string TopicCount = Cache + ".36";
        public const string MessageStoreSize = Cache + ".37";

        public const string msecAvgReadThru = Cache + ".38";
        public const string msecAvgWriteBehind = Cache + ".39";
        public const string msecAvgWriteThru = Cache + ".40";

        public const string RequestsPerSec = Cache + ".50";
        public const string ResponsesPerSec = Cache + ".51";



        public const string ClientBytesSentPerSecStats = Cache + ".52";
        public const string ClientBytesRecievedPerSecStats = Cache + ".53";
        public const string mSecPerCacheOperation = Cache + ".54";
        public const string mSecPerCacheOperationBase = Cache + ".55";
        public const string SystemCpuUsage = Cache + ".56";
        public const string SystemFreeMemory = Cache + ".57";
        public const string SystemMemoryUsage = Cache + ".58";
        public const string VMCpuUsage = Cache + ".59";
        public const string VMCommittedMemory = Cache + ".60";
        public const string VMMaxMemory = Cache + ".61";
        public const string VMMemroyUsage = Cache + ".62";
        public const string VMNetworkUsage = Cache + ".63";
    }
}
