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
using System.Collections;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Util;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal sealed class GetEnumeratorCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
        }

        //TODO:KeyPackage
        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
				if (!base.immatureId.Equals("-2"))
				{
					//PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
					//_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
				}
                return;
            }

            int count = 0;
            string keyPackage = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                
				IDictionaryEnumerator dicEnu = (IDictionaryEnumerator)nCache.Cache.GetEnumerator();
				//IEnumerator enu = (IEnumerator)nCache.Cache.GetEnumerator();
                //KeyPackageBuilder.PackageKeys(dicEnu, out keyPackage, out count);
                stopWatch.Stop();
				//PROTOBUF:RESPONSE
				Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
				Alachisoft.NCache.Common.Protobuf.GetEnumeratorResponse getEnumeratorResponse = new Alachisoft.NCache.Common.Protobuf.GetEnumeratorResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
				response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_ENUMERATOR;
				response.getEnum = getEnumeratorResponse;

				Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeys(dicEnu, getEnumeratorResponse.keys);

				_serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.GET_ENUMERATOR));

                //_resultPacket = clientManager.ReplyPacket("GETENUMRESULT \"" + count + "\"" + cmdInfo.RequestId + "\"" + keyPackage, new byte[0]);
            }
            catch (Exception exc)
            {
				//PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetEnumerator.ToLower());
                        log.GenerateGetEnumeratorAPILogItem(overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());

                        // Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                    }
                }
                catch
                {

                }
            }
        }

       

        //PROTOBUF

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetEnumeratorCommand getEnumeratorCommand = command.getEnumeratorCommand;

            cmdInfo.RequestId = getEnumeratorCommand.requestId.ToString();

            return cmdInfo;
        }

       
    }
}
