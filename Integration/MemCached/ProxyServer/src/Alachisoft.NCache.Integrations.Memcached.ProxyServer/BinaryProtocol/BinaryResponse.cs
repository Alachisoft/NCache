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

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol
{
    class BinaryResponse
    {
        private BinaryResponseHeader _header;
        public BinaryResponseHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        private BinaryResponsePayload _payload;
        public BinaryResponsePayload PayLoad
        {
            get { return _payload; }
            set { _payload = value; }
        }

        public BinaryResponse()
        {
            _header = new BinaryResponseHeader();
            _payload = new BinaryResponsePayload();
        }

        public byte[] BuildResponse()
        {
            _header.MagicByte = Magic.Response;
            _header.KeyLength = PayLoad.Key.Length;
            _header.ExtraLength = PayLoad.Extra.Length;
            _header.TotalBodyLenght = _header.KeyLength + _header.ExtraLength + PayLoad.Value.Length;

            byte[] response = new byte[24 + _header.TotalBodyLenght];
            Buffer.BlockCopy(_header.ToByteArray(), 0, response, 0, 24);
            Buffer.BlockCopy(PayLoad.Extra, 0, response, 24, PayLoad.Extra.Length);
            Buffer.BlockCopy(PayLoad.Key, 0, response, 24 + PayLoad.Extra.Length, PayLoad.Key.Length);
            Buffer.BlockCopy(PayLoad.Value, 0, response, 24 + PayLoad.Extra.Length + PayLoad.Key.Length, PayLoad.Value.Length);
            return response;
        }
    }
}
