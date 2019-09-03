using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.ErrorHandling;
using RuntimeErrorCodes = Alachisoft.NCache.Runtime.ErrorHandling.ErrorCodes;

namespace Alachisoft.NCache.Client
{
    public class NCacheErrorCodes
    {
       
        //init
        public const int SERVER_INFO_NOT_FOUND = ErrorCodes.CacheInit.SERVER_INFO_NOT_FOUND;//"'client.ncconf' not found or does not contain server information"
        public const int CACHE_ID_EMPTY_STRING = ErrorCodes.CacheInit.CACHE_ID_EMPTY_STRING;//cacheId cannot be an empty string
        public const int CACHE_NOT_REGISTERED_ON_NODE = ErrorCodes.CacheInit.CACHE_NOT_REGISTERED_ON_NODE;//"cache with name '{0}' not registered on specified node"
        public const int CACHE_ID_NOT_REGISTERED = ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED;//"Specified cacheId is not registered."
        public const int CACHE_ALREADY_RUNNING = ErrorCodes.CacheInit.CACHE_ALREADY_RUNNING;//"Specified cacheId is already running"
        public const int CACHE_NOT_INIT = ErrorCodes.CacheInit.CACHE_NOT_INIT;//"Cache is not initialized"
        public const int L1_CACHE_NOT_INIT = ErrorCodes.CacheInit.L1_CACHE_NOT_INIT;//"level1Cache not initialized"
        public const int L2_CACHE_NOT_INIT = ErrorCodes.CacheInit.L2_CACHE_NOT_INIT;//"level2Cache not initialized"
        public const int CACHE_KEY_NULL = ErrorCodes.CacheInit.CACHE_KEY_NULL;//"cache key null"
        public const int CACHE_ID_NULL = ErrorCodes.CacheInit.CACHE_ID_NULL;//"Cache id can not be null"
        public const int SAME_NAME_FOR_BOTH_CACHES = ErrorCodes.CacheInit.SAME_NAME_FOR_BOTH_CACHES;//"Same cache-name cannot be specified for both Primary and Client caches."
        public const int CLUSTER_INIT_IN_INPROC = ErrorCodes.CacheInit.CLUSTER_INIT_IN_INPROC;//"Cluster cache cannot be initialized in In-Proc mode."
        public const int CANT_START_AS_INPROC = ErrorCodes.CacheInit.CANT_START_AS_INPROC;//"Cannot start cache of type '{0}' as in-proc cache."
       
        //common
        public const int NOT_ENOUGH_ITEMS_EVICTED = ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED;//"The cache is full and not enough items could be evicted."
        public const int REQUEST_TIMEOUT = ErrorCodes.Common.REQUEST_TIMEOUT;//"Request timeout due to node down"
        public const int OPERATION_INTERRUPTED = ErrorCodes.Common.OPERATION_INTERRUPTED;//"Operation has been interrupted due to loss of connectivity between server and client. "
        public const int ENUMERATION_MODIFIED = ErrorCodes.Common.ENUMERATION_MODIFIED;//"Enumeration has been modified"
        public const int NO_SERVER_AVAILABLE = ErrorCodes.Common.NO_SERVER_AVAILABLE_FOR_CACHE;
        //pubsub
        public const int SUBSCRIPTION_EXISTS = ErrorCodes.PubSub.SUBSCRIPTION_EXISTS;//Active subscription with this name already exists
        public const int TOPIC_NOT_FOUND = ErrorCodes.PubSub.TOPIC_NOT_FOUND;//"Topic '{0}' does not exists."
        public const int MESSAGE_ID_ALREADY_EXISTS = ErrorCodes.PubSub.MESSAGE_ID_ALREADY_EXISTS;//"The specified message id already exists."
        public const int TOPIC_DISPOSED = ErrorCodes.PubSub.TOPIC_DISPOSED;//"Topic '{0}' is disposed."
        public const int DEFAULT_TOPICS = ErrorCodes.PubSub.DEFAULT_TOPICS;//"Operation cannot be performed on default topics."
        public const int PATTERN_BASED_PUBLISHING_NOT_ALLOWED = ErrorCodes.PubSub.PATTERN_BASED_PUBLISHING_NOT_ALLOWED;//"Message publishing on pattern based topic is not allowed"
       
        //expiration
        public const int ERROR_LOADING_ORACLE_DATA_ACCESS = ErrorCodes.Expiration.ERROR_LOADING_ORACLE_DATA_ACCESS;//"Could not load assembly 'Oracle.DataAccess.dll'. Please make sure Oracle Data Provider for .NET is installed"
        public const int ABSOLUTE_DEFAULT_EXPIRATION = ErrorCodes.Expiration.ABSOLUTE_DEFAULT_EXPIRATION;//"Absolute Default expiration value is less than 5 seconds."
        public const int ABSOLUTE_LONGER_EXPIRATION = ErrorCodes.Expiration.ABSOLUTE_LONGER_EXPIRATION;//"Absolute Longer expiration value is less than 5 seconds."
        public const int SLIDING_DEFAULT_EXPIRATION = ErrorCodes.Expiration.SLIDING_DEFAULT_EXPIRATION;//"Sliding Default expiration value is less than 5 seconds."
        public const int SLIDING_LONGER_EXPIRATION = ErrorCodes.Expiration.SLIDING_LONGER_EXPIRATION;// "Sliding Longer expiration value is less than 5 seconds."

        //basic operations
        public const int KEY_ALREADY_EXISTS = ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS;//"The specified key already exists."
        public const int BACKING_SOURCE_NOT_AVAILABLE = ErrorCodes.BackingSource.BACKING_SOURCE_NOT_AVAILABLE;//"Backing source not available. Verify backing source settings"
        public const int INSERTED_ITEM_DATAGROUP_MISMATCH = ErrorCodes.BasicCacheOperations.INSERTED_ITEM_DATAGROUP_MISMATCH;//"Data group of the inserted item does not match the existing item's data group."
        public const int ITEM_LOCKED = ErrorCodes.BasicCacheOperations.ITEM_LOCKED;//"Item is locked."
        public const int ITEM_WITH_VERSION_DOESNT_EXIST = ErrorCodes.BasicCacheOperations.ITEM_WITH_VERSION_DOESNT_EXIST;//"An item with specified version doesn't exist."

        //streaming
        public const int GROUP_SUBGROUP_STREAM_MISMATCH = ErrorCodes.Streaming.GROUP_SUBGROUP_STREAM_MISMATCH;//"Data group/subgroup of the stream does not match the existing stream's data group/subgroup"
        public const int GROUP_STREAM_MISMATCH = ErrorCodes.Streaming.GROUP_STREAM_MISMATCH;//"Data group of the stream does not match the existing stream's data group"

      
        //backing source
        public const int SYNCHRONIZATION_WITH_DATASOURCE_FAILED = ErrorCodes.BackingSource.SYNCHRONIZATION_WITH_DATASOURCE;//"Error occurred while synchronization with data source: '{0}'"
        public const int INVALID_IWRITETHRU_PROVIDER_NAME = ErrorCodes.BackingSource.INVALID_IWRITE_THRU_PROVIDER;
        public const int INVALID_IREADTHRU_PROVIDER_NAME = ErrorCodes.BackingSource.INVALID_IREAD_THRU_PROVIDER;

        //notifications
        public const int COLLECTION_TYPE_MISMATCH = ErrorCodes.Notifications.COLLECTION_TYPE_MISMATCH;// "Collection type mismatch. Notifications cannot be registered via the handle at hand."
        public const int COLLECTION_EVENTS_ON_NONCOLLECTION = ErrorCodes.Notifications.COLLECTION_EVENTS_ON_NONCOLLECTION;//"Cannot register collection events on non-collection item."

        //json 
        public const int ATTRIBUTE_ALREADY_EXISTS = RuntimeErrorCodes.Json.ATTRIBUTE_ALREADY_EXISTS;// "An attribute with the same name aleady exists."
        public const int REFERENCE_TO_PARENT = RuntimeErrorCodes.Json.REFERENCE_TO_PARENT;//"Reference to parent at nested level detected."
        public const int REFERENCE_TO_SELF = RuntimeErrorCodes.Json.REFERENCE_TO_SELF;//"'{0}' cannot contain an attribute that is a reference to self."
        //search operations
        public const int KEY_CONFLICTS_WITH_INDEXED_ATTRIBUTE = ErrorCodes.SearchOperations.KEY_CONFLICTS_WITH_INDEXED_ATTRIBUTE;// "Key in named tags conflicts with the indexed attribute name of the specified object."
        public const int GROUP_TAGS_ON_SAME_ITEM = ErrorCodes.SearchOperations.GROUP_TAGS_ON_SAME_ITEM;//"You cannot set both groups and tags on the same cache item."
        
    }
}
