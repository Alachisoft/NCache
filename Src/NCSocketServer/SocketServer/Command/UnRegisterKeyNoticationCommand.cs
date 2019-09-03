//  Copyright (c) 2019 Alachisoft
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
using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;


namespace Alachisoft.NCache.SocketServer.Command
{
    class UnRegisterKeyNoticationCommand : CommandBase 
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public short RemoveCallbackId;
            public short UpdateCallbackId;
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

            try
            {
                overload = command.MethodOverload;
                stopWatch.Start();

                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    //_commandBytes = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                nCache.Cache.UnregisterKeyNotificationCallback(cmdInfo.Key
                  , new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, EventDataFilter.None) //DataFilter not required while unregistration
                  , new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId, EventDataFilter.None) //DataFilter not required while unregistration
                  , new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                stopWatch.Stop();

                //Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
				Alachisoft.NCache.Common.Protobuf.UnregisterKeyNotifResponse unregisterKeyNotifResponse = new Alachisoft.NCache.Common.Protobuf.UnregisterKeyNotifResponse();
                //            response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                //            response.commandID = command.commandID;
                //            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.UNREGISTER_KEY_NOTIF;
                //response.unregisterKeyNotifResponse = unregisterKeyNotifResponse;

                //            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.UNREGISTER_KEY_NOTIF));

                //_commandBytes = clientManager.ReplyPacket("UNREGKEYNOTIFRESULT \"" + cmdInfo.RequestId + "\"", new byte[0]);

                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(unregisterKeyNotifResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(unregisterKeyNotifResponse, Common.Protobuf.Response.Type.UNREGISTER_KEY_NOTIF));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.unregisterKeyNotifResponse = unregisterKeyNotifResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.UNREGISTER_KEY_NOTIF);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                exception = exc.ToString();

                //_commandBytes = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.UnRegisterKeyNotificationCallback.ToLower());
                        log.GenerateKeyNotificationCallback(1, cmdInfo.UpdateCallbackId, cmdInfo.RemoveCallbackId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());



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
        //    CommandInfo cmdInfo;

        //    try
        //    {
        //        cmdInfo = ParseCommand(ref command);
        //    }
        //    catch (Exception exc)
        //    {
        //        if (!base.immatureId.Equals("-2")) _commandBytes = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
        //        return;
        //    }

        //    try
        //    {
        //        NCache nCache = clientManager.CmdExecuter as NCache;
        //        nCache.Cache.UnregisterKeyNotificationCallback(cmdInfo.Key,
        //          new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId)
        //          , new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId));

        //        _commandBytes = clientManager.ReplyPacket("UNREGKEYNOTIFRESULT \"" + cmdInfo.RequestId + "\"", new byte[0]);
        //    }
        //    catch (Exception exc)
        //    {
        //        _commandBytes = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
        //    }
        //}

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.UnRegisterKeyNotifCommand unRegisterKeyNotifCommand = command.unRegisterKeyNotifCommand;
            cmdInfo.Key = unRegisterKeyNotifCommand.key;
            cmdInfo.RemoveCallbackId = (short)unRegisterKeyNotifCommand.removeCallbackId;
            cmdInfo.RequestId = unRegisterKeyNotifCommand.requestId.ToString();
            cmdInfo.UpdateCallbackId = (short)unRegisterKeyNotifCommand.updateCallbackId;

            return cmdInfo;
        }


        //private CommandInfo ParseCommand(ref string command)
        //{
        //    CommandInfo cmdInfo = new CommandInfo();

        //    int beginQuoteIndex = 0, endQuoteIndex = 0;

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);

        //    //if (beginQuoteIndex + 1 == endQuoteIndex)
        //    //    throw new ArgumentNullException("Keys");
        //    cmdInfo.Key = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);

        //    //if (beginQuoteIndex + 1 == endQuoteIndex) throw new ArgumentNullException("requestId");
        //    cmdInfo.RequestId = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
        //    base.immatureId = cmdInfo.RequestId;

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.UpdateCallbackId = Convert.ToInt16(command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1));

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.RemoveCallbackId = Convert.ToInt16(command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1));

        //    return cmdInfo;
        //}

    }
}
