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
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    public class LockMetaInfo : ICompactSerializable
    {
        private object _lockId;
        private DateTime _lockDate;
        private TimeSpan _lockAge;
        private LockAccessType _accessType;
        private LockExpiration _lockExpiration;
        private Alachisoft.NCache.Common.Locking.LockManager _lockManager;

        public object LockId
        {
            get { return _lockId; }
            set{ _lockId = value; }
        }

        public TimeSpan LockAge
        {
            get { return _lockAge; }
            set
            {
                lock (this)
                { _lockAge = value; }
            }
        }

        public DateTime LockDate
        {
            get { return _lockDate; }
            set { _lockDate = value;}
        }

        public LockAccessType LockAccessType
        {
            get { return _accessType; }
            set {_accessType = value; }
        }

        public LockExpiration LockExpiration
        {
            get { return _lockExpiration; }
            set { _lockExpiration = value;  }
        }

        public Alachisoft.NCache.Common.Locking.LockManager LockManager
        {
            get { return _lockManager; }
            set { _lockManager = value; }
        }

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            _lockId=reader.ReadObject();
            _lockDate = reader.ReadDateTime();
            _lockExpiration = reader.ReadObject() as LockExpiration;
            _lockManager = reader.ReadObject() as LockManager;                
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.WriteObject(_lockId);
            writer.Write(_lockDate);
            writer.WriteObject(_lockExpiration);
            writer.WriteObject(_lockManager);
        }
    }
}