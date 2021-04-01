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
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Enumeration
{
    internal class SnapshotEnumerationProvider : IEnumerationProvider
    {
        /// <summary> The en-wrapped instance of cache. </summary>
        private CacheBase _cache;

        /// <summary>
        /// The en-wrapped instance of enumerator.
        /// </summary>
        private EnumerationPointer _pointer;

        /// <summary>
        /// Current list of keys in the cache when the enumerator is taken on cache
        /// </summary>
        private Array _snapshot;        

        /// <summary>
        /// Sequence ID of the chunk being send for this particular enumerator.
        /// Holds -1 if all the chunks have been sent.
        /// </summary>
        private int _chunkId = 0;


        #region IEnumerationProvider Members

        public void Initialize(EnumerationPointer pointer, LocalCache cache)
        {
            _cache = cache;
            _pointer = pointer;
            _snapshot = CacheSnapshotPool.Instance.GetSnaphot(pointer.Id, cache);
        }

        public EnumerationDataChunk GetNextChunk(Alachisoft.NCache.Common.DataStructures.EnumerationPointer pointer)
        {
            int count = 0;            
            EnumerationDataChunk chunk = new EnumerationDataChunk();
            chunk.Data = new List<string>();
            int currentIndex = pointer.ChunkId;
            while (currentIndex < _snapshot.Length - 1 && count < ServiceConfiguration.EnumeratorChunkSize)
            {
                currentIndex++;
                chunk.Data.Add(_snapshot.GetValue(currentIndex).ToString());
                count++;
            }

            if (currentIndex == _snapshot.Length - 1)
                _pointer.ChunkId = -1;
            else
                _pointer.ChunkId = currentIndex; //Set the chunkId to strating index of the next chunk to fetch.
            
            chunk.Pointer = _pointer;

            return chunk;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            CacheSnapshotPool.Instance.DiposeSnapshot(_pointer.Id, _cache); //Disposes the snapshot from pool for this particular pointer
            _cache = null;
            _pointer = null;
            _snapshot = null;
        }

        #endregion
    }
}
