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

namespace Alachisoft.NCache.SocketServer.MultiBufferReceive
{
    internal static class SocketBufferUtil
    {
        internal static byte[] ReadBytesFromSocketBuffer(int count, SocketBuffer socketBuffer)
        {
            if (count > socketBuffer.UnreadBytes) throw new Exception("Did not find enough data to read.");
            var retBytes = new byte[count];
            Buffer.BlockCopy(socketBuffer.Buffer, (int)socketBuffer.ReadIndex, retBytes, 0, count);
            socketBuffer.BytesRead(count);
            return retBytes;
        }

        internal static int ReadBytesFromSocketBuffer(byte[] buffer, int offset, int count, SocketBuffer socketBuffer)
        {
            int byteToRead = (int)socketBuffer.UnreadBytes > count ? count : (int)socketBuffer.UnreadBytes;
            Buffer.BlockCopy(socketBuffer.Buffer, (int)socketBuffer.ReadIndex, buffer, offset, byteToRead);
            socketBuffer.BytesRead(byteToRead);
            return byteToRead;
        }
    }
}
