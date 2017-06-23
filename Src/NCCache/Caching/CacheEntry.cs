// Copyright (c) 2017 Alachisoft
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
using System.Collections;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.Util;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Caching.Queries;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class CacheEntry : CacheItemBase, IDisposable, ICloneable, ICompactSerializable, ICustomSerializable, ISizable
    {
        private static int PrimitiveDTSize = 200;
        private static byte FLATTENED = 0x01;

        /// <summary> The falgs for the entry. </summary>
        private BitSet _bitset = new BitSet();

        /// <summary> The eviction hint to be associated with the object. </summary>
        private EvictionHint _evh;

        /// <summary> The expiration hint to be associated with the object. </summary>
        private ExpirationHint _exh;

        /// <summary> The query information for this item.</summary>
        private Hashtable _queryInfo = null;

        /// <summary> Time at which this item was created. </summary>
        private DateTime _creationTime = new DateTime();

        /// <summary> Time at which this item was Last modified. </summary>
        private DateTime _lastModifiedTime = new DateTime();
		private CacheItemPriority _priorityValue;
        private long _size = -1;

        private LockMetaInfo lockMetaInfo = null;

        private IndexInformation _indexInfo;

        public IndexInformation IndexInfo
        {
            get { return _indexInfo; }
            set { _indexInfo = value; }
        }
       
        private string _type = null;

        public CacheEntry() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="val">the object to be added to the cache</param>
        /// <param name="expiryHint">expiration hint for the object</param>
        /// <param name="evictionHint">eviction hint for the object</param>
        public CacheEntry(object val, ExpirationHint expiryHint, EvictionHint evictionHint)
            : base(val)
        {
            _exh = expiryHint;
            _evh = evictionHint;
            
            _bitset.SetBit(FLATTENED);

            _creationTime = System.DateTime.Now;
            _lastModifiedTime = System.DateTime.Now;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            lock (this)
            {
                if (_exh != null)
                {
                    ((IDisposable)_exh).Dispose();
                    _exh = null;
                }
                _evh = null;
            }
        }

        #endregion

        public string ObjectType
        {
            get
            {
                return this._type;
            }
            set
            {
                this._type = Common.Util.StringPool.PoolString(value);
            }
        }

        public bool HasQueryInfo
        {
            get 
            {
                if(_queryInfo != null)
                {
                    if (_queryInfo["query-info"] != null)
                        return true;
                }

                return false;
                
            }
            
        }


        /// <summary>
        /// Eviction hint for the object.
        /// </summary>
        public EvictionHint EvictionHint
        {
            get { return _evh; }
            set
            {
                lock (this)
                {
                    _evh = value; 
                }
            }
        }

		public CacheItemPriority Priority
        {
            get { return _priorityValue; }
            set { _priorityValue = value; }
        }

        /// <summary>
        /// Expiration hint for the object.
        /// </summary>
        public ExpirationHint ExpirationHint
        {
            get { return _exh; }
            set
            {
                lock (this)
                { _exh = value; }
            }
        }

        /// <summary> 
        /// The query information for this item.
        /// </summary>
        public Hashtable QueryInfo
        {
            get { return _queryInfo; }
            set
            {
                lock (this)
                { _queryInfo = value; }
            }
        }

        public LockMetaInfo LockMetaInfo 
        {
            get { return lockMetaInfo; }
        }

        public object LockId
        {
            get 
            {
                if (lockMetaInfo != null)
                    return lockMetaInfo.LockId;
                else
                    return null;
            }
            set
            {
                lock (this)
                {
                    if (lockMetaInfo == null)
                    {
                        lockMetaInfo = new LockMetaInfo();
                    }

                    lockMetaInfo.LockId = value;
                }
            }
        }

        public TimeSpan LockAge
        {
            get {
                if (lockMetaInfo != null)
                    return lockMetaInfo.LockAge;
                else
                    return new TimeSpan();
            }
            set
            {
                lock (this)
                {
                    if (lockMetaInfo == null)
                    {
                        lockMetaInfo = new LockMetaInfo();
                    }

                    lockMetaInfo.LockAge = value;
                }
            }
        }

        public DateTime LockDate
        {
            get
            {
                if (lockMetaInfo != null)
                    return lockMetaInfo.LockDate;
                else
                    return new DateTime();
            }
            set
            {
                lock (this)
                {
                    if (lockMetaInfo == null)
                    {
                        lockMetaInfo = new LockMetaInfo();
                    }

                    lockMetaInfo.LockDate = value;                
                }
            }
        }

        public DateTime CreationTime
        {
            get { return _creationTime; }
            set
            {
                lock (this)
                { _creationTime = value; }
            }
        }

        public DateTime LastModifiedTime
        {
            get { return _lastModifiedTime; }
            set
            {
                lock (this)
                { _lastModifiedTime = value; }
            }
        }

        public LockAccessType LockAccessType
        {
            get 
            {
                if (lockMetaInfo != null)
                    return lockMetaInfo.LockAccessType;
                else
                    return new LockAccessType();
            }
            set
            {
                lock (this)
                {
                    if (lockMetaInfo == null)
                    {
                        lockMetaInfo = new LockMetaInfo();
                    }

                    lockMetaInfo.LockAccessType = value;      
                }
            }
        }

       
        public LockExpiration LockExpiration
        {
            get 
            {
                if (lockMetaInfo != null)
                    return lockMetaInfo.LockExpiration;
                else return null;
            }
            set 
            {
                if (lockMetaInfo == null)
                    lockMetaInfo = new LockMetaInfo();
                
                lockMetaInfo.LockExpiration = value; 
            }
        }

        public bool IsLocked(ref object lockId, ref DateTime lockDate)
        {
            lock (this)
            {
                if (this.Flag.IsAnyBitSet(BitSetConstants.LockedItem))
                {
                    if (this.LockExpiration == null || !this.LockExpiration.HasExpired())
                    {
                        
                        lockId = this.LockId;
                        lockDate = this.LockDate;
                        return true;
                    }
                    else
                    {
                        ReleaseLock();
                        return false;
                    }
                }
                return false;
            }
        }

        public bool CompareLock(object lockId)
        {
            lock (this)
            {
                if (this.Flag.IsAnyBitSet(BitSetConstants.LockedItem))
                {
                    if (lockId == null) return false;
                    if (Object.Equals(this.LockId, lockId))
                        return true;
                }
                return false;                
            }
        }

        /// <summary>
        /// Determines whether an item is locked or not. 
        /// </summary>
        /// <param name="lockId"></param>
        /// <returns></returns>
        public bool IsItemLocked()
        {
            lock (this)
            {
                if (this.LockExpiration == null || !this.LockExpiration.HasExpired())
                {
                    return this.Flag.IsAnyBitSet(BitSetConstants.LockedItem);
                }
                return false;
            }
        }

        public void ReleaseLock()
        {
            lock (this)
            {
                this.LockId = null;
                this.LockDate = new DateTime();
                this.Flag.UnsetBit(BitSetConstants.LockedItem);
            }
        }

        public void CopyLock(object lockId, DateTime lockDate, LockExpiration lockExpiration)
        {
            lock (this)
            {
                if(lockId != null)
                    this.Flag.SetBit(BitSetConstants.LockedItem);
                else
                    this.Flag.UnsetBit(BitSetConstants.LockedItem);

                this.LockId = lockId;
                this.LockDate = lockDate;
                this.LockExpiration = lockExpiration;
            }
        }

        public bool Lock(LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate)
        {
            lock (this)
            {
                if (!this.IsLocked(ref lockId, ref lockDate))
                {
                    this.Flag.SetBit(BitSetConstants.LockedItem);
                    this.LockId = lockId;
                    this.LockDate = lockDate;
                    this.LockExpiration = lockExpiration;
                    if (this.LockExpiration != null) this.LockExpiration.Set();
                    return true;
                }
                else
                {
                    lockId = this.LockId;
                    lockDate = this.LockDate;
                }

                return false;
            }
        }

        public void UpdateLastModifiedTime(CacheEntry entry)
        {
            lock (this)
            {
                this._creationTime = entry.CreationTime;
            }
        }
        public Array UserData
        {
            get
            {
                Array userData = null;
                if (Value != null)
                {
                    UserBinaryObject ubObject = null;
                    if (Value is CallbackEntry)
                    {
                        if (((CallbackEntry)Value).Value != null)
                            ubObject = ((CallbackEntry)Value).Value as UserBinaryObject;
                    }
                    else
                        ubObject = Value as UserBinaryObject;

                    userData = ubObject.Data;
                }
                return userData;
            }
        }
        /// <summary> 
        /// The actual object provided by the client application 
        /// </summary>
        public override object Value
        {
            get { return base.Value; }
            set
            {
                lock (this)
                {
                    if (_bitset != null)
                    {
                        if (value is byte[] || value is UserBinaryObject)
                            _bitset.SetBit(FLATTENED);
                        else
                            _bitset.UnsetBit(FLATTENED);
                    }

                    object val1 = value;
                    if (value is Array && !(val1 is byte[]))
                    {
                        val1 = UserBinaryObject.CreateUserBinaryObject(((Array)value));
                    }

                    if (base.Value is CallbackEntry && val1 is UserBinaryObject)
                    {
                        CallbackEntry cbEntry = base.Value as CallbackEntry;

                        cbEntry.Value = val1;
                    }
                    else
                        base.Value = val1;
                }
            }
        }

        internal void AddCallbackInfo(CallbackInfo updateCallback, CallbackInfo removeCallback)
        {
            lock (this)
            {
                CallbackEntry cbEntry;

                if (Value is CallbackEntry)
                {
                    cbEntry = Value as CallbackEntry;
                }
                else
                {
                    cbEntry = new CallbackEntry();
                    cbEntry.Value = Value;
                    cbEntry.Flag = Flag;
                    Value = cbEntry;
                }

                if (updateCallback != null)
                    cbEntry.AddItemUpdateCallback(updateCallback);
                if (removeCallback != null)
                    cbEntry.AddItemRemoveCallback(removeCallback);
            }
        }

        internal void RemoveCallbackInfo(CallbackInfo updateCallback, CallbackInfo removeCallback)
        {
            lock (this)
            {
                if (updateCallback != null || removeCallback != null)
                {
                    CallbackEntry cbEntry = null;
                    if (Value is CallbackEntry)
                    {
                        cbEntry = Value as CallbackEntry;

                        if (updateCallback != null)
                            cbEntry.RemoveItemUpdateCallback(updateCallback);
                        if (removeCallback != null)
                            cbEntry.RemoveItemRemoveCallback(removeCallback);

                    }
                }
            }
        }
        /// <summary>
        /// Flat status of the object.
        /// </summary>
        internal bool IsFlattened
        {
            get { return _bitset.IsBitSet(FLATTENED); }
        }
        
        
        public BitSet Flag
        {
            set
            {
                lock (this)
                { _bitset = value; }
            }
            get { return _bitset; }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance. The value is not copied.
        /// </summary>
        /// <returns>A new object that is a copy of this instance without value.</returns>
        public CacheEntry CloneWithoutValue()
        { 
            CacheEntry e = new CacheEntry();
            lock (this)
            {
                e._exh = _exh;
                e._evh = _evh;
                e._bitset = (BitSet)_bitset.Clone();

                e._queryInfo = _queryInfo;
               
                if (this.LockMetaInfo != null)
                {
                    e.LockId = this.LockId;
                    e.LockDate = this.LockDate;
                    e.LockAge = this.LockAge;
                    e.LockExpiration = this.LockExpiration;
                    e.LockMetaInfo.LockManager = this.LockMetaInfo.LockManager;
                }
                e._size = _size;
                e._creationTime = this._creationTime;
                e._lastModifiedTime = this._lastModifiedTime;

                if (this.Value is CallbackEntry)
                {
                    CallbackEntry cbEntry = (CallbackEntry)this.Value;
                    cbEntry = cbEntry.Clone() as CallbackEntry;
                    cbEntry.Value = null;
                    e.Value = cbEntry;
                }
                e._type = _type;
            }

            return e;

        }

        #region	/                 --- ICloneable ---           /

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            CacheEntry e = new CacheEntry(Value, _exh, _evh);

            lock (this)
            {
                e._bitset = (BitSet)_bitset.Clone();
                e.Priority = Priority;

                e._queryInfo = _queryInfo;
                if (this.LockMetaInfo != null)
                {
                    e.LockId = this.LockId;
                    e.LockDate = this.LockDate;
                    e.LockAge = this.LockAge;
                    e.LockExpiration = this.LockExpiration;
                    e.LockMetaInfo.LockManager = this.LockMetaInfo.LockManager;
                }
               
                e._size = _size;
                e._creationTime = this._creationTime;
                e._lastModifiedTime = this._lastModifiedTime;

                e._type = this._type;
                e.IndexInfo = this.IndexInfo;
            }
            return e;
        }


        #endregion

        /// <summary>
        /// Creates a new object that is a copy of the current instance and that is routable as well.
        /// </summary>
        /// <returns>A routable copy of this instance.</returns>
        internal CacheEntry FlattenedClone(string cacheContext)
        {
            CacheEntry e = (CacheEntry)Clone();
            e.FlattenObject(cacheContext);
            return e;
        }

        /// <summary>
        /// Falttens, i.e. serializes the object contained in value.
        /// </summary>
        internal object FlattenObject(string cacheContext)
        {
            return Value;
        }

        /// <summary>
        /// DeFalttens, i.e. deserializes the object contained in value.
        /// </summary>
        internal object DeflattenObject(string cacheContext)
        {
            lock (this)
            {
                if (IsFlattened)
                {
                    // Setting the Value resets the Flat flag!
                    Value = CompactBinaryFormatter.FromByteBuffer(UserData as byte[], cacheContext);
                }
            }
            return Value;
        }

        /// <summary>
        /// Gets the deflatted value of the of the object in the value. It does not
        /// deflatten the actual object.
        /// </summary>
        internal object DeflattedValue(string cacheContext)
        {
            object obj = Value;

            //There is possibility that two threads simultaneously do deserialization; therefore
            //we must deserialize the entry in synchronized fashion.
            lock (this)
            {
                if (IsFlattened)
                {
                    // Setting the Value resets the Flat flag!
                    UserBinaryObject ub = null;
                    CallbackEntry cbEntry = obj as CallbackEntry;
                    if (cbEntry != null)
                    {
                        ub = cbEntry.Value as UserBinaryObject;
                    }
                    else
                        ub = obj as UserBinaryObject;

                    byte[] data = ub.GetFullObject();

                    _size = data.Length;
                    obj = CompactBinaryFormatter.FromByteBuffer(data, cacheContext);
                    if (cbEntry != null)
                    {
                        cbEntry.Value = obj;
                        obj = cbEntry;
                    }
                }
            }
            return obj;
        }

        /// <summary>
        /// in case of local inproc caches, first time the object is 
        /// accessed we keep the deserialized user object. This way 
        /// on the upcoming get requests, we save the cost of deserialization
        /// every time.
        /// </summary>
        /// <param name="cacheContext"></param>
        internal void KeepDeflattedValue(string cacheContext)
        {
            lock (this)
            {
                try
                {
                    if (IsFlattened)
                    {
                        Value = DeflattedValue(cacheContext);
                        _bitset.UnsetBit(FLATTENED);
                    }
                }
                catch (Exception e)
                {
                }
            }
        }

        public override string ToString()
        {
            return "CacheEntry[" + Value.ToString() + "]";
        }

        #region	/                 --- ICompactSerializable ---           /
        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            lock (this)
            {
                Value = reader.ReadObject();
                _bitset = new BitSet(reader.ReadByte());
                _evh = EvictionHint.ReadEvcHint(reader);
                _exh = ExpirationHint.ReadExpHint(reader);
                _queryInfo = (Hashtable)reader.ReadObject();
                _size = reader.ReadInt64();
                lockMetaInfo = reader.ReadObject() as LockMetaInfo;
                _creationTime = reader.ReadDateTime();
                _lastModifiedTime = reader.ReadDateTime();
                _priorityValue = (CacheItemPriority)reader.ReadInt32();
                _type = reader.ReadObject() as string;
            }
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            lock (this)
            {
                writer.WriteObject(Value);
                writer.Write(_bitset.Data);
                EvictionHint.WriteEvcHint(writer, _evh);
                ExpirationHint.WriteExpHint(writer, _exh);
                writer.WriteObject(_queryInfo);
                writer.Write(_size);
                writer.WriteObject(lockMetaInfo);
                writer.Write(_creationTime);
                writer.Write(_lastModifiedTime);
                writer.Write((int)_priorityValue);
                writer.WriteObject(this._type);
            }
        }

        #endregion


        #region ICustomSerializable Members

        public void DeserializeLocal(System.IO.BinaryReader reader)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SerializeLocal(System.IO.BinaryWriter writer)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region ISizable Members

        public int Size
        {
            get { return (int)(PrimitiveDTSize + DataSize); }
        }


        public int InMemorySize
        {
            get
            { return (int)(PrimitiveDTSize + InMemoryDataSize); }
        }
    
        #endregion


        public long InMemoryDataSize 
        {
            get
            {
                int size = 0;
                if (Value != null)
                {
                    if (Value is UserBinaryObject)
                    {
                        size = ((UserBinaryObject)Value).InMemorySize;
                    }
                    else if (Value is CallbackEntry)
                    {
                        CallbackEntry entry = (CallbackEntry)Value;
                        if (entry.Value != null)
                        {
                            if (entry.Value is UserBinaryObject)
                                size = ((UserBinaryObject)(entry.Value)).InMemorySize;
                            else if (entry.Value is byte[])
                                size = ((byte[])entry.Value).Length;
                            else
                            {
                                Type type = entry.Value.GetType();
                                if (type is ValueType)
                                    size = System.Runtime.InteropServices.Marshal.SizeOf(type);
                                else
                                    size = IntPtr.Size;
                            }
                        }
                        size += entry.InMemorySize;
                    }
                }             

                return size;
            }         
        }

        public long DataSize
        {
            get
            {
                if (_size > -1) return _size;
                int size = 0;
                if (Value != null)
                {
                    if (Value is UserBinaryObject)
                    {
                        size = ((UserBinaryObject)Value).Size;
                    }
                    else if (Value is CallbackEntry)
                    {
                        CallbackEntry entry = (CallbackEntry)Value;
                        if (entry.Value != null && entry.Value is UserBinaryObject)
                            size = ((UserBinaryObject)(entry.Value)).Size;
                    }
                }

                return size;
            }

            set
            {
                _size = value;
            }
        }

        public int Length
        {
            get
            {
                int size = 0;
                if (Value != null)
                {
                    if (Value is UserBinaryObject)
                    {
                        size = ((UserBinaryObject)Value).Length;
                    }
                    else if (Value is CallbackEntry)
                    {
                        if (((CallbackEntry)Value).Value != null)
                            size = ((UserBinaryObject)((CallbackEntry)Value).Value).Length;
                    }
                }

                return size;
            }
            set
            {
                throw new NotSupportedException("Set length is not supported.");
            }
        }

    }
}
