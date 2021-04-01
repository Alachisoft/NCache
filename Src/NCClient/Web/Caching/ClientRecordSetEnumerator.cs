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
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Client
{
    internal class ClientRecordSetEnumerator : IClientRecordSetEnumerator
    {
        Queue _keyList;
        int _index = 0;
        ClusteredArrayList _chunkKeys;
        const int CHUNKSIZE = 100;
        HashVector _dataWithValue;
        ClusteredArray<object> _currentValue;
        Cache _cache;
        bool _getData;
        bool _hasKeys;

        internal ClientRecordSetEnumerator(IList keyList, Cache cache, bool getData)
        {
            if (keyList.Count > 0)
            {
                _hasKeys = true;
            }
            _keyList = new Queue(keyList);
            _getData = getData;
            _cache = cache;
            _dataWithValue = GetDataWithKeys;
        }

        public bool MoveNext()
        {
            if (_dataWithValue != null && _index < _chunkKeys.Count)
            {
                object key = _chunkKeys[_index++];
                if (_dataWithValue.ContainsKey(key))
                {
                    if (_getData)
                    {
                        _currentValue = new ClusteredArray<object>(2);
                        _currentValue[0] = key;
                        _currentValue[1] = _dataWithValue[key];
                        return true;
                    }
                    else
                    {
                        _currentValue = new ClusteredArray<object>(1);
                        _currentValue[0] = key;
                        return true;
                    }
                }
                else
                {
                    return HasMoreData;
                }

            }
            return HasMoreData;
        }

        public void Dispose()
        {
            _keyList = null;
            _chunkKeys = null;
            _currentValue = null;
            _dataWithValue = null;
        }

        public ClusteredArray<object> Current
        {
            get { return _currentValue; }
        }

        private bool HasMoreData
        {
            get
            {
                _dataWithValue = GetDataWithKeys;
                if (_dataWithValue == null || _dataWithValue.Count <= 0) return false;
                object key = _chunkKeys[_index++];
                if (_dataWithValue.ContainsKey(key))
                {
                    if (_getData)
                    {
                        _currentValue = new ClusteredArray<object>(2);
                        _currentValue[0] = key;
                        _currentValue[1] = _dataWithValue[key];
                        return true;
                    }
                    else
                    {
                        _currentValue = new ClusteredArray<object>(1);
                        _currentValue[0] = key;
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        private ClusteredArrayList GetKeys
        {
            get
            {
                if (_keyList != null && _keyList.Count > 0)
                {
                    int count = CHUNKSIZE < _keyList.Count ? CHUNKSIZE : _keyList.Count;
                    ClusteredArrayList keys = new ClusteredArrayList();
                    for (int i = 0; i < count; i++)
                    {
                        keys.Add(_keyList.Dequeue());
                    }
                    return keys;
                }
                return null;
            }
        }

        private HashVector GetDataWithKeys
        {
            get
            {
                int prevOverload = 0;
                _index = 0;
                ClusteredArrayList keys = GetKeys;
                if (keys != null && _cache != null)
                {
                    if (_getData)
                    {
                        prevOverload = TargetMethodAttribute.MethodOverload;
                        TargetMethodAttribute.MethodOverload = 0;
                        IDictionary retObjs = (IDictionary)_cache.GetBulk<object>(GetStringKeys(keys));
                        TargetMethodAttribute.MethodOverload = prevOverload;

                        if (retObjs != null)
                        {
                            HashVector data = new HashVector();
                            _chunkKeys = new ClusteredArrayList();
                            foreach (object item in retObjs)
                            {
                                DictionaryEntry entry = (DictionaryEntry)item;
                                if (entry.Value != null && !(entry.Value is Exception))
                                {
                                    string key = entry.Key as string;
                                    _chunkKeys.Add(key);
                                    data.Add(key, entry.Value);
                                }
                            }
                            return data;
                        }
                        else
                        {
                            return GetDataWithKeys;
                        }
                    }
                    else
                    {
                        HashVector data = new HashVector();
                        _chunkKeys = new ClusteredArrayList();
                        for (int i = 0; i < keys.Count; i++)
                        {
                            data.Add(keys[i], null);
                            _chunkKeys.Add(keys[i]);
                        }
                        return data;
                    }
                }
                return null;
            }
        }


        private string[] GetStringKeys(ClusteredArrayList list)
        {
            string[] keys = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                keys[i] = list[i] as string;
            }
            return keys;
        }

        public int FieldCount
        {
            get
            {
                if (_hasKeys)
                {
                    return _getData ? 2 : 1;
                }
                return 0;
            }
        }
    }
}