using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.ErrorHandling
{
  public class ErrorCodes
    {
        /*
         Cache general exceptions             10000-14999
         Licensing                            15000-19999
         PubSub                               20000-2499
         CacheLoader
         BackingSource
        
         DataTypes
         Streaming
         Tags and Groups
         Dependencies
         JSon
         Expiration
         Eviction
         Security and Encryption
         Locking
         Events 
         Split brain detection
         Map Reduce
         State Transfer
         Cluster
         */

        public class CacheInit
        {
            public const int SAME_NAME_FOR_BOTH_CACHES = 10000;
            public const int CLUSTER_INIT_IN_INPROC = 10001;
            public const int CANT_START_AS_INPROC=10002;
            public const int CACHE_KEY_NULL=10003;
            public const int CACHE_ID_NULL = 10004;
            public const int INVALID_BIND_IP_CLIENT_CONFIG = 10005;
            public const int CACHE_NOT_INIT = 10006;
            public const int L1_CACHE_NOT_INIT = 10007;
            public const int L2_CACHE_NOT_INIT = 10008;
            public const int SERVER_INFO_NOT_FOUND = 10009;
            public const int CACHE_ID_EMPTY_STRING = 10010;
            public const int CACHE_NOT_REGISTERED_ON_NODE = 10011;
            public const int CACHE_ALREADY_RUNNING = 10012;
            public const int CACHE_ID_NOT_REGISTERED = 10013;
            
        }
        public class Licensing
        {
            public const int INVALID_INSTALLCODE = 15000;
            public const int LICENSE_EXPIRED = 15001;
            public const int INVALID_LICENSE_PROVIDER = 15002;
            public const int LICENSE_STAGE_001 = 15003;
            public const int LICENSE_STAGE_002 = 15004;
            public const int LICENSE_STAGE_003 = 15005;
            public const int LICENSE_STAGE_004 = 15006;
            public const int LICENSE_STAGE_005 = 15007;
            public const int LICENSE_STAGE_006 = 15008;
            public const int LICENSE_STAGE_007 = 15009;
            public const int LICENSE_STAGE_008 = 15010;
            public const int LICENSE_STAGE_009 = 15011;
            public const int LICENSE_STAGE_010 = 15012;
            public const int LICENSE_STAGE_011 = 15013;
            public const int LICENSE_EXPIRED_STAGE_011 = 15014;
            public const int LICENSE_EXPIRED_STAGE_012 = 15015;
            public const int LICENSE_NOT_VERIFIED = 15016;
            public const int EVALUATION_PERIOD_EXPIRED = 15017;
            public const int LICENSING_DLL_MISSING = 15018;
            public const int LICENSING_INFO_CORRUPTED = 15019;
            public const int NCLICENSE_LOAD_FAILURE = 15020;

        }
        public class Common
        {
            public const int NO_SERVER_AVAILABLE = 17500;
            public const int UNEXPECTED_BEHAVIOUR = 17501;
            public const int CONNECTIVITY_LOST = 17502;
            public const int OUTPUT_CORRUPTED_ON_NODE = 17503;
            public const int NUMBER_OF_EVENTS_PROCESSING_THREADS = 17504;
            public const int INVALID_VALUE_ASYNC_EVENT_NOTIF = 17505;
            public const int OPERATION_INTERRUPTED = 17506;
            public const int ENUMERATION_MODIFIED = 17507;
            public const int REQUEST_TIMEOUT = 17508;
            public const int NO_SERVER_AVAILABLE_FOR_CACHE = 17509;
            public const int NULL_VALUE = 17510;
            public const int EMPTY_KEY = 17511;
            public const int DEPENDENCY_KEY_NOT_FOUND = 17512;
            public const int NOT_ENOUGH_ITEMS_EVICTED = 17513;

        }
        public class PubSub
        {
            public const int MESSAGE_ID_ALREADY_EXISTS = 20000;
            public const int TOPIC_NOT_FOUND = 20001;
            public const int DEFAULT_TOPICS = 20002;
            public const int GET_MESSAGE_OPERATION_FAILED = 20003;
            public const int ACKNOWLEDGE_MESSAGE_FAILURE = 20004;
            public const int SUBSCRIPTION_EXISTS = 20005;
            public const int PATTERN_BASED_PUBLISHING_NOT_ALLOWED = 20006;
            public const int TOPIC_DISPOSED = 20007;
            public const int INVALID_TOPIC_NAME = 20008;
            public const int INVALID_TOPIC_PATTERN = 20009;
            public const int NULL_EXCEPTION = 20010;
            public const int TOPIC_OPERATION_FAILED = 20011;
          
        }
    

        #region
        public class Expiration
        {
            public const int ERROR_LOADING_ORACLE_DATA_ACCESS = 30000;
            public const int ABSOLUTE_DEFAULT_EXPIRATION= 30001;
            public const int ABSOLUTE_LONGER_EXPIRATION = 30002;
            public const int SLIDING_DEFAULT_EXPIRATION = 30003;
            public const int SLIDING_LONGER_EXPIRATION = 30004;
            
        }
        #endregion

     
       

   

        #region
        #endregion
     
        #region
        public class Serialization
        {
            public const int ASSEMBLY_VERSION_DIFFERENT = 60000;

        }
        #endregion

        #region
        public class Streaming
        {
            public const int GROUP_SUBGROUP_STREAM_MISMATCH = 65000;
            public const int GROUP_STREAM_MISMATCH = 65001;
        

        }
        #endregion

        #region
        public class BackingSource
        {
            public const int BACKING_SOURCE_NOT_AVAILABLE = 70000;
            public const int SYNCHRONIZATION_WITH_DATASOURCE = 70001;
            public const int SYNCHRONIZATION_CACHE_WITH_DATASOURCE = 70002;
            public const int WRITE_OPERATION_FAILED = 70003;
            public const int IWRITE_THRU_PROVIDER_FAILED = 70004;
            public const int INVALID_IWRITE_THRU_PROVIDER = 70005;
            public const int INVALID_IREAD_THRU_PROVIDER = 70006;

        }
        #endregion
        #region Search operations
        public class SearchOperations
        {
            public const int GET_KEYS_BY_TAG_FAILED = 75000;
            public const int GET_BY_TAGS_FAILED = 75001;
            public const int REMOVE_BY_TAG_FAILED = 75002;
            public const int GROUP_TAGS_ON_SAME_ITEM = 75003;
            public const int KEY_CONFLICTS_WITH_INDEXED_ATTRIBUTE = 75004;
            public const int GROUP_FOR_SUBGROUP = 75005;

        }
        #endregion
        
        #region
        public class BasicCacheOperations
        {
            public const int GET_OPERATION_FAILED = 80000;
            
            public const int KEY_ALREADY_EXISTS = 80002;
            public const int ADD_OPERATION_FAILED = 80003;
            public const int INSERTED_ITEM_DATAGROUP_MISMATCH = 80004;
            public const int REMOVE_OPERATION_FAILED = 80005;
            public const int DELETE_OPERATION_FAILED = 80006;
            public const int INSERT_OPERATION_FAILED = 80007;
            public const int CONTAINS_OPERATION_FAILED = 80008;
            public const int ITEM_LOCKED = 80009;
            public const int ITEM_WITH_VERSION_DOESNT_EXIST = 80010;


        }
        #endregion

        #region
        public class Notifications
        {
            public const int REGISTER_KEY_NOTIFICATION = 85000;
            public const int UNREGISTER_KEY_NOTIFICATION = 85001;
            public const int REGISTER_COLLECTION_NOTIFICATION = 85002;
            public const int UNREGISTER_COLLECTION_NOTIFICATON = 85003;
            public const int REGISTER_TASK_NOTIF_CALLBACK = 85004;
            public const int COLLECTION_EVENTS_ON_NONCOLLECTION = 85005;
            public const int COLLECTION_TYPE_MISMATCH = 85006;


        }
        #endregion
        public class Security
        {
            public const int CLIENT_SERVER_SECURITY_MISMATCH = 90000;

        }
        public class SQLDependency
        {
            public const int INCORRECT_SYNTAX = 95000;
        }
    }
}
