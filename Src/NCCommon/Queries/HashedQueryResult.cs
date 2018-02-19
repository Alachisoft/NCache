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


using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using System;
using System.Collections;

namespace Alachisoft.NCache.Common.Queries
{
    public class HashedQueryResult : IQueryResult
    {
        private HashVector _resultKeys = new HashVector();
        private HashVector _temp = new HashVector();

        public int Count
        { get { return _resultKeys.Count; } }

        public void Add(IDictionary other, CollectionOperation mergeType)
        {
            IDictionaryEnumerator ide = other.GetEnumerator();
            switch (mergeType)
            {
                case CollectionOperation.Union:
                    while (ide.MoveNext())
                        _resultKeys[ide.Key] = null;
                    break;
                case CollectionOperation.Intersection:
                    while (ide.MoveNext())
                    {
                        if (_resultKeys.Contains(ide.Key))
                            _temp[ide.Key] = null;
                    }
                    break;
                case CollectionOperation.Subtract:
                    while (ide.MoveNext())
                    {
                        _resultKeys.Remove(ide.Key);
                    }
                    break;
            }
        }

        public void Mark(CollectionOperation mergeType)
        {
            switch (mergeType)
            {
                case CollectionOperation.Union:
                    break;
                case CollectionOperation.Intersection:
                    _resultKeys = _temp;
                    _temp = new HashVector();
                    break;
                case CollectionOperation.Subtract:
                    break;
            }
        }

        public void Merge(IQueryResult other, CollectionOperation mergeType)
        {
            HashedQueryResult hashedResult = other as HashedQueryResult;
            if (hashedResult == null)
                throw new Exception("Failed to merge query results. Type mismatch");
            switch (mergeType)
            {
                case CollectionOperation.Union:
                    if (this._resultKeys.Count < hashedResult._resultKeys.Count)
                    {
                        HashVector temp = this._resultKeys;
                        this._resultKeys = hashedResult._resultKeys;
                        hashedResult._resultKeys = temp;
                    }
                    break;
                case CollectionOperation.Intersection:
                    break;
                case CollectionOperation.Subtract:
                    break;
            }
            this.Add(hashedResult._resultKeys, mergeType);
            this.Mark(mergeType);
        }

        public void Populate(IDictionaryEnumerator e)
        {
            if (e != null)
            {
                if (e is RedBlackEnumerator)
                {
                    while (e.MoveNext())
                    {
                        Hashtable tbl = e.Value as Hashtable;
                        this.Add(tbl, CollectionOperation.Union);
                        this.Mark(CollectionOperation.Union);
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        _resultKeys[e.Key] = null;
                    }
                }
            }
        }

        public ClusteredArrayList GetArrayList()
        {
            return new ClusteredArrayList(_resultKeys.Keys);
        }

        public IEnumerator GetEnumerator()
        {
            return _resultKeys.Keys.GetEnumerator();
        }


        public void AddObject(object obj, CollectionOperation op)
        {
            switch (op)
            {
                case CollectionOperation.Union:
                    _resultKeys[obj] = null;
                    break;
                case CollectionOperation.Intersection:
                    if (_resultKeys.Contains(obj))
                        _temp[obj] = null;
                    break;
                case CollectionOperation.Subtract:
                    _resultKeys.Remove(obj);
                    break;
            }
        }
    }
}
