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
