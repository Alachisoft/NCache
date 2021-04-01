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
using System.Threading;

using Alachisoft.NCache.Util;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Logger;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
	/// Abstract base class that defines an interface used by the Cache. Different sort of 
	/// expiration policies must derive from this class including the complex CacheDependency 
	/// classes in Web.Caching package.
	/// </summary>	
    [Serializable]
	public abstract class ExpirationHint : SimpleLease, IComparable, IDisposable, ICompactSerializable,ISizable
	{
		public const int 				EXPIRED = 1;
		public const int 				NEEDS_RESYNC = 2;
		public const int 				IS_VARIANT = 4;
		public const int 				NON_ROUTABLE = 8;
		public const int 				DISPOSED = 16;
        public const int ExpirationHintSize = 24;

        private string                  _cacheKey;
		private int						_bits;
		private IExpirationEventSink	_objNotify;
		[CLSCompliant(false)]
        public ExpirationHintType       _hintType;
		protected ExpirationHint()
		{            
            _hintType = ExpirationHintType.Parent;
		}

		#region	/                 --- IDisposable ---           /

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
            if (!IsDisposed)
            {
                SetBit(DISPOSED);
                DisposeInternal();
            }
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		protected virtual void DisposeInternal()
		{
		}

		#endregion

		/// <summary> key to compare expiration hints. </summary>
		internal abstract int SortKey { get; }

        [NonSerialized] internal ILogger _ncacheLog = null;

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

		/// <summary> Property that returns true when the expiration has taken place, returns false otherwise.</summary>
		public bool HasExpired { get { return IsBitSet(EXPIRED); } }
		/// <summary> virtual method that returns true if user has selected to Re-Sync the object
		/// when expired else false. </summary>
		public bool NeedsReSync { get { return IsBitSet(NEEDS_RESYNC); } }
		/// <summary> Return if hint is to be updated on Reset </summary>
		public bool IsVariant { get { return IsBitSet(IS_VARIANT); } }
		/// <summary> Returns true if the hint can be routed to other nodes, otherwise returns false.</summary>
		public bool IsRoutable { get { return !IsBitSet(NON_ROUTABLE); } }
		/// <summary> Returns true if the hint can be routed to other nodes, otherwise returns false.</summary>
		public bool IsDisposed { get { return IsBitSet(DISPOSED); } }
        /// <summary> Returns true if the hint is indexable in expiration manager, otherwise returns false.</summary>
        virtual public bool IsIndexable { get { return true; } }        

        virtual public string CacheKey { set { _cacheKey = value; } get { return _cacheKey; } }

		/// <summary>
		/// virtual method that returns true when the expiration has taken place, returns 
		/// false otherwise.
		/// </summary>
		internal virtual bool DetermineExpiration(CacheRuntimeContext context)
		{ 
			return HasExpired;
		}

        /// <summary>
        /// virtual method that returns true when the expiration has taken place, returns
        /// false otherwise. Used only for those hints that are validated at the time of Get
        /// operation on the cache.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal virtual bool CheckExpired(CacheRuntimeContext context)
        {
            return false;
        }


		/// <summary>
		/// Resets the value of the hint. Called by the Cache manager upon a successful HIT.
		/// </summary>
		internal virtual bool Reset(CacheRuntimeContext context)
		{
			_bits &= ~EXPIRED;
            if (_ncacheLog == null)
                _ncacheLog = context.NCacheLog;
            return true;
		}

        internal virtual void ResetVariant(CacheRuntimeContext context)
        {
            if (this.IsVariant)
            {
                Reset(context);
            }
        }

		protected internal void SetExpirationEventSink(IExpirationEventSink objNotify)
		{
			this._objNotify = objNotify;
		}
 
		protected virtual void NotifyExpiration(object sender, EventArgs e)
		{
			if (this.SetBit(EXPIRED))
			{
				IExpirationEventSink changed1 = this._objNotify;
				if(changed1 != null)
				{
					changed1.DependentExpired(sender, e);
				}
			}
		}

		/// <summary> Returns true if the hint can be routed to other nodes, otherwise returns false.</summary>
		public bool IsBitSet(int bit) 
		{ 
			return ((_bits & bit) != 0); 
		}

		/// <summary>
		/// Sets various flags of this expiration hint.
		/// </summary>
		public virtual bool SetBit(int bit)
		{
			while (true)
			{
				int oldBits = this._bits;
				if ((oldBits & bit) != 0)
				{
					return false;
				}
				int newBits = Interlocked.CompareExchange(ref this._bits, (int) (oldBits|bit), oldBits);
				if (newBits == oldBits)
				{
					return true;
				}
			}
		}
 
		#region	/                 --- IComparable ---           /

		/// <summary>
		/// Compares the current instance with another object of the same type.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>A 32-bit signed integer that indicates the relative order of the comparands.</returns>
		int IComparable.CompareTo(object obj)
		{
			if (obj is ExpirationHint)
			{
				return SortKey.CompareTo( ((ExpirationHint)obj).SortKey);
			}
			else
			{
				return 1; 
			}
		}

		#endregion

        public static ExpirationHint ReadExpHint(CompactReader reader, PoolManager poolManager)
        {
            ExpirationHintType expHint = ExpirationHintType.Parent;
            expHint = (ExpirationHintType)reader.ReadInt16();

            switch (expHint)
            {
                case ExpirationHintType.NULL:
                    return null;
                
                case ExpirationHintType.Parent:
                    return (ExpirationHint)reader.ReadObject();
                
                case ExpirationHintType.FixedExpiration:
                    var fe = FixedExpiration.Create(poolManager);
                    fe.Deserialize(reader);
                    return fe;
                
                case ExpirationHintType.TTLExpiration:
                    var ttle = TTLExpiration.Create(poolManager);
                    ((ICompactSerializable)ttle).Deserialize(reader);
                    return ttle;                    
                
                case ExpirationHintType.FixedIdleExpiration:
                    var fie = FixedIdleExpiration.Create(poolManager);
                    ((ICompactSerializable)fie).Deserialize(reader);
                    return fie;     
                    
#if !( DEVELOPMENT || CLIENT)
                case ExpirationHintType.NodeExpiration:
                    var ne = NodeExpiration.Create(poolManager);
                    ((ICompactSerializable)ne).Deserialize(reader);
                    return ne;
#endif
                case ExpirationHintType.IdleExpiration:
                    var ie = IdleExpiration.Create(poolManager);
                    ((ICompactSerializable)ie).Deserialize(reader);
                    return ie;
                    
                case ExpirationHintType.AggregateExpirationHint:
                    var aeh = AggregateExpirationHint.Create(poolManager);
                    ((ICompactSerializable)aeh).Deserialize(reader);
                    return aeh;
                    
                case ExpirationHintType.DependencyHint:                    
                    break;
                
                default:
                    break;            
            }
            return null;
        }


        public static void WriteExpHint(CompactWriter writer, ExpirationHint expHint)
        {
            if (expHint == null)
            {
                writer.Write((short)ExpirationHintType.NULL);
                return;
            }

            writer.Write((short)expHint._hintType);
            ((ICompactSerializable)expHint).Serialize(writer);
            
            return;
        }

        /// <summary>
        /// Override this method for hints that should be reinitialized when they are moved to another partition.
        /// e.g SqlYukondependency Hint must be reinitialized after state transfer so that its listeners are created on 
        /// new subcoordinator.
        /// </summary>
        /// <param name="context">CacheRuntimeContex for required contextual information.</param>
        /// <returns>True if reinitialization was successfull.</returns>
        internal virtual bool ReInitializeHint(CacheRuntimeContext context)
        {
            return false;
        }

        /// <summary>
        /// Gets the hint which has caused the expiration.
        /// </summary>
        /// <returns></returns>
        internal virtual ExpirationHint GetExpiringHint()
        {
            return this;
        }
		#region ICompactSerializable Members

		public void Deserialize(CompactReader reader)
		{
            this._hintType = (ExpirationHintType)reader.ReadObject();
			this._bits = reader.ReadInt32();
		}

		public void Serialize(CompactWriter writer)
		{
            writer.WriteObject(this._hintType);
			writer.Write(_bits);
		}

		#endregion

        public virtual int Size
        {
            get 
            {
                return ExpirationHintSize;
            }
        }

        public virtual int InMemorySize
        {
            get
            {
                return ExpirationHintSize;
            }
        }

        #region ILeasable

        public override void ResetLeasable()
        {
            _bits = 0;
            _cacheKey = string.Empty;
            _objNotify = default(IExpirationEventSink); 
        }

        public override void ReturnLeasableToPool()
        {

        }

        #endregion

        #region - [Deep Cloning] -

        // It's good for devirtualization that we override deep clone in child classes
        // instead of having a single method that calls overriden DeepCloneInternal() 
        // on a supposedly new method (that doesn't exist right now) that returns the 
        // appropriate ExpirationHint derived instance to this method.
        public abstract ExpirationHint DeepClone(PoolManager poolManager);

        protected virtual void DeepCloneInternal(PoolManager poolManager, ExpirationHint clonedHint)
        {
            if (clonedHint == null)
                return;

            clonedHint._bits = _bits;
            clonedHint._cacheKey = _cacheKey;
            clonedHint._hintType = _hintType;
            clonedHint._objNotify = _objNotify;
            clonedHint._ncacheLog = _ncacheLog;
        }

        #endregion
    }
}
