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
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Client
{
    class ShutDownServerInfo
    {
        private string _uniqueId;
        private Address _blockserverIP = null;
        private long _blockinterval;
        private object _waitForBlockActivity = new object();
        private DateTime _startBlockingTime;

        public ShutDownServerInfo()
        {
            _startBlockingTime = DateTime.Now;
        }

        public string UniqueBlockingId
        {
            get { return _uniqueId; }
            set { _uniqueId = value; }
        }

        public Address BlockServerAddress
        {
            get { return _blockserverIP; }
            set { _blockserverIP = value; }
        }

        public long BlockInterval
        {
            get { return _blockinterval; }
            set { _blockinterval = value; }
        }

        public object WaitForBlockedActivity
        {
            get { return _waitForBlockActivity; }
            set { _waitForBlockActivity = value; }
        }

        public DateTime StartBlockingTime
        {
            get { return _startBlockingTime; }
            set { _startBlockingTime = value; }
        }
    }
}
