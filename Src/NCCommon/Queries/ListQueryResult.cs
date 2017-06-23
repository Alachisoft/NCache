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
using System;
using System.Collections;

namespace Alachisoft.NCache.Common.Queries
{
    public class ListQueryResult : IQueryResult
    {
        ClusteredArrayList _resultKeys = new ClusteredArrayList();

        public ListQueryResult()
        {
        }

        public ListQueryResult(IList result)
        {
            _resultKeys.AddRange(result);
        }

        public int Count
        {
            get { return _resultKeys.Count; }
        }

        public void AddObject(object obj, CollectionOperation op)
        {
            switch (op)
            {
                case CollectionOperation.Union:
                    _resultKeys.Add(obj);
                    break;
                case CollectionOperation.Intersection:
                    throw new NotImplementedException();
                case CollectionOperation.Subtract:
                    throw new NotImplementedException();
            }
        }

        public void Add(IDictionary other, CollectionOperation op)
        {
            switch (op)
            {
                case CollectionOperation.Union:
                    _resultKeys.AddRange(other.Keys);
                    break;
                case CollectionOperation.Intersection:
                    throw new NotImplementedException();
                case CollectionOperation.Subtract:
                    throw new NotImplementedException();
            }
        }

        public void Mark(CollectionOperation op)
        {

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
                        _resultKeys.Add(e.Key);
                    }
                }
            }
        }

        public void Merge(IQueryResult other, CollectionOperation op)
        {
            throw new NotImplementedException();
        }

        public ClusteredArrayList GetArrayList()
        {
            return _resultKeys;
        }

        public IEnumerator GetEnumerator()
        {
            return _resultKeys.GetEnumerator();
        }

    }
}
