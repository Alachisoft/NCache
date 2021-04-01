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
using System.Net.Sockets;
using System.IO;

namespace Alachisoft.NCache.SocketServer.Util
{
    internal static class SocketHelper
    {
        internal static void TransferConnection(ClientManager clientManager, string cacheId, Alachisoft.NCache.Common.Protobuf.Command command, long acknowledgementId)
        {
            if (!CacheProvider.Provider.IsRunning(cacheId))
                throw new Exception("Specified cache is not running.");

            int cacheProcessPID = CacheProvider.Provider.GetCacheProcessID(cacheId);

            if (cacheProcessPID == 0)
                cacheProcessPID = CacheProvider.Provider.GetProcessID(cacheId);
           // clientManager.IsDisposed = true;
            
            SocketInformation socketInfo = clientManager.ClientSocket.DuplicateAndClose(cacheProcessPID);
            socketInfo.Options |= SocketInformationOptions.UseOnlyOverlappedIO;
            byte[] commandByte = SerializeCommand(command);

            CacheProvider.Provider.TransferConnection(socketInfo, cacheId, commandByte);
            clientManager.Dispose();
        }

        internal static byte[] SerializeCommand(Alachisoft.NCache.Common.Protobuf.Command command)
        {
            byte[] commandBytes;

            using (MemoryStream stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Command>(stream, command);
                commandBytes = stream.ToArray();
            }
            return commandBytes;
        }

        internal static ArraySegment<TReturn>[] GetArraySegments<TReturn>(IList list)
        {
            ArraySegment<TReturn>[] segments = new ArraySegment<TReturn>[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                TReturn[] array = (TReturn[])list[i];
                segments[i] = new ArraySegment<TReturn>(array);
            }

            return segments;
        }
    }
}

