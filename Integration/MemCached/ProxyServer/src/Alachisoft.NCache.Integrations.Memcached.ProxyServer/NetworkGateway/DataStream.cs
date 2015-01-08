// Copyright (c) 2015 Alachisoft
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway
{
    public class DataStream
    {
        private MemoryStream _memoryStream;
        private int _readIndex = 0;
        private int _writeIndex = 0;

        public DataStream()
        {
            _memoryStream = new MemoryStream();
        }

        public DataStream(int capacity)
        {
            _memoryStream = new MemoryStream(capacity);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
                lock (this)
                {
                    _memoryStream.Position = _readIndex;

                    int streamLength = _writeIndex-_readIndex;
                    int bytesToRead=count<streamLength?count:streamLength;
                    bytesRead = _memoryStream.Read(buffer, offset, bytesToRead);
                    _readIndex += bytesRead;
                    if (_readIndex == _writeIndex)
                    {
                        _readIndex = _writeIndex = 0;
                    }
                }
            return bytesRead;
        }

        public byte[] ReadAll()
        {
            byte[] data = new byte[0];
                lock (this)
                {
                    int totalBytes=_writeIndex-_readIndex;
                    data = new byte[totalBytes];
                    _memoryStream.Position = _readIndex;
                    _memoryStream.Read(data, 0, totalBytes);
                    _readIndex = _writeIndex = 0;
                }
            return data;
        }

        public void Write(byte[] buffer)
        {
            if (buffer != null)
                this.Write(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
                lock (this)
                {
                    _memoryStream.Position = _writeIndex;
                    _memoryStream.Write(buffer, offset, count);
                    _writeIndex += count;
                }
        }

        public long Lenght
        {
            get
            {
                lock (this)
                {
                    return _writeIndex - _readIndex;
                }
            }
        }

        public void Dispose()
        {
            _memoryStream.Dispose();
        }
    }
}
