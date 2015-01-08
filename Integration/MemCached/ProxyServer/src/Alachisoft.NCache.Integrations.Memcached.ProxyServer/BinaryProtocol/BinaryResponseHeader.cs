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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.MemcachedEncoding;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol
{
    class BinaryResponseHeader : BinaryHeader
    {
        private BinaryResponseStatus _status;
        public BinaryResponseStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public BinaryResponseHeader()
        {
            _magic = Magic.Response;
        }

        public byte[] ToByteArray()
        {
            byte[] header = new byte[24];

            header[0] = (byte)_magic;
            header[1] = (byte)_opcode;

            Buffer.BlockCopy(BinaryConverter.GetBytes((ushort)_keyLenght), 0, header, 2,2);

            header[4] = (byte)(_extraLenght & 255);
            header[5] = _dataType;

            Buffer.BlockCopy(BinaryConverter.GetBytes((ushort)_status), 0, header, 6, 2);

            Buffer.BlockCopy(BinaryConverter.GetBytes(_totalBodyLength), 0, header, 8, 4);

            Buffer.BlockCopy(BinaryConverter.GetBytes(_opaque), 0, header, 12, 4);

            Buffer.BlockCopy(BinaryConverter.GetBytes(_cas), 0, header, 16, 8);

            return header;
        }
    }
}
