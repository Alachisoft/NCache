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
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.EvictionPolicies
{
    /// <summary>
    /// Eviction Hint based on counter; Used in case of LFU based eviction.
    /// </summary>

    [Serializable]
    public class CounterHint : EvictionHint, ICompactSerializable
    {
        /// <summary>Count for the hint</summary>
        [CLSCompliant(false)]
        protected short		_count = 1;

        new internal static int InMemorySize = 24;


        static CounterHint()
        {
            //for _count
            InMemorySize = Common.MemoryUtil.GetInMemoryInstanceSize(EvictionHint.InMemorySize + Common.MemoryUtil.NetShortSize);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CounterHint()
        {
            _hintType = EvictionHintType.CounterHint;
        }

        /// <summary>
        /// Constructor, Just for debugging
        /// </summary>
        public CounterHint(short count)
        {
            _hintType = EvictionHintType.CounterHint;
            _count = count;
        }


        /// <summary>Get the count of the hint</summary>
        public short Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Return if hint is to be changed on Update
        /// </summary>
        public override bool IsVariant
        {
            get 
            {
                if (_count < 32767)
                {
                    return true;
                }
                return false;
            }
        }


        /// <summary>
        /// Update the hint if required
        /// </summary>
        /// <returns></returns>
        public override bool Update()
        {
            if (_count < 32767)
            {
                _count++;
                return true;
            }

            return false;
        }

        public override void Reset(CacheItemPriority cacheItemPriority)
        {
            _count = 1;
        }



        #region	/                 --- ICompactSerializable ---           /

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _count = reader.ReadInt16();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_count);
        }

        #endregion

        #region Creating CounterHint

        public static CounterHint Create(PoolManager poolManager)
        {
            return poolManager.GetCounterHintPool()?.Rent(true) ?? new CounterHint();
        }

        public static CounterHint Create(PoolManager poolManager, short count)
        {
            var instance = Create(poolManager);
            instance._count = count;
            return instance;
        }
        
        #endregion

        #region ILeaseable

        public override void ResetLeasable()
        {
            base.ResetLeasable();
            _hintType = EvictionHintType.CounterHint;
            _count = 1;
        }

        public override void ReturnLeasableToPool()
        {

        }

        #endregion

        #region - [Deep Cloning] -

        public override EvictionHint DeepClone(PoolManager poolManager)
        {
            var clonedCounterHint = poolManager.GetCounterHintPool()?.Rent() ?? new CounterHint();
            clonedCounterHint._count = _count;
            clonedCounterHint._hintType = _hintType;

            return clonedCounterHint;
        }

        #endregion
    }
}