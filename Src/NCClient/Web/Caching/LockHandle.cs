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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// An instance of this class is used to lock and unlock the cache items in pessimistic concurrency model.
    /// </summary>
    public class LockHandle
    {
        private string _lockId;
        private DateTime _lockDate;

        ///<summary>Constructor</summary>
        public LockHandle() { }

        /// <summary>
        /// Overloaded constructor.
        /// </summary>
        /// <param name="lockId"></param>
        /// <param name="lockDate"></param>
        public LockHandle(string lockId, DateTime lockDate)
        {
            _lockId = lockId;
            _lockDate = lockDate;
        }

        /// <summary>
        /// Gets and sets the lock-id.
        /// </summary>
        public string LockId
        {
            get { return _lockId; }
            set { _lockId = value; }
        }

        /// <summary>
        /// Gets and sets the lock-date.
        /// </summary>
        public DateTime LockDate
        {
            get { return _lockDate; }
            set { _lockDate = value; }
        }
    }
}
