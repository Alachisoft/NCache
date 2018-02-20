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
using System.Linq;
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Queries.Filters;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Queries;
using RecordColumn = Alachisoft.NCache.Common.DataReader.RecordColumn;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class OrderByPredicate : Predicate, IComparable
    {
        private Predicate _childPredicate;

        private List<OrderByArgument> _orderByArguments;

        public OrderByPredicate()
        {
        }

        public Predicate ChildPredicate
        {
            get { return _childPredicate; }
            set
            {
                if (value is GroupByPredicate)
                {
                    GroupByPredicate gbp = value as GroupByPredicate;
                    for (int i = 0; i < _orderByArguments.Count; i++)
                    { }
                }

                _childPredicate = value;
            }
        }

        //Contains member atributes and aggregate functions to be returned in result
        public List<OrderByArgument> OrderByArguments
        {
            get { return _orderByArguments; }
            set { _orderByArguments = value; }
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            ChildPredicate.Execute(queryContext, nextPredicate);
           
            RecordSet resultRecordSet = new RecordSet();

            RecordColumn keyColumn = new RecordColumn(QueryKeyWords.KeyColumn);
            keyColumn.ColumnType = ColumnType.KeyColumn;
            keyColumn.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
            keyColumn.DataType = ColumnDataType.String;
            keyColumn.IsHidden = false;
            keyColumn.IsFilled = true;

            resultRecordSet.Columns.Add(keyColumn);

            RecordColumn valueColumn = new RecordColumn(QueryKeyWords.ValueColumn);
            valueColumn.ColumnType = ColumnType.ValueColumn;
            valueColumn.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
            valueColumn.DataType = ColumnDataType.CompressedValueEntry;
            valueColumn.IsHidden = true;
            valueColumn.IsFilled = false;

            resultRecordSet.Columns.Add(valueColumn);

            foreach (OrderByArgument orderBy in _orderByArguments)
            {
                RecordColumn column = new RecordColumn(orderBy.AttributeName);
                column.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
                column.ColumnType = ColumnType.AttributeColumn;
                column.IsFilled = true;
                column.IsHidden = true;

                resultRecordSet.Columns.Add(column);
            }

            MultiRootTree sortingTree = new MultiRootTree(_orderByArguments, false);
            if (queryContext.InternalQueryResult.Count > 0)
            {
                foreach (string key in queryContext.InternalQueryResult)
                {                    
                    KeyValuesContainer keyValues = new KeyValuesContainer();
                    keyValues.Key = key;
                   
                    bool invalidGroupKey = false;
                    for (int i = 0; i < _orderByArguments.Count; i++)
                    {
                        string attribute = _orderByArguments[i].AttributeName;
                        CacheEntry cacheentry = queryContext.Cache.GetEntryInternal(key, false);
                        if (cacheentry == null)
                        {
                            invalidGroupKey = true;
                            break;
                        }
                        
                        object attribValue = queryContext.Index.GetAttributeValue(key, attribute, cacheentry.IndexInfo);
                        if (attribValue == null)
                        {
                            invalidGroupKey = true;
                            break;
                        }
                        keyValues.Values[attribute] = attribValue; 
                    }

                    if (!invalidGroupKey) sortingTree.Add(keyValues);
                }
            }

            //generates RecordSet from tree.
            sortingTree.ToRecordSet(resultRecordSet);

            ReaderResultSet readerResult = new ReaderResultSet();
            readerResult.IsGrouped = false;
            readerResult.OrderByArguments = _orderByArguments;
            readerResult.RecordSet = resultRecordSet;
            queryContext.ResultSet.Type = QueryType.OrderByQuery;
            queryContext.ResultSet.ReaderResult = readerResult;
        }


        #region IComparable Members

        public int CompareTo(object obj)
        {

            GroupByPredicate other = obj as GroupByPredicate;

            if (other != null)
                return ((IComparable)ChildPredicate).CompareTo(other.ChildPredicate);

            return -1;
        }

        #endregion

        public override bool ApplyPredicate(object o)
        {
            return false;
        }
    }

}
