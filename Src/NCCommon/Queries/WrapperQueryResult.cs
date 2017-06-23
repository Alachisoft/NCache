// Copyright (c) 2017 Alachisoft
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
using System.Collections;

namespace Alachisoft.NCache.Common.Queries
{
    public class WrapperQueryResult : IQueryResult
    {
        internal HashVector _resultKeys = new HashVector();
        IQueryResult _wrappedResult;
        private HashVector _excludeResults;


        public WrapperQueryResult(IQueryResult result)
        {
            _wrappedResult = result;
        }

        public int Count
        {
            get { return _resultKeys.Count; }
        }

        public void AddExclusion(HashVector excludeResult)
        {
            if (_excludeResults == null)
                _excludeResults = new HashVector();
            IDictionaryEnumerator ie = excludeResult.GetEnumerator();
            while (ie.MoveNext())
            {
                if (!(_excludeResults.ContainsKey(ie.Key)))
                    _excludeResults.Add(ie.Key, null);
            }
        }

        public void Add(IDictionary other, CollectionOperation op)
        {
            IDictionaryEnumerator ide = other.GetEnumerator();

            while (ide.MoveNext())
            {
                AddObject(ide.Key, op);
            }
        }

        public void AddObject(object obj, CollectionOperation op)
        {
            if (_excludeResults != null && _excludeResults.ContainsKey(obj))
                return;

            if (!(_resultKeys.ContainsKey(obj)))
            {
                _resultKeys[obj] = null;
                _wrappedResult.AddObject(obj, op);
            }
        }

        public void Mark(CollectionOperation op)
        {
            _wrappedResult.Mark(op);
        }

        public void Populate(IDictionaryEnumerator e)
        {
            if (e != null)
            {
                if (e is RedBlackEnumerator)
                {
                    while (e.MoveNext())
                    {
                        HashVector tbl = e.Value as HashVector;
                        this.Add(tbl, CollectionOperation.Union);
                        this.Mark(CollectionOperation.Union);
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        if (!_resultKeys.ContainsKey(e.Key))
                            _resultKeys.Add(e.Key, null);
                    }
                }
            }
        }

        public void Merge(IQueryResult other, CollectionOperation op)
        {
            _wrappedResult.Merge(other, op);
        }

        public ClusteredArrayList GetArrayList()
        {
            return _wrappedResult.GetArrayList();
        }

        public IEnumerator GetEnumerator()
        {
            return _wrappedResult.GetEnumerator();
        }
    }
}
