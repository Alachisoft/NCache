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
    public class ClientOids : ParentOids
    {
        public const string CpuUsage = Client + ".0";
        public const string MemoryUsage = Client + ".1";
        public const string NetworkUsage = Client + ".2";
        public const string RequestPerSec = Client + ".3";
        public const string AddsPerSec = Client + ".4";
        public const string GetsPerSec = Client + ".5";
        public const string InsertsPerSec = Client + ".6";
        public const string DelsPerSec = Client + ".7";
        public const string ReadOpsPerSec = Client + ".8";
        public const string WriteOpsPerSec = Client + ".9";
        public const string RequestQueueSize = Client + ".10";
    }
}
