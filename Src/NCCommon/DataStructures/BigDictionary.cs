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
using System.Collections.Generic;
using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class BigDictionary : IDictionary
    {
        Dictionary<int, Hashtable> _dictionaries = new Dictionary<int, Hashtable>();
        object _syncLock = new object();
        int _itemCount;
        int _version;
        const long range = 2147483648;
        int MAX_HASH_CODE = (int)Math.Ceiling((double)(range) / (double)10000);

        private int GetHashBucketId(object key)
        {
            int hashCode = AppUtil.GetHashCode(key as string);
            int hashBucketId = hashCode / MAX_HASH_CODE;

            if (hashBucketId < 0)
                hashBucketId = hashBucketId * -1;

            return hashBucketId;
        }

        private Hashtable AddInnerHashtable(int hashCode)
        {
            Hashtable innerTable = null;
            lock (_syncLock)
            {
                if (!_dictionaries.ContainsKey(hashCode))
                {
                    innerTable = new Hashtable();
                    _dictionaries.Add(hashCode, innerTable);
                }
                else
                {
                    innerTable = _dictionaries[hashCode];
                }
               
            }
            return innerTable;
        }

        public virtual void Add(object key, object value)
        {
            int hashCode = GetHashBucketId(key);
            Hashtable innerHashtable = null;
            if (!_dictionaries.ContainsKey(hashCode))
            {
                innerHashtable = AddInnerHashtable(hashCode);
            }
            else
            {
                innerHashtable = _dictionaries[hashCode];
            }

            if (innerHashtable != null)
            {
                innerHashtable.Add(key, value);
                _itemCount++;
                _version++;
            }

        }

        public virtual void Clear()
        {
            _itemCount = 0;
            _dictionaries.Clear();
        }

        public virtual bool Contains(object key)
        {
            int hashCode = GetHashBucketId(key);
            Hashtable innerHashtable = null;
            
            if(_dictionaries.ContainsKey(hashCode))
                innerHashtable = _dictionaries[hashCode];
            
            if (innerHashtable != null)
            {
                return innerHashtable.Contains(key);
            }
            return false;
        }

        public virtual IDictionaryEnumerator GetEnumerator()
        {
            return new BigDictionary.CollectionAndEnumerator(this);
        }

        public virtual bool IsFixedSize
        {
            get { return false; }
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public virtual ICollection Keys
        {
            get { return new KeyCollection(this); }
        }

        public virtual void Remove(object key)
        {
            int hashCode = GetHashBucketId(key);
            Hashtable innerHashtable = null;
            if (_dictionaries.ContainsKey(hashCode))
            {
                innerHashtable = _dictionaries[hashCode];
            }

            if (innerHashtable != null && innerHashtable.Contains(key))
            {
                innerHashtable.Remove(key);
                _itemCount--;
                _version++;

                if (innerHashtable.Count == 0)
                    _dictionaries.Remove(hashCode);
            }

           
        }

        public virtual ICollection Values
        {
            get { return new BigDictionary.ValueCollection(this); }
        }

        public virtual object this[object key]
        {
            get
            {
                int hashCode = GetHashBucketId(key);
                Hashtable innerHashtable = null;
                if (_dictionaries.ContainsKey(hashCode))
                {
                    innerHashtable = _dictionaries[hashCode];
                }
                
                if (innerHashtable != null && innerHashtable.ContainsKey(key))
                {
                    return innerHashtable[key];
                }
                return null;
            }
            set
            {
                int hashCode = GetHashBucketId(key);
                Hashtable innerHashtable = null;
                if (!_dictionaries.ContainsKey(hashCode))
                {
                    innerHashtable = AddInnerHashtable(hashCode);
                }
                else
                {
                    innerHashtable = _dictionaries[hashCode];
                }

                if (innerHashtable != null)
                {
                    if(!innerHashtable.Contains(key)) _itemCount++;
                    innerHashtable[key]= value;
                    _version++;
                }
            }
        }

        public virtual void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public virtual int Count
        {
            get { return _itemCount; }
        }

        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        public virtual object SyncRoot
        {
            get { return _syncLock; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new BigDictionary.CollectionAndEnumerator(this);
        }

        public static BigDictionary Synchronized(BigDictionary bDic)
        {
            if (bDic == null)
            {
                throw new ArgumentNullException("BigDictionary");
            }
            return new SyncBigDictionary(bDic);
        }



        #region/                        ---- Inner Classes ----                                                 /

        private class SyncBigDictionary : BigDictionary
        {
            // Fields
            protected BigDictionary _bDic;

            // Methods
            internal SyncBigDictionary(BigDictionary bDic)
            {
                this._bDic = bDic;
            }

            public override void Add(object key, object value)
            {
                lock (this._bDic.SyncRoot)
                {
                    this._bDic.Add(key, value);
                }
            }

            public override void Clear()
            {
                lock (this._bDic.SyncRoot)
                {
                    this._bDic.Clear();
                }
            }

            public override bool Contains(object key)
            {
                return this._bDic.Contains(key);
            }

            public override void CopyTo(Array array, int arrayIndex)
            {
                lock (this._bDic.SyncRoot)
                {
                    this._bDic.CopyTo(array, arrayIndex);
                }
            }

            public override IDictionaryEnumerator GetEnumerator()
            {
                return this._bDic.GetEnumerator();
            }

            public override void Remove(object key)
            {
                lock (this._bDic.SyncRoot)
                {
                    this._bDic.Remove(key);
                }
            }
            // Properties
            public override int Count
            {
                get
                {
                    return this._bDic.Count;
                }
            }

            public override bool IsFixedSize
            {
                get
                {
                    return this._bDic.IsFixedSize;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return this._bDic.IsReadOnly;
                }
            }

            public override bool IsSynchronized
            {
                get
                {
                    return true;
                }
            }

            public override object this[object key]
            {
                get
                {
                    return this._bDic[key];
                }
                set
                {
                    lock (this._bDic.SyncRoot)
                    {
                        this._bDic[key] = value;
                    }
                }
            }

            public override ICollection Keys
            {
                get
                {
                    lock (this._bDic.SyncRoot)
                    {
                        return this._bDic.Keys;
                    }
                }
            }

            public override object SyncRoot
            {
                get
                {
                    return this._bDic.SyncRoot;
                }
            }

            public override ICollection Values
            {
                get
                {
                    lock (this._bDic.SyncRoot)
                    {
                        return this._bDic.Values;
                    }
                }
            }
        }

        class CollectionAndEnumerator : IDictionaryEnumerator, ICollection
        {
            BigDictionary _bigDictionary;
            protected DictionaryEntry _current;
            IDictionaryEnumerator _currentInnerDictionaryEnumerator;
            Dictionary<int,Hashtable>.Enumerator _bigDictionaryEnumerator;
            int _version;
            int _count;
            object _syncRoot = new object();

            public CollectionAndEnumerator(BigDictionary bigDictionary)
            {
                _bigDictionary = bigDictionary;
                _version = bigDictionary._version;
                _count = bigDictionary._itemCount;
                _bigDictionaryEnumerator = _bigDictionary._dictionaries.GetEnumerator();
            }

            public DictionaryEntry Entry
            {
                get { return _current; }
            }

            public object Key
            {
                get { 
                     return _current.Key;
                }
            }

            public object Value
            {
                get { return _current.Value; }
            }

            public virtual object Current
            {
                get { return Value ; }
            }

            public bool MoveNext()
            {
                bool itemFound = false;
               
                if (_currentInnerDictionaryEnumerator != null && _currentInnerDictionaryEnumerator.MoveNext())
                {
                    _current = _currentInnerDictionaryEnumerator.Entry;
                    itemFound = true;
                }
                else
                {
                    _currentInnerDictionaryEnumerator = null;
                    while (_bigDictionaryEnumerator.MoveNext())
                    {
                        Hashtable innerTable = _bigDictionaryEnumerator.Current.Value;
                      
                        if (innerTable != null)
                        {
                            _currentInnerDictionaryEnumerator = innerTable.GetEnumerator();

                            if (_currentInnerDictionaryEnumerator != null && _currentInnerDictionaryEnumerator.MoveNext())
                            {
                                _current = _currentInnerDictionaryEnumerator.Entry;
                                itemFound = true;
                                break;
                            }
                        }
                    }
                }

                if (_version != _bigDictionary._version)
                    throw new Exception("Eunmerator has been modified");

                return itemFound;
            }

            public void Reset()
            {
                _currentInnerDictionaryEnumerator = null;
                _version = _bigDictionary._version;
                _bigDictionaryEnumerator = _bigDictionary._dictionaries.GetEnumerator();
            }

            public void CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { return _count; }
            }

            public bool IsSynchronized
            {
                get { return false; }
            }

            public object SyncRoot
            {
                get { return _syncRoot; }
            }

            public IEnumerator GetEnumerator()
            {
                return this;
            }
        }


        class KeyCollection : CollectionAndEnumerator
        {
       
            public KeyCollection(BigDictionary bigDictionary):base(bigDictionary)
            {
            }

            public new object Value
            {
                get { return _current.Key; }
            }

            public override object Current
            {
                get { return _current.Key; }
            }

        }

        class ValueCollection : CollectionAndEnumerator
        {

            public ValueCollection(BigDictionary bigDictionary)
                : base(bigDictionary)
            {
            }

            public override object Current
            {
                get { return _current.Value; }
            }

        }


        #endregion
    }

  


}
