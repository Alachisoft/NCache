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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    [ConfigurationRoot("cache-config")]
    public class CacheServerConfig : ICloneable, ICompactSerializable
    {
        bool _underMaintainanced = false;
        bool cacheIsRunning = false;
        bool cacheIsRegistered = false;
        bool licenseIsExpired = false;
        string name;
        bool inproc;
        //int cachePort=0;
        string configID;
        double configVersion;
        string lastModified;
        double depversion;


        //int managementPort = 0;

        RtContextValue _runtimeContextValue;
        /// <summary>
        /// This helps to differentiate between a local-cache, client-cache and clustered-cache
        /// </summary>
        string cacheType;
        Log log;
        PerfCounters perfCounters;
        ClientDeathDetection deathDetection;
        BackingSource backingSource;
       
        Notifications notifications;
        Cleanup cleanup;
        Storage storage;
        EvictionPolicy evictionPolicy;
        Cluster cluster;
        ReplicationStrategy _replicationStrategy;
        Security security;
        AutoLoadBalancing autoBalancing;
        ClientNodes clientNodes;
        ClientActivityNotification clientActivityNotification;


        SQLDependencyConfig _sqlDependencyConfig;
        private ServerMapping _serverMapping; 
		private DataFormat _dataFormat = Common.Enum.DataFormat.Binary;
        SynchronizationStrategy _synchronizationStrategy;



        public CacheServerConfig() 

        {
            log = new Log();
            notifications = new Notifications();
            deathDetection = new ClientDeathDetection();
            clientActivityNotification = new ClientActivityNotification();
            autoBalancing = new AutoLoadBalancing();
            _synchronizationStrategy = new SynchronizationStrategy();
         

        }
        public bool IsUnderMaintainanced
        {
            get { return _underMaintainanced; }
            set { _underMaintainanced = value; }
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
                    isRunning = false;
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

        [ConfigurationAttribute("deployment-version")]
        public double DeploymentVersion
        {
            get { return depversion; }
            set { depversion = value; }
        }

        [ConfigurationAttribute("config-version")]
        public double ConfigVersion
        {
            get { return configVersion; }
            set { configVersion = value; }
        }

        [ConfigurationAttribute("config-id")]
        public string ConfigID
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
        

#if SERVER || CLIENT
        [ConfigurationSection("replication-strategy")]
#endif
        public ReplicationStrategy ReplicationStrategy
        {
            get { return _replicationStrategy; }
            set { _replicationStrategy = value; }
        }

#if SERVER
        [ConfigurationSection("data-load-balancing")]
#endif
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

        public ClientDeathDetection ClientDeathDetection
        {
            get { return deathDetection; }
            set { deathDetection = value; }
        }


   
        [ConfigurationSection("backing-source")]

        public BackingSource BackingSource
        {
            get { return backingSource; }
            set { backingSource = value; }
        }


       


        [ConfigurationSection("notifications")]

        public Notifications Notifications
        {
            get { return notifications; }
            set { notifications = value; }
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

        [ConfigurationSection("synchronization")]
        public SynchronizationStrategy SynchronizationStrategy
        {
            set { _synchronizationStrategy = value; }
            get { return _synchronizationStrategy; }
        }

        [ConfigurationSection("cluster")]
        public Cluster Cluster
        {
            get { return cluster; }
            set { cluster = value; }
        }


        [ConfigurationSection("security")]

        public Security Security
        {
            get { return security; }
            set { security = value; }
        }

        [ConfigurationSection("sql-dependency")]
        public SQLDependencyConfig SQLDependencyConfig
        {
            get { return _sqlDependencyConfig; }
            set { _sqlDependencyConfig = value; }
        }
        
        public string DataFormat
        {
            get { return _dataFormat.ToString(); }
            set
            {
                if (value.ToLower().Equals("object"))
                {
                    _dataFormat = Common.Enum.DataFormat.Object;
                }
                else
                {
                    _dataFormat = Common.Enum.DataFormat.Binary;
                }
            }
        }

        [ConfigurationSection("client-activity-notification")]
        public ClientActivityNotification ClientActivityNotification
        {
            get { return clientActivityNotification; }
            set { clientActivityNotification = value; }
        }


        #region ICloneable Members

        public object Clone()
        {
            CacheServerConfig config = new CacheServerConfig();
            config.Name = Name != null ? (string)Name.Clone() : null;
            config.cacheType = this.cacheType;
            config.DataFormat = this.DataFormat;
            config.InProc = InProc;
            //config.cachePort = CachePort;
            config.configID = configID;
            config.depversion = depversion;
            config.configVersion = configVersion;
            config.LastModified = LastModified != null ? (string)LastModified.Clone() : null;

            config.clientNodes = clientNodes != null ? clientNodes.Clone() as ClientNodes : null;
            config.Log = Log != null ? (Log)Log.Clone() : null;
            config.PerfCounters = PerfCounters != null ? (PerfCounters)PerfCounters.Clone() : null;


#if SERVER || CLIENT
            config.ReplicationStrategy = ReplicationStrategy != null ? (ReplicationStrategy)ReplicationStrategy.Clone() : null;
            config.autoBalancing = this.autoBalancing != null ? (AutoLoadBalancing)this.autoBalancing.Clone() : null;
            
#endif

            config.Cleanup = Cleanup != null ? (Cleanup)Cleanup.Clone() : null;
            config.Storage = Storage != null ? (Storage)Storage.Clone() : null;
            config.EvictionPolicy = EvictionPolicy != null ? (EvictionPolicy)EvictionPolicy.Clone() : null;
            
            config.Cluster = Cluster != null ? (Cluster)Cluster.Clone() : null;


            config.backingSource = this.backingSource != null ? (BackingSource)this.backingSource.Clone() : null;
            config.Security = Security != null ? (Security)Security.Clone() : null;
            config.Notifications = Notifications != null ? (Notifications)Notifications.Clone() : null;
            config.SQLDependencyConfig = SQLDependencyConfig != null ? (SQLDependencyConfig)SQLDependencyConfig.Clone() : null;
            
            config.ClientDeathDetection = ClientDeathDetection != null ? (ClientDeathDetection)ClientDeathDetection.Clone() : null;
            config.SynchronizationStrategy = SynchronizationStrategy != null ? (SynchronizationStrategy)SynchronizationStrategy.Clone() : null;

            config.ClientActivityNotification = ClientActivityNotification != null
                ? (ClientActivityNotification) ClientActivityNotification.Clone()
                : null;
            config.IsRegistered = this.IsRegistered;
            config.IsRunning = this.IsRunning;
            config.licenseIsExpired = this.licenseIsExpired;
            config.RuntimeContext = this.RuntimeContext;

            return config;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            cacheIsRunning = reader.ReadBoolean();
            cacheIsRegistered = reader.ReadBoolean();
            licenseIsExpired = reader.ReadBoolean();
            name = reader.ReadObject() as string;
            inproc = reader.ReadBoolean();
            configID = reader.ReadString();
            configVersion = reader.ReadDouble();
            lastModified = reader.ReadObject() as string;
            cacheType = reader.ReadObject() as string;
            log = reader.ReadObject() as Log;
            perfCounters = reader.ReadObject() as PerfCounters;
            backingSource = reader.ReadObject() as BackingSource;
            notifications = reader.ReadObject() as Notifications;
            cleanup = reader.ReadObject() as Cleanup;
            storage = reader.ReadObject() as Storage;
            evictionPolicy = reader.ReadObject() as EvictionPolicy;
            cluster = reader.ReadObject() as Cluster;
            _replicationStrategy = reader.ReadObject() as ReplicationStrategy;
            security = reader.ReadObject() as Security;
            autoBalancing = reader.ReadObject() as AutoLoadBalancing;
            clientNodes = reader.ReadObject() as ClientNodes;
            _sqlDependencyConfig = reader.ReadObject() as SQLDependencyConfig;
             deathDetection = reader.ReadObject() as ClientDeathDetection;
            _runtimeContextValue = reader.ReadObject() as string == "1" ? RtContextValue.JVCACHE : RtContextValue.NCACHE;
            _synchronizationStrategy = reader.ReadObject() as SynchronizationStrategy;
            string temp = reader.ReadObject() as String;
            if (temp.ToLower().Equals("binary"))
            { 
                _dataFormat = Common.Enum.DataFormat.Binary; 
            }
            else if (temp.ToLower().Equals("object"))
            {
                _dataFormat = Common.Enum.DataFormat.Object; 
            }
            clientActivityNotification = reader.ReadObject() as ClientActivityNotification;
            depversion =(double) reader.ReadObject();

        }
        
        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {

            writer.Write(cacheIsRunning);
            writer.Write(cacheIsRegistered);
            writer.Write(licenseIsExpired);
            writer.WriteObject(name);
            writer.Write(inproc);
            writer.Write(configID);
            writer.Write(configVersion);
            writer.WriteObject(lastModified);
            writer.WriteObject(cacheType);
            writer.WriteObject(log);
            writer.WriteObject(perfCounters);
            writer.WriteObject(backingSource);
           
            writer.WriteObject(notifications);
            writer.WriteObject(cleanup);
            writer.WriteObject(storage);
            writer.WriteObject(evictionPolicy);
            writer.WriteObject(cluster);
            writer.WriteObject(_replicationStrategy);
            writer.WriteObject(security);
            writer.WriteObject(autoBalancing);
            writer.WriteObject(clientNodes);
            writer.WriteObject(_sqlDependencyConfig);
            writer.WriteObject(deathDetection);
            writer.WriteObject(_runtimeContextValue == RtContextValue.JVCACHE ? "1" : "0");
            writer.WriteObject(_synchronizationStrategy);
            writer.WriteObject(_dataFormat.ToString());
            writer.WriteObject(clientActivityNotification);
            writer.WriteObject(depversion);

        }
        #endregion
    }
}
