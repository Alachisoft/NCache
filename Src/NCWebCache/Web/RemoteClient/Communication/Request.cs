// Copyright (c) 2015 Alachisoft
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
using System.Text;
using Alachisoft.NCache.Web.Command;
using System.Collections;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Stats;
using Common = Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Web.Communication
{
    internal class Request
    {
        private long _requestId = -1;
        private bool _isAsync = false;
        private bool _isBulk = false;
        private bool _isAsyncCallbackSpecified = false;
        private string _name;
        private CommandResponse _finalResponse = null;
        private object _responseMutex = new object();

        private Alachisoft.NCache.Common.Protobuf.Response.Type _type;
        private Dictionary<Common.Net.Address, ResponseList> _responses = new Dictionary<Common.Net.Address, ResponseList>();
        private Dictionary<Common.Net.Address, CommandBase> _commands = new Dictionary<Common.Net.Address, CommandBase>();
        private Dictionary<Common.Net.Address, EnumerationDataChunk> _chunks = new Dictionary<Common.Net.Address, EnumerationDataChunk>();
        
        private long _timeout = 90000;
        private bool _isrequestTimeoutReset = false;
        private bool _resend = false;
        private Common.Net.Address _reRoutedAddress = null;

        public Request(bool isBulk, long timeout)
        {
            _isBulk = isBulk;
            _timeout = timeout;
        }

        internal bool IsRequestTimeoutReset
        {
            get { return _isrequestTimeoutReset; }
            set { _isrequestTimeoutReset = value; }
        }

        internal bool IsResendReuest
        {
            get { return _resend; }
            set { _resend = value; }
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

        internal bool IsDedicated(long viewId)
        {
                foreach (CommandBase command in _commands.Values)
                {
                    return command.clientLastViewId == viewId;
                }

                return false;
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
                    foreach (ResponseList responseList in _responses.Values)
                    {
                        if (responseList.IsComplete)
                            count++;
                    }
                }

                return count;
            }
        }

        internal string TimeoutMessage
        {
            get
            {
                StringBuilder sb = new StringBuilder("Operation timed out. No response from the server(s)." );

                sb.Append(" [");

                lock (_responseMutex)
                {
                    foreach (Common.Net.Address ip in Commands.Keys) 
                    {
                        if (!_responses.ContainsKey(ip))
                        {
                            sb.Append(ip + ", ");
                        }
                        else
                        {
                            ResponseList response = _responses[ip];
                            if (!response.IsComplete)
                            {
                                sb.Append(ip + ", ");
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
                    foreach (KeyValuePair<Common.Net.Address, ResponseList> responses in _responses)
                    {
                        foreach (CommandResponse rsp in responses.Value.Responses.Values)
                        {
                            //in case exception is not thrown from 1st server.
                            if (!string.IsNullOrEmpty(rsp.ExceptionMsg) && rsp.ExceptionType != Alachisoft.NCache.Common.Enum.ExceptionType.STATE_TRANSFER_EXCEPTION
                                && rsp.ExceptionType != Alachisoft.NCache.Common.Enum.ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND
                                && rsp.ExceptionType != Alachisoft.NCache.Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND)
                            {
                                _finalResponse = rsp;
                                return _finalResponse;
                            }
                            MergeResponse(responses.Key, rsp);                            
                        }
                    }
                }
                return _finalResponse;
            }
        }

        internal bool Responses
        {
            get
            {
                return NumberOfCompleteResponses == _commands.Count;
            }
        }

        internal void AddCommand(Common.Net.Address address, CommandBase command)
        {
            _name = command.CommandName;
            command.Parent = this;

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
                    ResponseList responseList = _responses[address];
                    if (!responseList.IsComplete)
                    {
                        responseList.AddResponse(response);
                    }
                    else
                    {
                        if (_reRoutedAddress != null && !_reRoutedAddress.Equals(address))
                        {
                            if (!_responses.ContainsKey(_reRoutedAddress))
                            {
                                ResponseList rspList = new ResponseList();
                                if (!rspList.IsComplete)
                                {
                                    rspList.AddResponse(response);
                                }

                                _responses.Add(_reRoutedAddress, rspList);
                            }
                            else
                            {
                                responseList = _responses[_reRoutedAddress];
                                if (!responseList.IsComplete)
                                {
                                    responseList.AddResponse(response);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void InitializeResponse(Common.Net.Address address)
        {
            lock (_responseMutex)
            {
                if (!_responses.ContainsKey(address))
                {
                    _responses.Add(address, new ResponseList());
                }
            }
        }

        internal bool RemoveResponse(Common.Net.Address address)
        {
            lock (_responseMutex)
            {
                _responses.Remove(address);

                bool removeRequestFromTable = _responses.Count == 0;

                return removeRequestFromTable;
            }
        }

        internal bool ExpectingResponseFrom(Common.Net.Address address)
        {
            lock (_responseMutex)
            {
                bool result = _responses.ContainsKey(address);
                return result;
            }
        }

        internal void Reset(Common.Net.Address ip)
        {
            lock (_responseMutex)
            {
                if (_responses.ContainsKey(ip))
                {
                    ResponseList responseList = _responses[ip];
                    responseList.Clear();

                    responseList.AddResponse(new CommandResponse(true, ip));

                    _responses[ip] = responseList; 
                }
            }
        }

        internal void ResetFailedResponse(Common.Net.Address ip)
        {
            lock (_responseMutex)
            {
                if (_responses.ContainsKey(ip))
                {
                    ResponseList responseList = _responses[ip];
                    responseList.Clear();
                    responseList.AddResponse(new CommandResponse(true, ip));
                }
            }
        }

        internal void ResetResponseNodeForShutDown(Common.Net.Address ip)
        {
            lock (_responseMutex)
            {
                if (_responses.ContainsKey(ip))
                {
                    ResponseList responseList = _responses[ip];
                    responseList.Clear();
                    responseList.AddResponse(new CommandResponse(false, ip));
                    _resend = true;
                }
            }
        }


        internal void SetAggregateFunctionResult()
        {
            switch (_finalResponse.ResultSet.AggregateFunctionType)
            {
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.MAX:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.MAX, _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.MIN:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.MIN, _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.COUNT:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.COUNT, _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.SUM:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.SUM, _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
                case Alachisoft.NCache.Common.Enum.AggregateFunctionType.AVG:
                    _finalResponse.ResultSet.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.AVG, _finalResponse.ResultSet.AggregateFunctionResult.Value);
                    break;
            }
        }

        internal void MergeResponse(Common.Net.Address address, CommandResponse response)
        {
            if (_finalResponse == null && response.Type != Alachisoft.NCache.Common.Protobuf.Response.Type.GET_NEXT_CHUNK)
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
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.ADD_BULK:
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.INSERT_BULK:
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.GET_BULK:
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.REMOVE_BULK:
                            ide = response.KeyValueDic.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                _finalResponse.KeyValueDic[ide.Key] = ide.Value;
                            }
                            break;                       
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.SEARCH:

                            if ((_finalResponse.ExceptionType == Alachisoft.NCache.Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND) 
                                || (_finalResponse.ExceptionType == Alachisoft.NCache.Common.Enum.ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND))
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
                       
                       case Alachisoft.NCache.Common.Protobuf.Response.Type.SEARCH_ENTRIES:

                            if ((_finalResponse.ExceptionType == Alachisoft.NCache.Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND) 
                                || (_finalResponse.ExceptionType == Alachisoft.NCache.Common.Enum.ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND))
                            {
                                _finalResponse = response;
                                break;
                            }
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
                                if (chunk.Pointer.NodeIpAddress==null)
                                    chunk.Pointer.NodeIpAddress = address;
                            }

                            _finalResponse.NextChunk = new List<EnumerationDataChunk>(_chunks.Values);

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

        private void MergeFailedResponse(CommandResponse response)
        {
            CommandBase command;
            _commands.TryGetValue(response.ResetConnectionIP, out command);

            switch (_type)
            {
                case Alachisoft.NCache.Common.Protobuf.Response.Type.ADD_BULK:
                case Alachisoft.NCache.Common.Protobuf.Response.Type.INSERT_BULK:
                case Alachisoft.NCache.Common.Protobuf.Response.Type.GET_BULK:
                case Alachisoft.NCache.Common.Protobuf.Response.Type.REMOVE_BULK:
                    string key;
                    for (int index = 0; index < command.BulkKeys.Length; index++)
                    {
                        key = command.BulkKeys[index];
                        _finalResponse.KeyValueDic[key] = new ConnectionException("Connection with server lost [" + response.ResetConnectionIP + "]");
                    }
                    _finalResponse.SetBroker = false;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Response.Type.SEARCH:
                case Alachisoft.NCache.Common.Protobuf.Response.Type.SEARCH_ENTRIES:
                    _finalResponse.SetBroker = true;
                    _finalResponse.ResetConnectionIP = response.ResetConnectionIP;
                    break;
            }
        }
    }
}
