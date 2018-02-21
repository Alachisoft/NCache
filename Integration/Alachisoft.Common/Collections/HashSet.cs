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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.Common.Collections
{
    public class HashSet<T> : ICollection<T>
    {
        Dictionary<T, object> items;

        public HashSet()
        {
            items = new Dictionary<T, object>();
        }

        #region ICollection<T> Members

        public void Add(T item)
        {
            items[item] = null;
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(T item)
        {
            bool contains = items.ContainsKey(item);
            return contains;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            items.Keys.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            bool removed = items.Remove(item);
            return removed;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return items.Keys.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
