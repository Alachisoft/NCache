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
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Caching.Topologies.Local;

namespace Alachisoft.NCache.Caching.Enumeration
{
    internal class EnumerationIndex
    {
        private LocalCache _cache;
        private Dictionary<EnumerationPointer, IEnumerationProvider> _index;

        internal EnumerationIndex(LocalCache cache)
        {
            _cache = cache;
        }

        internal EnumerationDataChunk GetNextChunk(EnumerationPointer pointer)
        {
            EnumerationDataChunk nextChunk = null;
            IEnumerationProvider provider = GetProvider(pointer);

            if (pointer.isDisposable && provider != null)
            {
                provider.Dispose();
                if (_index.ContainsKey(pointer))
                {
                    _index.Remove(pointer);
                }
                nextChunk = new EnumerationDataChunk();
                nextChunk.Pointer = pointer;
            }
            else if (provider != null)
            {
                nextChunk = provider.GetNextChunk(pointer);
                //Dispose the provider if this is the last chunk for it
                if(nextChunk.IsLastChunk)
                {
                    provider.Dispose();
                    if (_index.ContainsKey(pointer))
                    {
                        _index.Remove(pointer);
                    }
                }
            }

            return nextChunk;
        }

        private IEnumerationProvider GetProvider(EnumerationPointer pointer)
        {
            if (_index == null)
                _index = new Dictionary<EnumerationPointer, IEnumerationProvider>();

            IEnumerationProvider provider = null;

            if (_index.ContainsKey(pointer))
            {
                provider = _index[pointer];
            }
            else if(pointer.ChunkId == -1 && !pointer.IsSocketServerDispose && !pointer.isDisposable)
            {
                provider = new SnapshotEnumerationProvider();
                provider.Initialize(pointer, _cache);
                _index.Add(pointer, provider);
            }

            return provider;
        }

        internal bool Contains(EnumerationPointer pointer)
        {
            if (_index != null)
            {
                return _index.ContainsKey(pointer);
            }
            else
                return false;
        }


    }
}
