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
using System.Collections;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Config.Dom;
using System.Globalization;
using System.Threading;

namespace Alachisoft.NCache.Management
{
    /// <summary>
    /// Inernal class used to contain cofiguration data.
    /// </summary>
    public class CacheConfig
    {
        /// <summary> Type of the cache. i.e mirror-server, replicated-server, partitioned-server, partitioned-replica-server, local-cache.</summary>
        private string _cacheType = "";
        /// <summary> ID of the cache. </summary>
        private string _cacheId = "";
        /// <summary> ID of the partition this cache belongs to. </summary>
        private string _paritionId = "";
        /// <summary> Flag that indicates if we are to use the cache as inproc. </summary>
        private bool _useInProc;
        /// <summary> Server name of the machine hosting NCache service. </summary>
        private string _serverName = Environment.MachineName;
        /// <summary> Use TCP channel for communication. </summary>
        private bool _useTcp = true;
        /// <summary> TCP channel port. </summary>
        private long _port;
        /// <summary> Property string of the cache. </summary>
        private string _propertyString = "";
        ///<summary>Regisered DataSharing types.</summary>
        private Hashtable _dataSharingKnownTypes; 
        ///<summary>Regisered Backing Source.</summary>
        private Hashtable _backingSource;
        ///<summary>Regisered compact types.</summary>
        private Hashtable _cmptKnownTypes;
        ///<summary>Cluster port.</summary>
        private int _clusterPort;
        ///<summary>Cluster port.</summary>
        private int _managementPort;
        ///<summary>Cluster port.</summary>
        private int _socketPort;
        ///<summary>Cluster port range.</summary>
        private int _clusterPortRange;
        /// <summary>Fatal and error logs.</summary>
        private bool _errorLogsEnabled;
        /// <summary>info, debug and warning logs.</summary>
        private bool _detailedLogsEnabled;
        private long _cacheMaxSize;
        private long _cleanInterval;
        private float _evictRatio;
        private bool _compressionEnabled;
        private bool _securityEnabled;
        private string _securityDomainController;
        private Hashtable _securityUsers;
        private long _compressionThreshold;
        /// <summary>list of all the servers participating in a clustered cache.</summary>
        private ArrayList _servers;
    
        private static readonly string NET_TYPE = "net";
        private static readonly string NET_TYPE_WITH_COLON = ":net";
        private static readonly string JAVA_TYPE = "java";        
        private static readonly string JAVA_TYPE_WITH_COLON = ":java";
        private static string PLATFORM_TYPE = string.Empty;
        
        
        public ArrayList Servers
        {
            get { return _servers; }
        }

        public bool CompressionEnabled
        {
            get { return _compressionEnabled; }
        }

        public long CompressionThreshold
        {
            get { return _compressionThreshold; }
        }

        public bool SecurityEnabled
        {
            get { return _securityEnabled; }
        }

        public string SecurityDomainController
        {
            get { return _securityDomainController; }
        }

        public Hashtable SecurityUsers
        {
            get { return _securityUsers; }
        }

        public long CacheMaxSize
        {
            get { return _cacheMaxSize; }
        }

        public long CleanInterval
        {
            get { return _cleanInterval; }
        }

        public float EvictRatio
        {
            get { return _evictRatio; }
        }

        /// <summary> Type of the cache. i.e mirror-server, replicated-server, partitioned-server, partitioned-replica-server, local-cache.</summary>
        public string CacheType
        {
            get { return _cacheType; }
            set { _cacheType = value; }
        }

        /// <summary> ID of the cache. </summary>
        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        /// <summary> ID of the partition this cache belongs to. </summary>
        public string PartitionId
        {
            get { return _paritionId; }
            set { _paritionId = value; }
        }

        /// <summary> Flag that indicates if we are to use the cache as inproc. </summary>
        public bool UseInProc
        {
            get { return _useInProc; }
            set { _useInProc = value; }
        }

        /// <summary> Server name of the machine hosting NCache service. </summary>
        public string ServerName
        {
            get { return _serverName; }
            set { _serverName = value; }
        }

        /// <summary> Use TCP channel for communication. </summary>
        public bool UseTcp
        {
            get { return _useTcp; }
            set { _useTcp = value; }
        }

        /// <summary> TCP channel port. </summary>
        public long Port
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary> Property string of the cache. </summary>
        public string PropertyString
        {
            get { return _propertyString; }
            set { _propertyString = value; }
        }

        /// <summary> Registered DataSharing known types. </summary>
        public Hashtable DataSharingKnownTypes
        {
            get { return _dataSharingKnownTypes; }
            set { _dataSharingKnownTypes = value; }
        }

        /// <summary> Registered compact known types. </summary>
        public Hashtable CompactKnownTypes
        {
            get { return _cmptKnownTypes; }
            set { _cmptKnownTypes = value; }
        }
        /// <summary>
        /// Registered Backing Source.
        /// </summary>
        public Hashtable BackingSource
        {
            get { return _backingSource; }
            set { _backingSource = value; }
        }
        public bool IsUdpCluster
        {
            get
            {
                if (PropertyString.IndexOf("cluster") > 0)
                    if (PropertyString.IndexOf("udp") > 0)
                        return true;
                return false;
            }
        }

        /// <summary> Cluster port of the ndoe. </summary>
        public int ClusterPort
        {
            get { return _clusterPort; }
            set { _clusterPort = value; }
        }

        /// <summary> Management port of the node. </summary>
        public int ManagementPort
        {
            get { return _managementPort; }
            set { _managementPort = value; }
        }

        /// <summary> Client/Socket port of the node. </summary>
        public int SocketPort
        {
            get { return _socketPort; }
            set { _socketPort = value; }
        }

        /// <summary> Cluster port range. </summary>
        public int ClusterPortRange
        {
            get { return _clusterPortRange; }
            set { _clusterPortRange = value; }
        }

        public bool IsErrorLogsEnabled
        {
            get { return _errorLogsEnabled; }
            set { _errorLogsEnabled = value; }
        }

        public bool IsDetailedLogsEnabled
        {
            get { return _detailedLogsEnabled; }
            set { _detailedLogsEnabled = value; }
        }

        /// <summary> 
        /// Constructor
        /// </summary>
        public CacheConfig()
        {
            _port = CacheConfigManager.NCacheTcpPort;
            this._clusterPortRange = 2;
        }

        public CacheConfig(long tcpPort)
        {
            _port = tcpPort;
        }

        /// <summary>
        /// Populates the object from specified configuration object.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static CacheConfig FromDom(CacheServerConfig config)
        {
            Hashtable props = ConfigConverter.ToHashtable(config);
            return FromProperties(props);
        }
        
        /// <summary>
        /// Populates the object from specified configuration object.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        internal static CacheConfig FromConfiguration(CacheServerConfig configuration)
        {            
            CacheConfig cConfig = null;
            if (configuration != null)
            {
                cConfig = new CacheConfig();

                cConfig._useInProc = configuration.InProc;
                cConfig.CacheId = configuration.Name;
                
                if (configuration.Security != null)
                {
                    cConfig._securityEnabled = configuration.Security.Enabled;
                    cConfig._securityDomainController = configuration.Security.DomainController;

                    Hashtable users = new Hashtable();
                    if (configuration.Security.Users != null)
                    {
                        for (int i = 0; i < configuration.Security.Users.Length; i++)
                        {
                            users.Add(configuration.Security.Users[i].Id, null);
                        }
                    }
                    cConfig._securityUsers = users;
                }

                if (configuration.Cluster != null)
                {
                    if (configuration.Cluster.Channel != null)
                    {
                        cConfig._clusterPort = configuration.Cluster.Channel.TcpPort;
                        cConfig._clusterPortRange = configuration.Cluster.Channel.PortRange;
                    }
                    cConfig._servers = FromHostListToServers(configuration.Cluster.Channel.InitialHosts);

                    string topology = string.Empty;
                    switch (configuration.Cluster.Topology)
                    {
                        case "replicated": topology = "replicated-server"; break;
                        case "partitioned": topology = "partitioned-server"; break;
                        case "partition-replica": topology = "partitioned-replicas-server"; break;
                        case "mirror": topology = "mirror-server"; break;
                    }
                    cConfig._cacheType = topology;
                }
                else
                {
                    cConfig._cacheType = "local-cache";
                }

                if (configuration.Cleanup != null)
                {
                    cConfig._cleanInterval = configuration.Cleanup.Interval * 1000; ///to millisec
                }

                if (configuration.EvictionPolicy != null)
                {
                    cConfig._evictRatio = (float)Decimal.ToDouble(configuration.EvictionPolicy.EvictionRatio);
                }

                if (configuration.Storage != null)
                {
                    cConfig._cacheMaxSize = configuration.Storage.Size * 1048576; ///from mb to bytes
                }

                if (configuration.Log != null)
                {
                    cConfig._errorLogsEnabled = configuration.Log.TraceErrors;
                    cConfig._detailedLogsEnabled = configuration.Log.TraceDebug;
                }

                if (configuration.BackingSource != null)
                {
                    Hashtable settings = new Hashtable();
                    settings.Add("backing-source", GetBackingSource(configuration.BackingSource));
                    cConfig._backingSource = settings;
                }
            }
            return cConfig;
        }
        private static Hashtable GetCompactClass(CompactClass cls)
        {
            Hashtable settings = new Hashtable();
            settings.Add("id", cls.ID);
            settings.Add("name", cls.Name);
            settings.Add("assembly", cls.Assembly);
            settings.Add("portable", cls.Portable);
            settings.Add("type", cls.Type);
            settings.Add("generic-id", cls.GenericId);
            settings.Add("is-generic", cls.IsGeneric);
            settings.Add("num-of-args", cls.NumberOfArgs);
            Hashtable argTypes = null;
            if (cls.IsGeneric && cls.GenericArgumentTypeList != null)
            {
                argTypes = new Hashtable();
                foreach (GenericArgumentType gat in cls.GenericArgumentTypeList)
                    argTypes.Add(gat.ID, GetArgumentType(gat));
                settings.Add("arg-types", argTypes);
            }
            return settings;
        }

        private static Hashtable GetArgumentType(GenericArgumentType gat)
        {
            Hashtable concreteArgTypes = new Hashtable(gat.GenericArgsCompactTypeList.Count);
            Hashtable concreteArgType = null;
            if (gat.GenericArgsCompactTypeList != null)
                foreach (CompactClass cls in gat.GenericArgsCompactTypeList)
                {
                    concreteArgType = new Hashtable();
                    concreteArgType.Add("id", cls.ID);
                    concreteArgType.Add("name", cls.Name);
                    concreteArgType.Add("assembly", cls.Assembly);
                    concreteArgType.Add("portable", cls.Portable);
                    concreteArgType.Add("type", cls.Type);
                    concreteArgType.Add("generic-id", cls.GenericId);
                    concreteArgType.Add("is-generic", cls.IsGeneric);
                    concreteArgType.Add("num-of-args", cls.NumberOfArgs);
                    Hashtable argTypes = null;
                    if (cls.IsGeneric && cls.GenericArgumentTypeList != null)
                    {
                        argTypes = new Hashtable();
                        foreach (GenericArgumentType gat2 in cls.GenericArgumentTypeList)
                            argTypes.Add(gat2.ID, GetArgumentType(gat2));
                        concreteArgType.Add("arg-types", argTypes);
                    }
                    concreteArgTypes.Add(cls.ID, concreteArgType);
                }
            return concreteArgTypes;
        }
        
        private static Hashtable GetBackingSource(BackingSource backingSource)
        {
            Hashtable settings = new Hashtable();
            if (backingSource.Readthru != null)
                settings.Add("read-thru", GetReadThru(backingSource.Readthru));
            if (backingSource.Writethru != null)
                settings.Add("write-thru", GetWriteThru(backingSource.Writethru));
            return settings;
        }
        private static Hashtable GetWriteThru(Writethru writethru)
        {
            Hashtable settings = new Hashtable();
            settings["enabled"] = writethru.Enabled.ToString();

            if (writethru.Providers != null)
                settings.Add("write-thru-providers", GetProviders(writethru.Providers));

            if (writethru.WriteBehind != null)
                settings.Add("write-behind", GetWriteBehind(writethru.WriteBehind));

            return settings;
        }

        private static Hashtable GetReadThru(Readthru readthru)
        {
            Hashtable settings = new Hashtable();
            settings["enabled"] = readthru.Enabled.ToString();

            if (readthru.Providers != null)
                settings.Add("read-thru-providers", GetProviders(readthru.Providers));

            return settings;
        }

        private static Hashtable GetWriteBehind(WriteBehind writeBehind)
        {
            Hashtable settings = new Hashtable();

            if (writeBehind != null)
            {
                settings["mode"] = writeBehind.Mode;
                settings["throttling-rate-per-sec"] = writeBehind.Throttling;
                settings["failed-operations-queue-limit"] = writeBehind.RequeueLimit;
                settings["failed-operations-eviction-ratio"] = writeBehind.Eviction;
                if (writeBehind.BatchConfig != null)
                    settings.Add("batch-mode-config", GetBatchConfig(writeBehind.BatchConfig));
            }

            return settings;
        }

        private static Hashtable GetBatchConfig(BatchConfig batchConfig)
        {
            Hashtable settings = new Hashtable();
            if (batchConfig != null)
            {
                settings["batch-interval"] = batchConfig.BatchInterval;
                settings["operation-delay"] = batchConfig.OperationDelay;
            }
            return settings;
        }

        private static Hashtable GetProviders(Provider[] providers)
        {
            Hashtable settings = new Hashtable();

            if (providers != null && providers.Length > 0)
            {
                for (int i = 0; i < providers.Length; i++)
                {
                    settings[providers[i].ProviderName] = GetProvider(providers[i]);
                }
            }

            return settings;
        }

        private static Hashtable GetProvider(Provider provider)
        {
            Hashtable settings = new Hashtable();

            if (provider != null)
            {
                settings["provider-name"] = provider.ProviderName;
                settings["assembly-name"] = provider.AssemblyName;
                settings["class-name"] = provider.ClassName;
                settings["full-name"] = provider.FullProviderName;
                settings["default-provider"] = provider.IsDefaultProvider.ToString();
                Hashtable paramss = GetParameters(provider.Parameters);
                if (paramss != null)
                    settings["parameters"] = paramss;
            }

            return settings;
        }

        private static Hashtable GetParameters(Parameter[] parameters)
        {
            if (parameters == null)
                return null;

            Hashtable settings = new Hashtable();
            for (int i = 0; i < parameters.Length; i++)
                settings[parameters[i].Name] = parameters[i].ParamValue;


            return settings;
        }

        private static Hashtable GetCompactType(Alachisoft.NCache.Config.Dom.Type type)
        {
            Hashtable settings = new Hashtable();
            settings.Add("id", type.ID);
            settings.Add("handle", type.Name);
            settings.Add("portable", type.Portable);
            if (type.PortableClasses != null)
            {
                settings.Add("known-classes", GetCompactPortableClasses(type.PortableClasses));
                settings.Add("attribute-union-list", GetCompactAttributeListUnion(type.AttributeList));
            }
            return settings;
        }
        private static Hashtable GetCompactPortableClasses(PortableClass[] classes)
        {
            Hashtable settings = new Hashtable();
            foreach (PortableClass clas in classes)                
                settings.Add(clas.Name, GetCompactPortableClass(clas));
            return settings;
        }
        private static Hashtable GetCompactAttributeListUnion(AttributeListUnion attributeList)
        {
            Hashtable settings = new Hashtable();
            if (attributeList != null && attributeList.PortableAttributes != null)
                settings.Add("attribute", GetCompactPortableAttributes(attributeList.PortableAttributes));
            return settings;

        }
        private static Hashtable GetCompactPortableClass(PortableClass clas)
        {
            Hashtable settings = new Hashtable();
            settings.Add("name", clas.Name);
            settings.Add("handle-ID", clas.ID);
            settings.Add("assembly", clas.Assembly);
            settings.Add("type", clas.Type);
            if (clas.PortableAttributes != null)
                settings.Add("attribute", GetCompactPortableAttributes(clas.PortableAttributes));
            return settings;
        }

        private static Hashtable GetCompactPortableAttributes(PortableAttribute[] attributes)
        {
            Hashtable settings = new Hashtable();
            foreach (PortableAttribute attrib in attributes)
                settings.Add(attrib.Name + ":" + attrib.Type, GetCompactPortableAttribute(attrib));
            return settings;
        }

        private static Hashtable GetCompactPortableAttribute(PortableAttribute attrib)
        {
            Hashtable settings = new Hashtable();
            settings.Add("name", attrib.Name);
            settings.Add("type", attrib.Type);
            settings.Add("order", attrib.Order);            
            return settings;
        }
        
        private static Hashtable GetServerMapping(ServerMapping serverMapping)
        {
            Hashtable settings = new Hashtable();
            if (serverMapping.MappingServers != null && serverMapping.MappingServers.Length > 0)
            {
                foreach (Mapping mapping in serverMapping.MappingServers)
                    settings.Add(mapping.PrivateIP, GetMapping(mapping));
            }

            return settings;
        }

        private static Hashtable GetMapping(Mapping mapping)
        {
            Hashtable settings = new Hashtable();
            if (mapping != null)
            {
                settings.Add("private-ip", mapping.PrivateIP.ToString());
                settings.Add("private-port", mapping.PrivatePort.ToString());
                settings.Add("public-ip", mapping.PublicIP.ToString());
                settings.Add("public-port", mapping.PublicPort.ToString());
            }
            return settings;
        }


        /// <summary>
        /// Populates the object from specified configuration string.
        /// </summary>
        /// <returns></returns>
        internal static CacheConfig FromPropertyString(string props)
        {
            CacheConfig cConfig = null;
            if (props != null)
            {
                PropsConfigReader pcr = new PropsConfigReader(props);
                IDictionary cacheConfig = pcr.Properties;
                cConfig = CacheConfig.FromProperties(cacheConfig);
            }
            return cConfig;
        }

        internal static ArrayList GetConfigs(ArrayList props)
        {
            ArrayList configList = new ArrayList();

            foreach (Hashtable properties in props)
            {
                CacheConfig config = null;
                config = FromProperties(properties);
                if (config != null)
                {
                    configList.Add(config);
                }
            }

            return configList;
        }

        internal static ArrayList GetConfigs(ArrayList props, long tcpPort)
        {
            ArrayList configList = new ArrayList();

            foreach (Hashtable properties in props)
            {
                CacheConfig config = null;
                config = FromProperties(properties, tcpPort);
                if (config != null)
                {
                    configList.Add(config);
                }
            }

            return configList;
        }

        private static IDictionary ReplaceCacheId(IDictionary properties, string oldCacheId, string newCacheId)
        {
            oldCacheId = oldCacheId.ToLower();
            newCacheId = newCacheId.ToLower();

            IDictionary props = ((Hashtable)properties).Clone() as IDictionary;
            IDictionaryEnumerator ide = properties.GetEnumerator();
            while (ide.MoveNext())
            {
                if (((string)ide.Key).ToLower() == oldCacheId)
                {
                    if (ide.Value is IDictionary)
                    {
                        props.Remove(oldCacheId);
                        props[newCacheId] = ReplaceCacheId(ide.Value as IDictionary, oldCacheId, newCacheId); ;
                    }
                    else
                    {
                        props.Remove(oldCacheId);
                        props[newCacheId] = ide.Value;
                    }
                }
                else if (ide.Value is IDictionary)
                {
                    props[ide.Key] = ReplaceCacheId(ide.Value as IDictionary, oldCacheId, newCacheId);
                }
                else
                {
                    if (((string)ide.Value).ToLower() == oldCacheId)
                    {
                        props.Remove(ide.Key);
                        props[ide.Key] = newCacheId;
                    }
                }
            }
            return props;
        }

        internal static CacheConfig GetUpdatedConfig(IDictionary properties, string partId, string joiningNode, ref ArrayList affectedNodes, ref ArrayList affectedPartitions, string oldCacheId, string newCacheId)
        {
            //update the properties...

            string list = "";
            int clusterPort = 0;

            if (affectedNodes == null)
                affectedNodes = new ArrayList();

            properties = ReplaceCacheId(properties, oldCacheId, newCacheId);

            IDictionary cacheProps = properties["cache"] as IDictionary;

            if (cacheProps.Contains("cache-classes"))
            {
                IDictionary cacheClassesProps = cacheProps["cache-classes"] as IDictionary;

                string cacheName = Convert.ToString(cacheProps["name"]);
                cacheName = cacheName.ToLower();

                if (cacheClassesProps.Contains(cacheName))
                {
                    IDictionary topologyProps = cacheClassesProps[cacheName] as IDictionary;

                    if (topologyProps.Contains("cluster"))
                    {
                        IDictionary clusterProps = topologyProps["cluster"] as IDictionary;

                        if (clusterProps.Contains("channel"))
                        {
                            IDictionary channelProps = clusterProps["channel"] as IDictionary;

                            if (channelProps.Contains("tcp"))
                            {
                                IDictionary tcpProps = channelProps["tcp"] as IDictionary;

                                if (tcpProps.Contains("start_port"))
                                {
                                    clusterPort = Convert.ToInt32(tcpProps["start_port"]);
                                }
                            }

                            if (channelProps.Contains("tcpping"))
                            {
                                IDictionary tcppingProps = channelProps["tcpping"] as IDictionary;

                                if (tcppingProps.Contains("initial_hosts"))
                                {
                                    list = Convert.ToString(tcppingProps["initial_hosts"]).ToLower();

                                    string[] nodes = list.Split(',');
                                    foreach (string node in nodes)
                                    {
                                        string[] nodename = node.Split('[');
                                        affectedNodes.Add(nodename[0]);
                                    }

                                    if (list.IndexOf(joiningNode) == -1)
                                    {
                                        list = list + "," + joiningNode + "[" + clusterPort + "]";
                                        tcppingProps["initial_hosts"] = list;
                                    }
                                }
                            }

                            if (channelProps.Contains("partitions"))
                            {
                                if (partId != null && partId != string.Empty)
                                {
                                    Hashtable partitionsProps = channelProps["partitions"] as Hashtable;
                                    if (partitionsProps != null)
                                    {
                                        if (partitionsProps.Contains(partId.ToLower()))
                                        {
                                            string nodesList = Convert.ToString(partitionsProps[partId.ToLower()]).ToLower();
                                            if (nodesList.IndexOf(joiningNode) == -1)
                                            {
                                                nodesList = nodesList + ", " + joiningNode;
                                                partitionsProps[partId.ToLower()] = nodesList;
                                            }
                                        }

                                        foreach (string part in partitionsProps.Keys)
                                        {
                                            if (!affectedPartitions.Contains(part))
                                                affectedPartitions.Add(part);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //send the updated properties...
            return FromProperties(properties);
        }

        internal static CacheConfig GetUpdatedConfig(IDictionary properties, string partId, string newNode, ref ArrayList affectedNodes, ref ArrayList affectedPartitions, bool isJoining)
        {
            //update the properties...

            string list = "";
            int clusterPort = 0;

            if (affectedNodes == null)
                affectedNodes = new ArrayList();

            IDictionary cacheProps = properties["cache"] as IDictionary;

            if (cacheProps.Contains("cache-classes"))
            {
                IDictionary cacheClassesProps = cacheProps["cache-classes"] as IDictionary;

                string cacheName = Convert.ToString(cacheProps["name"]);
                cacheName = cacheName.ToLower();

                if (cacheClassesProps.Contains(cacheName))
                {
                    IDictionary topologyProps = cacheClassesProps[cacheName] as IDictionary;

                    if (topologyProps.Contains("cluster"))
                    {
                        IDictionary clusterProps = topologyProps["cluster"] as IDictionary;

                        if (clusterProps.Contains("channel"))
                        {
                            IDictionary channelProps = clusterProps["channel"] as IDictionary;

                            if (channelProps.Contains("tcp"))
                            {
                                IDictionary tcpProps = channelProps["tcp"] as IDictionary;

                                if (tcpProps.Contains("start_port"))
                                {
                                    clusterPort = Convert.ToInt32(tcpProps["start_port"]);
                                }
                            }

                            if (channelProps.Contains("tcpping"))
                            {
                                IDictionary tcppingProps = channelProps["tcpping"] as IDictionary;

                                if (tcppingProps.Contains("initial_hosts"))
                                {
                                    list = Convert.ToString(tcppingProps["initial_hosts"]).ToLower();

                                    string[] nodes = list.Split(',');

                                    if (isJoining)
                                    {
                                        foreach (string node in nodes)
                                        {
                                            string[] nodename = node.Split('[');
                                            affectedNodes.Add(nodename[0]);
                                        }

                                        if (list.IndexOf(newNode) == -1)
                                        {
                                            list = list + "," + newNode + "[" + clusterPort + "]";
                                            tcppingProps["initial_hosts"] = list;
                                        }
                                    }
                                    else
                                    {
                                        foreach (string node in nodes)
                                        {
                                            string[] nodename = node.Split('[');
                                            if (nodename[0] != newNode)
                                            {
                                                affectedNodes.Add(nodename[0]);
                                            }
                                        }

                                        list = string.Empty;
                                        foreach (string node in affectedNodes)
                                        {
                                            if (list.Length == 0) list = node + "[" + clusterPort + "]";
                                            else 
                                            list = list + "," + node + "[" + clusterPort + "]";
                                        }
                                        tcppingProps["initial_hosts"] = list;
                                    }
                                }
                            }

                            if (channelProps.Contains("partitions"))
                            {
                                if (partId != null && partId != string.Empty)
                                {
                                    Hashtable partitionsProps = channelProps["partitions"] as Hashtable;
                                    if (partitionsProps != null)
                                    {
                                        if (partitionsProps.Contains(partId.ToLower()))
                                        {
                                            string nodesList = Convert.ToString(partitionsProps[partId.ToLower()]).ToLower();
                                            if (nodesList.IndexOf(newNode) == -1)
                                            {
                                                nodesList = nodesList + ", " + newNode;
                                                partitionsProps[partId.ToLower()] = nodesList;
                                            }
                                        }

                                        foreach (string part in partitionsProps.Keys)
                                        {
                                            if (!affectedPartitions.Contains(part))
                                                affectedPartitions.Add(part);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //send the updated properties...
            return FromProperties(properties);
        }

        internal static CacheConfig GetUpdatedConfig2(IDictionary properties, string partId, string joiningNode, ref ArrayList affectedNodes, ref ArrayList affectedPartitions)
        {
            //update the properties...
            if (properties.Count == 1)
            {
                IDictionaryEnumerator ide = properties.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Value is IDictionary)
                        properties = ide.Value as IDictionary;
                    break;
                }
            }

            string list = "";
            int clusterPort = 0;

            if (affectedNodes == null)
                affectedNodes = new ArrayList();

            if (properties.Contains("cluster"))
            {
                IDictionary clusterProps = properties["cluster"] as IDictionary;
                if (clusterProps.Contains("channel"))
                {
                    IDictionary channelProps = clusterProps["channel"] as IDictionary;
                    if (channelProps.Contains("tcp-port"))
                    {
                        clusterPort = Convert.ToInt32(channelProps["tcp-port"]);
                    }

                    if (channelProps.Contains("initial-hosts"))
                    {
                        list = Convert.ToString(channelProps["initial-hosts"]).ToLower();

                        string[] nodes = list.Split(',');
                        foreach (string node in nodes)
                        {
                            string[] nodename = node.Split('[');
                            affectedNodes.Add(nodename[0]);
                        }

                        if (list.IndexOf(joiningNode) == -1)
                        {
                            list = list + "," + joiningNode + "[" + clusterPort + "]";
                            channelProps["initial-hosts"] = list;
                            channelProps["num-initial-hosts"] = "2";
                        }
                    }
                }
            }

            //send the updated properties...
            return FromProperties2(properties);
        }

        /// <summary>
        /// Populates the object from specified configuration.
        /// </summary>
        /// <returns></returns>
        internal static CacheConfig FromProperties(IDictionary properties, long tcpPort)
        {
            CacheConfig data = new CacheConfig(tcpPort);

            if (properties.Contains("partitionid"))
                data.PartitionId = properties["partitionid"].ToString().ToLower();

            IDictionary webprops = properties["web-cache"] as IDictionary;
            IDictionary cacheprops = properties["cache"] as IDictionary;

            if (properties == null)
                throw new ManagementException(@"Invalid configuration; missing 'web-cache' element.");


            try
            {
                if (cacheprops.Contains("cache-classes"))
                {
                    IDictionary cacheClassesProps = cacheprops["cache-classes"] as IDictionary;
                    string cacheName = Convert.ToString(cacheprops["name"]);
                    cacheName = cacheName.ToLower();
                    if (cacheClassesProps.Contains(cacheName))
                    {
                        IDictionary topologyProps = cacheClassesProps[cacheName] as IDictionary;
                        if (topologyProps.Contains("cluster"))
                        {
                            IDictionary clusterProps = topologyProps["cluster"] as IDictionary;
                            if (clusterProps.Contains("channel"))
                            {
                                IDictionary channelProps = clusterProps["channel"] as IDictionary;
                                if (channelProps.Contains("tcp"))
                                {
                                    IDictionary tcpProps = channelProps["tcp"] as IDictionary;
                                    if (tcpProps.Contains("start_port"))
                                    {
                                        data.ClusterPort = Convert.ToInt32(tcpProps["start_port"]);
                                    }
                                    if (tcpProps.Contains("port_range"))
                                    {
                                        data.ClusterPortRange = Convert.ToInt32(tcpProps["port_range"]);
                                    }
                                    else
                                    {
                                        data.ClusterPortRange = 2;
                                    }
                                }
                            }
                        }

                        if (topologyProps.Contains("type"))
                        {
                            data._cacheType = Convert.ToString(topologyProps["type"]);
                        }
                    }
                }

                // Get Error and Detailed logs enable status from the config file
                if (cacheprops.Contains("log"))
                {
                    IDictionary cacheLogProps = cacheprops["log"] as IDictionary;
                    if (cacheLogProps.Contains("enabled"))
                    {
                        bool logsEnabled = Convert.ToBoolean(cacheLogProps["enabled"]);
                        if (logsEnabled)
                        {
                            if (cacheLogProps.Contains("trace-errors"))
                                data.IsErrorLogsEnabled = Convert.ToBoolean(cacheLogProps["trace-errors"]);
                            if (cacheLogProps.Contains("trace-debug"))
                                data.IsDetailedLogsEnabled = Convert.ToBoolean(cacheLogProps["trace-debug"]);
                        }
                    }
                }
            }
            catch (Exception) { }

            data.CacheId = Convert.ToString(webprops["cache-id"]);
            if (data.CacheId == null || data.CacheId.Length == 0)
                throw new ManagementException(@"'cache-id' not specified in configuration.");

            if (webprops.Contains("channel"))
            {
                string channel = Convert.ToString(webprops["channel"]);
                channel = channel.ToLower();
                if (channel.CompareTo("http") == 0)
                    data.UseTcp = false;
            }

            if (webprops.Contains("shared"))
                data.UseInProc = !Convert.ToBoolean(webprops["shared"]);

            if (webprops.Contains("port"))
                data.Port = Convert.ToUInt32(webprops["port"]);

            if (webprops.Contains("server"))
                data.ServerName = Convert.ToString(webprops["server"]);

            properties.Remove("id");
            properties.Remove("type");
            data.PropertyString = ConfigReader.ToPropertiesString(properties);

            if (properties.Contains("data-sharing"))
            {
                data.DataSharingKnownTypes = (Hashtable)properties["data-sharing"];
            }

            return data;
        }

        /// <summary>
        /// Populates the object from specified configuration.
        /// </summary>
        /// <returns></returns>
        internal static CacheConfig FromProperties(IDictionary properties)
        {
            CacheConfig data = new CacheConfig();
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
         new System.Globalization.CultureInfo("en-US");
                if (properties.Contains("partitionid"))
                    data.PartitionId = properties["partitionid"].ToString().ToLower();

                IDictionary webprops = properties["web-cache"] as IDictionary;
                IDictionary cacheprops = properties["cache"] as IDictionary;

                if (properties == null)
                    throw new ManagementException(@"Invalid configuration; missing 'web-cache' element.");
                try
                {
                    if (cacheprops.Contains("compression"))
                    {
                        IDictionary compressionProps = cacheprops["compression"] as IDictionary;
                        data._compressionEnabled = Convert.ToBoolean(compressionProps["enabled"]);
                        data._compressionThreshold = Convert.ToInt64(compressionProps["threshold"]);
                    }

                    if (cacheprops.Contains("security"))
                    {
                        IDictionary securityProps = cacheprops["security"] as IDictionary;
                        data._securityEnabled = Convert.ToBoolean(securityProps["enabled"]);
                        data._securityDomainController = (string)securityProps["domain-controller"];
                        data._securityUsers = (Hashtable)securityProps["user"];
                    }

                    if (cacheprops.Contains("cache-classes"))
                    {
                        IDictionary cacheClassesProps = cacheprops["cache-classes"] as IDictionary;
                        string cacheName = Convert.ToString(cacheprops["name"]);
                        cacheName = cacheName.ToLower();
                        if (cacheClassesProps.Contains(cacheName))
                        {
                            IDictionary topologyProps = cacheClassesProps[cacheName] as IDictionary;
                            if (topologyProps.Contains("cluster"))
                            {
                                IDictionary clusterProps = topologyProps["cluster"] as IDictionary;
                                if (clusterProps.Contains("channel"))
                                {
                                    IDictionary channelProps = clusterProps["channel"] as IDictionary;
                                    if (channelProps.Contains("tcp"))
                                    {
                                        IDictionary tcpProps = channelProps["tcp"] as IDictionary;
                                        if (tcpProps.Contains("start_port"))
                                        {
                                            data.ClusterPort = Convert.ToInt32(tcpProps["start_port"]);
                                        }
                                        if (tcpProps.Contains("port_range"))
                                        {
                                            data.ClusterPortRange = Convert.ToInt32(tcpProps["port_range"]);
                                        }
                                        else
                                        {
                                            data.ClusterPortRange = 2;
                                        }
                                    }
                                    if (channelProps.Contains("tcpping"))
                                    {
                                        IDictionary tcppingProps = channelProps["tcpping"] as IDictionary;
                                        if (tcppingProps.Contains("initial_hosts"))
                                        {
                                            string hostString = (string)tcppingProps["initial_hosts"];
                                            data._servers = FromHostListToServers(hostString);
                                        }
                                    }
                                }
                            }

                            if (topologyProps.Contains("type"))
                            {
                                data._cacheType = Convert.ToString(topologyProps["type"]);
                            }

                            if (topologyProps.Contains("clean-interval"))
                            {
                                data._cleanInterval = Convert.ToInt64(topologyProps["clean-interval"]) * 1000; //convert to ms
                            }

                            if (topologyProps.Contains("scavenging-policy"))
                            {
                                IDictionary scavengingProps = topologyProps["scavenging-policy"] as IDictionary;
                                if (scavengingProps.Contains("evict-ratio"))
                                {
                                    data._evictRatio = Convert.ToSingle(scavengingProps["evict-ratio"]);
                                }
                            }

                            //We need to extract storage information from the properties 
                            //because user can now change the cache size at runtime.
                            //hot apply configuration option for cache size.
                            if (topologyProps.Contains("storage"))
                            {
                                IDictionary storageProps = topologyProps["storage"] as IDictionary;
                                if (storageProps.Contains("class"))
                                {
                                    string storageClass = storageProps["class"] as string;
                                    IDictionary storageProviderProps = storageProps[storageClass] as IDictionary;
                                    if (storageProviderProps.Contains("max-size"))
                                    {
                                        data._cacheMaxSize = Convert.ToInt64(storageProviderProps["max-size"]) * 1024 * 1024; //from MBs to bytes
                                    }
                                }
                            }
                            else if (topologyProps.Contains("internal-cache"))
                            {
                                IDictionary internalProps = topologyProps["internal-cache"] as IDictionary;

                                if (internalProps.Contains("clean-interval"))
                                {
                                    data._cleanInterval = Convert.ToInt64(internalProps["clean-interval"]) * 1000; //convert to ms
                                }

                                if (internalProps.Contains("scavenging-policy"))
                                {
                                    IDictionary scavengingProps = internalProps["scavenging-policy"] as IDictionary;
                                    if (scavengingProps.Contains("evict-ratio"))
                                    {
                                        data._evictRatio = Convert.ToSingle(scavengingProps["evict-ratio"]);
                                    }
                                }

                                if (internalProps.Contains("storage"))
                                {
                                    IDictionary storageProps = internalProps["storage"] as IDictionary;
                                    if (storageProps.Contains("class"))
                                    {
                                        string storageClass = storageProps["class"] as string;
                                        IDictionary storageProviderProps = storageProps[storageClass] as IDictionary;
                                        if (storageProviderProps.Contains("max-size"))
                                        {
                                            data._cacheMaxSize = Convert.ToInt64(storageProviderProps["max-size"]) * 1024 * 1024; //from MBs to bytes
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Get Error and Detailed logs enable status from the config file
                    if (cacheprops.Contains("log"))
                    {
                        IDictionary cacheLogProps = cacheprops["log"] as IDictionary;
                        if (cacheLogProps.Contains("enabled"))
                        {
                            bool logsEnabled = Convert.ToBoolean(cacheLogProps["enabled"]);
                            if (logsEnabled)
                            {
                                if (cacheLogProps.Contains("trace-errors"))
                                    data.IsErrorLogsEnabled = Convert.ToBoolean(cacheLogProps["trace-errors"]);
                                if (cacheLogProps.Contains("trace-debug"))
                                    data.IsDetailedLogsEnabled = Convert.ToBoolean(cacheLogProps["trace-debug"]);
                            }
                        }
                    }
                }
                catch (Exception) { }

                data.CacheId = Convert.ToString(webprops["cache-id"]);
                if (data.CacheId == null || data.CacheId.Length == 0)
                    throw new ManagementException(@"'cache-id' not specified in configuration.");

                if (webprops.Contains("channel"))
                {
                    string channel = Convert.ToString(webprops["channel"]);
                    channel = channel.ToLower();
                    if (channel.CompareTo("http") == 0)
                        data.UseTcp = false;
                }

                if (webprops.Contains("shared"))
                    data.UseInProc = !Convert.ToBoolean(webprops["shared"]);

                if (webprops.Contains("port"))
                    data.Port = Convert.ToUInt32(webprops["port"]);
                else
                    data.Port = data.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;

                if (webprops.Contains("server"))
                    data.ServerName = Convert.ToString(webprops["server"]);

                properties.Remove("id");
                properties.Remove("type");
                data.PropertyString = ConfigReader.ToPropertiesString(properties);

                if (properties.Contains("data-sharing"))
                {
                    data.DataSharingKnownTypes = (Hashtable)properties["data-sharing"];
                }

            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = cultureInfo;
            }
            return data;
        }

        private static ArrayList FromHostListToServers(string hostString)
        {
            ArrayList servers = new ArrayList();

            if (hostString.IndexOf(',') != -1)
            {
                string[] hosts = hostString.Split(new char[] { ',' });
                if (hosts != null)
                {
                    for (int i = 0; i < hosts.Length; i++)
                    {
                        hosts[i] = hosts[i].Trim();
                        servers.Add(hosts[i].Substring(0, hosts[i].IndexOf('[')));
                    }
                }
            }
            else
            {
                servers.Add(hostString.Trim().Substring(0, hostString.IndexOf('[')));
            }
            return servers;
        }

        /// <summary>
        /// Populates the object from specified configuration.
        /// </summary>
        /// <returns></returns>
        internal static CacheConfig FromProperties2(IDictionary properties)
        {
            CacheConfig data = new CacheConfig();

            try
            {
                if (properties.Contains("name"))
                    data._cacheId = properties["name"] as string;
                else
                    throw new ManagementException(@"'name' not specified in configuration.");

                if (properties.Contains("log"))
                {
                    IDictionary cacheLogProps = properties["log"] as IDictionary;
                    if (cacheLogProps.Contains("enabled"))
                    {
                        bool logsEnabled = Convert.ToBoolean(cacheLogProps["enabled"]);
                        if (logsEnabled)
                        {
                            if (cacheLogProps.Contains("trace-errors"))
                                data.IsErrorLogsEnabled = Convert.ToBoolean(cacheLogProps["trace-errors"]);
                            if (cacheLogProps.Contains("trace-debug"))
                                data.IsDetailedLogsEnabled = Convert.ToBoolean(cacheLogProps["trace-debug"]);
                        }
                    }
                }

                // Get start_port (ClusterPort) and port_range (ClusterPortRange) from the config file
                if (properties.Contains("cluster"))
                {
                    IDictionary clusterProps = properties["cluster"] as IDictionary;
                    if (clusterProps.Contains("channel"))
                    {
                        IDictionary channelProps = clusterProps["channel"] as IDictionary;
                        if (channelProps.Contains("tcp-port"))
                            data.ClusterPort = Convert.ToInt32(channelProps["tcp-port"]);
                        data.ClusterPortRange = 1;
                    }
                }
            }
            catch (Exception) { }

            data.UseInProc = Convert.ToBoolean(properties["inproc"]);
            data.PropertyString = ConfigReader.ToPropertiesString(properties);

            return data;
        }
    }
}
