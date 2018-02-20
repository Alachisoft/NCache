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
using Alachisoft.NCache.MapReduce;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Util;
using System.Collections;
using Alachisoft.NCache.Web.Aggregation;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class MapReduceTaskCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            long requestId;
            string taskId = "";
            int callbackId = 0;
            byte[] mapper = null;
            byte[] reducer = null;
            byte[] combiner = null;
            byte[] inputProvider = null;
            int outputOption = 0;
            byte[] keyfilter = null;
            string query = "";
            byte[] parameters = null;
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            { overload = command.MethodOverload;
                Alachisoft.NCache.Common.Protobuf.MapReduceTaskCommand mapReduceTaskCommand = command.mapReduceTaskCommand;
                overload = command.MethodOverload;
                if (mapReduceTaskCommand.mapper != null)
                    mapper = mapReduceTaskCommand.mapper;
                if (mapReduceTaskCommand.reducer != null)
                    reducer = mapReduceTaskCommand.reducer;
                if (mapReduceTaskCommand.combiner != null)
                    combiner = mapReduceTaskCommand.combiner;

                if (mapReduceTaskCommand.inputProvider!= null)
                    inputProvider = mapReduceTaskCommand.inputProvider;
                
                if (mapReduceTaskCommand.keyFilter != null)
                    keyfilter = mapReduceTaskCommand.keyFilter;
                if (mapReduceTaskCommand.queryParameters != null)
                    parameters = mapReduceTaskCommand.queryParameters;

                requestId = command.requestID;
                query = mapReduceTaskCommand.query;
                taskId = mapReduceTaskCommand.taskId;
                callbackId = mapReduceTaskCommand.callbackId;
                outputOption = mapReduceTaskCommand.outputOption;
            }
            catch (Exception ex)
            {
                if (base.immatureId != "-2")
                    _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID, command.commandID));
                return;
            }
            Runtime.MapReduce.MapReduceTask userTask = null;
            Filter filter = null;
            try
            {
                ICommandExecuter tmpVar = clientManager.CmdExecuter;
                NCache nCache = (NCache) ((tmpVar is NCache) ? tmpVar : null);

                userTask = GetMapReduceTask(mapper, combiner, reducer, inputProvider, nCache.Cache.Name);

                filter = GetFilter(keyfilter, query, parameters, nCache.Cache.Name);

                // the Actual Call.
                nCache.Cache.SubmitMapReduceTask(userTask, taskId, 
                    new Alachisoft.NCache.MapReduce.Notifications.TaskCallbackInfo(clientManager.ClientID, (short)callbackId), filter,
                    new Caching.OperationContext(Caching.OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
                stopWatch.Stop();
                //Build response
                Common.Protobuf.Response reponse = new Common.Protobuf.Response();
                reponse.mapReduceTaskResponse = new Common.Protobuf.MapReduceTaskResponse();
                reponse.commandID = command.commandID;
                reponse.requestId = Convert.ToInt64(requestId);
                reponse.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.MAP_REDUCE_TASK;
                _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(reponse));
            }
            catch (Exception ex)
            {
                exception = ex.ToString();
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        if (userTask.Mapper is AggregatorMapper)
                        {
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.Aggregate.ToLower());
                            log.GenerateAggregateTaskAPILogItem(userTask.ToString(), userTask.Mapper.ToString(), query, parameters, filter != null ? filter.KeyFilter.ToString() : null, 0, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                        }
                        else
                        {
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.ExecuteTask.ToLower());
                            log.GenerateExecuteTaskAPILogItem(userTask.ToString(), filter!=null? filter.KeyFilter.ToString():null, query, parameters, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private Filter GetFilter(byte[] keyfilter, string query, byte[] parameters, string cacheName)
        {
            Filter finalFilter = null;
            Runtime.MapReduce.IKeyFilter _kFilter = null;
            QueryFilter _qFilter = null;
            if (keyfilter != null)
                _kFilter = (Runtime.MapReduce.IKeyFilter)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(keyfilter, cacheName);

            if ((query != null && !string.IsNullOrEmpty(query)))
            {
                if (parameters != null)
                    _qFilter = new QueryFilter(query, (Hashtable)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(parameters, cacheName));
                else
                    _qFilter = new QueryFilter(query, null);
            }

            if (_kFilter != null)
                finalFilter = new Filter(_kFilter);
            if (_qFilter != null)
                finalFilter = new Filter(_qFilter);

            return finalFilter;
        }

        private Runtime.MapReduce.MapReduceTask GetMapReduceTask(byte[] mapper, byte[] combiner, byte[] reducer, byte[] input, string cacheName)
        {
            Runtime.MapReduce.IMapper tMapper = null;
            Runtime.MapReduce.IReducerFactory tReducer = null;
            Runtime.MapReduce.ICombinerFactory tCombiner = null;
            Runtime.MapReduce.MapReduceInput inputProvider = null;

            if (mapper != null)
                tMapper = (Runtime.MapReduce.IMapper)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(mapper, cacheName);
            if (reducer != null)
                tReducer = (Runtime.MapReduce.IReducerFactory)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(reducer, cacheName);
            if (combiner != null)
                tCombiner = (Runtime.MapReduce.ICombinerFactory)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(combiner, cacheName);

            if (input != null)
                inputProvider = (Runtime.MapReduce.MapReduceInput)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(input, cacheName);

            Runtime.MapReduce.MapReduceTask t = new Runtime.MapReduce.MapReduceTask();
            t.Mapper = tMapper;
            t.Reducer = tReducer;
            t.Combiner = tCombiner;
            t.InputProvider = inputProvider;
            return t;
        }
    }
}
