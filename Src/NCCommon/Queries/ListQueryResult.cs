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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Queries.Filters;

namespace Alachisoft.NCache.Common.Queries
{
    public class ListQueryResult : IQueryResult
    {
        ClusteredArrayList _resultKeys = new ClusteredArrayList();

        public ListQueryResult(IKeyFilter kf,IKeyFilter cf)
        {
            KeyFilter = kf;
            CompoundFilter = cf;
        }

        public ListQueryResult(IKeyFilter kf,IKeyFilter cf,IList result):this(kf,cf)
        {
            _resultKeys.AddRange(result);           
        }

        public IKeyFilter KeyFilter { get; set; }

        public IKeyFilter CompoundFilter { get; set; }

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
                    break;
                case CollectionOperation.Subtract:
                    throw new NotImplementedException();
                    break;
            }
        }

        private Boolean EvaluateFilter(Object key, Boolean filter)
        {
            if (KeyFilter == null && CompoundFilter == null) return true;

            if (!filter) return true;

            if (KeyFilter != null && CompoundFilter != null)
                return KeyFilter.Evaluate((String)key) && CompoundFilter.Evaluate((String)key);

            if (KeyFilter != null)
                return KeyFilter.Evaluate((String)key);

            if (CompoundFilter != null)
                return CompoundFilter.Evaluate((String)key);

            return false;
        }

        public void Add(IDictionary other, CollectionOperation op, bool filterResult = true)
        {
            switch (op)
            {
                case CollectionOperation.Union:
                {
                    if (other != null && other.Keys != null)
                    {
                        IEnumerator ienum = other.Keys.GetEnumerator();
                        while (ienum.MoveNext())
                        {
                            if (EvaluateFilter(ienum.Current, filterResult))
                                _resultKeys.Add(ienum.Current); //AddRange(other.Keys);
                        }
                    }
                }
                    break;
                case CollectionOperation.Intersection:
                    throw new NotImplementedException();
                    break;
                case CollectionOperation.Subtract:
                    throw new NotImplementedException();
                    break;
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