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
// limitations under the License
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.DataReader;

namespace Alachisoft.NCache.Common.DataStructures
{

    public class MultiRootTree
    {
        private class LeafNode
        {
            List<string> _keys;
            KeyValuesContainer _values;

            public LeafNode()
            {
                _keys = new List<string>();
            }

            public List<string> Keys
            {
                get { return _keys; }
            }

            public KeyValuesContainer AttributeValues
            {
                get { return _values; }
                set { _values = value; }
            }

        }

        private static bool _isGrouped;
        //Act as attribute name
        private string _currentAttribute;
        private Order _sortingOrder;
        private List<OrderByArgument> _orderingAttributes;
        
        //contains attributeValue-listofkeys/subtree
        private IDictionary _nodeStore;

        private int _levels;

        public int Levels
        {
            get { return _levels; }
        }

        private MultiRootTree(int levels, List<OrderByArgument> orderingAttributes)
        {
            _levels = levels;
            _orderingAttributes = orderingAttributes;
            OrderByArgument currentOrderAttribute = orderingAttributes[orderingAttributes.Count - _levels];
            _currentAttribute = currentOrderAttribute.AttributeName;
            _sortingOrder = currentOrderAttribute.Order;
            if(_sortingOrder==Order.ASC)
                _nodeStore = new SortedDictionary<object, object>();
            else
                _nodeStore = new SortedDictionary<object, object>(new ReverseComparer<object>());
        }

        public MultiRootTree(List<OrderByArgument> orderingAttributes, bool isGrouped)
            : this(orderingAttributes.Count, orderingAttributes)
        {
            _isGrouped = isGrouped;
        }

        public void Add(KeyValuesContainer value)
        {
            object node = _nodeStore[value.Values[_currentAttribute]];
            if (_levels == 1)
            {
                if (node is LeafNode)
                    ((LeafNode)node).Keys.Add(value.Key);
                else
                {
                    LeafNode leaf = new LeafNode();
                    leaf.Keys.Add(value.Key);
                    _nodeStore[value.Values[_currentAttribute]] = leaf;
                    leaf.AttributeValues = value;
                }
            }
            else
            {
                if (node is MultiRootTree)
                    ((MultiRootTree)node).Add(value);
                else
                {
                    MultiRootTree mrt = new MultiRootTree(_levels - 1, _orderingAttributes);
                    mrt.Add(value);
                    _nodeStore[value.Values[_currentAttribute]] = mrt;
                }
            }
        }

        private void GenerateRecordSet(RecordSet recordSet)
        {
            IDictionaryEnumerator ide = _nodeStore.GetEnumerator();
            while (ide.MoveNext())
            {
                if (ide.Value is LeafNode)
                {
                    LeafNode leaf = (LeafNode)ide.Value;
                    List<string> keys = leaf.Keys;
                    if (_isGrouped)
                    {
                        RecordRow row = recordSet.CreateRow() as RecordRow;
                        if (leaf.AttributeValues != null)
                        {
                            foreach (KeyValuePair<string, object> entry in leaf.AttributeValues.Values)
                            {
                                if (recordSet.Columns[entry.Key].DataType == ColumnDataType.Object)
                                    recordSet.Columns[entry.Key].DataType = RecordSet.ToColumnDataType(entry.Value);
                                row[entry.Key] = entry.Value;
                            }
                        }
                        row.Tag = keys;
                        recordSet.Rows.Add(row);
                    }
                    else
                    {
                        foreach (string key in keys)
                        {
                            RecordRow row = recordSet.CreateRow() as RecordRow;
                            if (leaf.AttributeValues != null)
                            {
                                foreach (KeyValuePair<string, object> entry in leaf.AttributeValues.Values)
                                {
                                    if (recordSet.Columns[entry.Key].DataType == ColumnDataType.Object)
                                        recordSet.Columns[entry.Key].DataType = RecordSet.ToColumnDataType(entry.Value);
                                    row[entry.Key] = entry.Value;
                                }
                            }
                            row[QueryKeyWords.KeyColumn] = key;
                            recordSet.Rows.Add(row);
                        }
                    }
                }
                else
                {
                    (ide.Value as MultiRootTree).GenerateRecordSet(recordSet);
                }
            }
        }
        
        public void ToRecordSet(RecordSet recordSet)
        {
            GenerateRecordSet(recordSet);
        }
    }
}
