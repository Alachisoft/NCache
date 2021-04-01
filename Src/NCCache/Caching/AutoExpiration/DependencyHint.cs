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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
    /// Base class for dependency based item evictions.
    /// </summary>
    [Serializable]
    public abstract class DependencyHint : ExpirationHint, ICompactSerializable
    {
        public const int DependencyHintSize = 4;
        /// <summary> The datetime to start monitoring after. </summary>
        [CLSCompliant(false)]
        protected DateTime	_startAfter;

        /// <summary>
        /// Constructor.
        /// </summary>
        protected DependencyHint()
        {
            _hintType = ExpirationHintType.DependencyHint;
            _startAfter = DateTime.Now;
        }

        #region Creating DependencyHint

        protected static void Construct(DependencyHint hint, DateTime startAfter)
        {
            if (hint != null)
            {
                hint._startAfter = startAfter;
            }
        }

        #endregion

        public DateTime StartAfterTime { get { return _startAfter; } }

        /// <summary> key to compare expiration hints. </summary>
        internal sealed override int SortKey { get { return AppUtil.DiffSeconds(_startAfter); } }

        /// <summary>
        /// <summary> Returns true if the hint is indexable in expiration manager, otherwise returns false.
        /// </summary>
        public override bool IsIndexable { get { return false; } }

        /// <summary>
        /// virtual method that returns true when the expiration has taken place, returns 
        /// false otherwise.
        /// </summary>
        internal override bool DetermineExpiration(CacheRuntimeContext context)
        { 
            if (_startAfter.CompareTo(DateTime.Now) > 0)
            {
                return false;
            }
            if(!HasExpired)
            {
                if(HasChanged)
                    NotifyExpiration(this, null);
            }
            return HasExpired;
        }

        /// <summary>
        /// method that returns true when the expiration has taken place, returns
        /// false otherwise. Used only for those hints that are validated at the time of Get
        /// operation on the cache.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override bool CheckExpired(CacheRuntimeContext context)
        {
            return DetermineExpiration(context);
        }


        /// <summary>
        /// Gets a value indicating whether the CacheDependency object has changed.
        /// </summary>
        public abstract bool HasChanged { get; }

        #region	/                 --- ICompactSerializable ---           /

        public virtual new void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _startAfter = reader.ReadDateTime();
        }

        public virtual new void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_startAfter);
        }

        #endregion

        public override int Size
        {
            get { return base.Size + DependencyHintSize; }
        }

        public override int InMemorySize
        {
            get 
            {
                int inMemorySize = Size;

                inMemorySize += inMemorySize <= 24 ? 0 : MemoryUtil.NetOverHead;

                return inMemorySize;
            }
        }

        #region ILeasable

        public override void ResetLeasable()
        {
            base.ResetLeasable();

            _startAfter = DateTime.Now;
            _hintType = ExpirationHintType.DependencyHint;
        }

        #endregion

        #region - [Deep Cloning] -

        protected override void DeepCloneInternal(PoolManager poolManager, ExpirationHint clonedHint)
        {
            if (clonedHint == null)
                return;

            base.DeepCloneInternal(poolManager, clonedHint);

            if (clonedHint is DependencyHint clonedDependencyHint)
            {
                clonedDependencyHint._startAfter = _startAfter;
            }
        }

        #endregion
    }
}