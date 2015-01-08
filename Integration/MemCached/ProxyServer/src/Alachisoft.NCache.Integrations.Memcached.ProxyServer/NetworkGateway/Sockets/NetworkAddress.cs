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

namespace MemcachedProxyForNCache.NetworkGateway.Sockets
{
    class NetworkAddress
    {
        string _ipAddress;
        int _port;

        NetworkAddress(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public string IPAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; }
        }

        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
    }
}
