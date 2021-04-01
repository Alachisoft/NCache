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
using System.Text;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;


namespace Alachisoft.NCache.Caching.AutoExpiration
{
    public class LockExpiration : ICompactSerializable
    {
        private long _lastTimeStamp;
        private long _lockTTL;
        private TimeSpan _ttl;

        public LockExpiration() { }

        public LockExpiration(TimeSpan lockTimeout)
        {
            _ttl = lockTimeout;
        }

        public void Set()
        {
            _lockTTL = _ttl.Ticks;
            _lastTimeStamp = AppUtil.DiffTicks(DateTime.Now);
        }

        private long SortKey 
        {
            get 
            {
                long total = _lastTimeStamp + _lockTTL;
                // if overflown...
                if (total < 0) { total = DateTime.MaxValue.Ticks; }
                return total;
            } 
        }

        public bool HasExpired()
        {
            if (SortKey.CompareTo(AppUtil.DiffTicks(DateTime.Now)) < 0)
                return true;
            return false;
        }

        public TimeSpan TTL
        {
            get { return _ttl; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _lockTTL = reader.ReadInt64();
            _lastTimeStamp = reader.ReadInt64();
            _ttl = (TimeSpan)reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_lockTTL);
            writer.Write(_lastTimeStamp);
            writer.WriteObject(_ttl);
        }

        #endregion

        #region - [Deep Cloning] -

        public LockExpiration DeepClone(PoolManager poolManager)
        {
            var clonedLockExpiration = new LockExpiration();
            clonedLockExpiration._lastTimeStamp = _lastTimeStamp;
            clonedLockExpiration._lockTTL = _lockTTL;
            clonedLockExpiration._ttl = _ttl;

            return clonedLockExpiration;
        }

        #endregion
    }
}
