// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System.IO;

namespace Alachisoft.Common
{
    public static class StreamExtensions
    {
        const int BUFFER_SIZE = 65536;

        public static int CopyTo(this Stream source, Stream destination)
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            int bytesRead;
            int bytesCopied = 0;

            do
            {
                bytesRead = source.Read(buffer, 0, BUFFER_SIZE);
                if (bytesRead > 0)
                    destination.Write(buffer, 0, bytesRead);
                bytesCopied += bytesRead;
            }
            while (bytesRead > 0);

            return bytesCopied;
        }

        public static byte[] ReadToEnd(this Stream source)
        {
            MemoryStream stream = new MemoryStream();
            CopyTo(source, stream);
            stream.Dispose();
            byte[] bytes = stream.ToArray();
            return bytes;
        }
    }
}
