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
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Alachisoft.NCache.Web.Command;
using System.Collections;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Events;

namespace Alachisoft.NCache.Web.Communication
{
    internal class Request
    {
        private long _requestId = -1;
        private bool _isAsync = false;
        private bool _isBulk = false;
        private bool _isAsyncCallbackSpecified = false;
        private string _name;
        private string _cacheId;
        private CommandResponse _finalResponse = null;
        private object _responseMutex = new object();

        private Alachisoft.NCache.Common.Protobuf.Response.Type _type;

        private Dictionary<Common.Net.Address, Dictionary<int, ResponseList>> _responses =
            new Dictionary<Common.Net.Address, Dictionary<int, ResponseList>>();

        private Dictionary<Common.Net.Address, CommandBase> _commands =
            new Dictionary<Common.Net.Address, CommandBase>();

        private Dictionary<Common.Net.Address, EnumerationDataChunk> _chunks =
            new Dictionary<Common.Net.Address, EnumerationDataChunk>();

        private List<CommandBase> _failedCommands = new List<CommandBase>();


        private long _timeout = 90000;
        private bool _isrequestTimeoutReset = false;
        private Common.Net.Address _reRoutedAddress = null;
        private PollingResult pollingResult = new PollingResult();


        public Request(bool isBulk, long timeout)
        {
            _isBulk = isBulk;
            _timeout = timeout;
        }

        public bool IsDedicatedRequest
        {
            get
            {
                foreach (CommandBase command in _commands.Values)
                {
                    if (command.clientLastViewId == Broker.ForcedViewId)
                        return true;
                }

                return false;
            }
        }

        private int _commandID = 0;

        internal int GetNextCommandID()
        {
            return Interlocked.Increment(ref _commandID);
        }

        internal bool IsRequestTimeoutReset
        {
            get { return _isrequestTimeoutReset; }
            set { _isrequestTimeoutReset = value; }
        }

        internal long RequestTimeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        internal string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        internal List<CommandBase> FailedCommands
        {
            get { return _failedCommands; }
        }

        internal bool IsAyncCallbackSpecified
        {
            get { return _isAsyncCallbackSpecified; }
            set { _isAsyncCallbackSpecified = value; }
        }

        internal bool IsAsync
        {
            get { return _isAsync; }
            set { _isAsync = value; }
        }

        internal Common.Net.Address ReRoutedAddress
        {
            get { return _reRoutedAddress; }
            set { _reRoutedAddress = value; }
        }

        internal bool IsBulk
        {
            get { return _commands.Count > 1 || _isBulk; }
        }

        internal RequestType CommandRequestType
        {
            get
            {
                foreach (CommandBase command in _commands.Values)
                {
                    return command.CommandRequestType;
                }

                return RequestType.InternalCommand;
            }
        }

        internal long RequestId
        {
            get { return _requestId; }
            set
            {
                _requestId = value;

                foreach (CommandBase command in _commands.Values)
                {
                    command.RequestId = value;
                }
            }
        }

        internal Dictionary<Common.Net.Address, CommandBase> Commands
        {
            get { return _commands; }
        }

        internal int NumberOfCompleteResponses
        {
            get
            {
                int count = 0;

                lock (_responseMutex)
                {
                    foreach (Dictionary<int, ResponseList> allResponses in _responses.Values)
                    {
                        foreach (ResponseList responseList in allResponses.Values)
                        {
                            if (responseList.IsComplete)
                                count++;
                        }
                    }
                }

                return count;
            }
        }

        internal string TimeoutMessage
        {
            get
            {
                StringBuilder sb = new StringBuilder("Operation timed out. No response from the server(s).");

                sb.Append(" [");

                lock (_responseMutex)
                {
                    foreach (CommandBase commandBase in _commands.Values)
                    {
                        if (!_responses.ContainsKey(commandBase.FinalDestinationAddress))
                        {
                            sb.Append(commandBase.FinalDestinationAddress + ", ");
                        }
                        else
                        {
                            if (ExpectingResponseFrom(commandBase.FinalDestinationAddress))
                            {
                                sb.Append(commandBase.FinalDestinationAddress + ", ");
                            }
                        }
                    }
                }

                sb.Remove(sb.Length - 2, 2);
                sb.Append("]");

                return sb.ToString();
            }
        }

        internal CommandResponse Response
        {
            get
            {
                lock (_responseMutex)
                {
                    foreach (KeyValuePair<Common.Net.Address, Dictionary<int, ResponseList>> allResponses in _responses)
                    {
                        //Iterates over all ResponseList objects against one IP
                        foreach (ResponseList responses in allResponses.Value.Values)
                        {
                            //Iterates over all CommandResponse objects in each ResponseList
                            foreach (CommandResponse rsp in responses.Responses)
                            {
                                //in case exception is not thrown from 1st server.
                                if (!string.IsNullOrEmpty(rsp.ExceptionMsg) && rsp.ExceptionType !=
                                                                            Alachisoft.NCache.Common.Enum.ExceptionType
                                                                                .STATE_TRANSFER_EXCEPTION
                                                                            && rsp.ExceptionType !=
                                                                            Alachisoft.NCache.Common.Enum.ExceptionType
                                                                                .ATTRIBUTE_INDEX_NOT_FOUND
                                                                            && rsp.ExceptionType !=
                                                                            Alachisoft.NCache.Common.Enum.ExceptionType
                                                                                .TYPE_INDEX_NOT_FOUND)
                                {
                                    _finalResponse = rsp;
                                    return _finalResponse;
                                }

                                MergeResponse(allResponses.Key, rsp);
                            }
                        }
                    }
                }

                return _finalResponse;
            }
        }

        internal bool IsCompleteResponseReceived
        {
            get { return NumberOfCompleteResponses >= _commands.Count; }
        }

        internal void AddCommand(Common.Net.Address address, CommandBase command)
        {
            _name = command.CommandName;
            command.Parent = this;
            command.CommandID = this.GetNextCommandID();

            if (!_commands.ContainsKey(address))
                _commands.Add(address, command);
        }

        internal void AddResponse(Common.Net.Address address, CommandResponse response)
        {
            _type = response.Type;

            lock (_responseMutex)
            {
                if (_responses.ContainsKey(address))
                {
                    ResponseList responseList;
                    if (_responses[address].TryGetValue(response.CommandID, out responseList))
                    {
                        if (!responseList.IsComplete)
                        {
                            responseList.AddResponse(response);
                        }
                    }
                }
            }
        }


        internal void InitializeResponse(Common.Net.Address address, CommandBase command)
        {
            lock (_responseMutex)
            {
                if (!_responses.ContainsKey(address))
                {
                    _responses.Add(address, new Dictionary<int, ResponseList>());
                }

                Dictionary<int, ResponseList> allResponses = _responses[address];
                allResponses[command.CommandID] = new ResponseList(command);
            }

            command.FinalDestinationAddress = address;
        }

        internal void InitializeFailedResponse(Common.Net.Address address, CommandBase command)
        {
            lock (_responseMutex)
            {
                if (!_responses.ContainsKey(address))
                {
                    _responses.Add(address, new Dictionary<int, ResponseList>());
                }

                Dictionary<int, ResponseList> allResponses = _responses[address];
                allResponses[command.CommandID] = new ResponseList(command);
                allResponses[command.CommandID].AddResponse(new CommandResponse(true, address));
            }

            command.FinalDestinationAddress = address;
        }

        internal void RemoveResponse(Common.Net.Address address, int commandId)
        {
            lock (_responseMutex)
            {
                if (address != null && _responses.ContainsKey(address))
                {
                    _responses[address].Remove(commandId);
                    if (_responses[address].Count == 0)
                        _responses.Remove(address);
                }
            }
        }

        internal void ClearResponses()
        {
            lock (_responseMutex)
            {
                _responses.Clear();
            }
        }

        internal bool ExpectingResponseFrom(Common.Net.Address address)
        {
            lock (_responseMutex)
            {
                if (_responses.ContainsKey(address))
                {
                    //True if response from specified ip address is not completed, else false
                    foreach (ResponseList responseList in _responses[address].Values)
                    {
                        if (!responseList.IsComplete)
                            return true;
                    }
                }
            }

            return false;
        }

        internal void Reset(Common.Net.Address ip)
        {
            lock (_responseMutex)
            {
                if (_responses.ContainsKey(ip))
                {
                    Dictionary<int, ResponseList> allResponses = _responses[ip];
                    foreach (ResponseList responseList in allResponses.Values)
                    {
                        if (!responseList.IsComplete)
                        {
                            responseList.Clear();
                            responseList.AddResponse(new CommandResponse(true, ip));
                            _failedCommands.Add(responseList.Command);
                        }
                    }
                }
            }
        }


        internal void SetAggregateFunctionResult()
        {
            switch (_finalResponse.ResultSet.AggregateFunctionType)
            {
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.MAX:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.MAX,
                        _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.MIN:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.MIN,
                        _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.COUNT:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.COUNT,
                        _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.SUM:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.SUM,
                        _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.AVG:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.AVG,
                        _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
            }
        }

        internal void MergeResponse(Common.Net.Address address, CommandResponse response)
        {
            if (_finalResponse == null &&
                (response.Type != Alachisoft.NCache.Common.Protobuf.Response.Type.GET_NEXT_CHUNK &&
                 response.Type != Alachisoft.NCache.Common.Protobuf.Response.Type.GET_READER_CHUNK &&
                 response.Type != Alachisoft.NCache.Common.Protobuf.Response.Type.EXECUTE_READER &&
                 response.Type != Alachisoft.NCache.Common.Protobuf.Response.Type.EXECUTE_READER_CQ))
            {
                _finalResponse = response;

                if (response.IsBrokerReset)
                {
                    MergeFailedResponse(response);
                }
            }
            else
            {
                if (response.IsBrokerReset)
                {
                    MergeFailedResponse(response);
                }
                else
                {
                    IDictionaryEnumerator ide = null;
                    switch (response.Type)
                    {
                        case Common.Protobuf.Response.Type.POLL:
                            _finalResponse.PollingResult.RemovedKeys.AddRange(response.PollingResult.RemovedKeys);
                            _finalResponse.PollingResult.UpdatedKeys.AddRange(response.PollingResult.UpdatedKeys);

                            break;

                        case Common.Protobuf.Response.Type.ADD_BULK:
                        case Common.Protobuf.Response.Type.INSERT_BULK:
                        case Common.Protobuf.Response.Type.GET_BULK:
                        case Common.Protobuf.Response.Type.REMOVE_BULK:
                        case Common.Protobuf.Response.Type.GET_GROUP_DATA:
                        case Common.Protobuf.Response.Type.GET_TAG:
                        case Common.Protobuf.Response.Type.HYBRID_BULK:
                        case Common.Protobuf.Response.Type.INVOKE_ENTRY_PROCESSOR:
                        case Common.Protobuf.Response.Type.MESSAGE_ACKNOWLEDGEMENT:
                            ide = response.KeyValueDic.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                _finalResponse.KeyValueDic[ide.Key] = ide.Value;
                            }

                            ide = response.KeyVersionDic.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                _finalResponse.KeyVersionDic[ide.Key] = ide.Value;
                            }

                            break;
                        case Common.Protobuf.Response.Type.GET_MESSAGE:

                            foreach (var item in response.MessageDic)
                            {
                                if (!_finalResponse.MessageDic.ContainsKey(item.Key))
                                {
                                    _finalResponse.MessageDic.Add(item.Key, item.Value);
                                }
                                else
                                {
                                    foreach (var message in item.Value)
                                        _finalResponse.MessageDic[item.Key].Add(message);
                                }
                            }

                            break;
                        case Common.Protobuf.Response.Type.GET_GROUP_KEYS:
                        case Common.Protobuf.Response.Type.GET_KEYS_TAG:
                            _finalResponse.KeyList.AddRange(response.KeyList);
                            break;

                        case Common.Protobuf.Response.Type.EXECUTE_READER:
                        case Common.Protobuf.Response.Type.EXECUTE_READER_CQ:

                            if (_finalResponse == null)
                            {
                                _finalResponse = response;
                            }

                            if ((_finalResponse.ExceptionType == Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND)
                                || (_finalResponse.ExceptionType ==
                                    Common.Enum.ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND))
                            {
                                _finalResponse = response;
                                break;
                            }

                            List<ReaderResultSet> protoReaders =
                                response.Type == Common.Protobuf.Response.Type.EXECUTE_READER
                                    ? response.ProtobufResponse.executeReaderResponse.readerResultSets
                                    : response.ProtobufResponse.executeReaderCQResponse.readerResultSets;

                            if (protoReaders != null && protoReaders.Count > 0)
                            {
                                foreach (ReaderResultSet protoReaderResultSet in protoReaders)
                                {
                                    Common.DataReader.ReaderResultSet readerResultSet = null;

                                    foreach (Common.DataReader.ReaderResultSet set in _finalResponse.ReaderResultSets)
                                    {
                                        if (protoReaderResultSet.readerId == set.ReaderID &&
                                            protoReaderResultSet.nodeAddress == set.NodeAddress)
                                        {
                                            readerResultSet = set;
                                            break;
                                        }
                                    }

                                    if (readerResultSet != null)
                                    {
                                        PopulateRows((Common.DataReader.RecordSet) readerResultSet.RecordSet,
                                            protoReaderResultSet.recordSet.rows);
                                    }
                                    else
                                    {
                                        readerResultSet = ConvertToReaderResult(protoReaderResultSet);
                                        _finalResponse.ReaderResultSets.Add(readerResultSet);
                                    }
                                }
                            }

                            break;

                        case Common.Protobuf.Response.Type.GET_READER_CHUNK:

                            if (_finalResponse == null)
                                _finalResponse = response;

                            ReaderResultSet protoReaderChunkResultSet =
                                response.ProtobufResponse.getReaderChunkResponse.readerResultSets;
                            Common.DataReader.ReaderResultSet readerChunkResultSet = _finalResponse.ReaderNextChunk;

                            if (readerChunkResultSet != null)
                            {
                                PopulateRows((Common.DataReader.RecordSet) readerChunkResultSet.RecordSet,
                                    protoReaderChunkResultSet.recordSet.rows);
                            }
                            else
                            {
                                readerChunkResultSet = ConvertToReaderResult(protoReaderChunkResultSet);
                                _finalResponse.ReaderNextChunk = readerChunkResultSet;
                            }

                            break;

                        case Common.Protobuf.Response.Type.DELETE_QUERY:
                        case Common.Protobuf.Response.Type.REMOVE_QUERY:

                            if ((_finalResponse.ExceptionType == Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND) ||
                                (_finalResponse.ExceptionType == Alachisoft.NCache.Common.Enum.ExceptionType
                                     .ATTRIBUTE_INDEX_NOT_FOUND))
                            {
                                if ((response.ExceptionType != Common.Enum.ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND) ||
                                    (response.ExceptionType != Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND))
                                {
                                    _finalResponse = response;
                                }
                            }
                            else if (_finalResponse != null &&
                                     (response.ExceptionType != Common.Enum.ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND) ||
                                     (response.ExceptionType != Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND))
                            {
                                _finalResponse.RemovedKeyCount += response.RemovedKeyCount;
                            }

                            break;

                        case Common.Protobuf.Response.Type.SEARCH_CQ:
                            _finalResponse.KeyList.AddRange(response.KeyList);
                            if (response.ResultSet != null && !string.IsNullOrEmpty(response.ResultSet.CQUniqueId))
                            {
                                string uniqueID = response.ResultSet.CQUniqueId;
                                if (!string.IsNullOrEmpty(uniqueID) && uniqueID != "-1")
                                {
                                    if (_finalResponse.ResultSet != null &&
                                        string.IsNullOrEmpty(_finalResponse.ResultSet.CQUniqueId))
                                        _finalResponse.ResultSet.CQUniqueId = uniqueID;
                                }
                            }

                            break;
                        case Common.Protobuf.Response.Type.SEARCH_ENTRIES:

                            if ((_finalResponse.ExceptionType == Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND)
                                || (_finalResponse.ExceptionType ==
                                    Common.Enum.ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND))
                            {
                                _finalResponse = response;
                                break;
                            }

                            switch (response.ResultSet.Type)
                            {
                                case NCache.Caching.Queries.QueryType.GroupByAggregateFunction:
                                    break;
                                default:
                                    switch (response.ResultSet.AggregateFunctionType)
                                    {
                                        case Alachisoft.NCache.Common.Enum.AggregateFunctionType.NOTAPPLICABLE:
                                            ide = response.KeyValueDic.GetEnumerator();
                                            while (ide.MoveNext())
                                            {
                                                _finalResponse.KeyValueDic[ide.Key] = ide.Value;
                                            }

                                            break;

                                        default:
                                            if (!_finalResponse.ResultSet.IsInitialized)
                                            {
                                                SetAggregateFunctionResult();
                                                _finalResponse.ResultSet.Initialize(_finalResponse.ResultSet);
                                            }

                                            _finalResponse.ResultSet.Compile(response.ResultSet);
                                            break;
                                    }

                                    break;
                            }

                            break;


                        case Alachisoft.NCache.Common.Protobuf.Response.Type.SEARCH:

                            if ((_finalResponse.ExceptionType ==
                                 Alachisoft.NCache.Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND)
                                || (_finalResponse.ExceptionType == Alachisoft.NCache.Common.Enum.ExceptionType
                                        .ATTRIBUTE_INDEX_NOT_FOUND))
                            {
                                _finalResponse = response;
                                break;
                            }

                            switch (response.ResultSet.AggregateFunctionType)
                            {
                                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.NOTAPPLICABLE:
                                    _finalResponse.KeyList.AddRange(response.KeyList);
                                    break;

                                default:
                                    if (!_finalResponse.ResultSet.IsInitialized)
                                    {
                                        SetAggregateFunctionResult();
                                        _finalResponse.ResultSet.Initialize(_finalResponse.ResultSet);
                                    }

                                    _finalResponse.ResultSet.Compile(response.ResultSet);
                                    break;
                            }

                            break;
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.GET_SERVER_MAPPING:
                            _finalResponse.ServerMappingList = response.ServerMappingList;
                            break;
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.SEARCH_ENTRIES_CQ:
                            ide = response.KeyValueDic.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                _finalResponse.KeyValueDic[ide.Key] = ide.Value;
                            }

                            if (response.ResultSet != null && response.ResultSet.CQUniqueId != null)
                            {
                                string uniqueID = response.ResultSet.CQUniqueId;
                                if (!string.IsNullOrEmpty(uniqueID) && uniqueID != null)
                                {
                                    if (_finalResponse.ResultSet != null &&
                                        string.IsNullOrEmpty(_finalResponse.ResultSet.CQUniqueId))
                                    {
                                        _finalResponse.ResultSet.CQUniqueId = response.ResultSet.CQUniqueId;
                                    }
                                }
                            }

                            break;

                        case Alachisoft.NCache.Common.Protobuf.Response.Type.GET_NEXT_CHUNK:
                            if (_finalResponse == null)
                                _finalResponse = response;

                            EnumerationDataChunk chunk = null;
                            if (_chunks.ContainsKey(address))
                            {
                                chunk = _chunks[address];
                            }
                            else
                            {
                                chunk = new EnumerationDataChunk();
                                chunk.Data = new List<string>();
                                _chunks.Add(address, chunk);
                            }

                            for (int i = 0; i < response.NextChunk.Count; i++)
                            {
                                chunk.Data.AddRange(response.NextChunk[i].Data);
                                chunk.Pointer = response.NextChunk[i].Pointer;
                                if (chunk.Pointer.NodeIpAddress == null)
                                    chunk.Pointer.NodeIpAddress = address;
                            }

                            _finalResponse.NextChunk = new List<EnumerationDataChunk>(_chunks.Values);

                            break;
                        case Common.Protobuf.Response.Type.TASK_ENUMERATOR:
                            _finalResponse.TaskEnumerator.AddRange(response.TaskEnumerator);
                            break;
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.EXCEPTION:
                            if (response.ExceptionType == Common.Enum.ExceptionType.STATE_TRANSFER_EXCEPTION)
                            {
                                _finalResponse = response;
                            }

                            break;
                        default:
                            break;
                    }
                }
            }
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
                arg.Order = (Common.Queries.Order) Convert.ToInt32(obaProto.order);
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
                        (Common.Enum.AggregateFunctionType) Convert.ToInt32(columnProto.aggregateFunctionType);
                    column.ColumnType = (Common.Enum.ColumnType) Convert.ToInt32(columnProto.columnType);
                    column.DataType = (Common.Enum.ColumnDataType) Convert.ToInt32(columnProto.dataType);
                    column.IsFilled = columnProto.isFilled;
                    column.IsHidden = columnProto.isHidden;
                    recordSet.AddColumn(column);
                }

                PopulateRows(recordSet, recordSetProto.rows);
            }

            readerResultSet.RecordSet = recordSet;
            return readerResultSet;
        }

        private void PopulateRows(Common.DataReader.RecordSet recordSet, List<RecordRow> rows)
        {
            try
            {
                if (recordSet != null && rows != null)
                {
                    foreach (RecordRow rowProto in rows)
                    {
                        Common.DataReader.RecordRow row = recordSet.CreateRow();
                        if (recordSet.Columns != null)
                            for (int i = 0; i < recordSet.Columns.Count; i++)
                            {
                                if (rowProto.values[i] != null)
                                {
                                    if (recordSet.Columns[i].DataType != null)
                                        switch (recordSet.Columns[i].DataType)
                                        {
                                            case Common.Enum.ColumnDataType.AverageResult:

                                                Common.Queries.AverageResult avgResult =
                                                    new Common.Queries.AverageResult();
                                                avgResult.Sum = Convert.ToDecimal(rowProto.values[i].avgResult.sum);
                                                avgResult.Count = Convert.ToDecimal(rowProto.values[i].avgResult.count);
                                                row[i] = avgResult;
                                                break;
                                            case Common.Enum.ColumnDataType.CompressedValueEntry:
                                                Value val = rowProto.values[i].binaryObject;
                                                UserBinaryObject ubObject =
                                                    UserBinaryObject.CreateUserBinaryObject(val.data.ToArray());
                                                byte[] bytes = ubObject.GetFullObject();
                                                CompressedValueEntry cmpEntry = new CompressedValueEntry();
                                                cmpEntry.Flag = new BitSet((byte) rowProto.values[i].flag);
                                                cmpEntry.Value = bytes;
                                                row[i] = ConvertToUserObject(cmpEntry);
                                                break;
                                            default:
                                                row[i] = Common.DataReader.RecordSet.ToObject(
                                                    rowProto.values[i].stringValue, recordSet.Columns[i].DataType);
                                                break;
                                        }
                                }
                            }

                        recordSet.AddRow(row);
                    }
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidReaderException("Reader state has been lost.: ", ex);
            }
        }

        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
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


            return CompactBinaryFormatter.FromByteBuffer((byte[]) cmpEntry.Value, CacheId);
        }

        private void MergeFailedResponse(CommandResponse response)
        {
            CommandBase command;
            bool success = _commands.TryGetValue(response.ResetConnectionIP, out command);
            if (!success)
                _commands.TryGetValue(new Address(IPAddress.Any, 9800), out command);

            switch (_type)
            {
                case Common.Protobuf.Response.Type.ADD_BULK:
                case Common.Protobuf.Response.Type.INSERT_BULK:
                case Common.Protobuf.Response.Type.GET_BULK:
                case Common.Protobuf.Response.Type.REMOVE_BULK:
                case Common.Protobuf.Response.Type.INVOKE_ENTRY_PROCESSOR:
                    string key;
                    for (int index = 0; index < command.BulkKeys.Length; index++)
                    {
                        key = command.BulkKeys[index];
                        _finalResponse.KeyValueDic[key] =
                            new ConnectionException("Connection with server lost [" + response.ResetConnectionIP + "]");
                    }

                    _finalResponse.SetBroker = false;
                    break;

                case Common.Protobuf.Response.Type.GET_GROUP_DATA:
                case Common.Protobuf.Response.Type.GET_TAG:
                case Common.Protobuf.Response.Type.GET_GROUP_KEYS:
                case Common.Protobuf.Response.Type.GET_KEYS_TAG:
                case Common.Protobuf.Response.Type.SEARCH:
                case Common.Protobuf.Response.Type.SEARCH_CQ:
                case Common.Protobuf.Response.Type.SEARCH_ENTRIES:
                case Common.Protobuf.Response.Type.SEARCH_ENTRIES_CQ:
                case Common.Protobuf.Response.Type.POLL:
                case Common.Protobuf.Response.Type.EXECUTE_READER:
                case Common.Protobuf.Response.Type.EXECUTE_READER_CQ:
                case Common.Protobuf.Response.Type.MESSAGE_ACKNOWLEDGEMENT:
                case Common.Protobuf.Response.Type.GET_MESSAGE:
                    _finalResponse.SetBroker = true;
                    _finalResponse.ResetConnectionIP = response.ResetConnectionIP;
                    break;
            }
        }
    }
}