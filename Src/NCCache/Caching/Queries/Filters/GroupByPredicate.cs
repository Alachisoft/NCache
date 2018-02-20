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
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Queries;
using RecordColumn = Alachisoft.NCache.Common.DataReader.RecordColumn;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class GroupByPredicate : Predicate, IComparable
    {
        private List<string> _groupingAttributes;
        private Predicate _childPredicate;
        
        private GroupByValueList _groupByValueList;
        private List<OrderByArgument> _orderingAttributes;

        public GroupByPredicate()
        {
            _groupingAttributes = new List<string>();
        }

        public Predicate ChildPredicate
        {
            get { return _childPredicate; }
            set { _childPredicate = value; }
        }

        //Contains member attributes and aggregate functions to be returned in result
        public GroupByValueList GroupByValueList
        {
            get { return _groupByValueList; }
            set { _groupByValueList = value; }
        }

        //contains attribute names on basis of which grouping will be performed
        public List<string> GroupingAttributes
        {
            get { return _groupingAttributes; }
        }

        public List<OrderByArgument> OrderingAttributes
        {
            get { return _orderingAttributes; }
            set { _orderingAttributes = value; }
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            RecordSet resultRecordSet = new RecordSet();
            foreach (string columnName in _groupByValueList.ObjectAttributes)
            {
                RecordColumn column = new RecordColumn(columnName);
                column.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
                column.ColumnType = ColumnType.AttributeColumn;
                column.IsFilled = true;
                column.IsHidden = false;
                resultRecordSet.Columns.Add(column);
            }
            foreach (AggregateFunctionPredicate afp in _groupByValueList.AggregateFunctions)
            {
                string columnName = afp.GetFunctionType().ToString() + "(" + afp.AttributeName + ")";
                if (resultRecordSet.Columns.Contains(columnName))
                    throw new ArgumentException("Invalid query. Same value cannot be selected twice.");

                RecordColumn column = new RecordColumn(columnName);
                column.IsHidden = false;
                column.ColumnType = ColumnType.AggregateResultColumn;
                column.AggregateFunctionType = afp.GetFunctionType();
                column.IsFilled = true;

                resultRecordSet.Columns.Add(column);
                afp.ChildPredicate = null;
            }

            ChildPredicate.Execute(queryContext, nextPredicate);
            if (_orderingAttributes == null)
            {
                _orderingAttributes = new List<OrderByArgument>(_groupingAttributes.Count);
                foreach (string groupby in _groupingAttributes)
                {
                    OrderByArgument oba = new OrderByArgument();
                    oba.AttributeName = groupby;
                    _orderingAttributes.Add(oba);
                }
            }

            MultiRootTree groupTree = new MultiRootTree(_orderingAttributes, true);
            if (queryContext.InternalQueryResult.Count > 0)
            {
                foreach (string key in queryContext.InternalQueryResult)
                {
                    KeyValuesContainer keyValues = new KeyValuesContainer();
                    keyValues.Key = key;
                    bool invalidGroupKey = false;
                    for (int i = 0; i < _orderingAttributes.Count; i++)
                    {
                        string attribute = _orderingAttributes[i].AttributeName;
                        CacheEntry cacheentry = queryContext.Cache.GetEntryInternal(key, false);
                        object attribValue = queryContext.Index.GetAttributeValue(key, attribute, cacheentry.IndexInfo);
                        if (attribValue == null)
                        {
                            invalidGroupKey = true;
                            break;
                        }
                        keyValues.Values[attribute] = attribValue;
                    }
                    if (!invalidGroupKey)
                        groupTree.Add(keyValues);
                }
            }

            //add remaining attributes in Group By clause as hidden columns
            foreach (string attribute in _groupingAttributes)
            {
                if (!resultRecordSet.Columns.Contains(attribute))
                {
                    RecordColumn column = new RecordColumn(attribute);
                    column.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
                    column.ColumnType = ColumnType.AttributeColumn;
                    column.IsHidden = true;
                    column.IsFilled = true;
                    resultRecordSet.Columns.Add(column);
                }
            }

            //generates RecordSet from tree.
            groupTree.ToRecordSet(resultRecordSet);

            for (int rowID = 0; rowID < resultRecordSet.Rows.Count; rowID++)
            {
                List<string> keysList = resultRecordSet.Rows[rowID].Tag as List<string>;
                int j = 0;
                queryContext.InternalQueryResult = new Common.Queries.ListQueryResult(queryContext.KeyFilter,queryContext.CompoundFilter, keysList);//Union(keysList as IEnumerable<string>);
              
                foreach (AggregateFunctionPredicate afp in this._groupByValueList.AggregateFunctions)
                {
                    afp.Execute(queryContext, null);
                    int columnId = _groupByValueList.ObjectAttributes.Count + j++;
                    if (resultRecordSet.Columns[columnId].DataType == ColumnDataType.Object)
                        resultRecordSet.Columns[columnId].DataType = RecordSet.ToColumnDataType(queryContext.ResultSet.AggregateFunctionResult.Value);

                    resultRecordSet.Rows[rowID][columnId] = queryContext.ResultSet.AggregateFunctionResult.Value;
                }
            }
            ReaderResultSet readerResult = new ReaderResultSet();
            readerResult.IsGrouped = true;
            readerResult.OrderByArguments = _orderingAttributes;
            readerResult.RecordSet = resultRecordSet;

            queryContext.ResultSet.Type = QueryType.GroupByAggregateFunction;
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