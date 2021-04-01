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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Automation.Util
{
    public class Message
    {
        public const string USERID = "Specifies the user-id used to authorize the user for this operation. It is required in case security is enabled on Cache Server. This user - id must be the active directory user - id prefixed with the domain name.";
        public const string PASSWORD = "Specifies the password of the user that is used to authorize the user for this operation.It is required in case security is enabled on Cache Server.This password must be the same as the active directory user password.";
        public const string STARTCACHES = "Specifies one or more name(s) of caches separated by space registered on the server.The cache(s) with this / these name(s) is/ are started on the server.Note: , separated cache names are to be specified in case of multiple caches.";
        public const string RUNNINGSERVER = "Specifies a server name where the NCache service is running. The default is the local machine.";
        public const string MAXSAMPLES = "Number of samples to be collected.";
        public const string SERVERS = "Specifies one or more server name(s) where the NCache service is running and a cache with the specified cache-name is running. The default is the local machine. Note: , separated server names are to be specified in case of multiple servers.";
        public const string DONOTSHOWDEFAULTCOUNTERS = "Specifies whether default counters are to be shown. They are shown by default. Note: This parameter is only valid if Counter Names are provided. ";
        public const string COUNTERNAMES = "Specifies one or more counter(s) not included in default counters that should be displayed. Note: , separated counter names are to be specified in case of multiple counters.";
        public const string CLIENTNODES = "Specifies the IP Addresses of the clients.  The default is the local machine. Note: , separated IP Addresses are to be specified in case of multiple IP Addresses. ";
        public const string CONTINUOUS = " Specifies that statistics are fetched continuously.";
        public const string SAMPLEINTERVAL = "Specifies the time between samples in seconds.The minimum value and default value are 1";
        public const string FORMAT = "Specifies display format. ";
        public const string PORT = "Specifies the port if the server channel is not using the default port.";
        public const string CREATECACHE = "Specifies the name of the cache for which cache will be registered. ";
        public const string INPROC = "Specify the isolation level for local cache.";
        public const string JSON = "Json formatted output";
        public const string CREATECACHESERVER = "Specifies the NCache server names/ips where Cache should be configured, seperated by commas e.g. 120.168.98.10, 120.168.98.9";
        public const string CACHESIZE = "Specifies the size(MB) of the cache to be created, default size is 1024 MB.";
        public const string EVICTIONPOLICY = "Specifies the eviction policy for cache items. Cached items will be cleaned from the cache according to the specified policy if the cache reaches its limit.Possible values are i.Priority ii.LFU(Only available in Enterprise edition) iii.LRU(default)(Only available in Enterprise edition)";
        public const string EVICTIONRATIO = "Specifies the eviction ratio(Percentage) for cache items. Cached items will be cleaned from the cache according to the specified ratio if the cache reaches its limit.Default value is 5(percent) ";
        public const string CLEANUPINTERVAL = "Specifies the time interval(seconds) after which cache cleanup is called.  Default clean - interval is 15(seconds)";
        public const string TOPOLOGY = "For topology other than local you have to give topology and cluster port It Specifies the topology in case of clustered cache. Possible values are " + "\n" + "i.local" + "\n" + "ii.mirrored(Only available in Enterprise edition)" + "\n" + "iii.replicated" + "\n" + "iv.partitioned(Only available in Enterprise edition)" + "\n" + "v.partitioned - replica(Only available in Enterprise edition)";
        public const string REPLICATIONSTRATEGY = "Only in case of 'partition-replicas-server' being the topology,this specifies the replication strategy" + "\n" + "i.async(default)" + "\n" + "ii.sync";
        public const string CLUSTERPORT = "Specifies the cache port of cache.";
        public const string DEFAULTPRIORITY = "Specifies the default priority in case of priority based eviction policy is selected. Possible values are" + "\n" + "i.high" + "\n" + "ii.above - normal" + "\n" + "iii.normal(default)" + "\n" + "iv.below - normal" + "\n" + "v.low";
        public const string CONFIGUREXML = "Specifies the path of the cache source config which will be configured.";
        public const string REMOVECACHE = " Specifies name of Clustered Cache to be removed.Cache must exist on source server.";
        public const string SERVER = " Specifies a server name where the NCache service is running and a cache with the specified cache-name is registered.The default is the local machine.";
        public const string CONFIGURE_CLIENT_CACHE_SERVER = "Specifies the clustered cache name. A clustered cache must be specified for creating a client cache.";
        public const string PESSIMISTIC = "Specifies if pessimistic Client Cache Syncronization Mode should be enable  Default is Optimistic.";
        public const string CLIENTNODE = "Specifies a client node which is registered with the Cluster Cache.";
        public const string UPDATE_SERVER_CONIG = "Specifies whether to update the client-nodes sections of server node(s) of the specified cluster.The default value is true.(Useful when cluster nodes and clients are in different networks)";
        public const string CLIENT_CACHE_INPROC = " Specifies whether the client cache is inProc or outProc. Default value is outProc";
        public const string CLIENT_CACHE_CLUSTERCACHE = "Specifies the clustered cache name. A clustered cache must be specified for each client cache.";
        public const string GET_CACHE_CONFIGURATION = "Specifies the name of the cache for which cache configuration will be generated. ";
        public const string GENERATE_CONFIG_PATH = "Specifies the path where config will be generated.";
        public const string EXISTING_SERVER = " Specifies a server name where the NCache service is running and a cache with the specified cache-name is registered.Cache configuration is copied from this server to the destination server. ";
        public const string ADD_NODE_CACHE = "Specifies name of clustered cache to which new server node is to be added. Cache must exist on source server";
        public const string NEW_SERVER = " Specifies a server name where a cache with the specified cache-name needs to be registered.The cache configuration is copied from the source server to the destination server.";
        public const string REMOVE_NODE = "Specifies id of cache registered on the server. The cache with this id is unregistered on the server.";
        public const string ADD_CLIENT_NODE_CACHE = " Specifies name of clustered cache to which client node is to be added. Cache must exist on source server.";
        public const string ACQUIRE_SERVER_MAPPING = "  Specifies whether to fetch the server mapping list from the server node(s).The default value is false.(Useful when cluster nodes and clients are in different networks)";
        public const string ADD_CLIENT_NODE = "Specifies a client node where the NCache service is running.";
        public const string REMOVE_CLIENT_NODE_CACHE = "Specifies id of Clustered Cache.Cache must exist on source server.";
        public const string REMOVE_CLIENT_NODE = " Specifies the node name to remove from the cache's current client nodes.";

        public const string CACHE_LOADER_NAME = "Specifies the name of the cache for which cache loader will be configured.";
        public const string ASSEMBLY_PATH = "Specifies the path of the assembly which will be configured as a startup loader. ";
        public const string LOADER_CLASS = "Specifies the fully qualified class from the startup loader which implements ICacheLoader/ICacheStartupProvider. ";
        public const string PARAMETER_LIST = "Specifies the list of the parameters passed to the cache loader e.g key1=value1$key2=value2...";
        public const string NO_DEPLOY = "Specify if no assembly should be deployed.";
        public const string DEP_ASM_PATH = " Specifies the dependant assembly folder/path";
        public const string RETRIES = "Specifies number of retries for loading data in cache.";
        public const string RETRY_INTERVAL = "Specifies the retry interval for loading data in cache.";
        public const string HINT_LIST = "Specifies the distribution hint list for cache.";
        public const string REMOVE_LOADER_CACHE = "Specifies the name of the cache for which cache loader will be removed. ";
        public const string ADD_SECURITY_CACHE_NAME = "Specifies the id/name of the cache for which security on the specified client node to be configured";
        public const string ADD_SECURITY_NODE_NAME = "Specifies the client node where security is to be configured.";
        public const string ADMIN_ID = "Specifies the administrator Id. To configure security on any node, administrative rights are required.";
        public const string ADMIN_PASSWORD = " Specifies the administrator password.";
        public const string DOMAIN_CONTROLLER = "Specifies the domain controller.";
        public const string PRIMARY_USER = "Specifies the primary user Id. ";
        public const string PRIMARY_PASSWORD = "Specifies the password for primary user.";
        public const string SECONDARY_USER = " Specifies the secondary user Id.";
        public const string SECONDARY_PASSWORD = "Specifies the password for secondary user.";
        public const string ENABLE_NODE_SECURITY = "  Specifies that the tool is being used for enabling security on a specific node.";
        public const string CONFIGURE_SECURITY_NODE_NAME = "Specifies the target node to enable/disbale security.";
        public const string DISABLE_NODE_SECURITY = "Specifies that the tool is being used for disabling security on a specific node.";
        public const string DISABLE_NODE_SECURITY_ADMIN = "This user name can also be an administrator, because to disable security you can either be an NCache node administrator or you have the administrator rights on the target node";
        public const string ADD_USER = " Specifies that the tool is being used for adding a user in Cache administrator's list.";
        public const string NEW_USER = " Specifies the user name to add.";
        public const string NEW_USER_PASWORD = "  Password of new user. This is an OPTIONAL switch. Only useful for secured auto-start caches. If given, tool will write user name and password(encrypted) in service config file for caches that require security credentials during auto-start.";
        public const string ADMIN_NAME = "Specifies the NCache administrator name.";
        public const string WRITE_TO_SOURCE = "Specifies whether to write to service configurations.";
        public const string REMOVE_USER = " Specifies that the tool is being used for removing a user from NCache node administrators.";
        public const string USER_TO_DELETE = "Specifies the existing user which is now being deleted or removed from NCache node administrators.";
        public const string CACHE_SECUIRTY_ID = "Specifies the target cache to enable security.";
        public const string USER_NAME = " Specifies the NCache administrator name.";
        public const string USER_PASSWORD = "Specifies the NCache administrator password.";
        public const string REMOVE_DATA_SHARE_CACHE = " Specifies the name of the cache for which data share will be removed. ";
        public const string DUMP_CACHE_DATA_CACHE = "Specifies name of the cache to be dumped.";
        public const string DUMP_CACHE_DATA_PATH = " A network or local path where dumped files should be stored during dump process or retrieved from during reload process.";
        public const string DUMP_CACHE_DATA_ASSEMBLEY = "Path where application assemblies are kept. These are required for serialization and deserialization of cached items.";
        public const string DUMP_CACHE_DATA_FILE_SIZE = "  Maximum size of dump file to be generated (default: 5 MB; minimum: 1 MB; maximum: 100 MB)";
        public const string DUMP_CACHE_DATA_BULK_SIZE = "Number of items inserted as a bulk operation (default: 1000; minimum: 1; maximum: 10000)";
        public const string DUMP_CACHE_DATA_RELOAD = "Indicates that data should be reloaded from the path";
        public const string DUMP_CACHE_DATA_EXPIRY = "Specifies how expirations associated with cached items are handled  during reload process.This applies to items added with ABSOLUTE EXPIRATION only. Available options are[none | adjusted | asbefore] i.Adjusted.  (default) Item is added to cache using the remaining expiration of the item. ii.None. Item is added with no expiration. iii.Asbefore. Item is added with original absolute expiration.";
        public const string DUMP_CACHE_DATA_THRESHOLD = "Maximum number of errors that can be tolerated, once this number is reached the tool will exit (default: 1; minimum: 1; maximum: 10000)";
        public const string NOLOGO = "Suppresses display of the logo banner.";
        public const string DUMP_CACHE_KEYS_CACHE = "Specifies name of the cache to be dumped. ";
        public const string DUMP_CACHE_KEYS_KEYCOUNT = "Specifies the number of keys. The default value is 1000. ";
        public const string DUMP_CACHE_KEYS_KEYFILTER = "Specifies the keys that contain this substring. Bydefault it is empty.";
        public const string LIST_CACHES_DETAILS = "Displays detailed information about the cache(s) registered on the server.";
        public const string MONITOR_SERVER_ACTION = " Specifies whether to start or stop monitoring. 'start' should be specified to start monitoring. 'stop' should be specified to stop monitoring";
        public const string ENABLE_SERVER_LOGGER = "Enable error and detailed logging for both socket-server and all clients that are currently connected or will connect in future.";
        public const string DISABLE_SERVER_LOGGER = "Disable error and detailed logging for both socket-server and all clients that are currently connected or will connect in future.";
        public const string STRESS_CACHE = "Name of the cache.";
        public const string STRESS_ITEM_COUNT = " How many total items you want to add. (default: infinite)     ";
        public const string STRESS_TEST_CASE_ITERATIONS = "How many iterations within a test case (default: 20)";
        public const string STRESS_TEST_CASE_ITERATIONS_DELAY = "How much delay (in seconds) between each test case iteration (default: 0)";
        public const string STRESS_GETS_PER_ITERATION = "How many gets within one iteration of a test case (default: 1)";
        public const string STRESS_UPDATES_PER_ITERATION = " How many updates within one iteration of a test case (default: 1)";
        public const string STRESS_ITEM_SIZE = "Specify in bytes the size of each cache item (default: 1024)  ";
        public const string STRESS_SLIDING_EXPIRATION = "Specify in seconds sliding expiration (default: 60; minimum: 15)";
        public const string STRESS_THREAD_COUNT = "How many client threads (default: 1; max: 3)";
        public const string STRESS_REPORT_INTERVAL = "Report after this many total iterations (default: 5000)";


        public const string CLASSNAME = "Specifies the fully qualified class.";
        public const string PARAMETERSLIST = "Specifies the list of the parameters passed.";
        public const string READTHRU = "Specifies if Read Thru is enabled. By default it is false.";
        public const string WRITETHRU = "Specifies if Write thru is enabled. By default it is false.";
        public const string PROVIDERNAME = "Specifies thge provider name.";
        public const string DEFAULT = "Specifies default settings.";
        public const string OPERATIONS_DELAY = "Specifies the delay between operations.";
        public const string BATCH_INTERVAL = "Specifies the interval for batch operations.";
        public const string OPERATIONS_PER_SECOND = "Specifies the number of operations to be performed per second.";
        public const string QUEUE_LIMIT = "Spewcifies the limit of operation queue.";
        public const string CACHENAME = "Specifies the name of the cache whose settings are being configured.";
        public const string ATTRIBUTESLIST = "Specifies the attributes that have to be used while defining query indexes on cache.";
        public const string NONCOMPACT_ATTRIBUTES = "Specifies the attributes that are non-compact.";
        public const string CHUNKSIZE = "Specifies the number of elements a chunk should contain before it is transmitted to combiner or reducer.";
        public const string MAXTASKS = "Specifies the maximum number of tasks to be executed in parallel.";
        public const string MAXEXCEPTIONS = "Specifies maximum avoidable exceptions while executing the task.";
        public const string DONTDEFAULTCOUNTERS = " ";

        public const string ADD_TEST_DATA_CACHENAME = "Specifies name of cache registered on the server.The cache with this name is started on the server.";
        public const string ADD_TEST_DATA_ITEM_COUNT = " Number of items to be added to the cache. By default 10 items are added to the cache. ";
        public const string ADD_TEST_DATA_ITEM_SIZE = "Size in bytes of each item to be added to the cache. By default items of 1k (1024 bytes) are added to the cache.";
        public const string ADD_TEST_DATA_ITEM_ABSOLUTEEXPIRATION = " Specify in seconds, absolute expiration (default: 300; minimum: 15)";

        public const string FORCECLEAR = "Force the clearing of the cache. If not specified, the user is asked before clearing the cache";
        public const string CLEARWEBCONTENT = "Clear JavaScript and CSS only";


        public const string ALIAS = "Alias is like the alternate name of the cache you are using in as a Bridge Cache";
        public const string CACHESERVER = "Cache Server is the node name or ip address of Cache Server";

        public const string ACTIVENODE = "Specifies the bridge active node.";
        public const string PASSIVENODE = "Specifies the bridge passive node.";
        public const string MAXQUEUESIZE = "Specifies an upper limit for the size of queue in MB. Default is 2048 MB";
        public const string QUEUEREPLICATIONINTERVAL = "Specifies time interval in seconds between two consecutive replication operation on target cache. Default is 2 seconds.";
        public const string REPLICATORQUEUESIZE = "Specifies each replicator queue size. Default is 2048 MB";
        public const string CONNECTIONRETRYINTERVAL = "Specifies time interval in seconds between consecutive connection retries to target cache if not connected. Default is 5 seconds.";
        public const string QUEUEOPTIMIZED = "Specifies if queue is optimized. Default is false.";
       
        public const string SERIALIZATIONFORMAT = "Specifies the serialization format of the objects stored in the cache, either Binary or Json (default: Binary).";
        public const string DATAFORMAT = "Specifies the format of objects store in cache, either Serialized or Object (default: Object for Inproc & Serialized fot Outproc)";

        public const string DETAIL = "Specifies the durable subscription details to be included in result.";
        public const string SHOWALL = "Specifies the default and user created topics to be included in result.";
        public const string KEY = "Specifies the license key for NCache.";
        public const string DEACTIVATIONKEY = "Specifies the deactivation key for NCache.";
        public const string FIRSTNAME = "Specifies the first name of the user.";
        public const string LASTNAME = "Specifies the last name of user.";
        public const string EMAILADDRESS = "Specifies email address of the user";
        public const string ENVIRONMENT = "Environment name for which the machine is been activated should be specified here in case of ServerOnly licensing";
        public const string CLIENTS = "Number of client licenses needs to be activated with the specified environment in case of ServerOnly licensing";        
        public const string COMPANYNAME = "Specifies the user's company name.";
        public const string ADDRESS = "Specifies the company's address of the user.";
        public const string CITYNAME = "Specifies the user's company city name.";
        public const string STATE = "Specifies the user's state.";
        public const string COUNTRYNAME = "Specifies the user's country.";
        public const string PHONENUMBER = "Specifies the user's company phone number.";
        public const string AUTHCODE = "Specify the authcode recieved from Alachisoft in order to manually activate this product.";        
        public const string ZIPCODE = "Specifies the user's area zip code.";

		
        #region forcache creation
        public const string REPLICATED_TOPOLOGY_NAME = "replicated";
        public const string PARTITIONED_TOPOLOGY_NAME = "partitioned";
        public const string LOCAL_TOPOLOGY_NAME = "local";
        #endregion
    }

}
