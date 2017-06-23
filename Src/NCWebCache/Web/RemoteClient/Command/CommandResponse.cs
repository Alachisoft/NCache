// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Common.DataStructures;

using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Command
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

        private CompressedValueEntry _flagValueEntry = new CompressedValueEntry();

        /// <summary>Notification Id</summary>
        private byte[] _notifId;

        /// <summary>Tells if the broker is reset due to lost connection</summary>
        private bool _brokerReset = false;

        /// <summary>Cache Initialization Token</summary>
        private byte[] _token;

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

        /// <summary>Hold the getGroupKeys from search </summary>
        private ArrayList _resultList = new ArrayList();

        /// <summary>Hold bucket size returned from server</summary>
        private int _bucketSize = 0;

        /// <summary>Hold total number of buckets count</summary>
        private int _totalBuckets = 0;

        /// <summary> CacheItem object used to return CacheItem form GetCacheItem command</summary>

        private Web.Caching.CacheItem _cacheItem = null;

        private System.Net.IPAddress _clusterIp;
        private int _clusterPort;

        private List<BulkEventItemResponse> _eventList;
        // Azure remote cloud work
        private List<NCache.Config.Mapping> _serverMappingList;

        private Dictionary<string, int> _runningServers = new Dictionary<string, int>();
        
        private System.Net.IPAddress _serverIp;
        private int _serverPort;
        private bool _reconnectClients = false;

        private ExceptionType _excType;

        private TypeInfoMap _completeTypeMap;

        private HotConfig _hotConfig;

        private object _lockId;
        private DateTime _lockDate;
        private bool _parseLockingInfo;
        private bool _lockAcquired;
        private bool _isLocked;

        private string _cacheType;

       

        private bool _enableErrorLogs;
        private bool _enableDetailedLogs;
        private string _exceptionString;
        internal bool _operationSuccess;

        private bool _exists = false;
        private long _count = 0;

        private NewHashmap _newHashmap = null;

        private string _queryId;

        private Alachisoft.NCache.Caching.Queries.QueryResultSet _resultSet;


        private List<EnumerationDataChunk> _enumerationDataChunk = new List<EnumerationDataChunk>();

        Response _response;

        Alachisoft.NCache.Caching.EventId _eventId;
        EventDataFilter _dataFilter = EventDataFilter.None;


        private List<Common.DataReader.ReaderResultSet> _readerResultSets = new List<Common.DataReader.ReaderResultSet>();
        private Common.DataReader.ReaderResultSet _readerNextChunk = null;

        public List<Common.DataReader.ReaderResultSet> ReaderResultSets
        { get { return _readerResultSets; } }

        public Common.DataReader.ReaderResultSet ReaderNextChunk
        {
            get { return _readerNextChunk; }
            set { _readerNextChunk = value; }
        }
       

        public Alachisoft.NCache.Caching.EventId EventId
        {
            get { return _eventId; }
        }

        public Response ProtobufResponse
        {
            get { return _response; }
        }

        public int RemovedKeyCount
        {
            get { return _removedKeyCount; }
            set { _removedKeyCount = value; }
        }

        public EventDataFilter DataFilter { get { return _dataFilter; } }
        /// <summary>
        /// by default one response is sent back for each request. If required, a single response
        /// can be segmented into smaller chunks. In that case, these properties must be properly set.
        /// </summary>
        private int _sequenceId = 1;
        private int _numberOfChunks = 1;
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

        internal byte[] Token
        {
            get { return _token; }
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

                switch (value.responseType)
                {
                    case Response.Type.ADD:
                    case Response.Type.DELETE:
                    case Response.Type.DELETE_BULK:
                    case Response.Type.REGISTER_NOTIF:
                    case Response.Type.CLEAR:
                    case Response.Type.REGISTER_KEY_NOTIF:
                    case Response.Type.REGISTER_BULK_KEY_NOTIF:
                    case Response.Type.UNREGISTER_BULK_KEY_NOTIF:
                    case Response.Type.UNREGISTER_KEY_NOTIF:
                    case Response.Type.UNLOCK:
                    case Response.Type.DISPOSE:
                    case Response.Type.DISPOSE_READER:
                        _requestId = value.requestId;
                        break;

                    case Response.Type.COMPACT_TYPE_REGISTER_EVENT:
                        _value = value.compactTypeRegisterEvent.compactTypes;
                        break;

                    case Response.Type.COUNT:
                        _requestId = value.requestId;
                        this._count = value.count.count;
                        break;

                    case Response.Type.ADD_ATTRIBUTE:
                        _requestId = value.requestId;
                        _operationSuccess = value.addAttributeResponse.success;
                        break;

                    case Response.Type.CONTAINS:
                        _requestId = value.requestId;
                        this._exists = value.contain.exists;
                        break;

                    case Response.Type.CONFIG_MODIFIED_EVENT:
                        _hotConfig = HotConfig.FromString(value.configModified.hotConfig);
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
                            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value.itemRemovedCallback.value.ToArray());
                            Value = ubObject.GetFullObject();
                        }

                        _eventId = new Alachisoft.NCache.Caching.EventId();
                        _eventId.EventUniqueID = value.itemRemovedCallback.eventId.eventUniqueId;
                        _eventId.EventCounter = value.itemRemovedCallback.eventId.eventCounter;
                        _eventId.OperationCounter = value.itemRemovedCallback.eventId.operationCounter;
                        _eventId.EventType = NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;

                        break;

                    case Response.Type.CACHE_STOPPED_EVENT:
                        _cacheId = value.cacheStopped.cacheId;
                        break;
                        
                    case Response.Type.GET_SERVER_MAPPING:
                        _requestId = value.requestId;
                        if (value.getServerMappingResponse.serverMapping != null)
                        {
                            _serverMappingList = new List<Config.Mapping>();
                            foreach(Common.Protobuf.ServerMapping mappingObject in value.getServerMappingResponse.serverMapping)
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

                    case Response.Type.INSERT:
                        _requestId = value.requestId;
                        break;

                    case Response.Type.INIT:
                        _requestId = value.requestId;
                        _cacheType = value.initCache.cacheType;
                        _token = value.initCache.token;
                        _response = value;
                        break;

                    case Response.Type.GET:
                        _requestId = value.requestId;
                        _flagValueEntry.Flag = new BitSet((byte)value.get.flag);
                        _lockId = String.IsNullOrEmpty(value.get.lockId) ? null : value.get.lockId;
                        _lockDate = new DateTime(value.get.lockTime);

                        if (value.get.data.Count > 0)
                        {
                            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value.get.data.ToArray());
                            _value = ubObject.GetFullObject();
                        }

                        break;

                    case Response.Type.REMOVE:
                        _requestId = value.requestId;
                        _flagValueEntry.Flag = new BitSet((byte)value.remove.flag);

                        if (value.remove.value != null && value.remove.value.Count > 0)
                        {
                            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(value.remove.value.ToArray());
                            Value = ubObject.GetFullObject();
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

                        Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse keyValuePackage = value.bulkGet.keyValuePackage;
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
                                _flagValueEntry.Value = bytes;

                                _resultDic.Add(key, _flagValueEntry);

                                _flagValueEntry = null;
                            }
                        }
                        break;

                    case Response.Type.SEARCH_ENTRIES:
                        _requestId = value.requestId;

                        SearchEntriesResponse searchEntriesResponse = value.searchEntries;
                        Alachisoft.NCache.Common.Protobuf.QueryResultSet protoResultSet = searchEntriesResponse.queryResultSet;
                        _resultSet = new Alachisoft.NCache.Caching.Queries.QueryResultSet();

                        switch (protoResultSet.queryType)
                        {
                            case QueryType.AGGREGATE_FUNCTIONS:
                                _resultSet.Type = Alachisoft.NCache.Caching.Queries.QueryType.AggregateFunction;
                                _resultSet.AggregateFunctionType = (Alachisoft.NCache.Common.Enum.AggregateFunctionType)(int)protoResultSet.aggregateFunctionType;

                                if (protoResultSet.aggregateFunctionResult.value != null)
                                {
                                    _resultSet.AggregateFunctionResult = new DictionaryEntry(protoResultSet.aggregateFunctionResult.key, CompactBinaryFormatter.FromByteBuffer(protoResultSet.aggregateFunctionResult.value, null));
                                }
                                else
                                {
                                    _resultSet.AggregateFunctionResult = new DictionaryEntry(protoResultSet.aggregateFunctionResult.key, null);
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
                        }
                        break;

                    case Response.Type.EXECUTE_READER:
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
                                _resultDic.Add(key, new OperationFailedException(exc.message));
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
                                // 20110204 proto to QueryResultSet; if value is "" this means null is returned
                                _resultSet.AggregateFunctionType = (Alachisoft.NCache.Common.Enum.AggregateFunctionType)(int)protoResultSet.aggregateFunctionType;
                                if (protoResultSet.aggregateFunctionResult.value != null)
                                {
                                    _resultSet.AggregateFunctionResult = new DictionaryEntry(protoResultSet.aggregateFunctionResult.key, CompactBinaryFormatter.FromByteBuffer(protoResultSet.aggregateFunctionResult.value, null));
                                }
                                else
                                {
                                    _resultSet.AggregateFunctionResult = new DictionaryEntry(protoResultSet.aggregateFunctionResult.key, null);
                                }
                                break;

                            case QueryType.SEARCH_KEYS:
                                _resultSet.Type = Alachisoft.NCache.Caching.Queries.QueryType.SearchKeys;
                                _resultList.AddRange(protoResultSet.searchKeyResults);
                                _resultSet.SearchKeysResult = _resultList;
                                break;
                        }

                        break;

                    case Response.Type.GET_ENUMERATOR:
                        _requestId = value.requestId;
                        _resultList.AddRange(value.getEnum.keys);
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

                    case Response.Type.GET_CACHE_ITEM:

                        _requestId = value.requestId;

                        if (!string.IsNullOrEmpty(value.getItem.lockId))
                            _lockId = value.getItem.lockId;

                        _lockDate = new DateTime(value.getItem.lockTicks);
                        if (value.getItem != null && value.getItem.value.Count > 0)
                        {
                            this._cacheItem = new Web.Caching.CacheItem();
                            this._cacheItem._creationTime = new DateTime(value.getItem.creationTime);
                            this._cacheItem._lastModifiedTime = new DateTime(value.getItem.lastModifiedTime);
                            if (value.getItem.absExp != 0)
                            {
                                this._cacheItem.AbsoluteExpiration = new DateTime(value.getItem.absExp, DateTimeKind.Utc);
                            }
                            else
                            {
                                this._cacheItem.AbsoluteExpiration = Web.Caching.Cache.NoAbsoluteExpiration;
                            }
                            this._cacheItem.SlidingExpiration = new TimeSpan(value.getItem.sldExp);
                            this._cacheItem.FlagMap = new BitSet((byte)value.getItem.flag);
                            this._cacheItem.Priority = (CacheItemPriority)value.getItem.priority;
                            UserBinaryObject userObj = UserBinaryObject.CreateUserBinaryObject(value.getItem.value.ToArray());
                            this._cacheItem.Value = userObj.GetFullObject();
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

                    case Alachisoft.NCache.Common.Protobuf.Response.Type.HASHMAP_CHANGED_EVENT:
                        _requestId = value.requestId;
                        _value = value.hashmapChanged.table;
                        break;

                    case Response.Type.EXCEPTION:
                        _requestId = value.requestId;
                        _excType = (ExceptionType)value.exception.type;
                        _exceptionString = value.exception.message;
                        break;
                    case Response.Type.GET_OPTIMAL_SERVER:
                        _requestId = value.requestId;
                        _serverIp = System.Net.IPAddress.Parse(value.getOptimalServer.server);
                        _serverPort = value.getOptimalServer.port;
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

                    case Response.Type.GET_CACHE_BINDING:
                        _requestId = value.requestId;
                        _serverIp = System.Net.IPAddress.Parse(value.getCacheBindingResponse.server);
                        _serverPort = value.getCacheBindingResponse.port;
                        break;
              }
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


        internal Web.Caching.CacheItem Item

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
                    case ExceptionType.TYPE_INDEX_NOT_FOUND:
                        throw new OperationFailedException(_exceptionString);
                    case ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND:
                        throw new OperationFailedException(_exceptionString);
                    case ExceptionType.STATE_TRANSFER_EXCEPTION:
                        throw new StateTransferInProgressException(_exceptionString);
                    case ExceptionType.INVALID_READER_EXCEPTION:
                        throw new InvalidReaderException(_exceptionString);

                    case ExceptionType.MAX_CLIENTS_REACHED:
                        throw new System.Exception("Server cannot accept more than 3 clients in this edition of NCache.");
                }
            }
            else if (_brokerReset)
                throw new ConnectionException("Connection with server lost [" + _resetConnectionIP + "]");

        }
    }
}
