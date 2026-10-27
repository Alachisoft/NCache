//  Copyright (c) 2018 Alachisoft
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
    /// Eviction Hint based on the timestamp; Used in case of LRU based Eviction.
    /// </summary>

    [Serializable]
    public class TimestampHint : EvictionHint, ICompactSerializable
    {
        /// <summary>Time stamp for the hint</summary>
        //protected int _dt;
        [CLSCompliant(false)]
        protected DateTime _dt;
        
        new internal  static int InMemorySize = 32;
        
        static TimestampHint()
        {                                      
            InMemorySize = Common.MemoryUtil.GetInMemoryInstanceSize(EvictionHint.InMemorySize + Common.MemoryUtil.NetDateTimeSize);
        } 


        /// <summary>
        /// Constructor.
        /// </summary>
        public TimestampHint()
        {
            _hintType = EvictionHintType.TimestampHint;
            _dt = DateTime.UtcNow;
        }
        /// <summary>Return time stamp for the hint</summary>
        //public int TimeStamp
        public DateTime TimeStamp
        {
            get { return _dt;}
        }		

        /// <summary>
        /// Return if the hint is to be changed on Update
        /// </summary>
        public override bool IsVariant
        {
            get { return true; }
        }


        /// <summary>
        /// Update the hint if required
        /// </summary>
        /// <returns></returns>
        public override bool Update()
        {
            _dt = DateTime.UtcNow;
            return true;
        }

        public override void Reset(CacheItemPriority cacheItemPriority)
        {
            _dt = DateTime.UtcNow;
        }

        //#region	/                 --- IComparable ---           /

        ///// <summary>
        ///// Compares the current instance with another object of the same type.
        ///// </summary>
        ///// <param name="obj">An object to compare with this instance.</param>
        ///// <returns>A 32-bit signed integer that indicates the relative order of the comparands.</returns>
        //public override int CompareTo(object obj)
        //{
        //    if (obj is TimestampHint)
        //    {
        //        return _dt.CompareTo(((TimestampHint)obj)._dt);
        //    }
        //    else
        //    {
        //        return 0;
        //    }
        //}

        //#endregion

        #region	/                 --- ICompactSerializable ---           /

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _dt = reader.ReadDateTime();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_dt);
        }
        #endregion

        #region Creating TimestampHint

        public static TimestampHint Create(PoolManager poolManager)
        {
            return poolManager.GetTimestampHintPool()?.Rent(true) ?? new TimestampHint();
        }

        public static TimestampHint Create(PoolManager poolManager, DateTime date)
        {
            var instance = Create(poolManager);
            instance._dt = date;
            return instance;
        }

        #endregion
        
        #region ILeaseable

        public override void ResetLeasable()
        {
            base.ResetLeasable();
            _dt = DateTime.UtcNow;
            _hintType = EvictionHintType.TimestampHint;
        }
        public override void ReturnLeasableToPool()
        {
            //PoolManager.GetTimestampHintPool().Return(this);
        }

        #endregion

        #region - [Deep Cloning] -

        public override EvictionHint DeepClone(PoolManager poolManager)
        {
            var clonedTimestampHint = poolManager.GetTimestampHintPool()?.Rent() ?? new TimestampHint();
            clonedTimestampHint._dt = _dt;
            clonedTimestampHint._hintType = _hintType;

            return clonedTimestampHint;
        }

        #endregion

    }
}