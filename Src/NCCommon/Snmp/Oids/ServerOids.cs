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
    public class ServerOids : ParentOids
    {
        public const string NodeName = Server + ".0";

        public const string RequestsPerSec = Server + ".50";
        public const string ResponsesPerSec = Server + ".51";
        public const string ClientBytesSentPerSecStats = Server + ".52";
        public const string ClientBytesRecievedPerSecStats = Server + ".53";
        public const string mSecPerCacheOperation = Server + ".54";
        public const string mSecPerCacheOperationBase = Server + ".55";
        public const string SystemCpuUsage = Server + ".56";
        public const string SystemFreeMemory = Server + ".57";
        public const string SystemMemoryUsage = Server + ".58";
        public const string VMCpuUsage = Server + ".59";
        public const string VMCommittedMemory = Server + ".60";
        public const string VMMaxMemory = Server + ".61";
        public const string VMMemroyUsage = Server + ".62";
        public const string VMNetworkUsage = Server + ".63";


    }
}