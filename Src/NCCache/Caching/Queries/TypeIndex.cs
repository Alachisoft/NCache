// Copyright (c) 2015 Alachisoft
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
using System;

namespace Alachisoft.NCache.Caching.Queries
{
    public class TypeIndex : IQueryIndex
    {
        private Hashtable _indexTable;
        private bool _indexForAll;

        public TypeIndex(string type, bool indexForAll)
        {
            _indexForAll = indexForAll;
            _indexTable = new Hashtable();
            _indexTable[type.ToLower()] = new Hashtable();
        }

        public void AddToIndex(object key, object value)
        {
            if (_indexTable.Contains(value.GetType().ToString().ToLower()))
            {
                Hashtable tbl = _indexTable[value.GetType().ToString().ToLower()] as Hashtable;
                tbl[key] = null;
            }
        }

        public void RemoveFromIndex(object key, object value)
        {
            if (_indexTable.Contains(value.GetType().ToString().ToLower()))
            {
                Hashtable tbl = _indexTable[value.GetType().ToString().ToLower()] as Hashtable;
                if (tbl.Contains(key))
                {
                    tbl.Remove(key);
                }
            }
        }

        public void Clear()
        {
            if (_indexForAll)
            {
                _indexTable.Clear();
            }
            else
            {
                IDictionaryEnumerator e = _indexTable.GetEnumerator();
                while (e.MoveNext())
                {
                    Hashtable tbl = e.Value as Hashtable;
                    tbl.Clear();
                }
            }
        }

        public IDictionaryEnumerator GetEnumerator(string typeName)
        {
            IDictionaryEnumerator en = _indexTable.GetEnumerator();

            while (en.MoveNext())
            {
                Hashtable tbl = en.Value as Hashtable;
                return tbl.GetEnumerator();
            }

            return null;
        }

        //only needed in case of AttributeIndex
        public IndexInformation GetIndexInformation(object key) { return null; }

        public void RemoveFromIndex(object key) { }

        public long IndexInMemorySize
        {
            get { throw new NotImplementedException("TypeIndex.IndexInMemorySize"); }
        }
    }
}
