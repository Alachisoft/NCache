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
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class CommandResponse
    {
        /// <summary> </summary>		
        private string _key;

        /// <summary> Represents the _type of the response.</summary>
        private Alachisoft.NCache.Common.Protobuf.Response.Type _type;

        /// <summary> Requested requestId of the command.</summary>
        private long _requestId = 0;

        private string _intendedRecipient = "";

        private int _removedKeyCount;

        /// <summary> CallbackId which will be used to get actual callback from callback table</summary>
        private short _callbackId = 0;

        /// <summary> Remove reason if item is removed.</summary>
        private int _reason;

        /// <summary> Object</summary>
        private byte[] _value;

        private IList _dataList;

        private int _bytesRead;

        private int _streamLength;

        private CompressedValueEntry _flagValueEntry = new CompressedValueEntry();

        /// <summary>Notification Id</summary>
        private byte[] _notifId;

        /// <summary>Tells if the broker is reset due to lost connection</summary>
        private bool _brokerReset = false;


        /// <summary>Tells with which ip connection is broken</summary>
        private Common.Net.Address _resetConnectionIP;

        public Common.Net.Address ResetConnectionIP
        {
            get { return _resetConnectionIP; }
            internal set { _resetConnectionIP = value; }
        }

        /// <summary></summary>
        private object _asyncOpResult = null;

        /// <summary></summary>
        private OpCode _operationCode;

        /// <summary></summary>
        private string _cacheId = null;

        /// <summary> Hold the result bulk operations </summary>
        private Hashtable _resultDic = new Hashtable();

        /// <summary> Hold the pub-sub messages </summary>
        private IDictionary<string, IList<MessageItem>> _messageDic = new HashVector<string, IList<MessageItem>>();

        //Hold patterns subscribed by client
        private List<string> _registeredPatterns = new List<string>();

        /// <summary> Hold the versions for add/insert bulk operations </summary>
        private Hashtable _versionDic = new Hashtable();

        /// <summary>Hold the getGroupKeys from search and get getGroupKeys commands</summary>
        private ArrayList _resultList = new ArrayList();

        /// <summary>Hold bucket size returned from server</summary>
        private int _bucketSize = 0;

        /// <summary>Hold total number of buckets count</summary>
        private int _totalBuckets = 0;

        private bool _isAuthorized = false;
        /// <summary> CacheItem object used to return CacheItem form GetCacheItem command</summary>

        private CacheItem _cacheItem = null;

        private IPAddress _clusterIp;
        private int _clusterPort;

        private Dictionary<string, int> _runningServers = new Dictionary<string, int>();
        private List<BulkEventItemResponse> _eventList;
        private PollingResult pollingResult = null;
        internal PollingResult PollingResult
        {
            get { return pollingResult; }
        }


        private List<Config.Mapping> _serverMappingList;
        private int _commandID;
        private long _itemAddVersion;
        private EntryType _entryType;


        public int CommandID
        {
            get { return _commandID; }
            set { _commandID = value; }
        }
        
        private IList<Runtime.Caching.ClientInfo> _connectedClients;


        public EntryType EntryType
        {
            get { return _entryType; }
            set
            {
                _entryType = value;
            }
        }
       
        private System.Net.IPAddress _serverIp;
        private int _serverPort;
        private int _cacheManagementPort;
        private bool _reconnectClients = false;

        private ExceptionType _excType;

        private TypeInfoMap _completeTypeMap;

        private int _errorCode;
        private string _stackTrace;
        private long _compressionThrehold;
        private bool _compressionEnabled;

        private HotConfig _hotConfig;

        private object _lockId;
        private DateTime _lockDate;
        private bool _parseLockingInfo;
        private bool _lockAcquired;
        private bool _isLocked;
        private ulong _itemVersion;

        private string _cacheType;
        

        private bool _enableErrorLogs;
        private bool _enableDetailedLogs;
        private string _exceptionString;
        internal bool _operationSuccess;

        private InquiryRequestResponse _inquiryResponse;
        private bool _requestInquiryEnabled;
        private bool _secureConnectionEnabled;

        private bool _exists = false;
        private long _count = 0;
        private long _messageCount = 0;

        private NewHashmap _newHashmap = null;

        private string _queryId;

      




        private List<EnumerationDataChunk> _enumerationDataChunk = new List<EnumerationDataChunk>();

        Response _response;

        Alachisoft.NCache.Caching.EventId _eventId;
        EventDataFilter _dataFilter = EventDataFilter.None;


        private bool _isPersistEnabled = false;
        private int _persistenceInterval;

        public bool IsPersistenceEnabled
        {
            get { return _isPersistEnabled; }
        }

        public int PersistInterval
        {
            get { return _persistenceInterval; }
        }

        public Alachisoft.NCache.Caching.EventId EventId
        {
            get { return _eventId; }
        }

        public Response ProtobufResponse
        {
            get { return _response; }
        }

        internal bool RequestInquiryEnabled
        {
            get { return _requestInquiryEnabled; }
        }

        internal InquiryRequestResponse InquiryResponse
        {
            get { return this._inquiryResponse; }
        }

        internal bool SecureConnectionEnabled { get { return _secureConnectionEnabled; } }

        public int RemovedKeyCount
        {
            get { return _removedKeyCount; }
            set { _removedKeyCount = value; }
        }

        public bool IsAuthorized
        {
            get { return _isAuthorized; }
            set { _isAuthorized = value; }
        }


        public EventDataFilter DataFilter { get { return _dataFilter; } }
        /// <summary>
        /// by default one response is sent back for each request. If required, a single response
        /// can be segmented into smaller chunks. In that case, these properties must be properly set.
        /// </summary>
        private int _sequenceId = 1;

        private int _numberOfChunks = 1;
        private int _taskCallbackId;
        private string taskFailureReason = null;
        public int TaskCallbackId
        {
            get { return _taskCallbackId; }
        }
        private string _taskId;

        public string TaskId
        {
            get { return _taskId; }
        }
        private int _taskStatus;

        public int TaskStatus
        {
            get { return _taskStatus; }
        }

        private bool _isRunning = false;
        private SerializationFormat _serializationFormat;

        internal int SequenceId
        {
            get { return _sequenceId; }
        }

        internal int NumberOfChunks
        {
            get { return _numberOfChunks; }
        }

        internal string CacheType
        {
            get { return _cacheType; }
        }
        

        internal TypeInfoMap TypeMap
        {
            get { return _completeTypeMap; }
            set { _completeTypeMap = value; }
        }


        internal bool IsLocked
        {
            get { return _isLocked; }
        }

        internal bool OperationWasuccessfull()
        {
            return _operationSuccess;
        }

        internal bool LockAcquired
        {
            get { return _lockAcquired; }
        }

        internal HotConfig HotConfig
        {
            get { return _hotConfig; }
        }

        internal long CompressionThreshold
        {
            get { return _compressionThrehold; }
        }

        internal bool CompressionEnabled
        {
            get { return _compressionEnabled; }
        }

        internal SerializationFormat SerializationFormat
        {
            get { return _serializationFormat; }
        }

        internal Hashtable KeyValueDic
        {
            get { return _resultDic; }
            set { _resultDic = value; }
        }

        internal IDictionary<string, IList<MessageItem>> MessageDic
        {
            get { return _messageDic; }
            set { _messageDic = value; }
        }

        internal List<string> RegisteredPatterns
        {
            get { return _registeredPatterns; }
            set { _registeredPatterns = value; }

        }
        internal Hashtable KeyVersionDic
        {
            get { return _versionDic; }
            set { _versionDic = value; }
        }

        internal ArrayList KeyList
        {
            get { return _resultList; }
            set { _resultList = value; }
        }

        internal List<EnumerationDataChunk> NextChunk
        {
            get { return _enumerationDataChunk; }
            set { _enumerationDataChunk = value; }
        }

        internal Alachisoft.NCache.Common.Protobuf.Response.Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        internal string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        internal object AsyncOpResult
        {
            get { return _asyncOpResult; }
        }

        internal ExceptionType ExceptionType
        {
            get { return _excType; }
        }

        internal string ExceptionMsg
        {
            get { return _exceptionString; }
        }

        internal OpCode OperationType
        {
            get { return _operationCode; }
        }
        
        internal System.Net.IPAddress ServerIPAddress
        {
            get { return _serverIp; }
            set { _serverIp = value; }
        }

        internal Dictionary<string, int> RunningServer
        {
            get
            {
                if (_runningServers != null)
                    return _runningServers;
                else
                    return new Dictionary<string, int>();
            }
        }

        internal List<Config.Mapping> ServerMappingList
        {
            get
            {
                if (_serverMappingList != null)
                    return _serverMappingList;
                else
                    return new List<Config.Mapping>();
            }

            set { _serverMappingList = value; }
        }

        internal List<BulkEventItemResponse> EventList
        {
            get
            {
                if (_eventList != null)
                    return _eventList;
                else
                    return new List<BulkEventItemResponse>();

            }
        }

        public IList<Runtime.Caching.ClientInfo> ConnectedClients { get { return _connectedClients; } }

        //////////////// changing here for cache mangement port

        internal int CacheMangementPort
        {
            get { return _cacheManagementPort; }
        }


        internal int ServerPort
        {
            get { return _serverPort; }
            set { _serverPort = value; }
        }

        internal object LockId
        {
            get { return _lockId; }
        }

        internal DateTime LockDate
        {
            get { return _lockDate; }
        }

        internal ulong ItemVersion
        {
            get { return _itemVersion; }
        }

        internal System.Net.IPAddress ClusterIPAddress
        {
            get { return _clusterIp; }
        }

        internal IList DataList
        {
            get { return _dataList; }
        }
        internal int ClusterPort
        {
            get { return _clusterPort; }
        }
        internal int BytesRead
        {
            get { return _bytesRead; }
        }
        internal int StreamLegnth
        {
            get { return _streamLength; }
        }

        /// <summary>
        /// Get size of each bucket
        /// </summary>
        internal int BucketSize
        {
            get { return this._bucketSize; }
        }

        internal bool ReconnectClients
        {
            get { return _reconnectClients; }
        }

        /// <summary>
        /// Get total number of buckets
        /// </summary>
        internal int TotalBuckets
        {
            get { return this._totalBuckets; }
        }

        internal bool Exists
        {
            get { return this._exists; }
        }

        internal long Count
        {
            get { return this._count; }
        }

        internal long MessageCount
        {
            get { return this._messageCount; }
        }

        internal NewHashmap Hashmap
        {
            get { return this._newHashmap; }
        }

        

        public EventTypeInternal EventType { get; set; }

        internal Response Result
        {
            set
            {
                List<string> keys = null;
                Alachisoft.NCache.Common.DataStructures.EnumerationPointer pointer = null;
                Alachisoft.NCache.Common.DataStructures.EnumerationDataChunk chunk = null;

                _type = value.responseType;
                _sequenceId = value.sequenceId;
                _numberOfChunks = value.numberOfChuncks;
                _commandID = value.commandID;


                switch (value.responseType)
                {
                    case Response.Type.ADD:
                        _itemVersion = (ulong)value.addResponse.itemversion;
                        _requestId = value.requestId;
                        break;
                    case Response.Type.REMOVE_GROUP:
                    case Response.Type.DELETE:
                    case Response.Type.DELETE_BULK:
                    case Response.Type.REGISTER_NOTIF:
                    case Response.Type.CLEAR:
                    case Response.Type.UNREGISTER_CQ:
                    case Response.Type.RAISE_CUSTOM_EVENT:
                    case Response.Type.REGISTER_KEY_NOTIF:
                    case Response.Type.REGISTER_BULK_KEY_NOTIF:
                    case Response.Type.UNREGISTER_BULK_KEY_NOTIF:
                    case Response.Type.UNREGISTER_KEY_NOTIF:
                    case Response.Type.UNLOCK:
                    case Response.Type.DISPOSE:
                    case Response.Type.DISPOSE_READER:
                    case Response.Type.REGISTER_POLL_NOTIF:
                    case Response.Type.TOUCH:
                    case Response.Type.MESSAGE_PUBLISH:
                    case Response.Type.MESSAGE_ACKNOWLEDGEMENT:
                        _requestId = value.requestId;
                        _commandID = value.commandID;
                        break;
                        
                    case Response.Type.COMPACT_TYPE_REGISTER_EVENT:
                        _value = value.compactTypeRegisterEvent.compactTypes;
                        break;

                    case Response.Type.COUNT:
                        _requestId = value.requestId;
                        this._count = value.count.count;
                        break;

                    case Response.Type.MESSAGE_COUNT:
                        _requestId = value.requestId;
                        this._messageCount = value.messageCountResponse.messageCount;
                        break;
                        
                    case Response.Type.ADD_ATTRIBUTE:
                        _requestId = value.requestId;
                        _operationSuccess = value.addAttributeResponse.success;
                        break;

                    case Response.Type.CONTAINS_BULK:
                        _requestId = value.requestId;
                        _resultDic = CompactBinaryFormatter.FromByteBuffer(value.containBulkResponse.exists, null) as Hashtable;
                        break;

                    case Response.Type.CONTAINS:
                        _requestId = value.requestId;
                        _exists = value.contain.exists;
                        break;

                    case Response.Type.CONFIG_MODIFIED_EVENT:
                        _hotConfig = HotConfig.FromString(value.configModified.hotConfig);
                        break;

                    case Response.Type.POLL_NOTIFY_CALLBACK:
                        _callbackId = (short)value.pollNotifyEventResponse.callbackId;
                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.pollNotifyEventResponse.eventId.eventUniqueId;
                        _eventId.EventCounter = value.pollNotifyEventResponse.eventId.eventCounter;
                        _eventId.OperationCounter = value.pollNotifyEventResponse.eventId.operationCounter;
                        _eventId.EventType = Persistence.EventType.POLL_REQUEST_EVENT;
                        EventType = (EventTypeInternal)value.pollNotifyEventResponse.eventType;
                        break;

                    case Response.Type.ITEM_UPDATED_CALLBACK:
                        _callbackId = (short)value.itemUpdatedCallback.callbackId;
                        _key = value.itemUpdatedCallback.key;
                        _dataFilter = (EventDataFilter)value.itemUpdatedCallback.dataFilter;
                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemUpdatedCallback.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemUpdatedCallback.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemUpdatedCallback.eventId.operationCounter;
                        _eventId.EventType = Persistence.EventType.ITEM_UPDATED_CALLBACK;
                        break;

                    case Response.Type.ITEM_REMOVED_CALLBACK:
                        _callbackId = (short)value.itemRemovedCallback.callbackId;
                        _key = value.itemRemovedCallback.key;
                        _reason = value.itemRemovedCallback.itemRemoveReason;
                        _flagValueEntry.Flag = new BitSet((byte)value.itemRemovedCallback.flag);
                        _dataFilter = (EventDataFilter)value.itemRemovedCallback.dataFilter;

                        if (value.itemRemovedCallback.value != null && value.itemRemovedCallback.value.Count > 0)
                        {
                            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value.itemRemovedCallback.value.ToArray());
                            Value = ubObject.GetFullObject();
                        }

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemRemovedCallback.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemRemovedCallback.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemRemovedCallback.eventId.operationCounter;
                        _eventId.EventType = Persistence.EventType.ITEM_REMOVED_CALLBACK;

                        break;

                    case Response.Type.ASYNC_OP_COMPLETED_CALLBACK:
                        _requestId = value.asyncOpCompletedCallback.requestId;
                        _key = value.asyncOpCompletedCallback.key;

                        if (value.asyncOpCompletedCallback.success)
                            _asyncOpResult = Alachisoft.NCache.Caching.AsyncOpResult.Success;
                        else
                            _asyncOpResult = new System.Exception(value.asyncOpCompletedCallback.exc.exception);

                        break;

      

                    case Response.Type.ITEM_ADDED_EVENT:
                        _key = value.itemAdded.key;

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemAdded.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemAdded.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemAdded.eventId.operationCounter;
                        _eventId.EventType = Alachisoft.NCache.Persistence.EventType.ITEM_ADDED_EVENT;
                        break;

                    case Response.Type.ITEM_UPDATED_EVENT:
                        _key = value.itemUpdated.key;

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemUpdated.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemUpdated.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemUpdated.eventId.operationCounter;
                        _eventId.EventType = Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_EVENT;
                        break;

                    case Response.Type.CACHE_STOPPED_EVENT:
                        _cacheId = value.cacheStopped.cacheId;
                        break;

                    case Response.Type.ITEM_REMOVED_EVENT:
                        _key = value.itemRemoved.key;
                        _reason = value.itemRemoved.itemRemoveReason;
                        _flagValueEntry.Flag = new BitSet((byte)value.itemRemoved.flag);
                        if (value.itemRemoved.value != null && value.itemRemoved.value.Count > 0)
                        {
                            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value.itemRemoved.value.ToArray());
                            Value = ubObject.GetFullObject();
                        }

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemRemoved.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemRemoved.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemRemoved.eventId.operationCounter;
                        _eventId.EventType = Alachisoft.NCache.Persistence.EventType.ITEM_REMOVED_EVENT;
                        break;

                    case Response.Type.CACHE_CLEARED_EVENT:
                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.cacheCleared.eventId.eventUniqueId;
                        _eventId.EventCounter = value.cacheCleared.eventId.eventCounter;
                        _eventId.OperationCounter = value.cacheCleared.eventId.operationCounter;
                        _eventId.EventType = Alachisoft.NCache.Persistence.EventType.CACHE_CLEARED_EVENT;
                        break;

                    case Response.Type.CUSTOM_EVENT:
                        _notifId = value.customEvent.key;
                        _value = value.customEvent.value;
                        break;



                    case Response.Type.GET_OPTIMAL_SERVER:
                        _requestId = value.requestId;
                        _serverIp = System.Net.IPAddress.Parse(value.getOptimalServer.server);
                        _serverPort = value.getOptimalServer.port;
                        break;

                    case Response.Type.GET_CACHE_MANAGEMENT_PORT:
                        _requestId = value.requestId;
                        _serverIp = System.Net.IPAddress.Parse(value.getCacheManagementPortResponse.server);
                        _cacheManagementPort = value.getCacheManagementPortResponse.port;
                        break;

                    case Response.Type.GET_CACHE_BINDING:
                        _requestId = value.requestId;
                        _serverIp = System.Net.IPAddress.Parse(value.getCacheBindingResponse.server);
                        _serverPort = value.getCacheBindingResponse.port;
                        break;

                    case Response.Type.GET_RUNNING_SERVERS:
                        _requestId = value.requestId;
                        if (value.getRunningServer.keyValuePair != null)
                        {
                            foreach (Common.Protobuf.KeyValuePair pair in value.getRunningServer.keyValuePair)
                            {
                                _runningServers.Add(pair.key, Convert.ToInt32(pair.value));
                            }
                        }
                        break;


                    case Response.Type.GET_SERVER_MAPPING:
                        _requestId = value.requestId;
                        if (value.getServerMappingResponse.serverMapping != null)
                        {
                            _serverMappingList = new List<Config.Mapping>();
                            foreach (Common.Protobuf.ServerMapping mappingObject in value.getServerMappingResponse.serverMapping)
                            {
                                Config.Mapping serverMapping = new Config.Mapping();
                                //For further usage this Config.Mapping object will be used.
                                serverMapping.PrivateIP = mappingObject.privateIp;
                                serverMapping.PrivatePort = mappingObject.privatePort;
                                serverMapping.PublicIP = mappingObject.publicIp;
                                serverMapping.PublicPort = mappingObject.publicPort;

                                _serverMappingList.Add(serverMapping);
                            }
                        }

                        break;

                    case Response.Type.BULK_EVENT:
                        if (value.bulkEventResponse.eventList != null)
                        {
                            _eventList = value.bulkEventResponse.eventList;
                        }
                        break;

                    case Response.Type.NODE_JOINED_EVENT:
                        _clusterIp = System.Net.IPAddress.Parse(value.nodeJoined.clusterIp);
                        _clusterPort = Convert.ToInt32(value.nodeJoined.clusterPort);
                        _serverIp = System.Net.IPAddress.Parse(value.nodeJoined.serverIp);
                        _serverPort = Convert.ToInt32(value.nodeJoined.serverPort);
                        _reconnectClients = value.nodeJoined.reconnect;
                        break;

                    case Response.Type.NODE_LEFT_EVENT:
                        _clusterIp = System.Net.IPAddress.Parse(value.nodeLeft.clusterIp);
                        _clusterPort = Convert.ToInt32(value.nodeLeft.clusterPort);
                        _serverIp = System.Net.IPAddress.Parse(value.nodeLeft.serverIp);
                        _serverPort = Convert.ToInt32(value.nodeLeft.serverPort);
                        break;

                    case Response.Type.INSERT:
                        _requestId = value.requestId;
                        _itemVersion = value.insert.version;
                        break;

                    case Response.Type.INIT:
                        _requestId = value.requestId;
                        _cacheType = value.initCache.cacheType;
                        _isPersistEnabled = value.initCache.isPersistenceEnabled;
                        _persistenceInterval = value.initCache.persistenceInterval;
                        _requestInquiryEnabled = value.initCache.requestLoggingEnabled;
                        _secureConnectionEnabled = value.initCache.secureConnectionEnabled;
                        _response = value;
                        break;

                    case Response.Type.GET:
                        _requestId = value.requestId;
                        _flagValueEntry.Flag = new BitSet((byte)value.get.flag);
                        _lockId = String.IsNullOrEmpty(value.get.lockId) ? null : value.get.lockId;
                        _lockDate = new DateTime(value.get.lockTime);
                        _itemVersion = value.get.version;
                        EntryType = MiscUtil.ProtoItemTypeToEntryType(value.get.itemType); // (EntryType)value.get.itemType;

                        if (value.get.data.Count > 0)
                        {
                            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value.get.data.ToArray());
                            _value = ubObject.GetFullObject();
                        }
                        break;

                    case Response.Type.REMOVE:
                        _requestId = value.requestId;
                        _flagValueEntry.Flag = new BitSet((byte)value.remove.flag);
                        EntryType = MiscUtil.ProtoItemTypeToEntryType(value.remove.itemType);

                        if (value.remove.value != null && value.remove.value.Count > 0)
                        {
                            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value.remove.value.ToArray());
                            _value = ubObject.GetFullObject();
                        }
                        break;

                    case Response.Type.LOCK:
                        _requestId = value.requestId;
                        _lockAcquired = value.lockResponse.locked;
                        _lockId = String.IsNullOrEmpty(value.lockResponse.lockId) ? null : value.lockResponse.lockId;
                        _lockDate = new DateTime(value.lockResponse.lockTime);
                        break;

                    case Response.Type.ISLOCKED:
                        _requestId = value.requestId;
                        _isLocked = value.isLockedResponse.isLocked;
                        _lockId = String.IsNullOrEmpty(value.isLockedResponse.lockId) ? null : value.isLockedResponse.lockId;
                        _lockDate = new DateTime(value.isLockedResponse.lockTime);
                        break;

                    case Response.Type.GET_LOGGING_INFO:
                        _requestId = value.requestId;
                        _enableErrorLogs = value.getLoggingInfoResponse.errorsEnabled;
                        _enableDetailedLogs = value.getLoggingInfoResponse.detailedErrorsEnabled;
                        break;

                    case Response.Type.LOGGING_INFO_MODIFIED_EVENT:
                        _enableErrorLogs = value.loggingInfoModified.enableErrorsLog;
                        _enableDetailedLogs = value.loggingInfoModified.enableDetailedErrorsLog;
                        break;

                    case Response.Type.GET_BULK:
                        _requestId = value.requestId;
                        _intendedRecipient = value.intendedRecipient;

                        KeyValuePackageResponse keyValuePackage = value.bulkGet.keyValuePackage;
                        if (keyValuePackage != null)
                        {
                            for (int i = 0; i < keyValuePackage.keys.Count; i++)
                            {

                                string key = keyValuePackage.keys[i];

                                Value val = keyValuePackage.values[i];

                                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());

                                _flagValueEntry = new CompressedValueEntry();
                                _flagValueEntry.Flag = new BitSet((byte)keyValuePackage.flag[i]);
                                _flagValueEntry.Type = MiscUtil.ProtoItemTypeToEntryType(keyValuePackage.itemType[i]);

                                _flagValueEntry.Value = ubObject.GetFullObject();

                                _resultDic.Add(key, _flagValueEntry);

                                _flagValueEntry = null;
                            }
                        }
                        break;

                    case Response.Type.BULK_GET_CACHEITEM:
                        _requestId = value.requestId;

                        BulkGetCacheItemResponse bulkGetCacheItemResponse = value.bulkGetCacheItem;

                        if (bulkGetCacheItemResponse != null)
                        {
                            foreach (KeyCacheItemPair keyCacheItemPair in bulkGetCacheItemResponse.KeyCacheItemPairs)
                            {
                                string key = keyCacheItemPair.key;
                                GetCacheItemResponse resp = keyCacheItemPair.cacheItem;
                                _resultDic.Add(key, GetCacheItem(resp));
                            }
                        }
                        break;

                    case Response.Type.REMOVE_BULK:
                        _requestId = value.requestId;
                        _intendedRecipient = value.intendedRecipient;

                        keyValuePackage = value.bulkRemove.keyValuePackage;
                        if (keyValuePackage != null)
                        {
                            for (int i = 0; i < keyValuePackage.keys.Count; i++)
                            {
                                string key = keyValuePackage.keys[i];

                                Value val = keyValuePackage.values[i];
                                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                byte[] bytes = ubObject.GetFullObject();

                                _flagValueEntry = new CompressedValueEntry();
                                _flagValueEntry.Flag = new BitSet((byte)keyValuePackage.flag[i]);
                                _flagValueEntry.Type = MiscUtil.ProtoItemTypeToEntryType(keyValuePackage.itemType[i]); //(EntryType)keyValuePackage.itemType[i];
                                _flagValueEntry.Value = ubObject.GetFullObject();

                                _resultDic.Add(key, _flagValueEntry);

                                _flagValueEntry = null;
                            }
                        }
                        break;



                    case Response.Type.EXECUTE_READER:
                    case Response.Type.EXECUTE_READER_CQ:
                    case Response.Type.GET_READER_CHUNK:
                        _requestId = value.requestId;
                        _response = value;
                        break;

                    case Response.Type.ADD_BULK:
                        _requestId = value.requestId;
                        _intendedRecipient = value.intendedRecipient;

                        Alachisoft.NCache.Common.Protobuf.KeyExceptionPackageResponse keyExceptions = value.bulkAdd.keyExceptionPackage;
                        if (keyExceptions != null)
                        {
                            for (int i = 0; i < keyExceptions.keys.Count; i++)
                            {
                                string key = keyExceptions.keys[i];
                                Alachisoft.NCache.Common.Protobuf.Exception exc = keyExceptions.exceptions[i];
                                _resultDic.Add(key, new OperationFailedException(exc.errorCode,exc.message));
                            }
                        }
                        Alachisoft.NCache.Common.Protobuf.KeyVersionPackageResponse keyVersions = value.bulkAdd.keyVersionPackage;
                        if (keyVersions != null)
                        {
                            for (int i = 0; i < keyVersions.keys.Count; i++)
                            {
                                string key = keyVersions.keys[i];
                                ulong ver = keyVersions.versions[i];
                                _versionDic.Add(key, ver);
                            }
                        }
                        break;

                    case Response.Type.INSERT_BULK:
                        _requestId = value.requestId;
                        _intendedRecipient = value.intendedRecipient;

                        keyExceptions = value.bulkInsert.keyExceptionPackage;
                        if (keyExceptions != null)
                        {
                            for (int i = 0; i < keyExceptions.keys.Count; i++)
                            {
                                string key = keyExceptions.keys[i];
                                Alachisoft.NCache.Common.Protobuf.Exception exc = keyExceptions.exceptions[i];
                                _resultDic.Add(key, new OperationFailedException(exc.errorCode,exc.message));
                            }
                        }
                        keyVersions = value.bulkInsert.keyVersionPackage;
                        if (keyVersions != null)
                        {
                            for (int i = 0; i < keyVersions.keys.Count; i++)
                            {
                                string key = keyVersions.keys[i];
                                ulong ver = keyVersions.versions[i];
                                _versionDic.Add(key, ver);
                            }
                        }
                        break;

                    case Response.Type.GET_ENUMERATOR:
                        _requestId = value.requestId;
                        _resultList.AddRange(value.getEnum.keys);
                        break;

                    case Response.Type.INQUIRY_REQUEST_RESPONSE:
                        _requestId = value.requestId;
                        _inquiryResponse = (InquiryRequestResponse)value.inquiryRequestResponse;
                        break;


                    case Response.Type.GET_NEXT_CHUNK:
                        _requestId = value.requestId;
                        _intendedRecipient = value.intendedRecipient;

                        pointer = EnumerationPointerConversionUtil.GetFromProtobufEnumerationPointer(value.getNextChunkResponse.enumerationPointer);
                        keys = value.getNextChunkResponse.keys;

                        chunk = new EnumerationDataChunk();
                        chunk.Data = keys;
                        chunk.Pointer = pointer;
                        _enumerationDataChunk.Add(chunk);
                        break;

                    case Response.Type.GET_GROUP_NEXT_CHUNK:
                        _requestId = value.requestId;

                        pointer = EnumerationPointerConversionUtil.GetFromProtobufGroupEnumerationPointer(value.getGroupNextChunkResponse.groupEnumerationPointer);
                        keys = value.getGroupNextChunkResponse.keys;

                        chunk = new EnumerationDataChunk();
                        chunk.Data = keys;
                        chunk.Pointer = pointer;
                        _enumerationDataChunk.Add(chunk);
                        break;

                    case Response.Type.GET_CACHE_ITEM:

                        _requestId = value.requestId;

                        if (!string.IsNullOrEmpty(value.getItem.lockId))
                            _lockId = value.getItem.lockId;

                        _lockDate = new DateTime(value.getItem.lockTicks);
                        if (value.getItem != null && value.getItem.value.Count > 0)
                        {
                            _cacheItem = GetCacheItem(value.getItem);
                        }

                        break;

                    case Response.Type.GET_TYPEINFO_MAP:
                        _requestId = value.requestId;

                        if (!String.IsNullOrEmpty(value.getTypemap.map))
                            TypeMap = new TypeInfoMap(value.getTypemap.map);
                        else
                            TypeMap = null;

                        break;

                    case Response.Type.GET_COMPACT_TYPES:
                        _requestId = value.requestId;

                        int start = 0;
                        int end = 0;

                        if (!String.IsNullOrEmpty(value.getCompactTypes.compactTypeString))
                            _resultDic = Alachisoft.NCache.Util.SerializationUtil.GetTypeMapFromProtocolString(value.getCompactTypes.compactTypeString, ref start, ref end);
                        else
                            _resultDic = null;

                        break;

                    case Response.Type.GET_SERIALIZATION_FORMAT:
                        _requestId = value.requestId;
                        _serializationFormat = (SerializationFormat)value.getSerializationFormatResponse.serializationFormat;
                        break;

                    case Response.Type.GET_HASHMAP:
                        _requestId = value.requestId;

                        Hashtable nodes = new Hashtable();
                        foreach (Common.Protobuf.KeyValuePair pair in value.getHashmap.keyValuePair)
                        {
                            nodes.Add(Convert.ToInt32(pair.key), pair.value);
                        }

                        ArrayList members = new ArrayList();
                        foreach (string member in value.getHashmap.members)
                        {
                            members.Add(new Alachisoft.NCache.Common.Net.Address(member, 0));
                        }

                        this._bucketSize = value.getHashmap.bucketSize;

                        this._newHashmap = new NewHashmap(
                            value.getHashmap.viewId,
                            nodes,
                            members);
                        break;

                    case Alachisoft.NCache.Common.Protobuf.Response.Type.HASHMAP_CHANGED_EVENT:
                        _requestId = value.requestId;
                        _value = value.hashmapChanged.table;
                        break;

                    case Response.Type.EXCEPTION:
                        _requestId = value.requestId;
                        _excType = (ExceptionType)value.exception.type;
                        _exceptionString = value.exception.message;
                        _errorCode = value.exception.errorCode;
                        if (_errorCode == 0)
                        {
                            _errorCode = -1;
                        }
                        _stackTrace = value.exception.stackTrace;
                        break;
                    case Response.Type.SYNC_EVENTS:
                        _requestId = value.requestId;
                        _response = value;
                        break;
                    case Response.Type.BLOCK_ACTIVITY:
                        _response = value;
                        break;
                    case Response.Type.UNBLOCK_ACTIVITY:
                        _response = value;
                        break;
                 
                    case Response.Type.POLL:
                        _requestId = value.requestId;
                        _commandID = value.commandID;
                        pollingResult = new PollingResult();
                        pollingResult.RemovedKeys.AddRange(value.pollResponse.removedKeys);
                        pollingResult.UpdatedKeys.AddRange(value.pollResponse.updatedKeys);
                        break;

                    case Response.Type.GET_CONNECTED_CLIENTS:
                        _response = value;
                        _requestId = value.requestId;
                        _connectedClients = new ClusteredList<Runtime.Caching.ClientInfo>();
                        foreach (var connectedClient in _response.getConnectedClientsResponse.connectedClients)
                        {
                            Runtime.Caching.ClientInfo clientInfo = new Runtime.Caching.ClientInfo();
                            clientInfo.ProcessID = connectedClient.processId;
                            clientInfo.AppName = connectedClient.appName;
                            clientInfo.ClientID = connectedClient.clientId;
                            clientInfo.MachineName = connectedClient.machineName;
                            clientInfo.IPAddress = IPAddress.Parse(connectedClient.ipAddress);
                            _connectedClients.Add(clientInfo);
                        }
                        break;
                    case Response.Type.GET_TOPIC:
                        _requestId = value.requestId;
                        _operationSuccess = value.getTopicResponse.success;
                        break;
                    case Response.Type.SUBSCRIBE_TOPIC:
                        _requestId = value.requestId;
                        _operationSuccess = value.subscribeTopicResponse.success;
                        break;
                    case Response.Type.UNSUBSCRIBE_TOPIC:
                        _requestId = value.requestId;
                        _operationSuccess = value.unSubscribeTopicResponse.success;
                        break;
                    case Response.Type.REMOVE_TOPIC:
                        _requestId = value.requestId;
                        _operationSuccess = value.removeTopicResponse.success;
                        break;

                    case Response.Type.GET_MESSAGE:
                        {
                            _requestId = value.requestId;
                            _intendedRecipient = value.intendedRecipient;
                            if (value.getMessageResponse != null && value.getMessageResponse.topicMessages != null)
                            {
                                GetEventMessages(value.getMessageResponse.topicMessages);
                                _registeredPatterns = value.getMessageResponse.RegisteredPatterns;

                            }
                            break;
                        }

                    case Response.Type.OPERATIONCHANGEDEVNET:
                        _response = value;
                        break;

                    case Response.Type.MODULE:
                        _response = value;
                        _requestId = value.requestId;
                        break;
                    case Response.Type.SURROGATE:
                        IsSurrogate = true;
                        ActualTargetNode = Address.Parse(value.surrogateResponse.targetServer);
                        ProcessSurrogateResponse(value);
                        break;

                   
                }
            }
        }

        private void ProcessSurrogateResponse(Response value)
        {
            var commandResponse = this.MemberwiseClone() as CommandResponse;
            Response response = null;
            using (var stream = new MemoryStream(value.surrogateResponse.command[0]))
            {
                response = ProtoBuf.Serializer.Deserialize<Response>(stream);
            }
            this.Type = response.responseType;
            this.Src = this.ActualTargetNode;
            this.IsSurrogate = false;
            this.Result = response;
        }

        internal void DeserializeResponse()
        {
            if (Connection.WriteRequestIdInResponse)
            {
                using (System.IO.Stream tempStream = new ClusteredMemoryStream(SerializedResponse))
                    Result = ResponseHelper.DeserializeResponse(Type, tempStream);
            }
        }
        private void UpdateDelimIndexes(string command, char delim, ref int beginQuoteIndex, ref int endQuoteIndex)
        {
            beginQuoteIndex = endQuoteIndex;
            endQuoteIndex = command.IndexOf(delim, beginQuoteIndex + 1);
        }

        internal byte[] Value
        {
            get { return _value; }
            set
            {
                if (value != null)
                {
                    _value = new byte[value.Length];
                    value.CopyTo(_value, 0);
                }
            }
        }

        internal CompressedValueEntry FlagValueEntry
        {
            get
            {
                _flagValueEntry.Value = Value;
                return _flagValueEntry;
            }
        }

        internal byte[] NotifId
        {
            get { return _notifId; }
        }

        internal long RequestId
        {
            get { return _requestId; }
            set { _requestId = value; }
        }
        internal string IntendedRecipient
        {
            get { return _intendedRecipient; }
        }
        internal short CallbackId
        {
            get { return _callbackId; }
        }

        public bool EnableErrorLogs
        {
            get { return this._enableErrorLogs; }
        }

        public bool EnableDetailedLogs
        {
            get { return this._enableDetailedLogs; }
        }


        internal CacheItem Item
        {
            get { return _cacheItem; }
        }

        internal CacheItemRemovedReason Reason
        {
            get
            {
                switch (_reason)
                {
                    case 1:
                        return CacheItemRemovedReason.Expired;

                    case 2:
                        return CacheItemRemovedReason.Removed;

                    default:
                        return CacheItemRemovedReason.Underused;

                }
            }
        }

        internal string Key
        {
            get { return _key; }
        }

        internal string QueryId
        {
            get { return _queryId; }
        }

       


        /// <summary>
        /// Creates a new instance of CacheResultItem 
        /// <param name="brokerReset">True if broker is reset due to lost connection, false otherwise</param>
        /// </summary>
        public CommandResponse(bool brokerReset, Common.Net.Address resetConnectionIP)
        {
            _key = null;
            _value = null;
            _brokerReset = brokerReset;
            _resetConnectionIP = resetConnectionIP;
        }

        public bool IsSendFailure { get; set; }

        public bool IsBrokerReset
        {
            get { return _brokerReset; }
        }
        public bool SetBroker
        {
            set { _brokerReset = value; }
        }

        public bool IsInternalResponse
        {
            get { return IsBrokerReset || IsSendFailure; }
        }
                
        public byte[] SerializedResponse { get; internal set; }
        public Address Src { get; internal set; }
        public bool NeedsDeserialization { get; internal set; }

        public bool IsSurrogate { get; internal set; }
        public Address ActualTargetNode { get; internal set; }

        internal void ParseResponse()
        {
            if (Type == Response.Type.EXCEPTION)
            {
                switch (_excType)
                {
                    case ExceptionType.OPERATIONFAILED:
                        throw new OperationFailedException(_errorCode,_exceptionString,_stackTrace);
                    case ExceptionType.AGGREGATE:
                        throw new Runtime.Exceptions.AggregateException(_errorCode,_exceptionString, null);
                    case ExceptionType.CONFIGURATION:
                        throw new ConfigurationException(_errorCode,_exceptionString,_stackTrace);
                    case ExceptionType.SECURITY:
                        throw new SecurityException(_errorCode,_exceptionString,_stackTrace);

                    case ExceptionType.GENERALFAILURE:
                        throw new GeneralFailureException(_errorCode,_exceptionString,_stackTrace);
                    case ExceptionType.NOTSUPPORTED:
                        throw new OperationNotSupportedException(_errorCode,_exceptionString,_stackTrace);
                    case ExceptionType.STREAM_ALREADY_LOCKED:
                        throw new StreamAlreadyLockedException();
                    case ExceptionType.STREAM_CLOSED:
                        throw new StreamCloseException();
                    case ExceptionType.STREAM_EXC:
                        throw new StreamException(_errorCode,_exceptionString);
                    case ExceptionType.STREAM_INVALID_LOCK:
                        throw new StreamInvalidLockException();
                    case ExceptionType.STREAM_NOT_FOUND:
                        throw new StreamNotFoundException();
                    case ExceptionType.TYPE_INDEX_NOT_FOUND:
                        throw new OperationFailedException(_errorCode, _exceptionString,_stackTrace);
                    case ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND:
                        throw new OperationFailedException(_errorCode,_exceptionString,_stackTrace);
                    case ExceptionType.STATE_TRANSFER_EXCEPTION:
                        throw new StateTransferInProgressException(_exceptionString);
                    case ExceptionType.INVALID_READER_EXCEPTION:
                        throw new InvalidReaderException(_errorCode,_exceptionString,_stackTrace);

                    case ExceptionType.LICENSING_EXCEPION:
                        throw new LicensingException(_errorCode,_exceptionString,_stackTrace);
                }
            }
            else if (_brokerReset)
                throw new ConnectionException("Connection with server lost [" + _resetConnectionIP + "]");
        }

       
        private void GetEventMessages(List<TopicMessages> messageList)
        {
            foreach (TopicMessages topicMessage in messageList)
            {
                if (!_messageDic.ContainsKey(topicMessage.topic))
                {
                    IList<MessageItem> messageItemList = new ClusteredList<MessageItem>();
                    foreach (Common.Protobuf.Message message in topicMessage.messageList)
                    {
                        MessageItem item = GetMessageItem(message);
                        messageItemList.Add(item);
                    }
                    _messageDic.Add(topicMessage.topic, messageItemList);
                }
                else
                {
                    IList<MessageItem> messageItemList = _messageDic[topicMessage.topic];
                    foreach (Common.Protobuf.Message message in topicMessage.messageList)
                    {
                        MessageItem item = GetMessageItem(message);
                        messageItemList.Add(item);
                    }
                }
            }
        }

        private MessageItem GetMessageItem(Common.Protobuf.Message message)
        {
            MessageItem messageItem = new MessageItem();

            messageItem.MessageId = message.messageId;

            messageItem.CreationTime = new DateTime(message.creationTime);
            messageItem.ExpirationTime = new TimeSpan(message.expirationTime);
            messageItem.Flag = new BitSet((byte)message.flag);
            messageItem.DeliveryOption = (DeliveryOption)message.deliveryOption;
            messageItem.SubscriptionType = (SubscriptionType)message.subscriptionType;
            messageItem.MessageFailureReason = (MessgeFailureReason)message.messageRemoveReason;

            if (message.recipientList != null)
                messageItem.RecipientList = new HashSet<string>(message.recipientList);

            if (message.subscriptionIdentifiers != null)
                messageItem.SubscriptionIdentifierList = new List<SubscriptionIdentifier>(GetSubscriptionIds(message.subscriptionIdentifiers));

            if (message.internalPayload == null)    // If it is a user message
            {
                Value val = message.payload;
                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());

                try
                {
                    messageItem.Payload = CompactBinaryFormatter.FromByteBuffer(ubObject.GetFullObject(), _cacheId);
                    if (messageItem.Payload is UserBinaryObject)
                        messageItem.Payload = ((UserBinaryObject)_cacheItem.GetValue<object>()).GetFullObject();
                    else
                        messageItem.Payload = ubObject.GetFullObject();
                }
                catch (System.Exception ex)
                {
                    messageItem.Payload = ubObject.GetFullObject();
                }

            }
            else   // If it is an event message
            {
                switch (message.internalPayload.payloadType)
                {
                    
                    case InternalPayload.PayloadType.CACHE_ITEM_EVENTS:
                        messageItem.Payload = CreateMessageEventItems(message.internalPayload.eventMessage);
                        break;
                }
            }
            return messageItem;
        }

        private MessageEventItem CreateMessageEventItem(EventMessage protobufEventMessage, MessageEventItem eventItem)
        {
            if (protobufEventMessage.eventType == EventMessage.EventType.ITEM_REMOVED_EVENT
                || protobufEventMessage.eventType == EventMessage.EventType.ITEM_REMOVED_CALLBACK)
            {
                switch (protobufEventMessage.@event.removeReason)
                {
                    case 1:
                        eventItem.Reason = CacheItemRemovedReason.Expired;
                        break;
                    case 2:
                        eventItem.Reason = CacheItemRemovedReason.Removed;
                        break;
                    default:
                        eventItem.Reason = CacheItemRemovedReason.Underused;
                        break;
                }
            }

            if (protobufEventMessage.@event.item != null)
            {
                eventItem.Item = new EventCacheItem();
                eventItem.Item = eventItem.ConvertToEventCacheItem(protobufEventMessage.@event.item);
            }

            if (protobufEventMessage.@event.oldItem != null)
            {
                eventItem.OldItem = new EventCacheItem();
                eventItem.OldItem = eventItem.ConvertToEventCacheItem(protobufEventMessage.@event.oldItem);
            }

            return eventItem;
        }

        private object CreateMessageEventItems(Common.Protobuf.EventMessage protobufEventMessage)
        {
            var eventItems = default(MessageEventItem[]);

            if (protobufEventMessage.queryIds.Count == 0 && protobufEventMessage.callbackIds.Count == 0)
            {
                MessageEventItem eventMessage = new MessageEventItem();
                eventMessage.Key = protobufEventMessage.key;
              
                eventMessage = CreateMessageEventItem(protobufEventMessage, eventMessage);
                switch (protobufEventMessage.eventType)
                {
                    case EventMessage.EventType.ITEM_ADDED_EVENT:
                        eventMessage.EventType = NCache.Persistence.EventType.ITEM_ADDED_EVENT;
                        break;
                    case EventMessage.EventType.ITEM_UPDATED_EVENT:
                        eventMessage.EventType = NCache.Persistence.EventType.ITEM_UPDATED_EVENT;
                        break;
                    case EventMessage.EventType.ITEM_REMOVED_EVENT:
                        eventMessage.EventType = NCache.Persistence.EventType.ITEM_REMOVED_EVENT;
                        break;
                }
                return eventMessage;
            }
            else
            {
              
                    eventItems = new MessageEventItem[protobufEventMessage.callbackIds.Count];

                for (int i = 0; i < eventItems.Length; i++)
                {
                    eventItems[i] = new MessageEventItem();
                    eventItems[i].Key = protobufEventMessage.key;
                  
                    eventItems[i].DataFilter = (EventDataFilter)protobufEventMessage.dataFilters[i];
                    eventItems[i] = CreateMessageEventItem(protobufEventMessage, eventItems[i]);

                    switch (protobufEventMessage.eventType)
                    {
                        case EventMessage.EventType.ITEM_REMOVED_CALLBACK:
                            eventItems[i].EventType = NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;
                            eventItems[i].Callback = Convert.ToInt16(protobufEventMessage.callbackIds[i]);
                            break;
                        case EventMessage.EventType.ITEM_UPDATED_CALLBACK:
                            eventItems[i].EventType = NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK;
                            eventItems[i].Callback = Convert.ToInt16(protobufEventMessage.callbackIds[i]);
                            break;
                      
                    }
                }
                return eventItems;
            }
        }

    
        private CacheItem GetCacheItem(GetCacheItemResponse response)
        {
            CacheItem cacheItem = new CacheItem();
            cacheItem.CreationTime = new DateTime(response.creationTime).ToLocalTime();
            cacheItem.LastModifiedTime = new DateTime(response.lastModifiedTime).ToLocalTime();
            cacheItem.EntryType = MiscUtil.ProtoItemTypeToEntryType(response.itemType);

            if (response.absExp != 0)
            {
                DateTime dateFromServer = new DateTime(response.absExp, DateTimeKind.Utc);
                cacheItem.Expiration = new Expiration(ExpirationType.Absolute)
                {
                    ExpireAfter = dateFromServer - DateTime.Now.ToUniversalTime()
                };
            }

            else if (response.sldExp != 0)
            {
                cacheItem.Expiration = new Expiration(ExpirationType.Sliding)
                {
                    ExpireAfter = new TimeSpan(response.sldExp)
                };
            }
            else
                cacheItem.Expiration = new Expiration();

           
            cacheItem.FlagMap = new BitSet((byte)response.flag);
            
            cacheItem.Priority = (CacheItemPriority)response.priority;
            cacheItem.SubGroup =  null;
     
            this.EntryType = MiscUtil.ProtoItemTypeToEntryType(response.itemType); // (EntryType)value.getItem.itemType;
            UserBinaryObject userObj = UserBinaryObject.CreateUserBinaryObject(response.value.ToArray());
            
           
            cacheItem.SetValue(userObj.GetFullObject());
            return cacheItem;
        }

        private List<SubscriptionIdentifier> GetSubscriptionIds(List<Common.Protobuf.SubscriptionIdRecepientList> subscriptionIdentifiers)
        {
            List<SubscriptionIdentifier> reciepientIdList = new List<SubscriptionIdentifier>();
            if (subscriptionIdentifiers != null)
            {
                foreach (var subscription in subscriptionIdentifiers)
                {
                    var subId = new SubscriptionIdentifier(subscription.subscriptionName, (SubscriptionPolicyType)subscription.policy);
                    reciepientIdList.Add(subId);
                }
            }
            return reciepientIdList;
        }
    }
}
