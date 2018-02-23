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
using System.Threading;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Logger;

using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
	/// Abstract base class that defines an interface used by the Cache. Different sort of 
	/// expiration policies must derive from this class including the complex CacheDependency 
	/// classes in Web.Caching package.
	/// </summary>	
    [Serializable]
	public abstract class ExpirationHint : IComparable, IDisposable, ICompactSerializable,ISizable
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
		void IDisposable.Dispose()
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

        //[NonSerialized] internal NewTrace nTrace = null;
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
				return 1; // Consider throwing an exception
			}
		}

		#endregion

        public static ExpirationHint ReadExpHint(CompactReader reader)
        {
            ExpirationHintType expHint = ExpirationHintType.Parent;
            expHint = (ExpirationHintType)reader.ReadInt16();
            ExpirationHint tmpObj = null;
            switch (expHint)
            {
                case ExpirationHintType.NULL:
                    return null;
                
                case ExpirationHintType.Parent:
                    tmpObj = (ExpirationHint)reader.ReadObject();
                    return (ExpirationHint)tmpObj;                    
                
                case ExpirationHintType.FixedExpiration:
                    FixedExpiration fe = new FixedExpiration();
                    ((ICompactSerializable)fe).Deserialize(reader);
                    return (ExpirationHint)fe;                    
                
                case ExpirationHintType.TTLExpiration:
                    TTLExpiration ttle = new TTLExpiration();
                    ((ICompactSerializable)ttle).Deserialize(reader);
                    return (ExpirationHint)ttle;                    
                
                case ExpirationHintType.TTLIdleExpiration:
                    TTLIdleExpiration ttlie = new TTLIdleExpiration();
                    ((ICompactSerializable)ttlie).Deserialize(reader);
                    return (ExpirationHint)ttlie;                    
                
                case ExpirationHintType.FixedIdleExpiration:
                    FixedIdleExpiration fie = new FixedIdleExpiration();
                    ((ICompactSerializable)fie).Deserialize(reader);
                    return (ExpirationHint)fie;                    
                
                case ExpirationHintType.FileDependency:
                    FileDependency fd = new FileDependency();
                    ((ICompactSerializable)fd).Deserialize(reader);
                    return (ExpirationHint)fd;
                    
                case ExpirationHintType.KeyDependency:
                    KeyDependency kd = new KeyDependency();
                    ((ICompactSerializable)kd).Deserialize(reader);
                    return (ExpirationHint)kd;
                    
#if !( DEVELOPMENT || CLIENT)
                case ExpirationHintType.NodeExpiration:
                    NodeExpiration ne = new NodeExpiration();
                    ((ICompactSerializable)ne).Deserialize(reader);
                    return (ExpirationHint)ne;
#endif
                case ExpirationHintType.Sql7CacheDependency:
                    Sql7CacheDependency s7cd = new Sql7CacheDependency();
                    ((ICompactSerializable)s7cd).Deserialize(reader);
                    return (ExpirationHint)s7cd;

                case ExpirationHintType.OleDbCacheDependency:
                    OleDbCacheDependency oledbDependency = new OleDbCacheDependency();
                    ((ICompactSerializable)oledbDependency).Deserialize(reader);
                    return (ExpirationHint)oledbDependency;

                case ExpirationHintType.SqlYukonCacheDependency:
                    SqlYukonCacheDependency sycd = new SqlYukonCacheDependency();
                    ((ICompactSerializable)sycd).Deserialize(reader);
                    return (ExpirationHint)sycd;

                case ExpirationHintType.OracleCacheDependency:
                    OracleCacheDependency orclcd = new OracleCacheDependency();
                    ((ICompactSerializable)orclcd).Deserialize(reader);
                    return (ExpirationHint)orclcd;


                case ExpirationHintType.IdleExpiration:
                    IdleExpiration ie = new IdleExpiration();
                    ((ICompactSerializable)ie).Deserialize(reader);
                    return (ExpirationHint)ie;
                    
                case ExpirationHintType.AggregateExpirationHint:
                    AggregateExpirationHint aeh = new AggregateExpirationHint();
                    ((ICompactSerializable)aeh).Deserialize(reader);
                    return (ExpirationHint)aeh;

                case ExpirationHintType.DBCacheDependency:
                    DBCacheDependency dbcd = new DBCacheDependency();
                    ((ICompactSerializable)dbcd).Deserialize(reader);
                    return (ExpirationHint)dbcd;                    

                case ExpirationHintType.ExtensibleDependency:
                    ExtensibleDependency ed = new ExtensibleDependency();
                    ed = (ExtensibleDependency)reader.ReadObject();
                    return (ExpirationHint)ed;

                case ExpirationHintType.NosDBCacheDependency:
                    NosDBCacheDependency nosDbd = new NosDBCacheDependency();
                    ((ICompactSerializable)nosDbd).Deserialize(reader);
                    return nosDbd;

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
            if (expHint._hintType == ExpirationHintType.ExtensibleDependency)
                writer.WriteObject(expHint);
            else
                ((ICompactSerializable)expHint).Serialize(writer);
            
            return;        
        }

        /// <summary>
        /// Override this method for hints that should be reinitialized when they are moved to another partition.
        /// e.g SqlYukondependency Hint must be reinitialized after state transfer so that its listeners are created on 
        /// new subcoordinator.
        /// </summary>
        /// <param name="context">CacheRuntimeContex for required contextual information.</param>
        /// <returns>True if reinitialization was successful.</returns>
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
    }
}
