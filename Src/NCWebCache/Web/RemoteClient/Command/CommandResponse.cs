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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Protobuf;
using System.Collections.Generic;
using System.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.MapReduce;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Processor;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class CommandResponse
    {
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
        private Common.Net.Address _resetConnectionIP; // = String.Empty;

        public Common.Net.Address ResetConnectionIP
        {
            get { return _resetConnectionIP; }
            internal set { _resetConnectionIP = value; }
        }

        private object _asyncOpResult = null;

        private OpCode _operationCode;
        
        private string _cacheId = null;

        /// <summary> Hold the result bulk operations </summary>
        private Hashtable _resultDic = new Hashtable();

        /// <summary> Hold the pub-sub messages </summary>
        private IDictionary<string, IList<MessageItem>> _messageDic = new HashVector<string, IList<MessageItem>>();

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

        private System.Net.IPAddress _clusterIp;
        private int _clusterPort;

        private Dictionary<string, int> _runningServers = new Dictionary<string, int>();
        private List<BulkEventItemResponse> _eventList;
        private PollingResult pollingResult = null;

        internal PollingResult PollingResult
        {
            get { return pollingResult; }
        }


        private List<NCache.Config.Mapping> _serverMappingList;
        private int _commandID;
        private long _itemAddVersion;

        public int CommandID
        {
            get { return _commandID; }
        }

        private ArrayList runningTasks = new ArrayList();
        private Runtime.MapReduce.TaskStatus taskProgress = null;
        private List<TaskEnumeratorResult> _taskEnumerator = new List<TaskEnumeratorResult>();
        Common.MapReduce.TaskEnumeratorResult _nextRecord = new TaskEnumeratorResult();

        private IList<Runtime.Caching.ClientInfo> _connectedClients;


        public Common.MapReduce.TaskEnumeratorResult TaskNextRecord
        {
            get { return _nextRecord; }
            set { _nextRecord = value; }
        }

        public List<TaskEnumeratorResult> TaskEnumerator
        {
            get { return _taskEnumerator; }
            set { _taskEnumerator = value; }
        }

        private List<Common.DataReader.ReaderResultSet> _readerResultSets =
            new List<Common.DataReader.ReaderResultSet>();

        private Common.DataReader.ReaderResultSet _readerNextChunk = null;

        public List<Common.DataReader.ReaderResultSet> ReaderResultSets
        {
            get { return _readerResultSets; }
        }

        public Common.DataReader.ReaderResultSet ReaderNextChunk
        {
            get { return _readerNextChunk; }
            set { _readerNextChunk = value; }
        }

        private System.Net.IPAddress _serverIp;
        private int _serverPort;


        private int _cacheManagementPort;
        private bool _reconnectClients = false;

        private ExceptionType _excType;

        private TypeInfoMap _completeTypeMap;


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

        private NewHashmap _newHashmap = null;

        private string _queryId;

        private QueryChangeType _changeType;

        private Alachisoft.NCache.Caching.Queries.QueryResultSet _resultSet;


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


        public int RemovedKeyCount
        {
            get { return _removedKeyCount; }
            set { _removedKeyCount = value; }
        }


        public EventDataFilter DataFilter
        {
            get { return _dataFilter; }
        }

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


        public ArrayList RunningTasks
        {
            get { return runningTasks; }
            set { runningTasks = value; }
        }

        public Runtime.MapReduce.TaskStatus TaskProgress
        {
            get { return taskProgress; }
            set { taskProgress = value; }
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

        internal List<NCache.Config.Mapping> ServerMappingList
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

        public IList<Runtime.Caching.ClientInfo> ConnectedClients
        {
            get { return _connectedClients; }
        }


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

        internal NewHashmap Hashmap
        {
            get { return this._newHashmap; }
        }

        public Alachisoft.NCache.Caching.Queries.QueryResultSet ResultSet
        {
            get { return _resultSet; }
        }

        public EventType EventType { get; set; }

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
                    case Response.Type.REMOVE_TAG:
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

                    case Response.Type.HYBRID_BULK:
                        _requestId = value.requestId;
                        _resultDic =
                            CompactBinaryFormatter.FromByteBuffer(value.hybridBulkResponse.binaryResult, null) as
                                Hashtable;
                        break;

                    case Response.Type.COUNT:
                        _requestId = value.requestId;
                        this._count = value.count.count;
                        break;

                    case Response.Type.ADD_DEPENDENCY:
                        _requestId = value.requestId;
                        _operationSuccess = value.addDep.success;
                        break;

                    case Response.Type.ADD_ATTRIBUTE:
                        _requestId = value.requestId;
                        _operationSuccess = value.addAttributeResponse.success;
                        break;

                    case Response.Type.ADD_SYNC_DEPENDENCY:
                        _requestId = value.requestId;
                        _operationSuccess = value.addSyncDependencyResponse.success;
                        break;

                    case Response.Type.CONTAINS:
                        _requestId = value.requestId;
                        this._exists = value.contain.exists;
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
                        _eventId.EventType = NCache.Persistence.EventType.POLL_REQUEST_EVENT;
                        EventType = (EventType)value.pollNotifyEventResponse.eventType;
                        break;

                    case Response.Type.ITEM_UPDATED_CALLBACK:
                        _callbackId = (short)value.itemUpdatedCallback.callbackId;
                        _key = value.itemUpdatedCallback.key;
                        _dataFilter = (EventDataFilter)value.itemUpdatedCallback.dataFilter;
                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemUpdatedCallback.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemUpdatedCallback.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemUpdatedCallback.eventId.operationCounter;
                        _eventId.EventType = NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK;
                        break;

                    case Response.Type.ITEM_REMOVED_CALLBACK:
                        _callbackId = (short)value.itemRemovedCallback.callbackId;
                        _key = value.itemRemovedCallback.key;
                        _reason = value.itemRemovedCallback.itemRemoveReason;
                        _flagValueEntry.Flag = new BitSet((byte)value.itemRemovedCallback.flag);
                        _dataFilter = (EventDataFilter)value.itemRemovedCallback.dataFilter;

                        if (value.itemRemovedCallback.value != null && value.itemRemovedCallback.value.Count > 0)
                        {
                            UserBinaryObject ubObject =
                                UserBinaryObject.CreateUserBinaryObject(value.itemRemovedCallback.value.ToArray());
                            Value = ubObject.GetFullObject();
                        }

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemRemovedCallback.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemRemovedCallback.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemRemovedCallback.eventId.operationCounter;
                        _eventId.EventType = NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;

                        break;

                    case Response.Type.TASK_CALLBACK:
                        this._requestId = value.requestId;
                        _eventId = new NCache.Caching.EventId();
                        if (value.TaskCallbackResponse.EventId != null)
                        {
                            _eventId.EventUniqueID = value.TaskCallbackResponse.EventId.eventUniqueId;
                            _eventId.EventCounter = value.TaskCallbackResponse.EventId.eventCounter;
                            _eventId.OperationCounter = value.TaskCallbackResponse.EventId.operationCounter;
                        }

                        _eventId.EventType = NCache.Persistence.EventType.TASK_CALLBACK;
                        this._taskCallbackId = value.TaskCallbackResponse.CallbackId;
                        this._taskId = value.TaskCallbackResponse.TaskId;
                        this._taskStatus = value.TaskCallbackResponse.TaskStatus;
                        this.taskFailureReason = value.TaskCallbackResponse.TaskFailureReason;
                        break;

                    case Response.Type.CQ_CALLBACK:
                        _key = value.cQCallbackResponse.key;
                        _queryId = value.cQCallbackResponse.queryId;
                        _changeType = (QueryChangeType)value.cQCallbackResponse.changeType;

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.cQCallbackResponse.eventId.eventUniqueId;
                        _eventId.EventCounter = value.cQCallbackResponse.eventId.eventCounter;
                        _eventId.OperationCounter = value.cQCallbackResponse.eventId.operationCounter;
                        _eventId.EventType = NCache.Persistence.EventType.CQ_CALLBACK;
                        _eventId.QueryChangeType =
                            (NCache.Caching.Queries.QueryChangeType)value.cQCallbackResponse.changeType;
                        _eventId.QueryId = _queryId;

                        break;

                    case Response.Type.REGISTER_CQ:
                        _queryId = value.registerCQResponse.cqId;
                        _requestId = value.requestId;
                        break;

                    case Response.Type.ASYNC_OP_COMPLETED_CALLBACK:
                        _requestId = value.asyncOpCompletedCallback.requestId;
                        _key = value.asyncOpCompletedCallback.key;

                        if (value.asyncOpCompletedCallback.success)
                            _asyncOpResult = NCache.Caching.AsyncOpResult.Success;
                        else
                            _asyncOpResult = new System.Exception(value.asyncOpCompletedCallback.exc.exception);

                        break;

                    case Response.Type.DS_UPDATE_CALLBACK:
                        _callbackId = (short)value.dsUpdateCallbackRespose.callbackId;
                        _operationCode = (OpCode)value.dsUpdateCallbackRespose.opCode;

                        foreach (Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult result in value
                            .dsUpdateCallbackRespose.result)
                        {
                            if (result.success)
                            {
                                _resultDic.Add(result.key, DataSourceOpResult.Success);
                            }
                            else if (result.exception != null)
                            {
                                System.Exception ex = new OperationFailedException(result.exception.exception);
                                _resultDic.Add(result.key, ex);
                            }
                            else
                            {
                                _resultDic.Add(result.key, DataSourceOpResult.Failure);
                            }
                        }

                        break;

                    case Response.Type.INVOKE_ENTRY_PROCESSOR:
                        this._requestId = value.requestId;
                        this._intendedRecipient = value.intendedRecipient;

                        InvokeEPKeyValuePackageResponse keyValuePair =
                            value.invokeEntryProcessorResponse.keyValuePackage;
                        Hashtable invokeEPResult = new Hashtable();
                        if (keyValuePair != null)
                        {
                            for (int i = 0; i < keyValuePair.keys.Count; i++)
                            {
                                string fetchedKey = keyValuePair.keys[i];

                                object result = null;
                                if (keyValuePair.values[i].Length > 0)
                                {
                                    result = CompactBinaryFormatter.FromByteBuffer(keyValuePair.values[i],
                                        this._cacheId);
                                }

                                EntryProcessorResult entry = null;
                                if (result != null)
                                    entry = (EntryProcessorResult)result;

                                if (entry != null)
                                    invokeEPResult.Add(fetchedKey, entry);
                                this.KeyValueDic = invokeEPResult;
                            }
                        }

                        break;

                    case Response.Type.ITEM_ADDED_EVENT:
                        _key = value.itemAdded.key;

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemAdded.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemAdded.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemAdded.eventId.operationCounter;
                        _eventId.EventType = NCache.Persistence.EventType.ITEM_ADDED_EVENT;
                        break;

                    case Response.Type.ITEM_UPDATED_EVENT:
                        _key = value.itemUpdated.key;

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemUpdated.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemUpdated.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemUpdated.eventId.operationCounter;
                        _eventId.EventType = NCache.Persistence.EventType.ITEM_UPDATED_EVENT;
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
                            UserBinaryObject ubObject =
                                UserBinaryObject.CreateUserBinaryObject(value.itemRemoved.value.ToArray());
                            Value = ubObject.GetFullObject();
                        }

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemRemoved.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemRemoved.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemRemoved.eventId.operationCounter;
                        _eventId.EventType = NCache.Persistence.EventType.ITEM_REMOVED_EVENT;
                        break;

                    case Response.Type.CACHE_CLEARED_EVENT:
                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.cacheCleared.eventId.eventUniqueId;
                        _eventId.EventCounter = value.cacheCleared.eventId.eventCounter;
                        _eventId.OperationCounter = value.cacheCleared.eventId.operationCounter;
                        _eventId.EventType = NCache.Persistence.EventType.CACHE_CLEARED_EVENT;
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
                            foreach (Common.Protobuf.ServerMapping mappingObject in value.getServerMappingResponse
                                .serverMapping)
                            {
                                Config.Mapping serverMapping = new Config.Mapping();
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
                        _response = value;
                        break;

                    case Response.Type.GET:
                        _requestId = value.requestId;
                        _flagValueEntry.Flag = new BitSet((byte)value.get.flag);
                        _lockId = String.IsNullOrEmpty(value.get.lockId) ? null : value.get.lockId;
                        _lockDate = new DateTime(value.get.lockTime);
                        _itemVersion = value.get.version;

                        if (value.get.data.Count > 0)
                        {
                            UserBinaryObject ubObject =
                                UserBinaryObject.CreateUserBinaryObject(value.get.data.ToArray());
                            _value = ubObject.GetFullObject();
                        }

                        break;

                    case Response.Type.REMOVE:
                        _requestId = value.requestId;
                        _flagValueEntry.Flag = new BitSet((byte)value.remove.flag);

                        if (value.remove.value != null && value.remove.value.Count > 0)
                        {
                            UserBinaryObject ubObject =
                                UserBinaryObject.CreateUserBinaryObject(value.remove.value.ToArray());
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
                        _lockId = String.IsNullOrEmpty(value.isLockedResponse.lockId)
                            ? null
                            : value.isLockedResponse.lockId;
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

                        Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse keyValuePackage =
                            value.bulkGet.keyValuePackage;
                        if (keyValuePackage != null)
                        {
                            for (int i = 0; i < keyValuePackage.keys.Count; i++)
                            {
                                string key = keyValuePackage.keys[i];

                                Alachisoft.NCache.Common.Protobuf.Value val = keyValuePackage.values[i];
                                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                byte[] bytes = ubObject.GetFullObject();

                                _flagValueEntry = new CompressedValueEntry();
                                _flagValueEntry.Flag = new BitSet((byte)keyValuePackage.flag[i]);

                                try
                                {
                                    object deserialized =
                                        Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(bytes, _cacheId);
                                    if (deserialized is UserBinaryObject)
                                        _flagValueEntry.Value = ((UserBinaryObject)deserialized).GetFullObject();
                                    else
                                        _flagValueEntry.Value = bytes;
                                }
                                catch (System.Exception e)
                                {
                                    _flagValueEntry.Value = bytes;
                                }

                                _resultDic.Add(key, _flagValueEntry);

                                _flagValueEntry = null;
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

                                Alachisoft.NCache.Common.Protobuf.Value val = keyValuePackage.values[i];
                                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                byte[] bytes = ubObject.GetFullObject();

                                _flagValueEntry = new CompressedValueEntry();
                                _flagValueEntry.Flag = new BitSet((byte)keyValuePackage.flag[i]);
                                try
                                {
                                    object deserialized =
                                        Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(bytes, _cacheId);
                                    if (deserialized is UserBinaryObject)
                                        _flagValueEntry.Value = ((UserBinaryObject)deserialized).GetFullObject();
                                    else
                                        _flagValueEntry.Value = bytes;
                                }
                                catch (System.Exception e)
                                {
                                    _flagValueEntry.Value = bytes;
                                }

                                _resultDic.Add(key, _flagValueEntry);

                                _flagValueEntry = null;
                            }
                        }

                        break;

                    case Response.Type.GET_GROUP_DATA:
                        _requestId = value.requestId;

                        keyValuePackage = value.getGroupData.keyValuePackage;
                        if (keyValuePackage != null)
                        {
                            for (int i = 0; i < keyValuePackage.keys.Count; i++)
                            {
                                string key = keyValuePackage.keys[i];

                                Alachisoft.NCache.Common.Protobuf.Value val = keyValuePackage.values[i];
                                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                byte[] bytes = ubObject.GetFullObject();

                                _flagValueEntry = new CompressedValueEntry();
                                _flagValueEntry.Flag = new BitSet((byte)keyValuePackage.flag[i]);
                                _flagValueEntry.Value = bytes;

                                _resultDic.Add(key, _flagValueEntry);

                                _flagValueEntry = null;
                            }
                        }

                        break;

                    case Response.Type.GET_TAG:
                        _requestId = value.requestId;

                        keyValuePackage = value.getTag.keyValuePackage;
                        if (keyValuePackage != null)
                        {
                            for (int i = 0; i < keyValuePackage.keys.Count; i++)
                            {
                                string key = keyValuePackage.keys[i];

                                Alachisoft.NCache.Common.Protobuf.Value val = keyValuePackage.values[i];
                                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                byte[] bytes = ubObject.GetFullObject();

                                _flagValueEntry = new CompressedValueEntry();
                                _flagValueEntry.Flag = new BitSet((byte)keyValuePackage.flag[i]);
                                _flagValueEntry.Value = bytes;

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

                    case Response.Type.GET_KEYS_TAG:
                        _requestId = value.requestId;
                        _resultList.AddRange(value.getKeysByTagResponse.keys);
                        break;

                    case Response.Type.SEARCH_ENTRIES:
                        _requestId = value.requestId;

                        SearchEntriesResponse searchEntriesResponse = value.searchEntries;
                        Alachisoft.NCache.Common.Protobuf.QueryResultSet protoResultSet =
                            searchEntriesResponse.queryResultSet;
                        _resultSet = new Alachisoft.NCache.Caching.Queries.QueryResultSet();

                        switch (protoResultSet.queryType)
                        {
                            case QueryType.AGGREGATE_FUNCTIONS:
                                _resultSet.Type = Alachisoft.NCache.Caching.Queries.QueryType.AggregateFunction;
                                _resultSet.AggregateFunctionType =
                                    (Alachisoft.NCache.Common.Enum.AggregateFunctionType)(int)protoResultSet
                                        .aggregateFunctionType;

                                if (protoResultSet.aggregateFunctionResult.value != null)
                                {
                                    _resultSet.AggregateFunctionResult = new DictionaryEntry(
                                        protoResultSet.aggregateFunctionResult.key,
                                        CompactBinaryFormatter.FromByteBuffer(
                                            protoResultSet.aggregateFunctionResult.value, null));
                                }
                                else
                                {
                                    _resultSet.AggregateFunctionResult =
                                        new DictionaryEntry(protoResultSet.aggregateFunctionResult.key, null);
                                }

                                break;

                            case QueryType.SEARCH_ENTRIES:
                                _resultSet.Type = Alachisoft.NCache.Caching.Queries.QueryType.SearchEntries;
                                keyValuePackage = value.searchEntries.queryResultSet.searchKeyEnteriesResult;

                                if (keyValuePackage != null)
                                {
                                    for (int i = 0; i < keyValuePackage.keys.Count; i++)
                                    {
                                        string key = keyValuePackage.keys[i];

                                        Alachisoft.NCache.Common.Protobuf.Value val = keyValuePackage.values[i];
                                        UserBinaryObject ubObject =
                                            UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                        byte[] bytes = ubObject.GetFullObject();

                                        _flagValueEntry = new CompressedValueEntry();
                                        _flagValueEntry.Flag = new BitSet((byte)keyValuePackage.flag[i]);
                                        _flagValueEntry.Value = bytes;

                                        _resultDic.Add(key, _flagValueEntry);

                                        _flagValueEntry = null;
                                    }
                                }

                                _resultSet.SearchEntriesResult = _resultDic;
                                break;
                            case QueryType.GROUPBY_AGGREGATE_FUNCTIONS:
                                _resultSet.Type = NCache.Caching.Queries.QueryType.GroupByAggregateFunction;
                                break;
                        }

                        break;
                    case Response.Type.SEARCH_ENTRIES_CQ:
                        _requestId = value.requestId;

                        SearchEntriesCQResponse searchEntriesCQResponse = value.searchEntriesCQResponse;
                        Alachisoft.NCache.Common.Protobuf.CQResultSet cqResultSet =
                            searchEntriesCQResponse.queryResultSet;
                        _resultSet = new Alachisoft.NCache.Caching.Queries.QueryResultSet();
                        if (!string.IsNullOrEmpty(cqResultSet.CQUniqueId))
                            _resultSet.CQUniqueId = cqResultSet.CQUniqueId;

                        _resultSet.Type = Alachisoft.NCache.Caching.Queries.QueryType.SearchEntries;
                        keyValuePackage = cqResultSet.searchKeyEnteriesResult;

                        if (keyValuePackage != null)
                        {
                            for (int i = 0; i < keyValuePackage.keys.Count; i++)
                            {
                                string key = keyValuePackage.keys[i];
                                Alachisoft.NCache.Common.Protobuf.Value val = keyValuePackage.values[i];
                                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                byte[] bytes = ubObject.GetFullObject();
                                _flagValueEntry = new CompressedValueEntry();
                                _flagValueEntry.Flag = new BitSet((byte)keyValuePackage.flag[i]);
                                _flagValueEntry.Value = bytes;

                                _resultDic.Add(key, _flagValueEntry);

                                _flagValueEntry = null;
                            }
                        }

                        _resultSet.SearchEntriesResult = _resultDic;

                        break;

                    case Response.Type.ADD_BULK:
                        _requestId = value.requestId;
                        _intendedRecipient = value.intendedRecipient;

                        Alachisoft.NCache.Common.Protobuf.KeyExceptionPackageResponse keyExceptions =
                            value.bulkAdd.keyExceptionPackage;
                        if (keyExceptions != null)
                        {
                            for (int i = 0; i < keyExceptions.keys.Count; i++)
                            {
                                string key = keyExceptions.keys[i];
                                Alachisoft.NCache.Common.Protobuf.Exception exc = keyExceptions.exceptions[i];
                                _resultDic.Add(key, new OperationFailedException(exc.message));
                            }
                        }

                        Alachisoft.NCache.Common.Protobuf.KeyVersionPackageResponse keyVersions =
                            value.bulkAdd.keyVersionPackage;
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
                                _resultDic.Add(key, new OperationFailedException(exc.message));
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

                    case Response.Type.GET_GROUP_KEYS:
                        _requestId = value.requestId;
                        _resultList.AddRange(value.getGroupKeys.keys);
                        break;

                    case Response.Type.SEARCH:
                        _requestId = value.requestId;

                        SearchResponse searchResponse = value.search;
                        _resultSet = new Alachisoft.NCache.Caching.Queries.QueryResultSet();
                        protoResultSet = searchResponse.queryResultSet;
                        switch (protoResultSet.queryType)
                        {
                            case QueryType.AGGREGATE_FUNCTIONS:
                                _resultSet.Type = Alachisoft.NCache.Caching.Queries.QueryType.AggregateFunction;
                                _resultSet.AggregateFunctionType =
                                    (Alachisoft.NCache.Common.Enum.AggregateFunctionType)(int)protoResultSet
                                        .aggregateFunctionType;
                                if (protoResultSet.aggregateFunctionResult.value != null)
                                {
                                    _resultSet.AggregateFunctionResult = new DictionaryEntry(
                                        protoResultSet.aggregateFunctionResult.key,
                                        CompactBinaryFormatter.FromByteBuffer(
                                            protoResultSet.aggregateFunctionResult.value, null));
                                }
                                else
                                {
                                    _resultSet.AggregateFunctionResult =
                                        new DictionaryEntry(protoResultSet.aggregateFunctionResult.key, null);
                                }

                                break;

                            case QueryType.SEARCH_KEYS:
                                _resultSet.Type = Alachisoft.NCache.Caching.Queries.QueryType.SearchKeys;
                                _resultList.AddRange(protoResultSet.searchKeyResults);
                                _resultSet.SearchKeysResult = _resultList;
                                break;
                        }

                        break;

                    case Response.Type.SEARCH_CQ:
                        _requestId = value.requestId;

                        SearchCQResponse searchCQResponse = value.searchCQResponse;
                        _resultSet = new Alachisoft.NCache.Caching.Queries.QueryResultSet();
                        cqResultSet = searchCQResponse.queryResultSet;
                        if (!string.IsNullOrEmpty(cqResultSet.CQUniqueId))
                            _resultSet.CQUniqueId = cqResultSet.CQUniqueId;
                        _resultSet.Type = Alachisoft.NCache.Caching.Queries.QueryType.SearchKeys;
                        _resultList.AddRange(cqResultSet.searchKeyResults);
                        _resultSet.SearchKeysResult = _resultList;

                        break;

                    case Response.Type.DELETE_QUERY:
                        _requestId = value.requestId;
                        _intendedRecipient = value.intendedRecipient;

                        break;

                    case Response.Type.REMOVE_QUERY:
                        _requestId = value.requestId;
                        _intendedRecipient = value.intendedRecipient;
                        _removedKeyCount = value.removeQueryResponse.removedKeyCount;
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

                        pointer = EnumerationPointerConversionUtil.GetFromProtobufEnumerationPointer(
                            value.getNextChunkResponse.enumerationPointer);
                        keys = value.getNextChunkResponse.keys;

                        chunk = new EnumerationDataChunk();
                        chunk.Data = keys;
                        chunk.Pointer = pointer;
                        _enumerationDataChunk.Add(chunk);
                        break;

                    case Response.Type.GET_GROUP_NEXT_CHUNK:
                        _requestId = value.requestId;

                        pointer = EnumerationPointerConversionUtil.GetFromProtobufGroupEnumerationPointer(
                            value.getGroupNextChunkResponse.groupEnumerationPointer);
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
                            this._cacheItem = new CacheItem();
                            this._cacheItem._creationTime = new DateTime(value.getItem.creationTime);
                            this._cacheItem._lastModifiedTime = new DateTime(value.getItem.lastModifiedTime);
                            if (value.getItem.absExp != 0)
                            {
                                this._cacheItem.AbsoluteExpiration =
                                    new DateTime(value.getItem.absExp, DateTimeKind.Utc);
                            }
                            else
                            {
                                this._cacheItem.AbsoluteExpiration =
                                    Alachisoft.NCache.Web.Caching.Cache.NoAbsoluteExpiration;
                            }

                            this._cacheItem.SlidingExpiration = new TimeSpan(value.getItem.sldExp);
                            this._cacheItem.Dependency = DependencyHelper.GetCacheDependency(value.getItem.dependency);
                            this._cacheItem.FlagMap = new BitSet((byte)value.getItem.flag);
                            this._cacheItem.Group = value.getItem.group.Length == 0 ? null : value.getItem.group;
                            this._cacheItem.IsResyncExpiredItems = value.getItem.needsResync;
                            this._cacheItem.Priority = (CacheItemPriority)value.getItem.priority;
                            this._cacheItem.SubGroup =
                                value.getItem.subGroup.Length == 0 ? null : value.getItem.subGroup;
                            Hashtable tagInfo = ProtobufHelper.GetHashtableFromTagInfoObj(value.getItem.tagInfo);
                            if (tagInfo != null)
                            {
                                ArrayList tags = (ArrayList)tagInfo["tags-list"];
                                this.Item.Tags = new Tag[tags.Count];
                                for (int i = 0; i < tags.Count; i++)
                                {
                                    this.Item.Tags[i] = new Tag((string)tags[i]);
                                }
                            }

                            Hashtable namedTagInfo =
                                ProtobufHelper.GetHashtableFromNamedTagInfoObjFromDotNet(value.getItem.namedTagInfo);
                            if (namedTagInfo != null)
                            {
                                Hashtable tagsList = namedTagInfo["named-tags-list"] as Hashtable;

                                NamedTagsDictionary namedTags = new NamedTagsDictionary();

                                foreach (DictionaryEntry tag in tagsList)
                                {
                                    Type tagType = tag.Value.GetType();
                                    string tagKey = tag.Key.ToString();

                                    if (tagType == typeof(int))
                                    {
                                        namedTags.Add(tagKey, (int)tag.Value);
                                    }
                                    else if (tagType == typeof(long))
                                    {
                                        namedTags.Add(tagKey, (long)tag.Value);
                                    }
                                    else if (tagType == typeof(float))
                                    {
                                        namedTags.Add(tagKey, (float)tag.Value);
                                    }
                                    else if (tagType == typeof(double))
                                    {
                                        namedTags.Add(tagKey, (double)tag.Value);
                                    }
                                    else if (tagType == typeof(decimal))
                                    {
                                        namedTags.Add(tagKey, (decimal)tag.Value);
                                    }
                                    else if (tagType == typeof(bool))
                                    {
                                        namedTags.Add(tagKey, (bool)tag.Value);
                                    }
                                    else if (tagType == typeof(char))
                                    {
                                        namedTags.Add(tagKey, (char)tag.Value);
                                    }
                                    else if (tagType == typeof(string))
                                    {
                                        namedTags.Add(tagKey, (string)tag.Value);
                                    }
                                    else if (tagType == typeof(DateTime))
                                    {
                                        namedTags.Add(tagKey, (DateTime)tag.Value);
                                    }
                                }

                                this._cacheItem.NamedTags = namedTags;
                            }

                            this._cacheItem.Version = value.getItem.version != 0
                                ? new CacheItemVersion(value.getItem.version)
                                : null;
                            UserBinaryObject userObj =
                                UserBinaryObject.CreateUserBinaryObject(value.getItem.value.ToArray());
                            try
                            {
                                this._cacheItem.Value =
                                    Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(
                                        userObj.GetFullObject(), _cacheId);
                                if (this._cacheItem.Value is UserBinaryObject)
                                    this._cacheItem.Value = ((UserBinaryObject)this._cacheItem.Value).GetFullObject();
                                else
                                    this._cacheItem.Value = userObj.GetFullObject();
                            }
                            catch (System.Exception ex)
                            {
                                this._cacheItem.Value = userObj.GetFullObject();
                            }
                        }

                        break;

                    case Response.Type.GET_TYPEINFO_MAP:
                        _requestId = value.requestId;

                        if (!String.IsNullOrEmpty(value.getTypemap.map))
                            TypeMap = new TypeInfoMap(value.getTypemap.map);
                        else
                            TypeMap = null;

                        break;

                    case Response.Type.GET_HASHMAP:
                        _requestId = value.requestId;

                        Hashtable nodes = new Hashtable();
                        foreach (KeyValuePair pair in value.getHashmap.keyValuePair)
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

                    case Response.Type.CLOSE_STREAM:
                        _requestId = value.requestId;
                        break;

                    case Response.Type.OPEN_STREAM:
                        _requestId = value.requestId;
                        _lockId = value.openStreamResponse.lockHandle;
                        break;

                    case Response.Type.READ_FROM_STREAM:
                        _requestId = value.requestId;
                        _bytesRead = value.readFromStreamResponse.bytesRead;
                        _dataList = value.readFromStreamResponse.buffer;
                        break;

                    case Response.Type.GET_STREAM_LENGTH:
                        _requestId = value.requestId;
                        _streamLength = (int)value.getStreamLengthResponse.streamLength;
                        break;

                    case Response.Type.WRITE_TO_STREAM:
                        _requestId = value.requestId;
                        break;

                    case Response.Type.EXCEPTION:
                        _requestId = value.requestId;
                        _excType = (ExceptionType)value.exception.type;
                        _exceptionString = value.exception.message;
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
                    case Response.Type.MAP_REDUCE_TASK:
                        _requestId = value.requestId;
                        _response = value;
                        break;
                    case Response.Type.CANCEL_TASK:
                        _requestId = value.requestId;
                        _response = value;
                        break;
                    case Response.Type.TASK_PROGRESS:
                        _requestId = value.requestId;
                        _response = value;
                        try
                        {
                            this.TaskProgress =
                                (Runtime.MapReduce.TaskStatus)Serialization.Formatters.CompactBinaryFormatter
                                    .FromByteBuffer(_response.TaskProgressResponse.progresses, _cacheId);
                        }
                        catch (System.Exception ex)
                        {
                        }

                        this._requestId = value.requestId;
                        break;
                    case Response.Type.RUNNING_TASKS:
                        this._requestId = value.requestId;
                        _response = value;
                        this.runningTasks = new ArrayList(_response.RunningTasksResponse.runningTasks);
                        break;
                    case Response.Type.TASK_NEXT_RECORD:
                        _response = value;
                        this._requestId = value.requestId;
                        Common.MapReduce.TaskEnumeratorResult resultt = new Common.MapReduce.TaskEnumeratorResult();
                        resultt.IsLastResult = _response.NextRecordResponse.IsLastResult;
                        resultt.NodeAddress = _response.NextRecordResponse.NodeAddress;
                        Common.MapReduce.TaskEnumeratorPointer pntr =
                            new Common.MapReduce.TaskEnumeratorPointer(_response.NextRecordResponse.ClientId,
                                _response.NextRecordResponse.TaskId, (short)_response.NextRecordResponse.CallbackId);
                        pntr.ClientAddress = new Address(_response.NextRecordResponse.ClientIp,
                            _response.NextRecordResponse.ClientPort);
                        pntr.ClusterAddress = new Address(_response.NextRecordResponse.ClusterIp,
                            _response.NextRecordResponse.ClusterPort);

                        object tkey = null;
                        object tvalue = null;
                        try
                        {
                            if (_response.NextRecordResponse.Key is byte[])
                                tkey = Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(
                                    _response.NextRecordResponse.Key, _cacheId);
                            if (_response.NextRecordResponse.Value is byte[])
                                tvalue = Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(
                                    _response.NextRecordResponse.Value, _cacheId);
                        }
                        catch (System.Exception ex)
                        {
                        }

                        if (tkey == null && tvalue == null)
                        {
                            resultt.IsLastResult = true;
                        }

                        DictionaryEntry nextRecordSet = new DictionaryEntry(tkey, tvalue);
                        resultt.RecordSet = nextRecordSet;
                        _nextRecord = resultt;
                        break;
                    case Response.Type.TASK_ENUMERATOR:
                        _response = value;
                        this._requestId = value.requestId;
                        TaskEnumeratorResponse taskEnumeratorResponse = _response.TaskEnumeratorResponse;
                        foreach (object rslt in _response.TaskEnumeratorResponse.TaskEnumeratorResult)
                        {
                            TaskEnumeratorResult enumResult =
                                (TaskEnumeratorResult)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(
                                    (byte[])rslt, _cacheId);

                            TaskEnumeratorPointer pointerGTE = new TaskEnumeratorPointer(enumResult.Pointer.ClientId,
                                enumResult.Pointer.TaskId, (short)enumResult.Pointer.CallbackId);

                            pointerGTE.ClientAddress = new Address(enumResult.Pointer.ClientAddress.IpAddress,
                                enumResult.Pointer.ClientAddress.Port);
                            pointerGTE.ClusterAddress = new Address(enumResult.Pointer.ClusterAddress.IpAddress,
                                enumResult.Pointer.ClusterAddress.Port);
                            enumResult.Pointer = pointerGTE;

                            if (enumResult.RecordSet.Key == null && enumResult.RecordSet.Value == null)
                            {
                                enumResult.IsLastResult = true;
                                enumResult.RecordSet = new DictionaryEntry();
                            }
                            else
                            {
                                enumResult.RecordSet = new DictionaryEntry(enumResult.RecordSet.Key,
                                    enumResult.RecordSet.Value);
                            }

                            _taskEnumerator.Add(enumResult);
                        }

                        break;

                    case Response.Type.EXPIRATION_RESPONSE:
                        _response = value;
                        this._requestId = value.requestId;
                        this._resultDic["absDefault"] = _response.getExpirationResponse.absDefault;
                        this._resultDic["absLonger"] = _response.getExpirationResponse.absDefault;
                        this._resultDic["sldDefault"] = _response.getExpirationResponse.sldDefault;
                        this._resultDic["sldLonger"] = _response.getExpirationResponse.sldLonger;
                        this._resultDic["absDefaultEnabled"] = _response.getExpirationResponse.absDefaultEnabled;
                        this._resultDic["absLongerEnabled"] = _response.getExpirationResponse.absLongerEnabled;
                        this._resultDic["sldDefaultEnabled"] = _response.getExpirationResponse.sldDefaultEnabled;
                        this._resultDic["sldLongerEnabled"] = _response.getExpirationResponse.sldLongerEnabled;
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
                            }

                            break;
                        }
                }
            }
        }

        private CacheDependency CreateDependencyFromString(ref string command, int beginQuoteIndex, int endQuoteIndex)
        {
            bool isInner = false;
            string interimCommand = null;
            int interimBeginIndex = 0, interimEndIndex = 0;

            DateTime startAfter = DateTime.Now;
            ArrayList keyList = new ArrayList(), fileList = new ArrayList();
            CacheDependency parent = null, dbDependencies = null;

            do
            {
                beginQuoteIndex += interimEndIndex;

                UpdateDelimIndexes(command, '\r', ref beginQuoteIndex, ref endQuoteIndex);
                if (endQuoteIndex < 0) break;

                interimCommand = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
                interimBeginIndex = interimEndIndex = 0;

                if (interimCommand.StartsWith("\nINNER") && !isInner)
                {
                    isInner = true;
                    parent = MakeDependency(keyList, fileList, dbDependencies, parent, startAfter);
                }

                else if (interimCommand.StartsWith("\nKEYDEPENDENCY") || interimCommand.StartsWith("\nFILEDEPENDENCY"))
                {
                    string value = null;

                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    while (true)
                    {
                        UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);

                        value = interimCommand.Substring(interimBeginIndex + 1,
                            interimEndIndex - interimBeginIndex - 1);
                        int valueBeginIndex = 0, valueEndIndex = 0;

                        if (value.Equals("STARTAFTER"))
                        {
                            UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                            startAfter = new DateTime(Convert.ToInt64(interimCommand.Substring(interimBeginIndex + 1,
                                interimEndIndex - interimBeginIndex - 1)));

                            interimBeginIndex += valueBeginIndex;
                            interimEndIndex += valueEndIndex;

                            break;
                        }
                        else
                        {
                            if (interimCommand.StartsWith("\nKEYDEPENDENCY")) keyList.Add(value);
                            else fileList.Add(value);
                        }
                    }
                }

                else if (interimCommand.StartsWith("\nOLEDBDEPENDENCY") ||
                         interimCommand.StartsWith("\nSQL7DEPENDENCY"))
                {
                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string connectionString =
                        interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string cacheKey =
                        interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);


                    if (interimCommand.StartsWith("\nOLEDBDEPENDENCY"))
                        dbDependencies = DBDependencyFactory.CreateOleDbCacheDependency(connectionString, cacheKey);
                    else
                        dbDependencies = DBDependencyFactory.CreateSqlCacheDependency(connectionString, cacheKey);
                }

                else if (interimCommand.StartsWith("\nYUKONDEPENDENCY") ||
                         interimCommand.StartsWith("\nORACLEDEPENDENCY"))
                {
                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string connectionString =
                        interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string queryString =
                        interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                    if (interimCommand.StartsWith("\nORACLEDEPENDENCY"))
                        dbDependencies = new OracleCacheDependency(connectionString, queryString);

                    else dbDependencies = new SqlCacheDependency(connectionString, queryString);
                }
            } while (endQuoteIndex > -1);

            return MakeDependency(keyList, fileList, dbDependencies, parent, startAfter);
        }

        private CacheDependency MakeDependency(ArrayList keyList, ArrayList fileList, CacheDependency dbDependency,
            CacheDependency parent, DateTime startAfter)
        {
            string[] keys = null, files = null;
            if (keyList.Count > 0) keys = (string[])keyList.ToArray(typeof(string));
            if (fileList.Count > 0) files = (string[])fileList.ToArray(typeof(string));

            if (keys != null || files != null)
            {
                if (dbDependency == null) parent = new CacheDependency(files, keys, parent, startAfter);
                else parent = new CacheDependency(files, keys, dbDependency, startAfter);
            }
            else if (dbDependency != null)
                parent = dbDependency;

            return parent;
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
                    case 0:
                        return CacheItemRemovedReason.DependencyChanged;

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

        internal QueryChangeType ChangeType
        {
            get { return _changeType; }
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

        public bool IsBrokerReset
        {
            get { return _brokerReset; }
        }

        public bool SetBroker
        {
            set { _brokerReset = value; }
        }

        internal void ParseResponse()
        {
            if (Type == Response.Type.EXCEPTION)
            {
                switch (_excType)
                {
                    case ExceptionType.OPERATIONFAILED:
                        throw new OperationFailedException(_exceptionString);
                    case ExceptionType.AGGREGATE:
                        throw new Runtime.Exceptions.AggregateException(_exceptionString, null);
                    case ExceptionType.CONFIGURATION:
                        throw new ConfigurationException(_exceptionString);


                    case ExceptionType.GENERALFAILURE:
                        throw new GeneralFailureException(_exceptionString);
                    case ExceptionType.NOTSUPPORTED:
                        throw new OperationNotSupportedException(_exceptionString);
                    case ExceptionType.STREAM_ALREADY_LOCKED:
                        throw new StreamAlreadyLockedException();
                    case ExceptionType.STREAM_CLOSED:
                        throw new StreamCloseException();
                    case ExceptionType.STREAM_EXC:
                        throw new StreamException(_exceptionString);
                    case ExceptionType.STREAM_INVALID_LOCK:
                        throw new StreamInvalidLockException();
                    case ExceptionType.STREAM_NOT_FOUND:
                        throw new StreamNotFoundException();
                    case ExceptionType.TYPE_INDEX_NOT_FOUND:
                        throw new OperationFailedException(_exceptionString);
                    case ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND:
                        throw new OperationFailedException(_exceptionString);
                    case ExceptionType.STATE_TRANSFER_EXCEPTION:
                        throw new StateTransferInProgressException(_exceptionString);
                    case ExceptionType.INVALID_READER_EXCEPTION:
                        throw new InvalidReaderException(_exceptionString);
                }
            }
            else if (_brokerReset)
                throw new ConnectionException("Connection with server lost [" + _resetConnectionIP + "]");
        }

        private Common.DataReader.ReaderResultSet ConvertToReaderResult(
            Common.Protobuf.ReaderResultSet readerResultSetProto)
        {
            if (readerResultSetProto == null)
                return null;
            Common.DataReader.ReaderResultSet readerResultSet = new Common.DataReader.ReaderResultSet();
            readerResultSet.IsGrouped = readerResultSetProto.isGrouped;
            readerResultSet.NodeAddress = readerResultSetProto.nodeAddress;
            readerResultSet.NextIndex = readerResultSetProto.nextIndex;
            readerResultSet.ReaderID = readerResultSetProto.readerId;

            List<Common.Queries.OrderByArgument> orderByArgs = new List<Common.Queries.OrderByArgument>();
            foreach (Common.Protobuf.OrderByArgument obaProto in readerResultSetProto.orderByArguments)
            {
                Common.Queries.OrderByArgument arg = new Common.Queries.OrderByArgument();
                arg.AttributeName = obaProto.attributeName;
                arg.Order = (Common.Queries.Order)Convert.ToInt32(obaProto.order);
                orderByArgs.Add(arg);
            }

            readerResultSet.OrderByArguments = orderByArgs;
            Common.DataReader.RecordSet recordSet = null;
            if (readerResultSetProto.recordSet != null)
            {
                recordSet = new Common.DataReader.RecordSet();
                Common.Protobuf.RecordSet recordSetProto = readerResultSetProto.recordSet;
                foreach (Common.Protobuf.RecordColumn columnProto in recordSetProto.columns)
                {
                    Common.DataReader.RecordColumn column = new Common.DataReader.RecordColumn(columnProto.name);
                    column.AggregateFunctionType =
                        (Common.Enum.AggregateFunctionType)Convert.ToInt32(columnProto.aggregateFunctionType);
                    column.ColumnType = (Common.Enum.ColumnType)Convert.ToInt32(columnProto.columnType);
                    column.DataType = (Common.Enum.ColumnDataType)Convert.ToInt32(columnProto.dataType);
                    column.IsFilled = columnProto.isFilled;
                    column.IsHidden = columnProto.isHidden;
                    recordSet.AddColumn(column);
                }

                foreach (Common.Protobuf.RecordRow rowProto in recordSetProto.rows)
                {
                    Common.DataReader.RecordRow row = recordSet.CreateRow();
                    for (int i = 0; i < recordSet.Columns.Count; i++)
                    {
                        if (rowProto.values[i] != null)
                        {
                            switch (recordSet.Columns[i].DataType)
                            {
                                case Common.Enum.ColumnDataType.AverageResult:

                                    Common.Queries.AverageResult avgResult = new Common.Queries.AverageResult();
                                    avgResult.Sum = Convert.ToDecimal(rowProto.values[i].avgResult.sum);
                                    avgResult.Count = Convert.ToDecimal(rowProto.values[i].avgResult.count);
                                    row[i] = avgResult;
                                    break;
                                case Common.Enum.ColumnDataType.CompressedValueEntry:
                                    Alachisoft.NCache.Common.Protobuf.Value val = rowProto.values[i].binaryObject;
                                    UserBinaryObject ubObject =
                                        UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                    byte[] bytes = ubObject.GetFullObject();
                                    CompressedValueEntry cmpEntry = new CompressedValueEntry();
                                    cmpEntry.Flag = new BitSet((byte)rowProto.values[i].flag);
                                    cmpEntry.Value = bytes;
                                    row[i] = ConvertToUserObject(cmpEntry);
                                    break;
                                default:
                                    row[i] = Common.DataReader.RecordSet.ToObject(rowProto.values[i].stringValue,
                                        recordSet.Columns[i].DataType);
                                    break;
                            }
                        }
                    }

                    recordSet.AddRow(row);
                }
            }

            readerResultSet.RecordSet = recordSet;
            return readerResultSet;
        }

        private object ConvertToUserObject(CompressedValueEntry cmpEntry)
        {
            if (cmpEntry.Value is UserBinaryObject)
            {
                UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                cmpEntry.Value = ubObject.GetFullObject();
            }

            if (cmpEntry.Value is CallbackEntry)
            {
                CallbackEntry e = cmpEntry.Value as CallbackEntry;
                cmpEntry.Value = e.Value;
            }


            return CompactBinaryFormatter.FromByteBuffer((byte[])cmpEntry.Value, _cacheId);
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

            Value val = message.payload;
            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
            try
            {
                messageItem.Payload = CompactBinaryFormatter.FromByteBuffer(ubObject.GetFullObject(), _cacheId);
                if (messageItem.Payload is UserBinaryObject)
                    messageItem.Payload = ((UserBinaryObject)_cacheItem.Value).GetFullObject();
                else
                    messageItem.Payload = ubObject.GetFullObject();
            }
            catch (System.Exception ex)
            {
                messageItem.Payload = ubObject.GetFullObject();
            }

            return messageItem;
        }
    }
}