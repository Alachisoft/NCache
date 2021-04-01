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
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class CacheItemBase: BookKeepingLease, Runtime.Serialization.ICompactSerializable
    {
        /// <summary> The actual object provided by the client application </summary>
        private object _value = null;

        protected CacheItemBase() { }


        protected static void Construct(CacheItemBase cacheItem, object value)
        {
            var bytes = value as byte[];

            if (bytes != null)
                value = UserBinaryObject.CreateUserBinaryObject(bytes, cacheItem.PoolManager);

            cacheItem.Value = value;
        }

        /// <summary> 
        /// The actual object provided by the client application 
        /// </summary>
        public virtual object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (ReferenceEquals(_value, value))
                    return;

                if (_value is UserBinaryObject userBinaryObject)
                    MiscUtil.ReturnUserBinaryObjectToPool(userBinaryObject, userBinaryObject.PoolManager);

                _value = value;
            }
        }

        #region ILeasable

        public override void ResetLeasable()
        {
            _value = null;
        }

        public override void ReturnLeasableToPool()
        {
            throw new InvalidOperationException("Cannot return CacheItemBase to pool.");
        }

        #endregion

        #region ICompact Serializable
        public virtual void Deserialize(CompactReader reader)
        {
            _value = reader.ReadObject();
        }

        public virtual void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_value);
        } 
        #endregion
    }
}