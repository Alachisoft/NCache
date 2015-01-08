// Copyright (c) 2015 Alachisoft
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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Config.Dom;
using Runtime = Alachisoft.NCache.Runtime;
namespace Alachisoft.NCache.Config.NewDom
{
    [Serializable]
    public class CacheServerConfigSetting : ICloneable,ICompactSerializable
    {
        string name;
        bool inproc;
        string lastModified;

        /// <summary>
        /// This helps to differentiate between a local-cache, clustered-cache
        /// </summary>
        string cacheType;

        Alachisoft.NCache.Config.Dom.Log log;
        Alachisoft.NCache.Config.Dom.PerfCounters perfCounters;
        Alachisoft.NCache.Config.Dom.QueryIndex indexes;

        Alachisoft.NCache.Config.Dom.Cleanup cleanup;
        Alachisoft.NCache.Config.Dom.Storage storage;
        Alachisoft.NCache.Config.Dom.EvictionPolicy evictionPolicy;
        Alachisoft.NCache.Config.Dom.AutoLoadBalancing autoBalancing;
        CacheTopology cacheTopology;
        string _alias = string.Empty;
        public CacheServerConfigSetting()
        {
            log = new Alachisoft.NCache.Config.Dom.Log();
        }

        [ConfigurationAttribute("cache-name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ConfigurationAttribute("alias")]
        public string Alias
        {
            get { return _alias; }
            set { _alias = value; }
        }

        [ConfigurationAttribute("inproc")]
        public bool InProc
        {
            get { return inproc; }
            set { inproc = value; }
        }

        [ConfigurationAttribute("last-modified")]
        public string LastModified
        {
            get { return lastModified; }
            set { lastModified = value; }
        }

        public string CacheType
        {
            ///[Ata]Type is part of 3.8 config. This is to be uncommented
            ///after development is complete.
            get
            {
                string type = this.cacheTopology.Topology;
                switch (type)
                {
                    case "partitioned":
                    case "replicated":
                        return "clustered-cache";
                    case "local-cache": 
                        return "local-cache";
                }

                return type;
            }
            set { cacheType = value; }
        }

        [ConfigurationSection("logging")]
        public Alachisoft.NCache.Config.Dom.Log Log
        {
            get { return log; }
            set { log = value; }
        }

        [ConfigurationSection("performance-counters")]
        public Alachisoft.NCache.Config.Dom.PerfCounters PerfCounters
        {
            get { return perfCounters; }
            set { perfCounters = value; }
        }
        [ConfigurationSection("data-load-balancing")]
        public Alachisoft.NCache.Config.Dom.AutoLoadBalancing AutoLoadBalancing
        {
            get { return autoBalancing; }
            set { autoBalancing = value; }
        }
        
        [ConfigurationSection("query-indexes")]
        public Alachisoft.NCache.Config.Dom.QueryIndex QueryIndices
        {
            get { return indexes; }
            set { indexes = value; }
        }





        [ConfigurationSection("cleanup")]
        public Alachisoft.NCache.Config.Dom.Cleanup Cleanup
        {
            get { return cleanup; }
            set { cleanup = value; }
        }

        [ConfigurationSection("storage", true, false)]
        public Alachisoft.NCache.Config.Dom.Storage Storage
        {
            get { return storage; }
            set { storage = value; }
        }

        [ConfigurationSection("eviction-policy")]
        public Alachisoft.NCache.Config.Dom.EvictionPolicy EvictionPolicy
        {
            get { return evictionPolicy; }
            set { evictionPolicy = value; }
        }

       

        [ConfigurationSection("cache-topology", true, false)]
        public CacheTopology CacheTopology
        {
            get { return cacheTopology; }
            set { cacheTopology = value; }
        }

        public string UniqueId
        {
            get
            {
                if (string.IsNullOrEmpty(_alias))
                    return name;
                return name + "[" + _alias + "]";
            }
         
        }

        #region ICloneable Members

        public object Clone()
        {
            CacheServerConfigSetting config = new CacheServerConfigSetting();
            config.Name = Name != null ? (string)Name.Clone() : null;
            config.cacheType = this.cacheType;
            config.InProc = InProc;
            config.Alias = Alias;            
            config.LastModified = LastModified != null ? (string)LastModified.Clone() : null;            
            config.Log = Log != null ? (Alachisoft.NCache.Config.Dom.Log)Log.Clone() : null;
            config.PerfCounters = PerfCounters != null ? (Alachisoft.NCache.Config.Dom.PerfCounters)PerfCounters.Clone() : null;
            config.autoBalancing = this.autoBalancing != null ? (Alachisoft.NCache.Config.Dom.AutoLoadBalancing)this.autoBalancing.Clone() : null;
            config.Cleanup = Cleanup != null ? (Alachisoft.NCache.Config.Dom.Cleanup)Cleanup.Clone() : null;
            config.Storage = Storage != null ? (Alachisoft.NCache.Config.Dom.Storage)Storage.Clone() : null;
            config.EvictionPolicy = EvictionPolicy != null ? (Alachisoft.NCache.Config.Dom.EvictionPolicy)EvictionPolicy.Clone() : null;
            config.QueryIndices = QueryIndices != null ? (Alachisoft.NCache.Config.Dom.QueryIndex)QueryIndices.Clone() : null;
            config.cacheTopology = this.cacheTopology;

            return config;
        }

        #endregion

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
        name =reader.ReadObject() as String;
        inproc = reader.ReadBoolean();
        lastModified = reader.ReadObject()as String;
        cacheType = reader.ReadObject()as String;
        log = reader.ReadObject() as Log;
        perfCounters = reader.ReadObject() as Alachisoft.NCache.Config.Dom.PerfCounters;
        autoBalancing = reader.ReadObject() as AutoLoadBalancing;
        indexes = reader.ReadObject() as QueryIndex;
        storage = reader.ReadObject() as Alachisoft.NCache.Config.Dom.Storage;
        evictionPolicy = reader.ReadObject() as EvictionPolicy;
        cacheTopology = reader.ReadObject() as CacheTopology;
        _alias = reader.ReadObject() as String;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(name);
            writer.Write(inproc);
            writer.WriteObject(lastModified);
            writer.WriteObject(cacheType);
            writer.WriteObject(log);
            writer.WriteObject(perfCounters);
            writer.WriteObject(autoBalancing);
            writer.WriteObject(indexes);
            writer.WriteObject(cleanup);
            writer.WriteObject(storage);
            writer.WriteObject(evictionPolicy);
            writer.WriteObject(cacheTopology);
            writer.WriteObject(_alias);
        }
        #endregion
    }
}
