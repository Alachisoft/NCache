using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.DataStructures
{
    public struct KeyValuePair : ICompactSerializable
    {
        public object Key;
        public object Value;

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            Key = reader.ReadObject();
            Value = reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(Key);
            writer.WriteObject(Value);
        }
    }

    /// <summary>
    /// This is a memory optimized implementation of a IDictionary which performs better in terms of memory as compare to a Hashtable.
    /// WARNING! This key value store performs good if number of items are less than ~20. Therefore to store large amount of data use Hashtable
    /// </summary>
    public class KeyValueStore : ISizableDictionary, ICompactSerializable
    {
        private KeyValuePair[] _pairs;
        private int _count;

        public KeyValueStore(int size)
        {
            _pairs = new KeyValuePair[size];
        }

        public void Add(object key, object value)
        {
            InsertInternal(key, value, true);
        }

        private void InsertInternal(object key, object value, bool checkKeyDuplication)
        {
            int firstEmptyIndex = -1;
            bool exists = false;
            int found = 0;
            for (int i = 0; i < _pairs.Length && found < _count + 1; i++)
            {
                if (_pairs[i].Key != null)
                {
                    found++;
                    if (_pairs[i].Key.Equals(key))
                    {
                        if (checkKeyDuplication)
                            throw new ArgumentException("Key already exists");
                        else
                        {
                            exists = true;
                            firstEmptyIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    if (firstEmptyIndex == -1) firstEmptyIndex = i;
                }
            }

            if (firstEmptyIndex == -1)
            {
                KeyValuePair[] newIndex = new KeyValuePair[_pairs.Length + 3];

                Array.Copy(_pairs, newIndex, _pairs.Length);
                firstEmptyIndex = _pairs.Length;
                _pairs = newIndex;
            }

            _pairs[firstEmptyIndex].Key = key;
            _pairs[firstEmptyIndex].Value = value;

            if (!exists) _count++;
        }

        public object Get(object key)
        {
            int found = 0;
            for (int i = 0; i < _pairs.Length && found < _count; i++)
            {
                if (_pairs[i].Key != null)
                {
                    found++;
                    if (_pairs[i].Key.Equals(key))
                        return _pairs[i].Value;
                }
            }

            return null;
        }


        public void Clear()
        {
            _pairs = new KeyValuePair[3];
            _count = 0;
        }

        public bool Contains(object key)
        {
            int found = 0;
            for (int i = 0; i < _pairs.Length && found < Count; i++)
            {
                if (_pairs[i].Key != null)
                {
                    found++;
                    if (_pairs[i].Key.Equals(key))
                        return true;
                }
            }

            return false;
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new KeyValueEnumerator(this._pairs);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection Keys
        {
            get { return new KeyValueCollection(this, true); }
        }

        public void Remove(object key)
        {
            int found = 0;
            for (int i = 0; i < _pairs.Length && found < _count; i++)
            {
                if (_pairs[i].Key != null)
                {
                    found++;
                    if (_pairs[i].Key.Equals(key))
                    {
                        _pairs[i].Key = _pairs[i].Value = null;
                        _count--;
                        break;
                    }
                }
            }
        }

        public ICollection Values
        {
            get { return new KeyValueCollection(this, false); }
        }

        public object this[object key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                InsertInternal(key, value, false);
            }
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0)
                throw new ArgumentException("Index can not be a negative number");

            if (array.Length + 1 < index || (array.Length - index) > _count)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < _pairs.Length; i++)
            {
                if (_pairs[i].Key != null)
                {
                    array.SetValue(new DictionaryEntry(_pairs[i].Key, _pairs[i].Value), index);
                    index++;
                }
            }

        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new KeyValueEnumerator(this._pairs);
        }

        public long IndexInMemorySize
        {
            get { return (_pairs.Length * 18) + 32; }
        }

        #region     /                               --- Inner Classes ------                    /

        class KeyValueEnumerator : IDictionaryEnumerator
        {
            private KeyValuePair[] _enumerable;
            private int _index = -1;
            private bool _expired;

            public KeyValueEnumerator(KeyValuePair[] enumerable)
            {
                _enumerable = enumerable;
            }

            public DictionaryEntry Entry
            {
                get
                {
                    ValidateEnumerator();
                    return new DictionaryEntry(_enumerable[_index].Key, _enumerable[_index].Value);
                }
            }

            public object Key
            {
                get
                {
                    ValidateEnumerator();
                    return _enumerable[_index].Key;
                }
            }

            private void ValidateEnumerator()
            {
                if (_expired)
                    throw new InvalidOperationException("Enumerator is already over");

                if (_index == -1)
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            }

            public object Value
            {
                get
                {
                    ValidateEnumerator();
                    return _enumerable[_index].Value;
                }
            }

            public object Current
            {
                get { return Entry; }
            }

            public bool MoveNext()
            {
                if (_enumerable.Length == 0)
                {
                    _expired = true;
                    return false;
                }

                int index = _index;
                while (true)
                {
                    index++;
                    if (index < _enumerable.Length)
                    {
                        if (_enumerable[index].Key != null)
                        {
                            _index = index;
                            return true;
                        }
                    }
                    else
                        break;
                }

                _expired = true;
                return false;
            }

            public void Reset()
            {
                _index = -1;
                _expired = false;
            }
        }

        class KeyValueCollection : ICollection
        {
            private KeyValuePair[] _pairs;
            private bool _isKeyCollection;
            private int _count;

            public KeyValueCollection(KeyValueStore store, bool keyCollection)
            {
                _pairs = store._pairs;
                _count = store._count;
                _isKeyCollection = keyCollection;
            }

            public void CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException("array");

                if (index < 0)
                    throw new ArgumentException("Index can not be a negative number");

                if (array.Length + 1 < index || (array.Length - index) > _count)
                    throw new IndexOutOfRangeException();

                for (int i = 0; i < _pairs.Length; i++)
                {
                    if (_pairs[i].Key != null)
                    {
                        array.SetValue(_isKeyCollection ? _pairs[i].Key : _pairs[i].Value, index);
                        index++;
                    }
                }
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
                get { return this; }
            }

            public IEnumerator GetEnumerator()
            {
                return new KeyValueCollectionEnumerator(_pairs, _isKeyCollection);
            }
        }

        class KeyValueCollectionEnumerator : IEnumerator
        {
            private KeyValuePair[] _enumerable;
            private int _index = -1;
            private bool _expired;
            private bool _keyEnumerator;

            public KeyValueCollectionEnumerator(KeyValuePair[] enumerable, bool isKeyEnumerator)
            {
                _enumerable = enumerable;
                _keyEnumerator = isKeyEnumerator;
            }

            private void ValidateEnumerator()
            {
                if (_expired)
                    throw new InvalidOperationException("Enumerator is already over");

                if (_index == -1)
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            }

            public object Current
            {
                get
                {
                    ValidateEnumerator();
                    return _keyEnumerator ? _enumerable[_index].Key : _enumerable[_index].Value;
                }
            }

            public bool MoveNext()
            {
                if (_enumerable.Length == 0)
                {
                    _expired = true;
                    return false;
                }

                int index = _index;
                while (true)
                {
                    index++;
                    if (index < _enumerable.Length)
                    {
                        if (_enumerable[index].Key != null)
                        {
                            _index = index;
                            return true;
                        }
                    }
                    else
                        break;
                }

                _expired = true;
                return false;
            }

            public void Reset()
            {
                _index = -1;
                _expired = false;
            }
        }


        #endregion




        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _count = reader.ReadInt32();
            _pairs = reader.ReadObject() as KeyValuePair[];
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_count);
            writer.WriteObject(_pairs);
        }
    }
}
