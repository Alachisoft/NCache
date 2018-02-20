// Copyright (c) 2018 Alachisoft
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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies
{
    [Serializable]
    internal class CacheInsResultWithEntry : ICompactSerializable
    {
        private CacheInsResult _result = CacheInsResult.Success;
        private CacheEntry _entry = null;

        /// <summary>
        /// The result of the Insert Operation.
        /// </summary>
        public CacheInsResult Result
        {
            get { return _result; }
            set { _result = value; }
        }

        /// <summary>
        /// Old CacheEntry in case result is SuccessOverwrite.
        /// </summary>
        public CacheEntry Entry
        {
            get { return _entry; }
            set { _entry = value; }
        }

        public CacheInsResultWithEntry()
        {}

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="result"></param>
        public CacheInsResultWithEntry(CacheEntry entry, CacheInsResult result)
        {
            _entry = entry;
            _result = result;
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            _entry = (CacheEntry)reader.ReadObject();
            _result = (CacheInsResult)reader.ReadObject();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.WriteObject(_entry);
            writer.WriteObject(_result);
        }

        #endregion
    }
}