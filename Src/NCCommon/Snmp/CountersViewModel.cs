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
using System;
using Lextm.SharpSnmpLib;
using Alachisoft.NCache.Common.Snmp.Oids;

namespace Alachisoft.NCache.Common.Snmp
{
    public class CountersViewModel
    {
        private Variable variable;
        private readonly string CounterOid;
        private string counterTitle;
        public readonly bool isServerCounter;
        public string RequestData;
        private string data;
        [System.ComponentModel.DisplayName("NCache")]
        public string Counter
        {
            get
            {
                return this.counterTitle;
            }
        }

        public string Data()
        {
            if (RequestData == "Null")
                return "N/A";
            else
                return RequestData;
        }

        [System.ComponentModel.DisplayName("NCache")]
        public string Value
        {
            get
            {
                string data = this.data;


                if (data != "Null")
                {
                    if (data == "0.0")
                    {
                        data = "0.000";
                    }
                    else if (data.Contains("."))
                    {
                        string[] Parts = data.Split('.');
                        if (Parts[1].Length > 3)
                        {
                            Parts[1] = Parts[1].Substring(0, 3);

                        }
                        else if (Parts[1].Length < 3)
                        {
                            for (int i = 0; i <= (3 - Parts[1].Length); i++)
                            {
                                Parts[1] = Parts[1] + "0";
                            }

                        }
                        data = Parts[0] + "." + Parts[1];
                    }
                }

                RequestData = data;
                if (this.isServerCounter)
                {
                    return "---";
                }
                else
                    return data.Equals("Null") ? "N/A" : data;

            }

        }

        string valueOfReplica;

        [System.ComponentModel.DisplayName("Rep-Value")]
        public string ValueOfReplica
        {
            get { return valueOfReplica; }
            set{valueOfReplica = value;}
        }

        string serverCounters;
        [System.ComponentModel.DisplayName("NCache Server")]
        public string ServerCounters
        {
            get { return serverCounters; }
            set { serverCounters = value; }
        }

        internal CountersViewModel(Variable variable)
        {
            this.variable = variable;
            this.CounterOid = variable.Id.ToString();
            this.data = this.variable.Data.ToString();
            this.counterTitle = this.IntializeCounterTitle();
            this.isServerCounter = false;
        }
        public CountersViewModel(string oid, string data)
        {
            this.CounterOid = String.Format(".{0}.0", oid);
            this.data = data;
            this.counterTitle = this.IntializeCounterTitle();
            this.isServerCounter = false;
        }
        private string IntializeCounterTitle()
        {
            if (this.CounterOid.StartsWith(String.Format(".{0}", ParentOids.Cache)))
            {
                if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.NodeName))) { return "Node Name"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.Count))) { return "Count"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.CacheLastAccessCount))) { return "CacheLastAccesCount"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.AddsPerSec))) { return "Additions/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.HitsPerSec))) { return "Hits/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.InsertsPerSec))) { return "Updates/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.MissPerSec))) { return "Misses/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.GetsPerSec))) { return "Fetches/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.DelsPerSec))) { return "Deletes/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.mSecPerAdd))) { return "Average us/add"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.mSecPerInsert))) { return "Average us/insert"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.mSecPerGet))) { return "Average us/fetch"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.mSecPerDel))) { return "Average us/remove"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.HitsRatioSec))) { return "Hits ratio/sec(%)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.ExpiryPerSec))) { return "Expirations/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.EvictionPerSec))) { return "Evictions/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.StateTransferPerSec))) { return "State Transfer/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.DataBalPerSec))) { return "Data balance/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.ReadThruPerSec))) { return "Read-thru/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.WriteThruPerSec))) { return "Write-thru/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.WriteBehindPerSecond))) { return "Write-behind/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.WriteBehindQueueCount))) { return "Write-behind queue count"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.MirrorQueueSize))) { return "Mirror queue size"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.RequestsPerSec))) { return "Requests/sec (Server)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.ResponsesPerSec))) { return "Responses/sec (Server)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.ClientBytesSentPerSecStats))) { return "Client Bytes Sent/sec (Server)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.ClientBytesRecievedPerSecStats))) { return "Client Bytes Receive/sec (Server)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.ClusterOpsPerSec))) { return "Cluster ops/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.mSecPerCacheOperation))) { return "Average us/cache operation "; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.SystemCpuUsage))) { return "Total CPU Usage (Server)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.SystemFreeMemory))) { return "Total Free Physical Memory (Server)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.SystemMemoryUsage))) { return "Total Memory Usage"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.VMCpuUsage))) { return "TayzGrid CPU Usage (Server)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.VMCommittedMemory))) { return "TayzGrid Available Memory (Server)"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.VMMaxMemory))) { return "TayzGrid Max Memory"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", CacheOids.VMMemroyUsage))) { return "TayzGrid Memory Usage"; }

            }
            else
            {
                if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.NodeName))) { return "Node Name"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.RequestsPerSec))) { return "Requests/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.ResponsesPerSec))) { return "Responses/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.ClientBytesSentPerSecStats))) { return "Client Bytes Sent/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.ClientBytesRecievedPerSecStats))) { return "Client Bytes Receive/sec"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.mSecPerCacheOperation))) { return "Average us/cache operation "; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.SystemCpuUsage))) { return "Total Cpu Usage"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.SystemFreeMemory))) { return "Total Free Physical Memory"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.SystemMemoryUsage))) { return "Total Memory Usage"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.VMCpuUsage))) { return "TayzGrid Cpu Usage"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.VMCommittedMemory))) { return "TayzGrid JVM Available Memory"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.VMMaxMemory))) { return "TayzGrid JVM Max Memory"; }

                else if (this.CounterOid.Equals(String.Format(".{0}.0", ServerOids.SystemMemoryUsage))) { return "TayzGrid JVM Memory Usage"; }
            }
            return String.Empty;
        }
    }
}
