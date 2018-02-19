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
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// Fixed time expiration based derivative of ExpirationHint.
	/// </summary>
	[Serializable]
	public class FixedExpiration : ExpirationHint, ICompactSerializable
	{
        /// <summary> FixedExpiration instance Size include _absoluteTime plus _milliseconds </summary>
        private const int FixedExpirationSize = 2 * Common.MemoryUtil.NetIntSize;
		/// <summary> The absolute time when this hint expires. </summary>
		private int _absoluteTime;
        private int _milliseconds;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="absoluteTime">absolute time when this hint expires</param>
		public FixedExpiration(DateTime absoluteTime)
		{
            _hintType = ExpirationHintType.FixedExpiration;
            _absoluteTime = AppUtil.DiffSeconds(absoluteTime);
            _milliseconds = AppUtil.DiffMilliseconds(absoluteTime);
		}

        public FixedExpiration()
        {
            _hintType = ExpirationHintType.FixedExpiration;            
        }
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

		public void Deserialize(CompactReader reader)
		{
            base.Deserialize(reader);
			_absoluteTime = reader.ReadInt32();
            _milliseconds = reader.ReadInt32();
		}

		public void Serialize(CompactWriter writer)
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
	}
}
