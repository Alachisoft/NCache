// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// Idle Time to Live based derivative of ExpirationHint.
	/// </summary>
	/// 
	[Serializable]
	public class IdleExpiration : ExpirationHint, ICompactSerializable
	{

        /// <summary> FixedExpiration instance Size include _idleTimeToLive plus _lastTimeStamp </summary>
        private const int IdleExpirationSize = 2 * Common.MemoryUtil.NetIntSize;

		/// <summary> the idle time to live value </summary>
		private UInt16		_idleTimeToLive;

		/// <summary> last timestamp when the expiration was checked </summary>
		private int		_lastTimeStamp;


        public IdleExpiration()
        {
            _hintType = ExpirationHintType.IdleExpiration;
        }
        
        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="idleTTL">the idle time to live value</param>
		public IdleExpiration(TimeSpan idleTTL)
		{			
            this.SetBit(IS_VARIANT);
			_idleTimeToLive = (UInt16)idleTTL.TotalSeconds;
            _lastTimeStamp = AppUtil.DiffSeconds(DateTime.Now);
            _hintType = ExpirationHintType.IdleExpiration;
		}


        public TimeSpan SlidingTime
        {
            get
            {
                return new TimeSpan(0, 0, (int)_idleTimeToLive);
            }
        }

        public override string ToString()
        {
            return string.Empty;
        }

        public int LastAccessTime
        {
            get { return _lastTimeStamp; }
        }

		/// <summary> key to compare expiration hints. </summary>
		internal override int SortKey { get { return _lastTimeStamp + _idleTimeToLive; } }

		/// <summary>
		/// virtual method that returns true when the expiration has taken place, returns 
		/// false otherwise.
		/// </summary>
		internal override bool DetermineExpiration(CacheRuntimeContext context)
		{ 
			if(HasExpired) 
				return true;

            if (SortKey.CompareTo(AppUtil.DiffSeconds(DateTime.Now)) < 0)
                this.NotifyExpiration(this, null);
			return HasExpired;
		}

		/// <summary>
		/// Resets the time to live counter. 
		/// </summary>
		internal override bool Reset(CacheRuntimeContext context)
		{
            _lastTimeStamp = AppUtil.DiffSeconds(DateTime.Now);
			return base.Reset(context);
		}

		#region	/                 --- ICompactSerializable ---           /

		public void Deserialize(CompactReader reader)
		{
            base.Deserialize(reader);
			_idleTimeToLive = reader.ReadUInt16();
			_lastTimeStamp = reader.ReadInt32();
		}

		public void Serialize(CompactWriter writer)
		{
            base.Serialize(writer);
			writer.Write(_idleTimeToLive);
			writer.Write(_lastTimeStamp);
		}

		#endregion

        #region ISizable Members

        public override int Size
        {
            get { return base.Size + IdleExpirationSize; }
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
	}
}
