// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol
{
    abstract class BinaryHeader
    {
        protected Magic _magic;
        public Magic MagicByte
        {
            get { return _magic; }
            set { _magic = value; }
        }

        protected Opcode _opcode;
        public Opcode Opcode
        {
            get { return _opcode; }
            set { _opcode = value; }
        }

        protected int _keyLenght = 0;
        public int KeyLength
        {
            get { return _keyLenght; }
            set { _keyLenght = value; }
        }

        protected int _extraLenght = 0;
        public int ExtraLength
        {
            get { return _extraLenght; }
            set { _extraLenght = value; }
        }

        protected byte _dataType;
        public byte DataType
        {
            get { return _dataType; }
            set { _dataType = value; }
        }

        protected int _totalBodyLength = 0;
        public int TotalBodyLenght
        {
            get { return _totalBodyLength; }
            set { _totalBodyLength = value; }
        }

        protected int _opaque;
        public int Opaque
        {
            get { return _opaque; }
            set { _opaque = value; }
        }

        protected ulong _cas;
        public ulong CAS
        {
            get { return _cas; }
            set { _cas = value; }
        }

        public int ValueLength
        {
            get { return this._totalBodyLength - this._keyLenght - this._extraLenght; }
        }

        public BinaryHeader()
        { }

        public BinaryHeader(byte[] header)
        {
            if (header.Length < 24)
                throw new ArgumentException("Length of header should be 24 bytes.");

            _magic = (Magic)header[0];
            _opcode = (Opcode)header[1];

            _keyLenght = (int)MemcachedEncoding.BinaryConverter.ToUInt16(header, 2);
            _extraLenght = (int)header[4];

            _dataType = header[5];
            //6,7 reserved
            _totalBodyLength = MemcachedEncoding.BinaryConverter.ToInt32(header, 8);
            _opaque = MemcachedEncoding.BinaryConverter.ToInt32(header,12);
            _cas = MemcachedEncoding.BinaryConverter.ToUInt64(header, 16);
        }
    }
}
