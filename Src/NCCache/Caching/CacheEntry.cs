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
using System.Collections;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Locking;
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Collections;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Util;


namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class CacheEntry : CacheItemBase, IStorageEntry, IDisposable, ICloneable, ICompactSerializable, ICustomSerializable, IStreamItem
    {
        private static int PrimitiveDTSize = 200;

        /// <summary> A Provider name for re-synchronization of cache</summary>
        private string _resyncProviderName;

        private string _providerName;

        /// <summary> The falgs for the entry. </summary>
        private BitSet _bitset = new BitSet();

        /// <summary> The eviction hint to be associated with the object. </summary>
        protected EvictionHint _evh;

        private string _lockClientId = string.Empty;
        private int _lockThreadId = -1;

        /// <summary> The expiration hint to be associated with the object. </summary>
        protected ExpirationHint _exh;

        /// <summary> The group with which this item is related.</summary>
        private GroupInfo _grpInfo = null;

        /// <summary> The query information for this item.</summary>
        private Hashtable _queryInfo = null;

        /// <summary> List of keys which are dependiong on this item. </summary>
        private HashVector _keysDependingOnMe;

        /// <summary> Time at which this item was created. </summary>
        private DateTime _creationTime = DateTime.UtcNow;

        /// <summary> Time at which this item was Last modified. </summary>
        private DateTime _lastModifiedTime = DateTime.UtcNow;
		private CacheItemPriority _priorityValue;
        private long _size = -1;
        private LockMetaInfo lockMetaInfo = null;
        private UInt64 _version = 0;
        private string _type = null;
       
        private Notifications _notifications;

        public Notifications Notifications
        {
            get { return _notifications; }
            set { _notifications = value; }
            //set { _notifications = PoolingUtilities.SwapSimpleLeasables(_notifications, value); }
        }

        //PullBasedCallbacks
        private ArrayList _itemRemovedListener = new ArrayList(2);
        private ArrayList _itemUpdateListener = new ArrayList(2);
       
        public ArrayList ItemUpdateCallbackListener
        {
            get { return _itemUpdateListener; }
        }

        public ArrayList ItemRemoveCallbackListener
        {
            get { return _itemRemovedListener; }
        }

        public virtual EntryType Type
        {
            get { return EntryType.CacheItem; }
        }
		
		
        public static CacheEntry CreateCacheEntry(PoolManager poolManager)
        {
            CacheEntry entry = null;

            if (poolManager != null)
            {
                entry = poolManager.GetCacheEntryPool().Rent(true);
                entry._creationTime = entry._lastModifiedTime = DateTime.UtcNow; 
            }
            else
                entry = new CacheEntry();
            return entry;
        }

        public static CacheEntry[] CreateCacheEntries(PoolManager poolManager, int count)
        {
            CacheEntry[] entries = null;
            if (poolManager != null)
                entries = poolManager.GetCacheEntryPool().Rent(count, true);
            else
                entries = new CacheEntry[count];
           
            return entries;
        }


        public static CacheEntry CreateCacheEntry (PoolManager poolManager , object value, ExpirationHint expirationHint, EvictionHint evictionHint)
        {
            CacheEntry entry = null;

            if (poolManager != null)
            {
                entry = poolManager.GetCacheEntryPool().Rent(true);
                entry._creationTime = entry._lastModifiedTime = DateTime.UtcNow;
            }
            else
                entry = new CacheEntry();

            Construct(entry, value);

            entry.ExpirationHint = expirationHint;
            entry.EvictionHint = evictionHint;
            return entry;
        }
      

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (this)
            {
                if (_exh != null)
                {
                    ((IDisposable)_exh).Dispose();

                    if ((this.KeysIAmDependingOn == null || this.KeysIAmDependingOn.Length == 0) && (this.KeysDependingOnMe == null || this.KeysDependingOnMe.Count == 0))
                    { _exh = null; }
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
                this._type = value;
            }
        }

        public bool HasQueryInfo
        {
            get 
            {
                if(_queryInfo != null)
                {
                    if (_queryInfo["query-info"] != null || _queryInfo["tag-info"] != null || _queryInfo["named-tag-info"] != null)
                        return true;
                }

                return false;
            }
        }

      

        public string ResyncProviderName
        {
            get
            {
                return _resyncProviderName; 
            }
            set
            {
                if (ReferenceEquals(_resyncProviderName, value)) return;

                _resyncProviderName = value;
            }
        }

        /// <summary>
        /// Get or set provider name
        /// </summary>
        public string ProviderName
        {
            get
            {
                return _providerName;
            }
            set
            {
                if (ReferenceEquals(_providerName, value)) return;

                _providerName = value;
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
                if (ReferenceEquals(_evh, value)) return;

                lock (this)
                {
                    if (_evh != null)
                        MiscUtil.ReturnEvictionHintToPool(_evh, _evh.PoolManager);

                    _evh = value;

                    if (_evh != null && _evh._hintType == EvictionHintType.PriorityEvictionHint)
                    {
                        var pevh = _evh as PriorityEvictionHint;
                        if (pevh != null)
                        {
                            _priorityValue = pevh.Priority;
                        }
                    }
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
                if (ReferenceEquals(_exh, value)) return;

                lock (this)
                {
                    if (_exh != null)
                        MiscUtil.ReturnExpirationHintToPool(_exh, _exh.PoolManager);

                    _exh = value;
                }
            }
        }

        /// <summary> 
        /// The group with which this item is related.
        /// </summary>
        public GroupInfo GroupInfo
        {
            get { return _grpInfo; }
            set
            {
                if (ReferenceEquals(_grpInfo, value)) return;

                lock (this)
                { _grpInfo = value; }
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
                if (ReferenceEquals(_queryInfo, value)) return;

                lock (this)
                { _queryInfo = value; }
            }
        }

        /// <summary> List of Keys depending on this item. </summary>
        public HashVector KeysDependingOnMe
        {
            get { return _keysDependingOnMe; }
            set
            {
                if (ReferenceEquals(_keysDependingOnMe, value)) return;

                lock (this)
                { _keysDependingOnMe = value; }
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

       

        /// <summary>
        /// Gets the LockManager for this cache entry.
        /// </summary>
        public LockManager RWLockManager
        {
            get
            {
                if (lockMetaInfo==null || lockMetaInfo.LockManager == null)
                {
                    lock (this)
                    {
                        if(lockMetaInfo == null)
                            lockMetaInfo=new LockMetaInfo();
                 
                        if (lockMetaInfo.LockManager == null)
                            lockMetaInfo.LockManager = new LockManager();
                    }
                }
                return lockMetaInfo.LockManager;
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
                    if (Object.Equals(this.LockId, lockId) || (string.IsNullOrEmpty(this.LockId as string) && string.IsNullOrEmpty(lockId as string)))
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

        public int OldInMemorySize { get; set; }

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

        public bool Lock(LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            string clientId = operationContext.GetValueByField(OperationContextFieldName.ClientId) as string;
            int clientThreadId = operationContext.GetValueByField(OperationContextFieldName.ClientThreadId) != null ? (int)operationContext.GetValueByField(OperationContextFieldName.ClientThreadId) : -1;

            lock (this)
            {
                if (!this.IsLocked(ref lockId, ref lockDate))
                {
                    this.Flag.SetBit(BitSetConstants.LockedItem);
                    this.LockId = lockId;
                    this.LockDate = lockDate;
                    this.LockExpiration = lockExpiration;
                    if (this.LockExpiration != null) this.LockExpiration.Set();
                    if (!String.IsNullOrEmpty(clientId) && clientThreadId != -1)
                    {
                        this._lockClientId = clientId;
                        this._lockThreadId = clientThreadId;
                        object isRetryOperation = operationContext.GetValueByField(OperationContextFieldName.IsRetryOperation);
                        if (isRetryOperation != null && (Boolean)isRetryOperation)
                        {
                            if (!String.IsNullOrEmpty(clientId) && clientThreadId != -1 &&
                                clientId.Equals(this._lockClientId) && clientThreadId == this._lockThreadId)
                                return true;
                        }
                    }
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

        [CLSCompliant(false)]
		public UInt64 Version
        {
            get { return _version; }
            set { _version = value; } 
           
        }

        public void UpdateVersion(CacheEntry entry)
        {
            lock (this)
            {
                CopyVersion(entry);
                _version++;
            }
        }

        private void CopyVersion(CacheEntry entry)
        {
            lock (this)
            {
                this._version = entry.Version;
            }
        }
       
        public void UpdateLastModifiedTime(CacheEntry entry)
        {
            lock (this)
            {
                if (entry!=null)
                    this._creationTime = entry.CreationTime;
            }
        }

        
		[CLSCompliant(false)]
		public bool IsNewer(ulong version)
        {
            lock (this)
            {
                return this.Version > version;
            }
        }

        [CLSCompliant(false)]
		public bool CompareVersion(ulong version)
        {
            lock (this)
            {
                return this._version == version;
            }
        }

        internal KeyDependencyInfo[] KeysIAmDependingOnWithDependencyInfo
        {
            get
            {
                if (ExpirationHint != null)
                {
                   // IList<KeyDependency> keyDependencies = new List<KeyDependency>();
                    // We're working with hashtable so that same keys' values are overwritten
                    Hashtable keysIAmDependingOnWithKeyDependencyInfo = new Hashtable();
 
                    KeyDependencyInfo[] keyDependencyInfos = new KeyDependencyInfo[keysIAmDependingOnWithKeyDependencyInfo.Count];
                    keysIAmDependingOnWithKeyDependencyInfo.Values.CopyTo(keyDependencyInfos, 0);
                    return keyDependencyInfos;
                }
                return new KeyDependencyInfo[] { };
            }
        }

        public object[] KeysIAmDependingOn
        {
            get
            {
                return null;
            }
        }

        public Array UserData
        {
            get
            {
                Array userData = null;
                if (Value != null)
                {
                    UserBinaryObject ubObject = Value as UserBinaryObject;
                    if (ubObject != null)
                        userData = ubObject.Data;
                }
                return userData;
            }
        }

        public byte[] FullUserData
        {
            get
            {
                byte[] fullUserData = null;
                if (Value != null)
                {
                    UserBinaryObject ubObject = Value as UserBinaryObject;

                    if (ubObject != null)
                        fullUserData = ubObject.GetFullObject();
                }
                return fullUserData;
            }
        }

        /// <summary> 
        /// The actual object provided by the client application 
        /// </summary>
        public override object Value
        {
            set
            {
                lock (this)
                {
                    if (_bitset != null)
                    {
                        if (value is byte[] || value is UserBinaryObject)
                            _bitset.SetBit(BitSetConstants.Flattened);

                        else
                            _bitset.UnsetBit(BitSetConstants.Flattened);
                    }

                    object val = value;

                    if (value is Array && !(value is byte[]))
                    {
                        val = UserBinaryObject.CreateUserBinaryObject((Array)value, PoolManager);
                    }
                    base.Value = val;
                }
            }
        }

        private void AddItemUpdateCallback(CallbackInfo cbInfo, bool keepOldFilter = false)
        {
            if (_itemUpdateListener == null)
                _itemUpdateListener = new ArrayList(2);

            int indexOfCallback = _itemUpdateListener.IndexOf(cbInfo);
            if (indexOfCallback != -1)
            {
                //update the data filter only
                CallbackInfo oldCallback = _itemUpdateListener[indexOfCallback] as CallbackInfo;
                if(!keepOldFilter) oldCallback.DataFilter = cbInfo.DataFilter;
            }
            else
            {
                _itemUpdateListener.Add(cbInfo);
            }
        }
        private void AddItemRemoveCallback(CallbackInfo cbInfo, bool keepOldFilter = false)
        {
            if (_itemRemovedListener == null)
                _itemRemovedListener = new ArrayList(2);

            int indexOfCallback = _itemRemovedListener.IndexOf(cbInfo);
            if (indexOfCallback != -1)
            {
                //update the data filter only
                CallbackInfo oldCallback = _itemRemovedListener[indexOfCallback] as CallbackInfo;
                if (!keepOldFilter) oldCallback.DataFilter = cbInfo.DataFilter;
            }
            else
            {
                _itemRemovedListener.Add(cbInfo);
            }
        }

        internal void AddCallbackInfo(CallbackInfo updateCallback, CallbackInfo removeCallback, bool keepOldFilter = false)
        {
            lock (this)
            {
                bool isPullBasedCallback = false;
                //pullbasedNotification
                if (updateCallback != null && updateCallback.CallbackType == CallbackType.PullBasedCallback)
                {
                    AddItemUpdateCallback(updateCallback,keepOldFilter);
                    isPullBasedCallback = true;
                }
                if (removeCallback != null && removeCallback.CallbackType == CallbackType.PullBasedCallback)
                {
                    AddItemRemoveCallback(removeCallback,keepOldFilter);
                    isPullBasedCallback = true;
                }
                if (!isPullBasedCallback)
                {
                    if (_notifications == null)
                    {
                        _notifications = new Notifications();
                    }

                    if (updateCallback != null)
                        _notifications.AddItemUpdateCallback(updateCallback,keepOldFilter);
                    if (removeCallback != null)
                        _notifications.AddItemRemoveCallback(removeCallback,keepOldFilter);
                }
            }
        }

        private void RemoveItemUpdateCallback(CallbackInfo cbInfo)
        {
            if (_itemUpdateListener != null && _itemUpdateListener.Contains(cbInfo))
            {
                _itemUpdateListener.Remove(cbInfo);
            }
        }

        private void RemoveItemRemoveCallback(CallbackInfo cbInfo)
        {
            if (_itemRemovedListener != null && _itemRemovedListener.Contains(cbInfo))
            {
                _itemRemovedListener.Remove(cbInfo);
            }
        }

        internal void RemoveCallbackInfo(CallbackInfo updateCallback, CallbackInfo removeCallback)
        {
            lock (this)
            {
                if (updateCallback != null || removeCallback != null)
                {
                    if (_notifications != null)
                    {
                        if (updateCallback != null && updateCallback.CallbackType == CallbackType.PushBasedNotification)
                            _notifications.RemoveItemUpdateCallback(updateCallback);
                        if (removeCallback != null && removeCallback.CallbackType == CallbackType.PushBasedNotification)
                            _notifications.RemoveItemRemoveCallback(removeCallback);

                    }
                }
                //don't use else as currently both pull and push can be configured simultaneously
                //if pull-based notifications
                {
                    if (updateCallback != null)
                        RemoveItemUpdateCallback(updateCallback);
                    if (removeCallback != null)
                        RemoveItemRemoveCallback(removeCallback);
                }

            }
        }
        /// <summary>
        /// Flat status of the object.
        /// </summary>
        internal bool IsFlattened
        {
            get { return _bitset.IsBitSet(BitSetConstants.Flattened); }
        }

        internal bool IsCompressed
        {
            get { return _bitset.IsBitSet(BitSetConstants.Compressed); }
        }

        public BitSet Flag
        {
            get { return _bitset; }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance. The value is not copied.
        /// </summary>
        /// <returns>A new object that is a copy of this instance without value.</returns>
        public virtual CacheEntry CloneWithoutValue()
        {
            CacheEntry e = PoolManager== null ? new CacheEntry() : PoolManager.GetCacheEntryPool().Rent(true);
            e._creationTime = e._lastModifiedTime = DateTime.UtcNow;
            CloneWithoutValue(e);
            return e;
        }

   

        protected virtual void CloneWithoutValue(CacheEntry e)
        {
            lock (this)
            {
                e._exh = _exh;
                e._evh = _evh;
                if (this._grpInfo != null)
                    e._grpInfo = (GroupInfo)this._grpInfo.Clone();
                e._bitset = (BitSet)_bitset.Clone();

                if (_queryInfo != null)
                {
                    if (e._queryInfo != null)
                    {
                        foreach (DictionaryEntry queryInfo in _queryInfo)
                        {
                            e._queryInfo.Add(queryInfo.Key, queryInfo.Value);
                        }
                    }
                    else
                    {
                        e._queryInfo = new Hashtable(_queryInfo);
                    }
                }

                if (_keysDependingOnMe != null)
                    e._keysDependingOnMe = _keysDependingOnMe.Clone() as HashVector;

                if (this.LockMetaInfo != null)
                {
                    e.LockId = this.LockId;
                    e.LockDate = this.LockDate;
                    e.LockAge = this.LockAge;
                    e.LockExpiration = this.LockExpiration;
                    e.LockMetaInfo.LockManager = this.LockMetaInfo.LockManager;
                }
                e._size = _size;
                e._version = this._version;
                e._creationTime = this._creationTime;
                e._lastModifiedTime = this._lastModifiedTime;
                e._resyncProviderName = this._resyncProviderName;
                e._providerName = this._providerName;
                if (_notifications != null)
                {
                    e._notifications = _notifications.Clone() as Notifications;
                }
                e._type = _type;
 		        e._itemRemovedListener = _itemRemovedListener;
                e._itemUpdateListener = _itemUpdateListener;

            }
        }

        #region	/                 --- ICloneable ---           /

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public virtual object Clone()
        {
            CacheEntry e =   PoolManager == null ? new CacheEntry() : PoolManager.GetCacheEntryPool().Rent(true);
            e._creationTime = e._lastModifiedTime = DateTime.UtcNow;
            Construct(e, Value);

            e.ExpirationHint = _exh;
            e.EvictionHint = _evh;
            CloneInternal(e);
            return e;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public virtual object CloneWithoutPool()
        {
            CacheEntry e = new CacheEntry();
            e.Value = Value;
            e.ExpirationHint = _exh;
            e.EvictionHint = _evh;
            CloneInternal(e);
            return e;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        protected virtual void CloneInternal(CacheEntry entry)
        {
            lock (this)
            {
                if (this._grpInfo != null)
                    entry._grpInfo = (GroupInfo)this._grpInfo.Clone();
                entry._bitset = new BitSet() { Data = _bitset.Data };
                entry.Priority = Priority;

                if (_queryInfo != null)
                {
                    if (entry._queryInfo != null)
                    {
                        foreach (DictionaryEntry queryInfo in _queryInfo)
                        {
                            entry._queryInfo.Add(queryInfo.Key, queryInfo.Value);
                        }
                    }
                    else
                    {
                        entry._queryInfo = new Hashtable(_queryInfo);
                    }
                }
                
                if (_keysDependingOnMe != null)
                    entry._keysDependingOnMe = _keysDependingOnMe.Clone() as HashVector;
                if (this.LockMetaInfo != null)
                {
                    entry.LockId = this.LockId;
                    entry.LockDate = this.LockDate;
                    entry.LockAge = this.LockAge;
                    entry.LockExpiration = this.LockExpiration;
                    entry.LockMetaInfo.LockManager = this.LockMetaInfo.LockManager;
                }

                entry._size = _size;
                entry._version = this._version;
                entry._creationTime = this._creationTime;
                entry._lastModifiedTime = this._lastModifiedTime;
                entry._resyncProviderName = this._resyncProviderName;
                entry._providerName = this._providerName;
                entry._type = this._type;
                if (_notifications != null)
                {
                    entry._notifications = _notifications.Clone() as Notifications;
                }
                entry._itemRemovedListener = _itemRemovedListener;
                entry._itemUpdateListener = _itemUpdateListener;
                
            }
        }

        #endregion

#if SERVER 
        /// <summary>
        /// Creates a new object that is a copy of the current instance and that is routable as well.
        /// </summary>
        /// <returns>A routable copy of this instance.</returns>
        internal CacheEntry RoutableClone(Address localAddress)
        {
            lock (this)
            {
                if ( _exh != null)
                {
                    NodeExpiration expiry = null;
                    
                        //see if expiration hint itself is non-routable then we only need 
                        //a node expiration to handle both the syncDependency and expiration.
                        //otherwise we need a node expiration for syncDependency and also need to
                        //maintain the actual routable expiration hint.

                        expiry = null;
                        if (localAddress != null)
                        {
                          //  expiry = /*PoolManager.GetNodeExpirationPool().Rent(true)*/;
                            expiry.Node = localAddress;
                        }
                        
                        if (!_exh.IsRoutable)
                            {
                                CacheEntry e = PoolManager==null? new CacheEntry() : PoolManager.GetCacheEntryPool().Rent(true);
                                e.Value = Value;
                                e.ExpirationHint = expiry;
                                e.EvictionHint = _evh; 

                                if (_grpInfo != null)
                                    e._grpInfo = (GroupInfo)_grpInfo.Clone();
                                e._bitset = (BitSet)_bitset.Clone();
                                e._version = this._version;
                                e._creationTime = this._creationTime;
                                e._lastModifiedTime = this._lastModifiedTime;

                                if (this.LockMetaInfo != null)
                                    e.LockExpiration = this.LockExpiration;

                                e._resyncProviderName = this._resyncProviderName;
                                e.Priority = Priority;

                                if (_notifications != null)
                                {
                                    e._notifications = _notifications.Clone() as Notifications;
                                }
                                e._itemRemovedListener = _itemRemovedListener;
                                e._itemUpdateListener = _itemUpdateListener;
                                return e;
                            }
                       
                }
            }
            return (CacheEntry)Clone();
        }
#endif

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
                    UserBinaryObject ub = obj as UserBinaryObject;

                    byte[] data = ub.GetFullObject();
                    if (IsCompressed)
                    {
                        _bitset.UnsetBit(BitSetConstants.Compressed);
                    }
                    _size = data.Length;
                    obj = CompactBinaryFormatter.FromByteBuffer(data, cacheContext);
                }
            }
            return obj;
        }

        /// <summary>
        /// muds:
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
                        _bitset.UnsetBit(BitSetConstants.Flattened);
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
        public override void Deserialize(CompactReader reader)
        {
            lock (this)
            {
                base.Deserialize(reader);
                _bitset = new BitSet() { Data = reader.ReadByte()};
                _evh = EvictionHint.ReadEvcHint(reader, PoolManager);
                _exh = ExpirationHint.ReadExpHint(reader, PoolManager);
                _grpInfo = GroupInfo.ReadGrpInfo(reader);
                
                _queryInfo = (Hashtable)reader.ReadObject();
                _keysDependingOnMe = (HashVector)reader.ReadObject();
                _size = reader.ReadInt64();
                lockMetaInfo = reader.ReadObject() as LockMetaInfo;
                _version = reader.ReadUInt64();
                _creationTime = reader.ReadDateTime();
                _lastModifiedTime = reader.ReadDateTime();
                ResyncProviderName = reader.ReadObject() as string;                
				_priorityValue = (CacheItemPriority)reader.ReadInt32();
                ProviderName = reader.ReadObject() as string;

                var objectType = reader.ReadObject() as string;
                _type = objectType;

                reader.ReadInt32(0);
                _itemUpdateListener = reader.ReadObject(ArrayList.Synchronized( new ArrayList[2])) as ArrayList;
                _itemRemovedListener = reader.ReadObject(ArrayList.Synchronized(new ArrayList[2])) as ArrayList;
                OldInMemorySize = reader.ReadInt32(0);
                _notifications = reader.ReadObject(null) as Notifications;
               


            }
        }

        public override void Serialize(CompactWriter writer)
        {
            lock (this)
            {
                base.Serialize(writer);
                writer.Write(_bitset.Data);
                EvictionHint.WriteEvcHint(writer, _evh);
                ExpirationHint.WriteExpHint(writer, _exh);
                GroupInfo.WriteGrpInfo(writer, _grpInfo);

                writer.WriteObject(_queryInfo);
                writer.WriteObject(_keysDependingOnMe);
                writer.Write(_size);
                writer.WriteObject(lockMetaInfo);
                writer.Write(_version);
                writer.Write(_creationTime);
                writer.Write(_lastModifiedTime);
                writer.WriteObject(ResyncProviderName);
                writer.Write((int)_priorityValue);
                writer.WriteObject(ProviderName);
                writer.WriteObject(this._type);
		        writer.Write((int)491);
                writer.WriteObject(_itemUpdateListener);
                writer.WriteObject(_itemRemovedListener);
                writer.Write(this.OldInMemorySize);
                writer.WriteObject(_notifications);
             
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

        public virtual int Size
        {
            get { return (int)(PrimitiveDTSize + DataSize); }
        }


        public virtual int InMemorySize
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
                    else
                    {
                        if (Value is byte[])
                            size = ((byte[])Value).Length;
                        else
                        {
                            Type type = Value.GetType();
                            if (type is ValueType)
                                size = System.Runtime.InteropServices.Marshal.SizeOf(type);
                            else
                                size = IntPtr.Size;
                        }
                    }
                }
                if (_notifications != null)
                {
                    size += _notifications.InMemorySize;
                }
                if (_grpInfo != null)
                {
                    size += _grpInfo.InMemorySize;
                }
                if (KeysDependingOnMe != null)
                {
                    size += (KeysDependingOnMe.BucketCount * MemoryUtil.NetHashtableOverHead);
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
                }

                return size;
            }

            set
            {
                _size = value;
            }
        }

        public void MergeCallbackListeners(CacheEntry entryToMerge)
        {
            if (entryToMerge != null)
            {
                if (entryToMerge._itemRemovedListener != null && entryToMerge._itemRemovedListener.Count > 0)
                {
                    if (_itemRemovedListener == null)
                        _itemRemovedListener = new ArrayList(2);

                    foreach (CallbackInfo cbInfo in entryToMerge._itemRemovedListener)
                    {
                        AddItemRemoveCallback(cbInfo);
                    }
                }

                if (entryToMerge._itemUpdateListener != null && entryToMerge._itemUpdateListener.Count > 0)
                {
                    if (_itemUpdateListener == null)
                        _itemUpdateListener = new ArrayList(2);

                    foreach (CallbackInfo cbInfo in entryToMerge._itemUpdateListener)
                    {
                        AddItemUpdateCallback(cbInfo);
                    }
                }
            }

        }

      
        #region IStreamItem Members

        public VirtualArray Read(int offset, int length)
        {
            VirtualArray vBuffer = null;
            UserBinaryObject ubObject = (UserBinaryObject)(Value);

            if (ubObject != null)
                vBuffer = ubObject.Read(offset, length);
            return vBuffer;
        }

        public void Write(VirtualArray vBuffer, int srcOffset, int dstOffset, int length)
        {
            UserBinaryObject ubObject = (UserBinaryObject)(Value);

            if (ubObject != null)
                ubObject.Write(vBuffer, srcOffset, dstOffset, length);
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
                }

                return size;
            }
            set
            {
                throw new NotSupportedException("Set length is not supported.");
            }
        }

        #endregion

        #region ILeasable

        public override void ResetLeasable()
        {
            base.ResetLeasable();

            _resyncProviderName = null;
            _providerName = null;

            if (_bitset == null)
                _bitset = new BitSet();

            _bitset.Data = 0;
            _evh = null;
            _lockClientId = string.Empty;
            _exh = null;
            _grpInfo = null;
            _keysDependingOnMe = null;
            _creationTime = DateTime.UtcNow;
            _lastModifiedTime = _creationTime;
            _type = null;
            _notifications = null;
            _itemRemovedListener.Clear();
            _itemUpdateListener.Clear();
            _queryInfo = null;
            lockMetaInfo = null;
            _lockThreadId = -1;
            _priorityValue = default(CacheItemPriority);
            _size = -1;
            _version = 0;
            IsStored = false;
          
        }

        public override void ReturnLeasableToPool()
        {
            MiscUtil.ReturnEvictionHintToPool(_evh, _evh?.PoolManager);
            MiscUtil.ReturnExpirationHintToPool(_exh, _exh?.PoolManager);
           
            if (Value is UserBinaryObject userBinaryObjectValue)
                MiscUtil.ReturnUserBinaryObjectToPool(userBinaryObjectValue, userBinaryObjectValue?.PoolManager);
  
        }

        #endregion

       
        public bool IsStored { get; internal set; }

        #region - [Deep Cloning] -

        public virtual CacheEntry DeepClone(PoolManager poolManager, bool suppressCloning = false)
        {
            var clonedEntry = poolManager.GetCacheEntryPool()?.Rent(true) ?? new CacheEntry();

            lock (this)
            {
                object value = Value;

                if (value is UserBinaryObject valueAsUserBinaryObject)
                {
                    clonedEntry.Value = valueAsUserBinaryObject.DeepClone(poolManager);
                    return DeepCloneInternal(poolManager, clonedEntry);
                }
                clonedEntry.Value = value; 
                return DeepCloneInternal(poolManager, clonedEntry);
            }
        }

        public virtual CacheEntry DeepCloneWithoutCloningValue(PoolManager poolManager, bool suppressCloning = false)
        {
            var clonedEntry = poolManager.GetCacheEntryPool()?.Rent(true) ?? new CacheEntry();

            lock (this)
            {
                clonedEntry.Value = Value;
                return DeepCloneInternal(poolManager, clonedEntry);
            }
        }

        public virtual CacheEntry DeepCloneWithoutValue(PoolManager poolManager, bool suppressCloning = false)
        {
            var clonedEntry = poolManager.GetCacheEntryPool()?.Rent(true) ?? new CacheEntry();

            lock (this)
            {
                clonedEntry.Value = null;
                return DeepCloneInternal(poolManager, clonedEntry);
            }
        }

        private CacheEntry DeepCloneInternal(PoolManager poolManager, CacheEntry clonedEntry)
        {
            clonedEntry.ExpirationHint = ExpirationHint?.DeepClone(poolManager);
            clonedEntry.EvictionHint = EvictionHint?.DeepClone(poolManager);
            clonedEntry.GroupInfo = GroupInfo?.DeepClone(null);
            clonedEntry._bitset.Data = Flag != null ? Flag.Data:(byte) 0;
            clonedEntry.Priority = Priority;
            
            clonedEntry.QueryInfo = QueryInfo.DeepClone();
            clonedEntry.KeysDependingOnMe = KeysDependingOnMe.DeepClone();

            if (LockMetaInfo != null)
            {
                clonedEntry.LockId = LockId;
                clonedEntry.LockDate = LockDate;
                clonedEntry.LockAge = LockAge;
                clonedEntry.LockExpiration = LockExpiration?.DeepClone(poolManager);
                clonedEntry.LockMetaInfo.LockManager = LockMetaInfo.LockManager;
            }

            clonedEntry._size = _size;
            clonedEntry._version = _version;
            clonedEntry._creationTime = _creationTime;
            clonedEntry._lastModifiedTime = _lastModifiedTime;
            clonedEntry._resyncProviderName = _resyncProviderName;
            clonedEntry._providerName = _providerName;     
            clonedEntry._type = _type;
            clonedEntry.Notifications = Notifications?.DeepClone(null);

        

            if (_itemRemovedListener != null)
            {
                if (clonedEntry._itemRemovedListener == null)
                {
                    clonedEntry._itemRemovedListener = new ArrayList(_itemRemovedListener);
                }
                else
                {
                    clonedEntry._itemRemovedListener.Clear();
                    clonedEntry._itemRemovedListener.AddRange(_itemRemovedListener);
                }
            }
            if (_itemUpdateListener != null)
            {
                if (clonedEntry._itemUpdateListener == null)
                {
                    clonedEntry._itemUpdateListener = new ArrayList(_itemUpdateListener);
                }
                else
                {
                    clonedEntry._itemUpdateListener.Clear();
                    clonedEntry._itemUpdateListener.AddRange(_itemUpdateListener);
                }
            }
            return clonedEntry;
        }

        #endregion
    }
}
