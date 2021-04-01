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
using System.IO;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.SocketServer.RequestLogging;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.SocketServer
{
    internal interface ICommandManager
    {
        void ProcessCommand(ClientManager clientManager, object command, short cmdType, long acknowledgementId, UsageStats stats, bool waitforResponse);
        object Deserialize(Stream buffer); 
        RequestStatus GetRequestStatus(string clientId, long requestId, long commandId);
        Bookie RequestLogger { get; }
        void RegisterOperationModeChangeEvent();
        void Dispose();
    }
}
