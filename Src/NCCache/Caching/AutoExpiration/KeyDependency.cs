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
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// Summary description for KeyExpiration.
	/// </summary>
	[Serializable]
	public class KeyDependency : DependencyHint
	{     
		/// <summary> keys the dependency is based upon. </summary>
		private string[] _cacheKeys;


        public KeyDependency()         
        {
            _hintType = ExpirationHintType.KeyDependency;
        }
        
        /// <summary>
		/// Initializes a new instance of the KeyExpiration class that monitors an array of 
		/// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>

        public KeyDependency(string key):this(new string[] { key }, DateTime.Now)		
        {
            _hintType = ExpirationHintType.KeyDependency;
        }

		/// <summary>
		/// Initializes a new instance of the KeyExpiration class that monitors an array of 
		/// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>

        public KeyDependency(string key, DateTime startAfter):this(new string[] { key }, startAfter)
		{
            _hintType = ExpirationHintType.KeyDependency;
        }

		/// <summary>
		/// Initializes a new instance of the KeyExpiration class that monitors an array of 
		/// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>

        public KeyDependency(string[] keys):this(keys , DateTime.Now)
		{
            _hintType = ExpirationHintType.KeyDependency;
        }

		/// <summary>
		/// Initializes a new instance of the KeyExpiration class that monitors an array of 
		/// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>

        public KeyDependency(string[] keys, DateTime startAfter):base(startAfter)
		{
            _hintType = ExpirationHintType.KeyDependency;
            _cacheKeys = keys;
		}

        /// <summary>
        /// Return array of cache keys
        /// </summary>
        public string[] CacheKeys
        {
            get { return _cacheKeys; }
        }

        /// <summary>
        /// Get ticks for time when change tracking begins
        /// </summary>
        public long StartAfterTicks
        {
            get { return base.StartAfterTime.Ticks; }
        }

        /// <summary>
        /// <summary> Returns true if the hint is indexable in expiration manager, otherwise returns false.
        /// </summary>
        public override bool IsIndexable { get { return true; } }


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
		/// Resets the value of this hint. Called by the Cache manager upon a successful HIT.
		/// </summary>
		internal override bool Reset(CacheRuntimeContext context)
		{
            return base.Reset(context);
		}

		/// <summary>
		/// returns true when the time to live has run out, returns false otherwise.
		/// </summary>
		public override bool HasChanged { get { return false; } }

        public override string ToString()
        {
            string toString = "KEYDEPENDENCY \"";
            for (int i = 0; i < _cacheKeys.Length; i++)
                toString += _cacheKeys[i] + "\"";
            toString += "STARTAFTER\"" + StartAfterTicks.ToString() + "\"\r\n";

            return toString;
        }


        #region ISizable Members

        public override int Size
        {
            get { return base.Size + KeyDependencySize; }
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

        private int KeyDependencySize 
        {
            get 
            { 
                int temp=0;
              
                if (_cacheKeys != null)
                {
                    temp += Common.MemoryUtil.GetStringSize(_cacheKeys);

                    //_cacheKeys will contain this this.Cachekey as KeysDependingOnMe, so we will calculate overhead for them as well
                    temp += _cacheKeys.Length *((Common.MemoryUtil.GetStringSize(this.CacheKey))+Common.MemoryUtil.NetHashtableOverHead);
                }

                temp += Common.MemoryUtil.NetLongSize; // _startAfterTicks

                return temp;                  
            }
        }
        #endregion
        
        #region	/                 --- ICompactSerializable ---           /

        public override void Deserialize(CompactReader reader)
		{
            base.Deserialize(reader);
			_cacheKeys = (string[])reader.ReadObject();
		}

		public override void Serialize(CompactWriter writer)
		{
            base.Serialize(writer);
			writer.WriteObject(_cacheKeys);
		}

		#endregion
	}
}
