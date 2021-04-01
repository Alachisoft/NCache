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
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Util;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetOptimalServerCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string CacheId;
            public string UserName;
            public string Password;
            public byte[] UserNameBinary;
            public byte[] PasswordBinary;
            public bool IsDotNetClient;

            public CommandInfo clone()
            {
                CommandInfo varCopy = new CommandInfo();

                varCopy.RequestId = this.RequestId;
                varCopy.CacheId = this.CacheId;
                varCopy.UserName = this.UserName;
                varCopy.Password = this.Password;
                varCopy.UserNameBinary = this.UserNameBinary;
                varCopy.PasswordBinary = this.PasswordBinary;
                varCopy.IsDotNetClient = this.IsDotNetClient;

                return varCopy;
            }
        }


        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            try
            {
                cmdInfo = ParseCommand(command, clientManager).clone();
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
                return;
            }

            Alachisoft.NCache.Caching.Cache cache = null;

            try
            {
                string server = ConnectionManager.ServerIpAddress;
                int port = ConnectionManager.ServerPort;
                cache = CacheProvider.Provider.GetCacheInstanceIgnoreReplica(cmdInfo.CacheId);
                if (cache == null) throw new Exception("Cache is not registered");
                if (!cache.IsRunning) throw new Exception("Cache is not running");


#if (SERVER ) 

                
                    if (cache.CacheType.Equals("replicated-server"))
                        cache.GetLeastLoadedServer(ref server, ref port);
                    else
                    {
                        if (cache.IsCoordinator) { /*return this node information...*/ }
                        else
                            cache.GetActiveServer(ref server, ref port);
                    }
                
#endif

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetOptimalServerResponse getOptimalServerResponse = new Alachisoft.NCache.Common.Protobuf.GetOptimalServerResponse();
                getOptimalServerResponse.server = server;
                getOptimalServerResponse.port = port;
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.getOptimalServer = getOptimalServerResponse;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_OPTIMAL_SERVER;

                //PROTOBUF:RESPONSE
                //_resultPacket = clientManager.ReplyPacket("OPTIMALSERVERRESULT \"" + cmdInfo.RequestId + "\"" + server + "\"" + port.ToString() + "\"", new byte[0]);

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
            }
        }

//        public override void ExecuteCommand(ClientManager clientManager, string command, byte[] data)
//        {
//            CommandInfo cmdInfo;
//            try
//            {
//                cmdInfo = ParseCommand(ref command,data);
//            }
//            catch (Exception exc)
//            {
//                if (!base.immatureId.Equals("-2")) _resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
//                return;
//            }

//            Cache cache = null;

//            try
//            {
//                string server = ConnectionManager.ServerIpAddress;
//                int port = ConnectionManager.ServerPort;

//                if(cmdInfo.IsDotNetClient)
//                    cache = CacheProvider.Provider.GetCacheInstanceIgnoreReplica(cmdInfo.CacheId, cmdInfo.UserNameBinary, cmdInfo.PasswordBinary);
//                else
//                    cache = CacheProvider.Provider.GetCacheInstanceIgnoreReplica(cmdInfo.CacheId, cmdInfo.UserName, cmdInfo.Password);

                
//                if (cache == null) throw new Exception("Cache is not registered");
//                if (!cache.IsRunning) throw new Exception("Cache is not running");

//#if (ENTERPRISE || PROFESSIONAL) && (!PROF_CLIENT && !PROF_DEV)
//                if (!cache.CacheType.Equals("mirror-server"))
//                    cache.GetLeastLoadedServer(ref server, ref port);
//                else
//                {
//                    if (cache.IsCoordinator) { /*return this node information...*/ }
//                    else
//                        cache.GetActiveServer(ref server, ref port);
//                    //muds:
//                    //return the active node information for the cache...
//                }
//#endif

//                _resultPacket = clientManager.ReplyPacket("OPTIMALSERVERRESULT \"" + cmdInfo.RequestId + "\"" + server + "\"" + port.ToString() + "\"", new byte[0]);
//            }
//            catch (Exception exc)
//            {
//                _resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
//            }            
//        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetOptimalServerCommand getOptimalServerCommand = command.getOptimalServerCommand;

            cmdInfo.CacheId = getOptimalServerCommand.cacheId;
            cmdInfo.IsDotNetClient = getOptimalServerCommand.isDotnetClient;
            cmdInfo.Password = getOptimalServerCommand.pwd;
            cmdInfo.PasswordBinary = getOptimalServerCommand.binaryPassword;
            cmdInfo.RequestId = getOptimalServerCommand.requestId.ToString();
            cmdInfo.UserName = getOptimalServerCommand.userId;
            cmdInfo.UserNameBinary = getOptimalServerCommand.binaryUserId;

            return cmdInfo;
        }

        //private CommandInfo ParseCommand(ref string command,byte[] data)
        //{
        //    CommandInfo cmdInfo = new CommandInfo();

        //    int beginQuoteIndex = 0, endQuoteIndex = 0;

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);

        //    //if (beginQuoteIndex + 1 == endQuoteIndex) throw new ArgumentNullException("requestId");
        //    cmdInfo.CacheId = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.RequestId = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
        //    base.immatureId = cmdInfo.RequestId;

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.UserName = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.Password = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.IsDotNetClient = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1).Equals("Y");

        //    if (cmdInfo.IsDotNetClient)
        //    {

        //        byte[] dataLength = HelperFxn.CopyPartial(data, 0, ConnectionManager.valSizeHolderBytesCount);


        //        int uidLength = Util.HelperFxn.ToInt32(dataLength);
        //        cmdInfo.UserNameBinary = HelperFxn.CopyPartial(data, ConnectionManager.valSizeHolderBytesCount, ConnectionManager.valSizeHolderBytesCount + uidLength);

        //        dataLength = HelperFxn.CopyTw(data, ConnectionManager.valSizeHolderBytesCount + uidLength, ConnectionManager.valSizeHolderBytesCount);

        //        int pwdLength = Util.HelperFxn.ToInt32(dataLength);
        //        cmdInfo.PasswordBinary = HelperFxn.CopyTw(data, 2 * ConnectionManager.valSizeHolderBytesCount + uidLength, pwdLength);
        //    }
        //    return cmdInfo;
        //}
    }
}
