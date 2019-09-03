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
using System.Net;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command
{
    class InitSecondarySocketCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public bool IsDotNetClient;
            public string ClientID;
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " parsing error " + exc.ToString());

                if (!base.immatureId.Equals("-2"))
                {
                    //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
                }
                return;
            }

            try
            {
                clientManager.ClientID = cmdInfo.ClientID;

                lock (ConnectionManager.ConnectionTable)
                {
                    if (ConnectionManager.ConnectionTable.Contains(clientManager.ClientID))
                    {
                        ClientManager cmgr = ConnectionManager.ConnectionTable[clientManager.ClientID] as ClientManager;
                        clientManager.CmdExecuter = cmgr.CmdExecuter;
                    }
                }

                //_resultPacket = clientManager.ReplyPacket("INITSECONDARYRESULT \"" + cmdInfo.RequestId + "\"", new byte[0]);
            }

            catch (SecurityException sec)
            {
                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(sec, cmdInfo.RequestId), base.ExceptionMessage(sec));
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(sec, command.requestID,command.commandID));
            }

            catch (Exception exc)
            {
                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
            }
        }

//        public override void ExecuteCommand(ClientManager clientManager, string command, byte[] data)
//        {
//            CommandInfo cmdInfo;

//            try
//            {
//                cmdInfo = ParseCommand(ref command, data);
//            }
//            catch (Exception exc)
//            {
//                if (SocketServer.Logger.IsErrorLogEnabled) SocketServer.Logger.WriteLogEntry("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " parsing error " + exc.ToString());

//                if (!base.immatureId.Equals("-2")) _resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
//                return;
//            }

//            try
//            {
//                clientManager.ClientID = cmdInfo.ClientID;

//                lock (ConnectionManager.ConnectionTable)
//                {
//                    if (ConnectionManager.ConnectionTable.Contains(clientManager.ClientID))
//                    {
//                        ClientManager cmgr = ConnectionManager.ConnectionTable[clientManager.ClientID] as ClientManager;
//                        clientManager.CmdExecuter = cmgr.CmdExecuter;
//                    }
//                }
//                _resultPacket = clientManager.ReplyPacket("INITSECONDARYRESULT \"" + cmdInfo.RequestId + "\"", new byte[0]);
//            }
//#if !EXPRESS
//            catch (SecurityException sec)
//            {
//                _resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(sec, cmdInfo.RequestId), base.ExceptionMessage(sec));
//            }
//#endif
//            catch (Exception exc)
//            {
//                _resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
//            }
//        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            return new CommandInfo();
        }

        //private CommandInfo ParseCommand(ref string command, byte[] data)
        //{
        //    CommandInfo cmdInfo = new CommandInfo();

        //    int beginQuoteIndex = 0, endQuoteIndex = 0;

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);

        //    cmdInfo.RequestId = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
        //    base.immatureId = cmdInfo.RequestId;

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.ClientID = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.IsDotNetClient = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1).Equals("Y");
                       
        //    return cmdInfo;
        //}
    }
}
