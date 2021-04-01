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
using System;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using System.Collections;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.FeatureUsageData;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ContainsCommand : CommandBase
    {
            public string RequestId;
            public IList Keys;

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload = 0;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Keys = new List<string>();
            string key = null;
            NCache nCache = clientManager.CmdExecuter as NCache;
            
            try
            {
                overload = command.MethodOverload;

                switch (command.type)
                {
                    case Common.Protobuf.Command.Type.CONTAINS:
                        Alachisoft.NCache.Common.Protobuf.ContainsCommand containsCommand = command.containsCommand;
                        
                        Keys.Add(containsCommand.key);
                        key = containsCommand.key;
                        RequestId = containsCommand.requestId.ToString();
                        break;

                    case Common.Protobuf.Command.Type.CONTAINS_BULK:
                        Alachisoft.NCache.Common.Protobuf.ContainsBulkCommand containsBulkCommand = command.containsBulkCommand;
                        FeatureUsageCollector.Instance.GetFeature(FeatureEnum.bulk_operations).UpdateUsageTime();
                        Keys = containsBulkCommand.keys;
                        RequestId = containsBulkCommand.requestId.ToString();
                        break;

                    default:
                        throw new Exception("Invalid Command Received in ContainsCommand");
                }
                var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                Hashtable exists = nCache.Cache.Contains(Keys, operationContext);

                stopWatch.Stop();

                if(clientManager.ClientVersion >= 5000)
                {
                    Alachisoft.NCache.Common.Protobuf.ContainBulkResponse containsBulkResponse = new Alachisoft.NCache.Common.Protobuf.ContainBulkResponse();

                    containsBulkResponse.exists = CompactBinaryFormatter.ToByteBuffer(exists, null);

                    ResponseHelper.SetResponse(containsBulkResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(containsBulkResponse, Common.Protobuf.Response.Type.CONTAINS_BULK));

                }
                else
                {
                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.ContainResponse containResponse = new Alachisoft.NCache.Common.Protobuf.ContainResponse();

                    IDictionary<string, bool> keysPresent = ExtractKeyStatus(exists, Keys);
                    containResponse.exists = keysPresent[key];
                    response.contain = containResponse;
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.CONTAINS);

                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

            }

            catch (Exception exc)
            {
                exception = exc.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }

            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Contains.ToLower());
                        log.GenerateContainsCommandAPILogItem(key, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());

                    }
                }
                catch
                {

                }
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ContCmd.Exec", "cmd executed on cache");

        }

        private static IDictionary<string, bool> ExtractKeyStatus(Hashtable hashtable, IList keys)
        {
            ArrayList availableKeys = null;
            IDictionary<string, bool> keyStatus = new Dictionary<string, bool>();

            if (hashtable.ContainsKey("items-found"))
                availableKeys = (ArrayList)hashtable["items-found"];

            foreach (string key in keys)
            {
                keyStatus[key] = false;

                if (availableKeys != null && availableKeys.Contains(key))
                    keyStatus[key] = true;
            }

            return keyStatus;
        }

    }
}
