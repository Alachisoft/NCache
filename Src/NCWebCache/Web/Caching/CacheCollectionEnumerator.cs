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

using System.Collections;

namespace Alachisoft.NCache.Web.Caching
{
    internal class CacheCollectionEnumerator : IEnumerator
    {
        private Hashtable _caches;
        private IDictionaryEnumerator _ce;

        internal CacheCollectionEnumerator(Hashtable caches)
        {
            this._caches = caches.Clone() as Hashtable;
            this._ce = _caches.GetEnumerator();
        }

        #region IEnumerator Members

        object IEnumerator.Current
        {
            get { return ((DictionaryEntry)_ce.Current).Value as Cache; }
        }

        bool IEnumerator.MoveNext()
        {
            return _ce.MoveNext();
        }

        void IEnumerator.Reset()
        {
            _ce.Reset();
        }

        #endregion
    }
}
