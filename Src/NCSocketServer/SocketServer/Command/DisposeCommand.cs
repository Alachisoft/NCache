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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command
{
    class DisposeCommand : CommandBase
    {

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            try
            {
                if (clientManager != null)
                {
                    clientManager._leftGracefully = true;
                    NCache nCache = clientManager.CmdExecuter as NCache;
                    if (nCache!=null)
                        nCache.Dispose();
                    stopWatch.Stop();

                 

                    Alachisoft.NCache.Common.Protobuf.DisposeResponse disposeResponse= new Alachisoft.NCache.Common.Protobuf.DisposeResponse();
                    if (clientManager.ClientVersion >= 5000)
                    {
                        Common.Util.ResponseHelper.SetResponse(disposeResponse, command.requestID, command.commandID);
                        _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(disposeResponse, Common.Protobuf.Response.Type.DISPOSE));
                    }
                    else
                    {
                        //PROTOBUF:RESPONSE
                        Common.Protobuf.Response response = new Common.Protobuf.Response();
                        response.disposeResponse = disposeResponse;
                        Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.DISPOSE);
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    }

                }
            }
            catch (Exception ex)
            {
                exception = ex.ToString();
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Dispose.ToLower());
                        log.GenerateDisposeAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());

                        // Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                    }
                }
                catch
                {

                }

            }

        }

        //public override void ExecuteCommand(ClientManager clientManager, string command, byte[] data)
        //{
        //    if (clientManager != null)
        //    {
        //        clientManager._leftGracefully = true;
        //        clientManager.CmdExecuter.Dispose();
        //    }
        //}
    }
}
