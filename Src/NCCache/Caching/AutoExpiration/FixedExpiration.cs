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
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Runtime.Serialization.IO;



namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// Fixed time expiration based derivative of ExpirationHint.
	/// </summary>
	[Serializable]
	public class FixedExpiration : ExpirationHint, ICompactSerializable
	{

        private const int FixedExpirationSize = 2*Common.MemoryUtil.NetIntSize;

        /// <summary> The absolute time when this hint expires. </summary>
		private int _absoluteTime;
        private int _milliseconds;

        public FixedExpiration()
        {
            _hintType = ExpirationHintType.FixedExpiration;
        }

        #region Creating FixedExpiration

        public static FixedExpiration Create(PoolManager poolManager)
        {
            return poolManager.GetFixedExpirationPool()?.Rent(true) ?? new FixedExpiration();
        }

        public static FixedExpiration Create(PoolManager poolManager, DateTime absoluteTime)
        {
            var expiration = Create(poolManager);
            Construct(expiration, absoluteTime);

            return expiration;
        }

        protected static void Construct(FixedExpiration expiration, DateTime absoluteTime)
        {
            if (expiration != null)
            {
                expiration._absoluteTime = AppUtil.DiffSeconds(absoluteTime);
                expiration._milliseconds = AppUtil.DiffMilliseconds(absoluteTime);
            }
        }

        #endregion

        /// <summary> key to compare expiration hints. </summary>
        internal override int SortKey
		{
			get
			{
				return _absoluteTime;
			}
		}


		/// <summary>
		/// virtual method that returns true when the expiration has taken place, returns 
		/// false otherwise.
		/// </summary>
		internal override bool DetermineExpiration(CacheRuntimeContext context)
		{ 
			if(HasExpired) 
				return true;

            if (_absoluteTime < AppUtil.DiffSeconds(DateTime.Now))
                this.NotifyExpiration(this, null);


			return HasExpired;
		}

        public DateTime AbsoluteTime
        {
            get { 
                DateTime dt = AppUtil.GetDateTime(_absoluteTime);
                return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, _milliseconds, DateTimeKind.Utc);
            }
        }

        public override string ToString()
        {
            return string.Empty;
        }

		#region	/                 --- ICompactSerializable ---           /

		public new void Deserialize(CompactReader reader)
		{
            base.Deserialize(reader);
			_absoluteTime = reader.ReadInt32();
            _milliseconds = reader.ReadInt32();
		}

		public new void Serialize(CompactWriter writer)
		{
            base.Serialize(writer);
			writer.Write(_absoluteTime);
            writer.Write(_milliseconds);
		}

		#endregion

        #region ISizable Members

        public override int Size
        {
            get { return base.Size + FixedExpirationSize; }
        }

        public override int InMemorySize
        {
            get
            {
                int inMemorySize = this.Size;

                inMemorySize += inMemorySize <= 24 ? 0 : Common.MemoryUtil.NetOverHead;

                return inMemorySize;
            }
        }

        #endregion

        #region ILeasable

        public override void ResetLeasable()
        {
            base.ResetLeasable();
            _absoluteTime = 0; _milliseconds = 0;
            _hintType = ExpirationHintType.FixedExpiration;
        }

        public override void ReturnLeasableToPool()
        {

        }

        #endregion

        #region - [Deep Cloning] -

        public override ExpirationHint DeepClone(PoolManager poolManager)
        {
            var clonedHint = poolManager.GetFixedExpirationPool()?.Rent(false) ?? new FixedExpiration();
            DeepCloneInternal(poolManager, clonedHint);
            return clonedHint;
        }

        protected sealed override void DeepCloneInternal(PoolManager poolManager, ExpirationHint clonedHint)
        {
            if (clonedHint == null)
                return;

            base.DeepCloneInternal(poolManager, clonedHint);

            if (clonedHint is FixedExpiration clonedFixedExpirationHint)
            {
                clonedFixedExpirationHint._absoluteTime = _absoluteTime;
                clonedFixedExpirationHint._milliseconds = _milliseconds;
            }
        }

        #endregion
    }
}
