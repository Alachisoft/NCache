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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Serialization.Formatters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.IO;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Client
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
        private Dictionary<Common.Net.Address, Dictionary<int, ResponseList>> _responses;
        private Dictionary<Common.Net.Address, CommandBase> _commands;
        private Dictionary<Common.Net.Address, EnumerationDataChunk> _chunks;
        private List<CommandBase> _failedCommands;
        private List<CommandResponse> _rawResponses = null;
		
		
        private long _timeout = 90000;
        private bool _isrequestTimeoutReset = false;
        private Common.Net.Address _reRoutedAddress = null;


        public Request(bool isBulk, long timeout)
        {
            _isBulk = isBulk;
            _timeout = timeout;
        }

        private bool _isDedicatedRequest = false;

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
            get
            {
                if (_failedCommands == null) _failedCommands = new List<CommandBase>();
                return _failedCommands; 
            }
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
            get
            {
                if (_commands == null) _commands = new Dictionary<Common.Net.Address, CommandBase>();
                return _commands;
            }
        }

        internal int NumberOfCompleteResponses(bool includeFailedResponsesOnly)
        {
            int count = 0;
            lock (_responseMutex)
            {
                DeserializeRawResponsesIfPresent();
                foreach (Dictionary<int, ResponseList> allResponses in _responses.Values)
                {
                    foreach (ResponseList responseList in allResponses.Values)
                    {
                        if (responseList.IsComplete(includeFailedResponsesOnly))
                            count++;
                    }
                }
            }

            return count;
        }

        internal string TimeoutMessage
        {
            get
            {
                StringBuilder sb = new StringBuilder("Operation timed out. No response from the server(s).");//+ rmsg);

                sb.Append(" [");

                lock (_responseMutex)
                {
                    foreach (var  commandBase in _commands)
                    {
                        if (!_responses.ContainsKey(commandBase.Key))
                        {
                            sb.Append(commandBase.Key + ", ");
                        }
                        else
                        {
                            if (ExpectingResponseFrom(commandBase.Key))
                            {
                                sb.Append(commandBase.Key + ", ");
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
                    DeserializeRawResponsesIfPresent();
                    foreach (KeyValuePair<Common.Net.Address, Dictionary<int, ResponseList>> allResponses in _responses)
                    {
                        //Iterates over all ResponseList objects against one IP
                        foreach (ResponseList responses in allResponses.Value.Values)
                        {
                            //Iterates over all CommandResponse objects in each ResponseList
                            foreach (CommandResponse rsp in responses.Responses)

                            {
                                //in case exception is not thrown from 1st server.
                                if (!string.IsNullOrEmpty(rsp.ExceptionMsg) && rsp.ExceptionType != Alachisoft.NCache.Common.Enum.ExceptionType.STATE_TRANSFER_EXCEPTION
                                    && rsp.ExceptionType != Alachisoft.NCache.Common.Enum.ExceptionType.ATTRIBUTE_INDEX_NOT_FOUND
                                    && rsp.ExceptionType != Alachisoft.NCache.Common.Enum.ExceptionType.TYPE_INDEX_NOT_FOUND)
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

        internal Dictionary<Address,ResponseList> GetRespnses()
        {
            Dictionary<Address, ResponseList> responses = new Dictionary<Address, ResponseList>();

            foreach (KeyValuePair<Common.Net.Address, Dictionary<int, ResponseList>> allResponses in _responses)
            {
                foreach (ResponseList rspList in allResponses.Value.Values)
                {
                    ResponseList existingList = null;
                    if (responses.TryGetValue(allResponses.Key,out existingList))
                    {
                        existingList.MergeWith(rspList);
                    }
                    else
                        responses.Add(allResponses.Key, rspList);
                }
            }

            return responses;
        }

        internal bool IsCompleteResponseReceived
        {
            get
            {
                return NumberOfCompleteResponses(false) >= _commands.Count;
            }
        }

        internal bool HasFailedResponses
        {
            get
            {
                return NumberOfCompleteResponses(true) > 0;
            }
        }

        internal void AddCommand(Common.Net.Address address, CommandBase command)
        {
            _name = command.CommandName;
            command.Parent = this;
            command.CommandID = this.GetNextCommandID();

            if (_commands == null) _commands = new Dictionary<Common.Net.Address, CommandBase>();

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
                    if (!response.IsInternalResponse && response.NeedsDeserialization)
                    {
                        if (_rawResponses == null)
                            _rawResponses = new List<CommandResponse>();

                        _rawResponses.Add(response);
                    }
                    else
                    {
                        ResponseList responseList;
                        if (response.CommandID <= 0)
                        {
                            var responses = _responses[address];
                            foreach(var rsp in responses.Values)
                            {
                                if (!rsp.IsComplete())
                                {
                                    rsp.AddResponse(response);
                                    break;
                                }
                            }
                            return;
                        }

                        
                        if (_responses[address].TryGetValue(response.CommandID, out responseList))
                        {
                            if (Connection.WriteRequestIdInResponse || !responseList.IsComplete())
                            {
                                responseList.AddResponse(response);
                            }
                        }
                    }
                }
            }
        }
        private void DeserializeRawResponsesIfPresent()
        {
            if (_rawResponses != null && _rawResponses.Count >0)
            {
                for (int i = 0; i < _rawResponses.Count; i++)
                {
                    CommandResponse respnse = _rawResponses[i];
                    using (Stream tempStream = new ClusteredMemoryStream(respnse.SerializedResponse))
                        respnse.Result = ResponseHelper.DeserializeResponse(respnse.Type, tempStream);

                    respnse.NeedsDeserialization = false;
                    AddResponse(respnse.Src, respnse);
                
                }
                _rawResponses.Clear();
            }
        }

        internal void InitializeResponse(Common.Net.Address address, CommandBase command)
        {
            if(command is SurrogateCommand)
            {
                //in case of surrogate command, we initialize response against the actual targetnode
                address = ((SurrogateCommand)command).ActualTargetNode;
            }

            lock (_responseMutex)
            {
                if (_responses == null) _responses = new Dictionary<Common.Net.Address, Dictionary<int, ResponseList>>();
                //RemoveResponse(address, command.CommandID);
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
                if (_responses == null) _responses = new Dictionary<Common.Net.Address, Dictionary<int, ResponseList>>();
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
                if (_responses != null)
                    _responses.Clear();
            }
        }

        internal bool ExpectingResponseFrom(Common.Net.Address address)
        {
            lock (_responseMutex)
            {
                DeserializeRawResponsesIfPresent();
                if (_responses.ContainsKey(address))
                {
                    //True if response from specified ip address is not completed, else false
                    foreach (ResponseList responseList in _responses[address].Values)
                    {
                        if (!responseList.IsComplete())
                            return true;
                    }
                }
            }
            return false;
        }

        internal void Reset(Common.Net.Address ip)
        {
            ResetInternal(ip);


        }

        private void ResetInternal(Address ip)
        {
            lock (_responseMutex)
            {
                DeserializeRawResponsesIfPresent();
                if (_responses.ContainsKey(ip))
                {
                    Dictionary<int, ResponseList> allResponses = _responses[ip];
                    foreach (ResponseList responseList in allResponses.Values)
                    {
                        if (!responseList.IsComplete())
                        {
                            responseList.Clear();
                            responseList.AddResponse(new CommandResponse(true, ip));
                            if (_failedCommands == null) _failedCommands = new List<CommandBase>();
                            _failedCommands.Add(responseList.Command);
                        }
                    }
                }
            }
        }

      

        internal void MergeResponse(Common.Net.Address address, CommandResponse response)
        {
            if (_finalResponse == null && (response.Type != Alachisoft.NCache.Common.Protobuf.Response.Type.GET_NEXT_CHUNK &&
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
                        case Common.Protobuf.Response.Type.BULK_GET_CACHEITEM:
                        
                        case Common.Protobuf.Response.Type.REMOVE_BULK:
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
                        case Common.Protobuf.Response.Type.CONTAINS_BULK:
                            ide = response.KeyValueDic.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                if (_finalResponse.KeyValueDic.ContainsKey(ide.Key))
                                {
                                    ((ArrayList)_finalResponse.KeyValueDic[ide.Key]).AddRange((ArrayList)ide.Value);
                                }
                                else
                                {
                                    _finalResponse.KeyValueDic[ide.Key] = ide.Value;
                                }
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
                            
                        case Alachisoft.NCache.Common.Protobuf.Response.Type.GET_SERVER_MAPPING:
                            _finalResponse.ServerMappingList = response.ServerMappingList;
                            break;

                        case Alachisoft.NCache.Common.Protobuf.Response.Type.GET_NEXT_CHUNK:
                            if (_finalResponse == null)
                                _finalResponse = response;

                            if (_chunks == null) _chunks = new Dictionary<Common.Net.Address, EnumerationDataChunk>();

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

       

        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
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
                    string key;
                    for (int index = 0; index < command.BulkKeys.Length; index++)
                    {
                        key = command.BulkKeys[index];
                        _finalResponse.KeyValueDic[key] = new ConnectionException("Connection with server lost [" + response.ResetConnectionIP + "]");
                    }
                    _finalResponse.SetBroker = false;
                    break;

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

        internal List<KeyValuePair<Address, CommandBase>> GetSendFailureCommands()
        {
            List<KeyValuePair<Address, CommandBase>> failedSendCommands = null;

            lock (_responseMutex)
            {
                DeserializeRawResponsesIfPresent();
                foreach (KeyValuePair<Address, Dictionary<int, ResponseList>> allResponses in _responses)
                {
                    //Iterates over all ResponseList objects against one IP
                    foreach (ResponseList responses in allResponses.Value.Values)
                    {
                        //Iterates over all CommandResponse objects in each ResponseList
                        foreach (CommandResponse rsp in responses.Responses)
                        {
                            //in case exception is not thrown from 1st server.
                            if (rsp.IsSendFailure)
                            {
                                if (failedSendCommands == null) failedSendCommands = new List<KeyValuePair<Address, CommandBase>>();

                                failedSendCommands.Add(new KeyValuePair<Address, CommandBase>(allResponses.Key, responses.Command));
                                break;
                            }
                        }
                    }
                }
            }

            return failedSendCommands;
        }

        internal void InitializeFailedSendResponse(Address address, CommandBase command)
        {
            lock (_responseMutex)
            {
                if (_responses == null) _responses = new Dictionary<Common.Net.Address, Dictionary<int, ResponseList>>();
                if (!_responses.ContainsKey(address))
                {
                    _responses.Add(address, new Dictionary<int, ResponseList>());
                }

                Dictionary<int, ResponseList> allResponses = _responses[address];
                allResponses[command.CommandID] = new ResponseList(command);
                allResponses[command.CommandID].AddResponse(new CommandResponse(false, address) { IsSendFailure = true });
            }
            command.FinalDestinationAddress = address;
        }

        internal void RemoveResponse(List<KeyValuePair<Address, CommandBase>> failedSendCommands)
        {
            lock (_responseMutex)
            {
                foreach (KeyValuePair<Address, CommandBase> pair in failedSendCommands)
                {
                    Address address = pair.Key;
                    CommandBase command = pair.Value;

                    if (address != null && _responses.ContainsKey(address))
                    {
                        _responses[address].Remove(command.CommandID);
                        if (_responses[address].Count == 0)
                            _responses.Remove(address);
                    }
                }
            }
        }
		
	
    }
}
