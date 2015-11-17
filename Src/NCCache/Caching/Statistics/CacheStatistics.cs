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
using System.Threading;

using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using System.Collections;
using System.Globalization;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Statistics
{
	/// <summary>
	/// Info class that holds statistics related to cache.
	/// </summary>
	[Serializable]
	public class CacheStatistics : ICloneable, ICompactSerializable
	{
		/// <summary> The name of the cache scheme. </summary>
		private string			_className = String.Empty;

		/// <summary> The up time of cache. </summary>
		private DateTime		_upTime;

		/// <summary> The current number of objects in the cache. </summary>
		private long			_count;

        /// <summary> The current number of session objects in the cache. </summary>
        /// <summary> We need this count so that user can not have more than 300 concurrent sessions in the cache. </summary>
        private long            _sessionCount;

		/// <summary> The highest number of objects contained by the cache at any time. </summary>
		private long			_hiCount;

		/// <summary> The maximum number of objects to be contained by the cache. </summary>
		private long			_maxCount;

        /// <summary> The maximum capacity of the cache. </summary>
        private long            _maxSize;

		/// <summary> The number of objects fetched successfuly. </summary>
		private long			_hitCount;

		/// <summary> The number of objects fetch failures. </summary>
		private long			_missCount;

        /// <summary> The number of objects fetch failures. </summary>
        private long            _dataSize;

		/// <summary> The name of the cache scheme. </summary>
		private string			_perfInst = String.Empty;

        /// <summary> A map of local buckets maintained at each node. </summary>
        private HashVector _localBuckets;

        //muds:
        //maximum of 4 unique clients can connect to the cache in Express Edition.        
        private Hashtable _clientsList = Hashtable.Synchronized(new Hashtable(4));
        //ata:
        //In express only client within the cluster can connect with the cache.
        //Currently we have limitation of 2 nodes cluster, therefore there can
        //be maximum 2 client (nodes).
        public const int MAX_CLIENTS_IN_EXPRESS = 2;

		/// <summary>
		/// Constructor.
		/// </summary>
		public CacheStatistics():this(String.Empty, String.Empty)
		{}

		/// <summary>
		/// Constructor.
		/// </summary>
		public CacheStatistics(string instanceName, string className)
		{
			_className = className;
			_perfInst = instanceName;
			_upTime = DateTime.Now;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="stat"></param>
		protected CacheStatistics(CacheStatistics stat)
		{
            lock (stat)
			{
				this._className = stat._className;
				this._perfInst = stat._perfInst;
				this._upTime = stat._upTime;
				this._count = stat._count;
				this._hiCount = stat._hiCount;
				this._maxCount = stat._maxCount;
                this._maxSize = stat._maxSize;
				this._hitCount = stat._hitCount;
				this._missCount = stat._missCount;
                this._localBuckets = stat._localBuckets != null ? stat._localBuckets.Clone() as HashVector : null;
			}
		}

        

		/// <summary>
		/// The type of caching scheme.
		/// </summary>
		public string ClassName
		{
			get { return _className; }
			set { _className = value; }  
		}

		/// <summary>
		/// The type of caching scheme.
		/// </summary>
		public string InstanceName
		{
			get { return _perfInst; }
			set { _perfInst = value; }
		}

		/// <summary>
		/// The name of the cache scheme.
		/// </summary>
		public DateTime UpTime
		{
			get { return _upTime; }
			set { _upTime = value; }  
		}

		/// <summary>
		/// The current number of objects in the cache.
		/// </summary>
		public long Count
		{
			get { return _count; }
			set { _count = value; }  
		}

        public long SessionCount
        {
            get { return _sessionCount; }
            set { _sessionCount = value; }
        }

        public Hashtable ClientsList
        {
            get { return _clientsList; }
            set { _clientsList = value; }
        }

		/// <summary>
		/// The highest number of objects contained by the cache at any time.
		/// </summary>
		public long HiCount
		{
			get { return _hiCount; } 
			set { _hiCount = value; }
		}

		/// <summary>
		/// The highest number of objects contained by the cache at any time.
		/// </summary>
		public long MaxCount
		{
			get { return _maxCount; } 
			set { _maxCount = value; }
		}

        /// <summary>
        /// The maximum capacity of the cache at any time.
        /// </summary>
        public virtual long MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; }
        }

		/// <summary>
		/// The number of objects fetched successfuly.
		/// </summary>
		public long HitCount
		{
			get { return _hitCount; } 
			set { _hitCount = value; }
		}

		/// <summary>
		/// The number of objects fetch failures.
		/// </summary>
		public long MissCount
		{
			get { return _missCount; } 
			set { _missCount = value; }
		}

        public HashVector LocalBuckets
        {
            get { return _localBuckets; }
            set { _localBuckets = value; }
        }

		#region	/                 --- ICloneable ---           /

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		public virtual object Clone()
        {
            return new CacheStatistics(this);
        }

		#endregion

		/// <summary>
		/// returns the string representation of the statistics.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			lock(this)
			{
				System.Text.StringBuilder ret = new System.Text.StringBuilder();
				ret.Append("Stats[Sch:" + ClassName + ", Cnt:" + Count.ToString() + ", ");
				ret.Append("Hi:" + HiCount.ToString() + ", ");
                ret.Append("MxS:" + MaxSize.ToString() + ", ");
				ret.Append("MxC:" + MaxCount.ToString() + ", ");
				ret.Append("Hit:" + HitCount.ToString() + ", ");
				ret.Append("Miss:" + MissCount.ToString() + "]");
				return ret.ToString(); 
			}
		}

		#region	/                 --- Internal statistics gathering helper methods ---           /


		/// <summary>
		/// Updates the count and HiCount of statistics
		/// </summary>
		/// <param name="count"></param>
		protected internal void UpdateCount(long count)
		{
			lock(this)
			{
				_count = count;
				if(_count > _hiCount)
					_hiCount = _count;
			}
        }

        /// <summary>
		/// Updates the session items count of statistics
		/// </summary>
        /// <param name="sessionCountUpdateFlag">
        /// This flag indicates how to update the sessionCount
        /// possible values are as follows: -
        /// 1. -1 (decrement the sessionCount by 1)
        /// 2. 0 (reset the sessionCount to 0)
        /// 3. +1 (increment the sessionCount by 1)
        /// </param>
		protected internal void UpdateSessionCount(int sessionCountUpdateFlag)
		{
            lock (this)
            {
                switch (sessionCountUpdateFlag)
                {
                    case -1:
                        _sessionCount--;
                        break;
                    case 0:
                        _sessionCount = 0;
                        break;
                    case 1:
                        _sessionCount++;
                        break;
                }
            }
		}

        /// <summary>
		/// Increases the miss count by one.
		/// </summary>
		protected internal void BumpMissCount() { lock(this) { ++ _missCount; } }

		/// <summary>
		/// Increases the hit count by one.
		/// </summary>
		protected internal void BumpHitCount() { lock(this) { ++ _hitCount; } }

		#endregion

     #region	/                 --- ICompactSerializable ---           /

        public virtual void Deserialize(CompactReader reader)
        {
            _className = reader.ReadObject() as string;
            _perfInst = reader.ReadObject() as string;
            _upTime = new DateTime(reader.ReadInt64());
            _count = reader.ReadInt64();
            _hiCount = reader.ReadInt64();
            _maxSize = reader.ReadInt64();
            _maxCount = reader.ReadInt64();
            _hitCount = reader.ReadInt64();
            _missCount = reader.ReadInt64();
            _clientsList = reader.ReadObject() as Hashtable;

            //muds:
            _localBuckets = new HashVector();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                BucketStatistics tmp = new BucketStatistics();
                int bucketId = reader.ReadInt32();
                tmp.DeserializeLocal(reader);
                _localBuckets[bucketId] = tmp;
            }
        }

        public virtual void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_className);
            writer.WriteObject(_perfInst);
            writer.Write(_upTime.Ticks);
            writer.Write(_count);
            writer.Write(_hiCount);
            writer.Write(_maxSize);
            writer.Write(_maxCount);
            writer.Write(_hitCount);
            writer.Write(_missCount);
            writer.WriteObject(_clientsList);
            //muds:
            int count = _localBuckets != null ? _localBuckets.Count : 0;
            writer.Write(count);
            if (_localBuckets != null)
            {
                IDictionaryEnumerator ide = _localBuckets.GetEnumerator();
                while (ide.MoveNext())
                {
                    writer.Write((int)ide.Key);
                    ((BucketStatistics)ide.Value).SerializeLocal(writer);
                }
            }
        }

        #endregion

        public static CacheStatistics ReadCacheStatistics(CompactReader reader)
        {
            byte isNull = reader.ReadByte();
            if (isNull == 1)
                return null;
            CacheStatistics newStats = new CacheStatistics();
            newStats.Deserialize(reader);
            return newStats;
        }

        public static void WriteCacheStatistics(CompactWriter writer, CacheStatistics stats)
        {
            byte isNull = 1;
            if (stats == null)
                writer.Write(isNull);
            else
            {
                isNull = 0;
                writer.Write(isNull);
                stats.Serialize(writer);
            }
            return;
        }  		
	}
}
