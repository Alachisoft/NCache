using Alachisoft.NCache.Common.DataStructures.Clustered;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.ErrorHandling
{
  public  class ErrorMessages
    {
        private static IDictionary<int, string> _errorMessageMap = new HashVector<int, string>();

        static ErrorMessages()
        {
            #region common exceptions
            _errorMessageMap.Add(ErrorCodes.Common.NO_SERVER_AVAILABLE, "No server is available to process the request.");
            _errorMessageMap.Add(ErrorCodes.Common.OUTPUT_CORRUPTED_ON_NODE, "Output corrupted on node : '{0}'and Exception is :");
            _errorMessageMap.Add(ErrorCodes.Common.NUMBER_OF_EVENTS_PROCESSING_THREADS, "Invalid value specified for NCacheClient.NumberofEventProccesingThreads.");
            _errorMessageMap.Add(ErrorCodes.Common.INVALID_VALUE_ASYNC_EVENT_NOTIF, "Invalid value specified for NCacheClient.AsynchronousEventNotification.");
            _errorMessageMap.Add(ErrorCodes.Common.OPERATION_INTERRUPTED, "Operation has been interrupted due to loss of connectivity between server and client. ");
            _errorMessageMap.Add(ErrorCodes.Common.ENUMERATION_MODIFIED, "Enumeration has been modified");
            _errorMessageMap.Add(ErrorCodes.Common.REQUEST_TIMEOUT, "Request timeout due to node down");
            _errorMessageMap.Add(ErrorCodes.Common.NO_SERVER_AVAILABLE_FOR_CACHE, "No server is available to process the request for '{0}'");
            _errorMessageMap.Add(ErrorCodes.Common.NULL_VALUE, "value cannot be null.");
            _errorMessageMap.Add(ErrorCodes.Common.EMPTY_KEY, "key cannot be empty string");
            _errorMessageMap.Add(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, "One of the dependency keys does not exist.");
            _errorMessageMap.Add(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, "The cache is full and not enough items could be evicted.");
            #endregion

            #region Cache Init Exceptions
            _errorMessageMap.Add(ErrorCodes.CacheInit.SAME_NAME_FOR_BOTH_CACHES, "Same cache-name cannot be specified for both Primary and Client caches.");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CLUSTER_INIT_IN_INPROC, "Cluster cache cannot be initialized in In-Proc mode.");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CANT_START_AS_INPROC, "Cannot start cache of type '{0}' as in-proc cache.");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CACHE_KEY_NULL, "cache key null");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CACHE_ID_NULL, "Cache id can not be null");
            _errorMessageMap.Add(ErrorCodes.CacheInit.INVALID_BIND_IP_CLIENT_CONFIG, "Invalid bind-ip-address specified in client configuration");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CACHE_NOT_INIT, "Cache is not initialized");
            _errorMessageMap.Add(ErrorCodes.CacheInit.L1_CACHE_NOT_INIT, "level1Cache not initialized");
            _errorMessageMap.Add(ErrorCodes.CacheInit.L2_CACHE_NOT_INIT, "level2Cache not initialized");
            _errorMessageMap.Add(ErrorCodes.CacheInit.SERVER_INFO_NOT_FOUND, "'client.ncconf' not found or does not contain server information");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CACHE_ID_EMPTY_STRING, "cacheId cannot be an empty string");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CACHE_NOT_REGISTERED_ON_NODE, "cache with name '{0}' not registered on specified node");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CACHE_ALREADY_RUNNING, "Specified cacheId is already running");
            _errorMessageMap.Add(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED, "Specified cacheId is not registered.");
            #endregion

            #region  Licensing exceptions
            _errorMessageMap.Add(ErrorCodes.Licensing.INVALID_INSTALLCODE, "Invalid Install Code");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_EXPIRED, "NCache license is expired.");
            _errorMessageMap.Add(ErrorCodes.Licensing.INVALID_LICENSE_PROVIDER, "Invalid License provider.");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_001, "Data not found -- Stage 001");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_002, "Product is deactivated -- Stage 002");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_003, "Invalid Authentication Code--Stage03");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_004, "Invalid Authentication Code--Stage04");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_005, "");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_006, "");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_007, "Invalid Authentication Code--Stage07");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_008, "Invalid Authentication Code--Stage08");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_009, "Invalid Authentication Code--Stage09");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_010, "Invalid Authentication Code--Stage10");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_STAGE_011, "Invalid Authentication Code--Stage11");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_EXPIRED_STAGE_011, "License for NCache has been expired.--Stage11");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_EXPIRED_STAGE_012, "License for NCache has been expired--Stage12");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSE_NOT_VERIFIED, "License not verified.'{0}'");
            _errorMessageMap.Add(ErrorCodes.Licensing.EVALUATION_PERIOD_EXPIRED, "Your library has expired the evaluation period of '{0}' days.");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSING_DLL_MISSING, "DLL NOT FOUND EXCEPTION {0}");
            _errorMessageMap.Add(ErrorCodes.Licensing.LICENSING_INFO_CORRUPTED, "EvaluationLicense information is either missing or corrupted.");
            _errorMessageMap.Add(ErrorCodes.Licensing.NCLICENSE_LOAD_FAILURE, "Unable to load nclicense.dll");
            #endregion

            #region PubSub Exceptions
            _errorMessageMap.Add(ErrorCodes.PubSub.MESSAGE_ID_ALREADY_EXISTS, "The specified message id already exists.");
            _errorMessageMap.Add(ErrorCodes.PubSub.TOPIC_NOT_FOUND, "Topic '{0}' does not exists.");
            _errorMessageMap.Add(ErrorCodes.PubSub.DEFAULT_TOPICS, "Operation cannot be performed on default topics.");
            _errorMessageMap.Add(ErrorCodes.PubSub.GET_MESSAGE_OPERATION_FAILED, "Get Message operation failed. Error : '{0}' inner");
            _errorMessageMap.Add(ErrorCodes.PubSub.ACKNOWLEDGE_MESSAGE_FAILURE, "AcknowledgeMessageReceipt operation failed.Error: {0}");
            _errorMessageMap.Add(ErrorCodes.PubSub.SUBSCRIPTION_EXISTS, "Active subscription with this name already exists");
            _errorMessageMap.Add(ErrorCodes.PubSub.PATTERN_BASED_PUBLISHING_NOT_ALLOWED, "Message publishing on pattern based topic is not allowed");
            _errorMessageMap.Add(ErrorCodes.PubSub.TOPIC_DISPOSED, "Topic '{0}' is disposed.");
            _errorMessageMap.Add(ErrorCodes.PubSub.INVALID_TOPIC_NAME, "TopicName is null or empty string");
            _errorMessageMap.Add(ErrorCodes.PubSub.INVALID_TOPIC_PATTERN, "Invalid topic pattern is specified");
            _errorMessageMap.Add(ErrorCodes.PubSub.NULL_EXCEPTION, "");
            _errorMessageMap.Add(ErrorCodes.PubSub.TOPIC_OPERATION_FAILED, "Topic operation failed. Error:  '{0}'"); 


            #endregion

          

            #region Expiration
            _errorMessageMap.Add(ErrorCodes.Expiration.ERROR_LOADING_ORACLE_DATA_ACCESS, "Could not load assembly 'Oracle.DataAccess.dll'. Please make sure Oracle Data Provider for .NET is installed");
            _errorMessageMap.Add(ErrorCodes.Expiration.ABSOLUTE_DEFAULT_EXPIRATION, "Absolute Default expiration value is less than 5 seconds.");
            _errorMessageMap.Add(ErrorCodes.Expiration.ABSOLUTE_LONGER_EXPIRATION, "Absolute Longer expiration value is less than 5 seconds.");
            _errorMessageMap.Add(ErrorCodes.Expiration.SLIDING_DEFAULT_EXPIRATION, "Sliding Default expiration value is less than 5 seconds.");
            _errorMessageMap.Add(ErrorCodes.Expiration.SLIDING_LONGER_EXPIRATION, "Sliding Longer expiration value is less than 5 seconds.");
            #endregion
         
       

            #region streaming
            _errorMessageMap.Add(ErrorCodes.Streaming.GROUP_STREAM_MISMATCH, "Data group of the stream does not match the existing stream's data group");
            _errorMessageMap.Add(ErrorCodes.Streaming.GROUP_SUBGROUP_STREAM_MISMATCH, "Data group/subgroup of the stream does not match the existing stream's data group/subgroup");
           
            #endregion

            #region Serialization
            _errorMessageMap.Add(ErrorCodes.Serialization.ASSEMBLY_VERSION_DIFFERENT, "Loaded assembly version is different from the registered version");
            #endregion

            #region BackingSource
            _errorMessageMap.Add(ErrorCodes.BackingSource.BACKING_SOURCE_NOT_AVAILABLE, "Backing source not available. Verify backing source settings.");
            _errorMessageMap.Add(ErrorCodes.BackingSource.SYNCHRONIZATION_WITH_DATASOURCE, "Error occurred while synchronization with data source: '{0}'");
            _errorMessageMap.Add(ErrorCodes.BackingSource.SYNCHRONIZATION_CACHE_WITH_DATASOURCE, "error while trying to synchronize the cache with data source. Error: '{0}'");
            _errorMessageMap.Add(ErrorCodes.BackingSource.WRITE_OPERATION_FAILED, "Data Source write operation failed. Error: '{0}'");
            _errorMessageMap.Add(ErrorCodes.BackingSource.IWRITE_THRU_PROVIDER_FAILED, "IWriteThruProvider failed.'{0}'");
            _errorMessageMap.Add(ErrorCodes.BackingSource.INVALID_IWRITE_THRU_PROVIDER, "Could not found IWriteThruProvider '{0}'.");
            _errorMessageMap.Add(ErrorCodes.BackingSource.INVALID_IREAD_THRU_PROVIDER, "Could not found IReadThruProvider '{0}'.");
           
            #endregion

            #region Search Operations(tags,groups,subgroups)
            _errorMessageMap.Add(ErrorCodes.SearchOperations.GET_KEYS_BY_TAG_FAILED, "GetKeysByTag operation failed. Error :  '{0}'");
            _errorMessageMap.Add(ErrorCodes.SearchOperations.GET_BY_TAGS_FAILED, "GetByTag operation failed. Error : '{0}'");
            _errorMessageMap.Add(ErrorCodes.SearchOperations.REMOVE_BY_TAG_FAILED, "RemoveByTag operation failed. Error : '{0}'");
            _errorMessageMap.Add(ErrorCodes.SearchOperations.GROUP_TAGS_ON_SAME_ITEM, "You cannot set both groups and tags on the same cache item.");
            _errorMessageMap.Add(ErrorCodes.SearchOperations.KEY_CONFLICTS_WITH_INDEXED_ATTRIBUTE, "Key in named tags conflicts with the indexed attribute name of the specified object.");
            _errorMessageMap.Add(ErrorCodes.SearchOperations.GROUP_FOR_SUBGROUP, "group must be specified for sub group");
            #endregion

            #region Basic operations
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.GET_OPERATION_FAILED, "Get operation failed. Error :  '{0}'");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS, "The specified key already exists.");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.ADD_OPERATION_FAILED, "Add operation failed. Error :  '{0}'");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.INSERTED_ITEM_DATAGROUP_MISMATCH, "Data group of the inserted item does not match the existing item's data group.");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.REMOVE_OPERATION_FAILED, "Remove operation failed. Error : '{0}'");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.DELETE_OPERATION_FAILED, "Delete operation failed. Error : '{0}'");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.INSERT_OPERATION_FAILED, "Insert operation failed. Error : '{0}'");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.CONTAINS_OPERATION_FAILED, "Contains operation failed. Error :  '{0}'");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.ITEM_LOCKED, "Item is locked.");
            _errorMessageMap.Add(ErrorCodes.BasicCacheOperations.ITEM_WITH_VERSION_DOESNT_EXIST, "An item with specified version doesn't exist.");
            #endregion

            #region Notifications
            _errorMessageMap.Add(ErrorCodes.Notifications.REGISTER_KEY_NOTIFICATION, "RegisterKeyNotification failed. Error :  '{0}'");
            _errorMessageMap.Add(ErrorCodes.Notifications.UNREGISTER_KEY_NOTIFICATION, "UnregisterKeyNotification failed. Error :  '{0}'");
            _errorMessageMap.Add(ErrorCodes.Notifications.REGISTER_COLLECTION_NOTIFICATION, "RegisterCollectionNotification failed. Error :  '{0}'");
            _errorMessageMap.Add(ErrorCodes.Notifications.UNREGISTER_COLLECTION_NOTIFICATON, "UnregisterCollectionNotification failed. Error : '{0}'");
            _errorMessageMap.Add(ErrorCodes.Notifications.REGISTER_TASK_NOTIF_CALLBACK, "RegisterTaskNotificationCallback failed. Error :  '{0}'");
            _errorMessageMap.Add(ErrorCodes.Notifications.COLLECTION_EVENTS_ON_NONCOLLECTION, "Cannot register collection events on non-collection item.");
            _errorMessageMap.Add(ErrorCodes.Notifications.COLLECTION_TYPE_MISMATCH, "Collection type mismatch. Notifications cannot be registered via the handle at hand.");
            #endregion

            #region
            _errorMessageMap.Add(ErrorCodes.Security.CLIENT_SERVER_SECURITY_MISMATCH, "Mistmatch between Client - Server connection security detected.The '{0}' node must have Secured-Connection(SSL / TLS) enabled in order to communicate with a {1} node with Secured-Connection.");
            #endregion

            #region
            _errorMessageMap.Add(ErrorCodes.SQLDependency.INCORRECT_SYNTAX, "'{0}'");//"Incorrect syntax near the keyword"
            #endregion

        }





        public static string GetErrorMessage(int errorCode,  params string[] parameters)
        {
            string errormessage = "exception";
            return ResolveError(errorCode, parameters);
        }

        internal static string ResolveError(int errorCode, params string[] parameters)
        {
            string message;
            if (_errorMessageMap.TryGetValue(errorCode, out message))
            {
                if (parameters == null || parameters.Length == 0)
                    return message;
                return String.Format(message, parameters);
            }
            return String.Format("Missing error message for code ({0}) in error to exception map", new object[] { errorCode });
        }

    }
}
