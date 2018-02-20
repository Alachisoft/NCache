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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Config.Dom;
namespace Alachisoft.NCache.Config.NewDom
{
    [Serializable]
    public class CacheServerConfigSetting : ICloneable,ICompactSerializable
    {
        //bool autoStartCacheOnServiceStartup = false;       
        bool inproc;
        //string lastModified;
        /// <summary>
        /// This helps to differentiate between a local-cache, client-cache and clustered-cache
        /// </summary>
        string cacheType;
        Alachisoft.NCache.Config.Dom.Log log;
        Alachisoft.NCache.Config.Dom.PerfCounters perfCounters;
        Alachisoft.NCache.Config.Dom.QueryIndex indexes;
        Alachisoft.NCache.Config.Dom.BackingSource backingSource;
        Alachisoft.NCache.Config.Dom.Notifications notifications;
        Alachisoft.NCache.Config.Dom.Cleanup cleanup;
        Alachisoft.NCache.Config.Dom.Storage storage;
        Alachisoft.NCache.Config.Dom.EvictionPolicy evictionPolicy;
        Alachisoft.NCache.Config.Dom.ClientDeathDetection deathDetection;
        ClientActivityNotification clientActivityNotification;
        CacheTopology cacheTopology;
        Alachisoft.NCache.Config.Dom.ExpirationPolicy expirationPolicy;
        Alachisoft.NCache.Config.Dom.SQLDependencyConfig _sqlDependencyConfig;
        private Alachisoft.NCache.Config.Dom.SynchronizationStrategy _synchronizationStrategy;
        private DataFormat _dataFormat;
        private TaskConfiguration _taskConfiguration;

        public CacheServerConfigSetting()
        {
            log = new Alachisoft.NCache.Config.Dom.Log();
            perfCounters = new Alachisoft.NCache.Config.Dom.PerfCounters();
            cleanup = new Alachisoft.NCache.Config.Dom.Cleanup();
            notifications = new Alachisoft.NCache.Config.Dom.Notifications();
            _taskConfiguration = new TaskConfiguration();
            deathDetection = new ClientDeathDetection();
            clientActivityNotification = new ClientActivityNotification();
            expirationPolicy = new Alachisoft.NCache.Config.Dom.ExpirationPolicy();
        }

        [ConfigurationAttribute("inproc")]
        public bool InProc
        {
            get { return inproc; }
            set { inproc = value; }
        }
     
        public string CacheType
        {           
            get
            {
                string type = this.cacheTopology.Topology;
                switch (type)
                {
                    case "replicated":
                    case "partitioned":
                    case "partitioned-replica":
                    case "mirrored":
                        return "clustered-cache";
                    case "local-cache": return "local-cache";
                    case "client-cache": return "client-cache";
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
    
        [ConfigurationSection("client-death-detection")]
        public Alachisoft.NCache.Config.Dom.ClientDeathDetection ClientDeathDetection
        {
            get { return deathDetection; }
            set { deathDetection = value; }
        }

        [ConfigurationSection("client-activity-notification")]
        public ClientActivityNotification ClientActivityNotification
        {
            get { return clientActivityNotification; }
            set { clientActivityNotification = value; }
        }

        [ConfigurationSection("query-indexes")]

        public Alachisoft.NCache.Config.Dom.QueryIndex QueryIndices
        {
            get { return indexes; }
            set { indexes = value; }
        }


        [ConfigurationSection("backing-source")]

        public Alachisoft.NCache.Config.Dom.BackingSource BackingSource
        {
            get { return backingSource; }
            set { backingSource = value; }
        }

        [ConfigurationSection("cache-notifications")]
        public Alachisoft.NCache.Config.Dom.Notifications Notifications
        {
            get { return notifications; }
            set { notifications = value; }
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

        [ConfigurationSection("expiration-policy")]
        public Alachisoft.NCache.Config.Dom.ExpirationPolicy ExpirationPolicy
        {
            get { return expirationPolicy; }
            set { expirationPolicy = value; }
        }

        [ConfigurationSection("sql-dependency")]
        public Alachisoft.NCache.Config.Dom.SQLDependencyConfig SQLDependencyConfig
        {
            get { return _sqlDependencyConfig; }
            set { _sqlDependencyConfig = value; }
        }
        
        [ConfigurationSection("synchronization")]
        public SynchronizationStrategy SynchronizationStrategy
        {
            set { _synchronizationStrategy = value; }
            get { return _synchronizationStrategy; }
        }

        [ConfigurationSection("cache-topology", true, false)]
        public CacheTopology CacheTopology
        {
            get { return cacheTopology; }
            set { cacheTopology = value; }
        }

        [ConfigurationSection("tasks-config")]
        public TaskConfiguration TaskConfiguration
        {
            get { return _taskConfiguration; }
            set { _taskConfiguration = value; }
        }
    
        public string DataFormat
        {
            get { return _dataFormat.ToString(); }
            set
            {
                _dataFormat = Common.Enum.DataFormat.Binary;
            }
        }

        #region ICloneable Members

        public object Clone()
        {
            CacheServerConfigSetting config = new CacheServerConfigSetting();
            config.cacheType = this.cacheType; 
            config.InProc = InProc;         
            config.Log = Log != null ? (Alachisoft.NCache.Config.Dom.Log)Log.Clone() : null;
            config.PerfCounters = PerfCounters != null ? (Alachisoft.NCache.Config.Dom.PerfCounters)PerfCounters.Clone() : null;


            config.Cleanup = Cleanup != null ? (Alachisoft.NCache.Config.Dom.Cleanup)Cleanup.Clone() : null;
            config.Storage = Storage != null ? (Alachisoft.NCache.Config.Dom.Storage)Storage.Clone() : null;
            config.EvictionPolicy = EvictionPolicy != null ? (Alachisoft.NCache.Config.Dom.EvictionPolicy)EvictionPolicy.Clone() : null;
            config.ExpirationPolicy = ExpirationPolicy != null ? (Alachisoft.NCache.Config.Dom.ExpirationPolicy)ExpirationPolicy.Clone() : null;
            config.backingSource = this.backingSource != null ? (Alachisoft.NCache.Config.Dom.BackingSource)this.backingSource.Clone() : null;            
            config.QueryIndices = QueryIndices != null ? (Alachisoft.NCache.Config.Dom.QueryIndex)QueryIndices.Clone() : null;
            config.Notifications = Notifications != null ? (Alachisoft.NCache.Config.Dom.Notifications)Notifications.Clone() : null;
            config.SQLDependencyConfig = SQLDependencyConfig != null ? (Alachisoft.NCache.Config.Dom.SQLDependencyConfig)SQLDependencyConfig.Clone() : null;
            config.SynchronizationStrategy = SynchronizationStrategy != null ? (Alachisoft.NCache.Config.Dom.SynchronizationStrategy)SynchronizationStrategy.Clone() : null;
            config.cacheTopology = this.cacheTopology;
            config.DataFormat = this.DataFormat;
            config.ClientDeathDetection = ClientDeathDetection != null ? (ClientDeathDetection)ClientDeathDetection.Clone() : null;
            config.TaskConfiguration = TaskConfiguration != null ?(TaskConfiguration)TaskConfiguration.Clone() : null;
            config.ClientActivityNotification = ClientActivityNotification != null
                ? (ClientActivityNotification) ClientActivityNotification.Clone()
                : null;
            return config;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
          
            inproc = reader.ReadBoolean();
            cacheType = reader.ReadObject() as String;
            log = reader.ReadObject() as Log;
            perfCounters = reader.ReadObject() as Alachisoft.NCache.Config.Dom.PerfCounters;
            indexes = reader.ReadObject() as QueryIndex;
            backingSource = reader.ReadObject() as BackingSource;
            notifications = reader.ReadObject() as Notifications;
            cleanup = reader.ReadObject() as Cleanup;
            storage = reader.ReadObject() as Alachisoft.NCache.Config.Dom.Storage;
            evictionPolicy = reader.ReadObject() as EvictionPolicy;
            expirationPolicy = reader.ReadObject() as ExpirationPolicy;
            _sqlDependencyConfig = reader.ReadObject() as SQLDependencyConfig;
            _synchronizationStrategy = reader.ReadObject() as SynchronizationStrategy;
            cacheTopology = reader.ReadObject() as CacheTopology;
            _taskConfiguration = reader.ReadObject() as TaskConfiguration;

            string temp = reader.ReadObject() as String;
            _dataFormat = Common.Enum.DataFormat.Binary;
            deathDetection = reader.ReadObject() as ClientDeathDetection;
            clientActivityNotification = reader.ReadObject() as ClientActivityNotification;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(inproc);
            writer.WriteObject(cacheType);
            writer.WriteObject(log);
            writer.WriteObject(perfCounters);
            writer.WriteObject(indexes);
            writer.WriteObject(backingSource);
            writer.WriteObject(notifications);
            writer.WriteObject(cleanup);
            writer.WriteObject(storage);
            writer.WriteObject(evictionPolicy);
            writer.WriteObject(expirationPolicy);
            writer.WriteObject(_sqlDependencyConfig);
            writer.WriteObject(_synchronizationStrategy);
            writer.WriteObject(cacheTopology);
            writer.WriteObject(_taskConfiguration);
            writer.WriteObject(_dataFormat.ToString());
            writer.WriteObject(deathDetection);
            writer.WriteObject(clientActivityNotification);
        }

        #endregion
    }
}
