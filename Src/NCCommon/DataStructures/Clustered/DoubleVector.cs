using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    [DebuggerTypeProxy(typeof(VectorDebugView))]
    public class DoubleVector<k, v> : IDictionary<k, v>
    {
        private HashVector<k, v> _keyToValue;
        private HashVector<v, k> _valueToKey;

        public DoubleVector()
        {
            _keyToValue = new HashVector<k, v>(3);
            _valueToKey = new HashVector<v, k>(3);
        }

        public DoubleVector(IDictionary<k, v> dictionary)
            : this()
        {
            foreach (var kvp in dictionary)
            {
                _keyToValue.Add(kvp.Key, kvp.Value);
                _valueToKey.Add(kvp.Value, kvp.Key);
            }
        }

        public void Add(k key, v value)
        {
            _keyToValue.Add(key, value);
            _valueToKey.Add(value, key);
        }

        public bool ContainsKey(k key)
        {
            return _keyToValue.ContainsKey(key);
        }

        public bool ContainsValue(v value)
        {
            return _valueToKey.ContainsKey(value);
        }

        public ICollection<k> Keys
        {
            get { return _keyToValue.Keys; }
        }

        public bool Remove(k key)
        {
            v value;
            if ((_keyToValue.TryGetValue(key, out value)))
            {
                _keyToValue.Remove(key);
                _valueToKey.Remove(value);
                return true;
            }
            return false;
        }

        public bool Remove(v value)
        {
            k key;
            if (_valueToKey.TryGetValue(value, out key))
            {
                _valueToKey.Remove(value);
                _keyToValue.Remove(key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(k key, out v value)
        {
            return _keyToValue.TryGetValue(key, out value);
        }

        public bool TryGetKey(v value, out k key)
        {
            return _valueToKey.TryGetValue(value, out key);
        }

        public ICollection<v> Values
        {
            get { return _keyToValue.Values; }
        }

        public v this[k key]
        {
            get
            {
                return _keyToValue[key];
            }
            set
            {
                v oldValue;
                if (_keyToValue.TryGetValue(key, out oldValue))
                    _valueToKey.Remove(oldValue);
                _keyToValue[key] = value;
                _valueToKey[value] = key;
            }
        }

        public k this[v val]
        {
            get { return _valueToKey[val]; }
            set
            {
                k oldKey;
                if (_valueToKey.TryGetValue(val, out oldKey)) _keyToValue.Remove(oldKey);
                _valueToKey[val] = value;
                _keyToValue[value] = val;
            }
        }

        public void Add(KeyValuePair<k, v> item)
        {
            _keyToValue.Add(item.Key, item.Value);
            _valueToKey.Add(item.Value, item.Key);
        }

        public void Clear()
        {
            _keyToValue.Clear();
            _valueToKey.Clear();
        }

        public bool Contains(KeyValuePair<k, v> item)
        {
            return _keyToValue.ContainsKey(item.Key) && _valueToKey.ContainsKey(item.Value);
        }

        public void CopyTo(KeyValuePair<k, v>[] array, int arrayIndex)
        {
            if (arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException("The specified array index is out of the length of the array. ");
            foreach (var kvp in _keyToValue)
            {
                if (arrayIndex == array.Length)
                    return;
                array[arrayIndex++] = kvp;
            }
        }

        public int Count
        {
            get { return _keyToValue.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<k, v> item)
        {
            _keyToValue.Remove(item.Key);
            return _valueToKey.Remove(item.Value);
        }

        public IEnumerator<KeyValuePair<k, v>> GetEnumerator()
        {
            return _keyToValue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _keyToValue.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<v, k>> GetValueEnumerator()
        {
            return _valueToKey.GetEnumerator();
        }
    }
}