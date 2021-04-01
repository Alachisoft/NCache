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
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;

namespace Alachisoft.NCache.SocketServer.Command
{
    class SyncEventCommand : Alachisoft.NCache.SocketServer.Command.CommandBase
    {
        protected struct CommandInfo
        {
            public Hashtable EventsList;
            public int CommandVersion;
            public string RequestId;
        }

        private OperationResult _syncEventResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _syncEventResult;
            }
        }


        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (System.Exception exc)
            {
                _syncEventResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                }
                return;
            }

            //TODO
            byte[] data = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;

                EventStatus eventStatus = nCache.GetEventsStatus();
                List<Alachisoft.NCache.Persistence.Event> syncEventResult = nCache.Cache.GetFilteredEvents(clientManager.ClientID, cmdInfo.EventsList, eventStatus);
                SyncEventResponseBuilder.BuildResponse(syncEventResult, cmdInfo.RequestId, _serializedResponsePackets, clientManager.ClientID, command.commandID, nCache.Cache, clientManager);

            }
            catch (System.Exception exc)
            {
                _syncEventResult = OperationResult.Failure;
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Hashtable eventList = new Hashtable();
            Alachisoft.NCache.Common.Protobuf.SyncEventsCommand syncEventsCommand = command.syncEventsCommand;
            List<Alachisoft.NCache.Common.Protobuf.EventIdCommand> eventIds = syncEventsCommand.eventIds;
            Alachisoft.NCache.Caching.EventId cacheEventId = null;
            foreach (EventIdCommand eventId in eventIds)
            {
                cacheEventId = new Alachisoft.NCache.Caching.EventId();
                cacheEventId.EventUniqueID = eventId.eventUniqueId;
                cacheEventId.EventCounter = eventId.eventCounter;
                cacheEventId.OperationCounter = eventId.operationCounter;
                cacheEventId.EventType = (Persistence.EventType)eventId.eventType;
                eventList.Add(cacheEventId, null);
            }
            cmdInfo.EventsList = eventList;
            cmdInfo.RequestId = syncEventsCommand.requestId.ToString();
            return cmdInfo;
        }

    }
}
