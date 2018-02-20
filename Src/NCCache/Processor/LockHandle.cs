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

using System;

namespace Alachisoft.NCache.Processor
{
    public class LockHandle
    {

        private String _lockId;
        private DateTime _lockDate = new DateTime(0);

        public LockHandle()
        {
            try
            {
                _lockDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).Date;
            }
            catch (Exception)
            {
            }
        }

        /**
         * Create a new LockHandle
         *
         * @param lockId Lock id
         * @param lockDate Lock date
         */
        public LockHandle(String lockId, DateTime lockDate)
        {
            this._lockId = lockId;
            this._lockDate = lockDate;
        }

        public string LockId
        {
            set
            {
                _lockId = value;
            }
            get
            {
                return _lockId;
            }
        }

        public DateTime LockDate
        {
            set
            {
                _lockDate = value;
            }
            get
            {
                return _lockDate;
            }
        }
    }

}
