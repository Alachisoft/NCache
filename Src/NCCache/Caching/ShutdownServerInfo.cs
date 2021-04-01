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
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching
{
    public class ShutDownServerInfo
    {
        private string _uniqueId;
        private Address _blockserverAddress = null;
        private Address _renderedAddress;
        private long _blockinterval;
        private DateTime _startShutDown;

        public ShutDownServerInfo()
        {
            _startShutDown = DateTime.Now;
        }

        public long BlockInterval
        {
            get
            {
                long startTime = (_startShutDown.Ticks - 621355968000000000) / 10000;
                long timeout = Convert.ToInt32(_blockinterval * 1000) - (int)((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime);
                return (timeout / 1000);
            }
            set { _blockinterval = value; }
        }

        public string UniqueBlockingId
        {
            get { return _uniqueId; }
            set { _uniqueId = value; }
        }

        public Address BlockServerAddress
        {
            get { return _blockserverAddress; }
            set { _blockserverAddress = value; }
        }

        public Address RenderedAddress
        {
            get { return _renderedAddress; }
            set { _renderedAddress = value; }
        }
    }

}
