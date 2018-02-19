// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    [ConfigurationRoot("cache-config")]
    public class CacheServerConfig: ICloneable,ICompactSerializable
    {
        bool cacheIsRunning = false;
        bool cacheIsRegistered = false;
        bool licenseIsExpired = false;
        string name;
        bool inproc;
        double configID;
        string lastModified;
        RtContextValue _runtimeContextValue;
        /// <summary>
        /// This helps to differentiate between a local-cache, and clustered-cache
        /// </summary>
        string cacheType; 
        
        Log log;
        PerfCounters perfCounters;
        QueryIndex indexes;
        Cleanup cleanup;
        Storage storage;
        EvictionPolicy evictionPolicy;
        Cluster cluster;
        AutoLoadBalancing autoBalancing;
        ClientNodes clientNodes;
        ClientDeathDetection deathDetection;

        private ServerMapping _serverMapping;

        public CacheServerConfig() 
        {
            log = new Log();
            deathDetection = new ClientDeathDetection();
           
        }

        public bool IsRegistered
        {
            get { return cacheIsRegistered; }
            set { cacheIsRegistered = value; }
        }

        public RtContextValue RuntimeContext
        {
            get { return _runtimeContextValue; }
            set { _runtimeContextValue = value; }
        }

       

        public bool IsRunning
        {
            get 
            {
                bool isRunning = cacheIsRunning;

                if (this.CacheType == "clustered-cache")
                {
                    foreach (StatusInfo cacheStatus in cluster.Nodes.Values)
                    {
                        if (cacheStatus.Status == CacheStatus.Running)
                        {
                            isRunning = true;
                            break;
                        }
                    }
                }

                return isRunning; 
            }

            set 
            {
                if (this.CacheType == "local-cache")
                    cacheIsRunning = value;
            }
        }

        public bool IsExpired
        {
            get { return licenseIsExpired; }
            set { licenseIsExpired = value; }
        }

        [ConfigurationAttribute("name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ConfigurationAttribute("inproc")]
        public bool InProc
        {
            get { return inproc; }
            set { inproc = value; }
        }

        [ConfigurationAttribute("config-id")]
        public double ConfigID
        {
            get { return configID; }
            set { configID = value; }
        }

        [ConfigurationAttribute("last-modified")]
        public string LastModified
        {
            get { return lastModified; }
            set { lastModified = value; }
        }

        [ConfigurationAttribute("type")]
        public string CacheType
        {
            get 
            {
                string type = this.cacheType;
                if (type == null)
                {
                    type = "local-cache";
                    if (this.cluster != null)
                    {
                        type = "clustered-cache";
                    }
                }
                return type;
            }
            set { cacheType = value; }
        }
         
        [ConfigurationSection("log")]
        public Log Log
        {
            get { return log; }
            set { log = value; }
        }

        [ConfigurationSection("perf-counters")]
        public PerfCounters PerfCounters
        {
            get { return perfCounters; }
            set { perfCounters = value; }
        }

        [ConfigurationSection("data-load-balancing")]
        public AutoLoadBalancing AutoLoadBalancing
        {
            get { return autoBalancing; }
            set { autoBalancing = value; }
        }

        [ConfigurationSection("client-nodes")]
        public ClientNodes ClientNodes
        {
            get { return clientNodes; }
            set { clientNodes = value; }
        }

        [ConfigurationSection("indexes")]  
        public QueryIndex QueryIndices
        {
            get { return indexes; }
            set { indexes = value; }
        }


        [ConfigurationSection("cleanup")]
        public Cleanup Cleanup
        {
            get { return cleanup; }
            set { cleanup = value; }
        }

        [ConfigurationSection("storage")]
        public Storage Storage
        {
            get { return storage; }
            set { storage = value; }
        }

        [ConfigurationSection("eviction-policy")]
        public EvictionPolicy EvictionPolicy
        {
            get { return evictionPolicy; }
            set { evictionPolicy = value; }
        }

        [ConfigurationSection("cluster")]
        public Cluster Cluster
        {
            get { return cluster; }
            set { cluster = value; }
        }

        [ConfigurationSection("client-death-detection")]
        public ClientDeathDetection ClientDeathDetection
        {
            get { return deathDetection; }
            set { deathDetection = value; }
        }


        #region ICloneable Members

        public object Clone()
        {
            CacheServerConfig config = new CacheServerConfig();
            config.Name = Name != null ? (string) Name.Clone(): null;
            config.cacheType = this.cacheType;
            config.InProc = InProc;
            config.ConfigID = ConfigID;
            config.LastModified = LastModified != null ? (string) LastModified.Clone() : null;
            
            config.clientNodes = clientNodes != null ? clientNodes.Clone() as ClientNodes : null;
            config.Log = Log != null ? (Log) Log.Clone(): null;
            config.PerfCounters = PerfCounters != null ? (PerfCounters) PerfCounters.Clone(): null;
            config.autoBalancing = this.autoBalancing != null ? (AutoLoadBalancing)this.autoBalancing.Clone() : null; 
            config.Cleanup = Cleanup != null ? (Cleanup)Cleanup.Clone() : null;
            config.Storage = Storage != null ? (Storage)Storage.Clone() : null;
            config.EvictionPolicy = EvictionPolicy != null ? (EvictionPolicy)EvictionPolicy.Clone() : null;
            config.Cluster = Cluster != null ? (Cluster)Cluster.Clone() : null;
            config.QueryIndices = QueryIndices != null ? (QueryIndex)QueryIndices.Clone() : null;
            config.IsRegistered = this.IsRegistered;
            config.IsRunning = this.IsRunning;
            config.licenseIsExpired = this.licenseIsExpired;
            config.RuntimeContext = this.RuntimeContext;
            config.ClientDeathDetection = this.deathDetection;
            return config;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {           
            cacheIsRunning=  reader.ReadBoolean();
            cacheIsRegistered = reader.ReadBoolean();
            licenseIsExpired = reader.ReadBoolean();
            name = reader.ReadObject() as string;
            inproc = reader.ReadBoolean();
            configID = reader.ReadDouble();
            lastModified = reader.ReadObject() as string;
            cacheType = reader.ReadObject() as string;
            log = reader.ReadObject() as Log;
            perfCounters = reader.ReadObject() as PerfCounters;
            autoBalancing = reader.ReadObject() as AutoLoadBalancing;
            indexes = reader.ReadObject() as QueryIndex;
            cleanup = reader.ReadObject() as Cleanup;
            storage = reader.ReadObject() as Storage;
            evictionPolicy = reader.ReadObject() as EvictionPolicy;
            cluster = reader.ReadObject() as Cluster;            
            clientNodes = reader.ReadObject() as ClientNodes;
            deathDetection = reader.ReadObject() as ClientDeathDetection;
            _runtimeContextValue = reader.ReadObject() as string == "1" ? RtContextValue.JVCACHE : RtContextValue.NCACHE;

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
           
            writer.Write(cacheIsRunning);
            writer.Write(cacheIsRegistered);
            writer.Write(licenseIsExpired);
            writer.WriteObject(name);
            writer.Write(inproc);
            writer.Write(configID);
            writer.WriteObject(lastModified);
            writer.WriteObject(cacheType);
            writer.WriteObject(log);
            writer.WriteObject(perfCounters);
            writer.WriteObject(autoBalancing);
            writer.WriteObject(indexes);
            writer.WriteObject(cleanup);
            writer.WriteObject(storage);
            writer.WriteObject(evictionPolicy);
            writer.WriteObject(cluster);
            writer.WriteObject(clientNodes);
            writer.WriteObject(deathDetection);
            writer.WriteObject(_runtimeContextValue == RtContextValue.JVCACHE ? "1" : "0");
        }

        #endregion
    }
}
