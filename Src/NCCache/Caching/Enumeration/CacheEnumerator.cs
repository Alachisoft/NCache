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
using Alachisoft.NCache.Common.DataStructures;
using System.Collections;

namespace Alachisoft.NCache.Caching.Enumeration
{
    internal class CacheEnumerator : IDictionaryEnumerator
    {
        private Cache _cache;
        private EnumerationDataChunk _currentChunk;
        private IEnumerator<string> _currentChunkEnumerator;
        private CompressedValueEntry _currentValue;

        private string _group;
        private string _subGroup;
        private DictionaryEntry _de;

        internal CacheEnumerator(string serializationContext, string group, string subGroup, Cache cache)
        {
            _cache = cache;
            _group = group;
            _subGroup = subGroup;

            Initialize(_group, _subGroup);
        }

        public void Initialize(string group, string subGroup)
        {
            EnumerationPointer pointer = null;

            if (!String.IsNullOrEmpty(group))
                pointer = new GroupEnumerationPointer(group, subGroup);
            else
                pointer = new EnumerationPointer();

            _currentChunk = _cache.GetNextChunk(pointer, new OperationContext());

            if (_currentChunk != null && _currentChunk.Data != null)
            {
                List<string> data = _currentChunk.Data;
                _currentChunkEnumerator = data.GetEnumerator();
            }
        }

        #region	/                 --- IEnumerator ---           /

        /// <summary>
        /// Set the enumerator to its initial position. which is before the first element in the collection
        /// </summary>
        public void Reset()
        {
            if (_currentChunk != null)
                _currentChunk.Pointer.Reset();

            if (_currentChunkEnumerator != null)
                _currentChunkEnumerator.Reset();
        }

        /// <summary>
        /// Advance the enumerator to the next element of the collection 
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            bool result = false;

            if (_currentChunkEnumerator != null)
            {
                result = _currentChunkEnumerator.MoveNext();

                if (!result)
                {
                    if (_currentChunk != null && !_currentChunk.IsLastChunk)
                    {
                        _currentChunk = _cache.GetNextChunk(_currentChunk.Pointer, new OperationContext());

                        if (_currentChunk != null && _currentChunk.Data != null)
                        {
                            _currentChunkEnumerator = _currentChunk.Data.GetEnumerator();
                            result = _currentChunkEnumerator.MoveNext();
                        }
                    }
                }
            }

            if (result)
                _currentValue = _cache.Get(Key);

            return result;
        }

        /// <summary>
        /// Gets the current element in the collection
        /// </summary>
        public object Current
        {
            get
            {
                return Entry;
            }
        }

        #endregion

        #region	/                 --- IDictionaryEnumerator ---           /

        /// <summary>
        /// Gets the key and value of the current dictionary entry.
        /// </summary>
        public DictionaryEntry Entry
        {
            get
            {
                _de.Key = Key;
                _de.Value = Value;

                return _de;
            }
        }

        /// <summary>
        /// Gets the key of the current dictionary entry 
        /// </summary>
        public object Key
        {
            get
            {
                object key = null;

                if (_currentChunkEnumerator != null)
                    key = _currentChunkEnumerator.Current;

                return key;
            }
        }

        /// <summary>
        /// Gets the value of the current dictionary entry
        /// </summary>
        public object Value
        {
            get
            {
                return _currentValue;
            }
        }

        #endregion
    }
}
