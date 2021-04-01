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
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// Combines multiple expiration hints and provides a single hint.
	/// </summary>
	///  
	[Serializable]
	public class AggregateExpirationHint : ExpirationHint, IExpirationEventSink, ICompactSerializable
	{
		/// <summary> expiration hints </summary>
		private ClusteredList<ExpirationHint> _hints = new ClusteredList<ExpirationHint>();
        private ExpirationHint _expiringHint;
        /// <summary>
        /// Constructor.
        /// </summary>
        public AggregateExpirationHint()
        {
            base._hintType = ExpirationHintType.AggregateExpirationHint;
        }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="first">expiration hints</param>
		public AggregateExpirationHint(params ExpirationHint[] hints)
		{
            base._hintType = ExpirationHintType.AggregateExpirationHint;
            Initialize(hints);
		}

        #region Creating AggregateExpirationHint

        public static AggregateExpirationHint Create(PoolManager poolManager)
        {
            return poolManager.GetAggregateExpirationHintPool()?.Rent(true) ?? new AggregateExpirationHint();
        }

        public static AggregateExpirationHint Create(PoolManager poolManager, params ExpirationHint[] hints)
        {
            var expirationHint = Create(poolManager);
            Construct(expirationHint, hints);

            return expirationHint;
        }

        protected static void Construct(AggregateExpirationHint expirationHint, params ExpirationHint[] hints)
        {
            expirationHint?.Initialize(hints);
        }

        #endregion

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        protected override void DisposeInternal()
		{
			for(int i=0; i<_hints.Count; i++)
			{
				((IDisposable)_hints[i]).Dispose();
			}
		}

		#endregion

		/// <summary> expiration hints </summary>
        public IList<ExpirationHint> Hints { get { return (IList<ExpirationHint>) _hints.Clone(); } }
        public IList<ExpirationHint> HintsWithoutClone { get { return _hints; } }


        /// <summary>
        /// Return the enumerator on internal hint collection
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return _hints.GetEnumerator();
        }

		/// <summary>
		/// Set the cache key
		/// </summary>
		public override string CacheKey
		{
			set
			{
				for(int i=0; i<_hints.Count; i++)
				{
					((ExpirationHint)_hints[i]).CacheKey = value;
				}
			}
		}

        public override bool SetBit(int bit)
        {
            bool result = false;
            //needs_resync is set for every individual hint.
            //this is because we treat the resync for db dependency differently.
            if (bit == NEEDS_RESYNC)
            {
                for (int i = 0; i < _hints.Count; i++)
                {
                    result = ((ExpirationHint)_hints[i]).SetBit(bit);
                    if (!result) return result;
                }
            }

            return base.SetBit(bit);
        }

        /// <summary>
        /// Add an expiration hint to the hints
        /// </summary>
        /// <param name="eh"></param>
        public void Add(ExpirationHint eh)
        {
            lock (this)
            {
                if (!eh.IsRoutable)
                    this.SetBit(NON_ROUTABLE);
                if (eh.IsVariant)
                    this.SetBit(IS_VARIANT);
                eh.SetExpirationEventSink(this);

                AggregateExpirationHint aggregate = eh as AggregateExpirationHint;
                if (aggregate!= null)
                {
                    foreach (ExpirationHint expirationHint in aggregate._hints)
                    {
                        _hints.Add(expirationHint);
                    }
                }
                else
                {
                    _hints.Add(eh);
                }

                bool isFixed = false;
                for (int i = _hints.Count-1; i >= 0 ; i--)
			    {
                    if(isFixed && _hints[i] is FixedExpiration)
                    {
                        var previousFixedExpiration = _hints[i];
                        _hints.RemoveAt(i);
                        previousFixedExpiration.ReturnLeasableToPool();

                        break;
                    }
    		        if(!isFixed && _hints[i] is FixedExpiration) 
                     isFixed = true;
			    }
                
            }
        }

		/// <summary> key to compare expiration hints. </summary>
		internal override int SortKey
		{
			get 
			{
                ExpirationHint minHint = (ExpirationHint)_hints[0];
				for(int i=0; i<_hints.Count; i++)
				{
					if (((IComparable)_hints[i]).CompareTo(minHint) < 0)
					{
                        minHint = (ExpirationHint)_hints[i];
					}
				}
				return minHint.SortKey;
			}
		}

		/// <summary>
		/// Initializes the aggregate hint with multiple dependent hints.
		/// </summary>
		/// <param name="first">expiration hints</param>
        protected void Initialize(ExpirationHint[] hints)
        {
            if (hints == null) throw new ArgumentNullException("hints");
            _hints.AddRange(hints);

            for (int i = 0; i < _hints.Count; i++)
            {
                if (!((ExpirationHint)_hints[i]).IsRoutable)
                    this.SetBit(NON_ROUTABLE);
                if (((ExpirationHint)_hints[i]).IsVariant)
                    this.SetBit(IS_VARIANT);
                ((ExpirationHint)_hints[i]).SetExpirationEventSink(this);
            }
        }


		/// <summary>
		/// Determines if any of the aggregated expiration hints has expired.
		/// </summary>
		/// <returns>true if expired</returns>
		internal override bool DetermineExpiration(CacheRuntimeContext context)
		{ 
			if(HasExpired) 
				return true;

			for(int i=0; i<_hints.Count; i++)
			{
				if(((ExpirationHint)_hints[i]).DetermineExpiration(context))
				{
					this.NotifyExpiration(_hints[i], null);
					break;
				}
			}
			return HasExpired;
		}

        /// <summary>
        /// Determines if any of the aggregated expiration hints has expired.
        /// </summary>
        /// <returns>true if expired</returns>
        internal override bool CheckExpired(CacheRuntimeContext context)
        {
            if (HasExpired)
                return true;

            for (int i = 0; i < _hints.Count; i++)
            {
                if (((ExpirationHint)_hints[i]).CheckExpired(context))
                {
                    this.NotifyExpiration(_hints[i], null);
                    break;
                }
            }
            return HasExpired;
        }

        protected override void NotifyExpiration(object sender, EventArgs e)
        {
            _expiringHint = sender as ExpirationHint;
            base.NotifyExpiration(sender, e);
        }
		/// <summary>
		/// Resets both the contained ExpirationHints.
		/// </summary>
		internal override bool Reset(CacheRuntimeContext context)
		{
			bool flag = base.Reset(context);
			for(int i=0; i<_hints.Count; i++)
			{
				if(((ExpirationHint)_hints[i]).Reset(context)) 
				{
					flag = true;
				}
			}
			return flag;
		}

        internal override ExpirationHint GetExpiringHint()
        {
            return _expiringHint;
        }
        /// <summary>
        /// Resets only the variant ExpirationHints.
        /// </summary>
        internal override void ResetVariant(CacheRuntimeContext context)
        {
            for (int i = 0; i < _hints.Count; i++)
            {
                ExpirationHint hint = (ExpirationHint)_hints[i];
                if (hint.IsVariant)
                {
                    hint.Reset(context);
                }
            }
        }

        public override string ToString()
        {
            string toString = (!(_hints[0] is IdleExpiration || _hints[0] is FixedExpiration)) ? toString = "INNER\r\n" : string.Empty;
            for (int i = 0; i < _hints.Count; i++)
            {
               toString += _hints[i].ToString();
            }
            return toString;
        }

        void IExpirationEventSink.DependentExpired(object sender, EventArgs e)
		{
			this.NotifyExpiration(sender, e);
		}

		#region	/                 --- ICompactSerializable ---           /

		public new void Deserialize(CompactReader reader)
		{
            base.Deserialize(reader);

			int length = reader.ReadInt32();

            if (_hints == null)
                _hints = new ClusteredList<ExpirationHint>(length);

            for (int i = 0; i < length; i++)
                _hints.Insert(i,(ExpirationHint)reader.ReadObject());
		}

		public new void Serialize(CompactWriter writer)
		{
            base.Serialize(writer);
			writer.Write(_hints.Count);
			for (int i = 0; i < _hints.Count; i++)
				writer.WriteObject(_hints[i]);
		}

		#endregion

#if SERVER
        internal AggregateExpirationHint GetRoutableClone(PoolManager poolManager, Address sourceNode)
        {
            if (_hints == null || _hints.Count == 0)
                return null;

            AggregateExpirationHint hint = Create(poolManager);

            NodeExpiration ne = null;
            for (int i = 0; i < _hints.Count; i++)
            {
                ExpirationHint eh = (ExpirationHint)_hints[i];
                if (!eh.IsRoutable && ne == null)
                {
                    ne = NodeExpiration.Create(poolManager, sourceNode);
                    hint.Add(ne);
                }
                else
                {
                    hint.Add(eh);
                }
            }
            return hint;
        }
#endif

        #region ISizable Implementation

        public override int Size
        {
            get { return base.Size + AggregateExpirationHintSize; }
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

        private int AggregateExpirationHintSize
        {
            get
            {
                int temp = 0;
                
                if (_hints != null)
                {
                    foreach (ISizable hint in _hints)
                    {
                        temp += hint.InMemorySize;
                    }
                }

                return temp;
            }
        }

        #endregion

        #region ILeasable

        public override void ResetLeasable()
        {
            base.ResetLeasable();

            _hints?.Clear();
            _expiringHint = null;
            _hintType = ExpirationHintType.AggregateExpirationHint;
        }

        public override void ReturnLeasableToPool()
        {
            if (_hints?.Count > 0)
            {
                foreach (var hint in _hints)
                {
                    MiscUtil.ReturnExpirationHintToPool(hint, hint?.PoolManager);

                }
            }
        }

        #endregion

        #region - [Deep Cloning] -

        public override ExpirationHint DeepClone(PoolManager poolManager)
        {
            var clonedHint = poolManager.GetAggregateExpirationHintPool()?.Rent(true) ?? new AggregateExpirationHint();
            DeepCloneInternal(poolManager, clonedHint);
            return clonedHint;
        }

        protected sealed override void DeepCloneInternal(PoolManager poolManager, ExpirationHint clonedHint)
        {
            if (clonedHint == null)
                return;

            base.DeepCloneInternal(poolManager, clonedHint);

            if (clonedHint is AggregateExpirationHint clonedAggregateExpirationHint)
            {
                if (_hints != null)
                {
                    var clonedHints = new ClusteredList<ExpirationHint>(_hints.Count);

                    foreach (var hint in _hints)
                    {
                        clonedHints.Add(hint.DeepClone(poolManager));
                    }
                    clonedAggregateExpirationHint._hints = clonedHints;
                }
                clonedAggregateExpirationHint._expiringHint = _expiringHint?.DeepClone(poolManager);
            }
        }

        #endregion
    }

}
