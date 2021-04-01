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

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Provides options for locking. 
    /// </summary>
    [Serializable]
    public sealed class LockOptions:Runtime.Serialization.ICompactSerializable
    {
        private object _lockId;
        private DateTime _lockDate;
        private TimeSpan _lockAge;


        public LockOptions() { }
        
        public LockOptions(object lockId, DateTime lockDate)
        {
            this._lockId = lockId;
            this._lockDate = lockDate;
        }
        /// <summary>
        /// Gets or Sets a unique lock value for a cache key.
        /// </summary>
        public object LockId
        {
            get { return _lockId; }
            set { _lockId = value; }
        }

        /// <summary>
        /// The DateTime when this lock was acquired on the cachekey. This DateTime is set on the cache server not on the web server.
        /// </summary>
        public DateTime LockDate
        {
            get { return _lockDate; }
            set { _lockDate = value; }
        }

        /// <summary>
        /// The lock Age of the current lock. This is computed on the cache server are returned to the client.
        /// </summary>
        public TimeSpan LockAge
        {
            get { return _lockAge; }
            set { _lockAge = value; }
        }

        #region Icompact Serializable
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _lockId = reader.ReadObject();
            _lockDate = reader.ReadDateTime();
            _lockAge =(TimeSpan) reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_lockId);
            writer.Write(_lockDate);
            writer.WriteObject(_lockAge);
        } 
        #endregion
    }
}
