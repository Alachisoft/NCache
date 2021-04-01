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

using Alachisoft.NCache.Common.DataStructures;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Client
{
    internal class WebCacheEnumerator<T> : IDictionaryEnumerator, IDisposable
    {
        private Cache _cache;
        private List<EnumerationDataChunk> _currentChunks;
        private IEnumerator<string> _currentChunkEnumerator;
        private object _currentValue;

        private string _serializationContext;
        private DictionaryEntry _de;

        internal WebCacheEnumerator(string serializationContext, Cache cache)
        {
            _cache = cache;
            _serializationContext = serializationContext;
            _de = new DictionaryEntry();
            Initialize();
        }

        public void Initialize()
        {
            List<EnumerationPointer> pointers = new List<EnumerationPointer>();
            pointers.Add(new EnumerationPointer());
            _currentChunks = _cache.GetNextChunk(pointers);
            List<string> data = new List<string>();
            for (int i = 0; i < _currentChunks.Count; i++)
            {
                if (_currentChunks[i] != null && _currentChunks[i].Data != null)
                {
                    data.AddRange(_currentChunks[i].Data);
                }
            }
            _currentChunkEnumerator = data.GetEnumerator();
        }

        #region	/                 --- IEnumerator ---           /

        /// <summary>
        /// Set the enumerator to its initial position. which is before the first element in the collection
        /// </summary>
        public void Reset()
        {
            if (_currentChunks != null && _currentChunks.Count > 0)
            {
                for (int i = 0; i < _currentChunks.Count; i++)
                {
                    _currentChunks[i].Pointer.Reset();
                }

            }

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
                    if (_currentChunks != null && !IsLastChunk(_currentChunks))
                    {
                        _currentChunks = _cache.GetNextChunk(GetPointerList(_currentChunks));

                        List<string> data = new List<string>();
                        for (int i = 0; i < _currentChunks.Count; i++)
                        {
                            if (_currentChunks[i] != null && _currentChunks[i].Data != null)
                            {
                                data.AddRange(_currentChunks[i].Data);
                            }
                        }
                        if (data != null && data.Count > 0)
                        {
                            _currentChunkEnumerator = data.GetEnumerator();
                            result = _currentChunkEnumerator.MoveNext();
                        }
                    }
                    else if (_currentChunks != null && _currentChunks.Count > 0)
                    {
                        List<EnumerationPointer> pointers = GetPointerList(_currentChunks);
                        if (pointers.Count > 0)
                            _cache.GetNextChunk(pointers); //just an empty call to dispose enumerator for this particular list of pointer
                    }
                }
            }

            return result;
        }

        private List<EnumerationPointer> GetPointerList(List<EnumerationDataChunk> chunks)
        {
            List<EnumerationPointer> pointers = new List<EnumerationPointer>();

            for (int i = 0; i < chunks.Count; i++)
            {
                if (!chunks[i].IsLastChunk)
                    pointers.Add(chunks[i].Pointer);
            }
            return pointers;
        }

        private bool IsLastChunk(List<EnumerationDataChunk> chunks)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                if (!chunks[i].IsLastChunk)
                {
                    return false;
                }
            }

            return true;
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
                try
                {
                    return _cache.Get<T>(Key as string);
                }
                catch (Exception ex)
                {
                    if (ex.Message.StartsWith("Connection with server lost"))
                    {
                        try
                        {
                            return _cache.Get<T>(Key as string);
                        }
                        catch (Exception inner)
                        {
                            throw inner;
                        }
                    }
                    throw ex;
                }
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_cache != null && _currentChunks != null)
            {
                List<EnumerationPointer> pointerlist = GetPointerList(_currentChunks);
                if (pointerlist.Count > 0)
                {
                    _cache.GetNextChunk(pointerlist); //just an empty call to dispose enumerator for this particular pointer
                }
            }

            _cache = null;
            _serializationContext = null;
        }

        #endregion
    }
}
