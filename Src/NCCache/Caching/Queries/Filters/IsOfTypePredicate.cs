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
using System;
using Alachisoft.NCache.Parser;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class IsOfTypePredicate : Predicate, IComparable
    {
        string typename;

        public IsOfTypePredicate(string name)
        {
            typename = name;    
        }

        public string TypeName
        {
            get { return typename; }
            set { typename = value; }
        }

        public override bool ApplyPredicate(object o)
        {
            if (typename == "*")
                throw new Parser.ParserException("Incorrect query format \'*\'.");

            return o.GetType().Name == typename;
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            if (typename == "*")
                throw new ParserException("Incorrect query format. \'*\' is not supported.");

            if (queryContext.IndexManager == null)
                throw new TypeIndexNotDefined("Index is not defined for '" + typename.ToString() + "'");

            queryContext.TypeName = typename;

            if (queryContext.Index == null) //try to get virtual index
            {
                //in case of DisableException is true, exception will not be thrown, and return new attribute index.
                if (QueryIndexManager.DisableException)
                {
                    queryContext.Index = new AttributeIndex(null, queryContext.Cache.Context.CacheRoot.Name, null);
                    return;
                }

                throw new TypeIndexNotDefined("Index is not defined for '" + typename.ToString() + "'");
            }
            else
            {
                
                //populate the tree for normal queries...
                if (nextPredicate == null && queryContext.PopulateTree)
                {
                    queryContext.Tree.Populate(queryContext.Index.GetEnumerator(typename));
                }
                else
                {
                    nextPredicate.Execute(queryContext, null);
                }
            }
        }

        public override string ToString()
        {
            return "typeof(Value)" + (Inverse ? " != " : " == ") + typename;
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is IsOfTypePredicate)
            {
                IsOfTypePredicate other = (IsOfTypePredicate)obj;
                if (Inverse == other.Inverse)
                    return typename.CompareTo(other.typename);
            }
            return -1;
        }

        #endregion
    }
}
