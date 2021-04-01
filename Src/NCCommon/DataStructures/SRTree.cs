//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Text;

using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class SRTree : ICloneable
    {
        private ClusteredArrayList _leftList;
        private ClusteredArrayList _rightList;

        public SRTree()
        {
            _leftList = new ClusteredArrayList();
            _rightList = new ClusteredArrayList();
        }

        public ClusteredArrayList LeftList
        {
            get { return _leftList; }
            set 
            { 
                if (value != null)
                    _leftList = value; 
            }
        }

        public ClusteredArrayList RightList
        {
            get { return _rightList; }
            set
            {
                if (value != null)
                {
                    _rightList = value;
                }
            }
        }


        /// <summary>
        /// Populates the trees' right list with objects contained in the enumerator.
        /// </summary>
        /// <param name="e"></param>
        public void Populate(IDictionaryEnumerator e) 
        {
            if (e != null)
            {
                if (_rightList == null) _rightList = new ClusteredArrayList();
                if (e is RedBlackEnumerator)
                {
                    while (e.MoveNext())
                    {
                        HashVector tbl = e.Value as HashVector;
                        _rightList.AddRange(tbl.Keys);
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        _rightList.Add(e.Key);
                    }
                }
            }
        }


        /// <summary>
        /// After reduction, the trees' right list becomes the left list 
        /// and left list vanishes away.
        /// </summary>
        public void Reduce()
        {
            _leftList = _rightList.Clone() as ClusteredArrayList;
            _rightList.Clear();
        }
        
        /// <summary>
        /// Shifts an object with the specified key from the left list to the right list.
        /// </summary>
        public void Shift(object key)
        {
            if (_leftList.Contains(key))
            {
                if (_rightList == null)
                    _rightList = new ClusteredArrayList();

                _leftList.Remove(key);
                _rightList.Add(key);
            }
        }

        /// <summary>
        /// returns an instance of SRTreeEnumerator.
        /// </summary>
        /// <returns></returns>
        public SRTreeEnumerator GetEnumerator()
        {
            if (_leftList != null)
            {
                return new SRTreeEnumerator(_leftList);
            }
            return null;
        }


        /// <summary>
        /// Merge the right lists of passed tree and current tree.
        /// </summary>
        /// <param name="tree"></param>
        public void Merge(SRTree tree)
        {
            if (tree == null)
                tree = new SRTree();
            
            if (tree.RightList == null)
                tree.RightList = new ClusteredArrayList();

            if (this.RightList != null)
            {
                IEnumerator en = this.RightList.GetEnumerator();
                while (en.MoveNext())
                {
                    if (!tree.RightList.Contains(en.Current))
                        tree.RightList.Add(en.Current);
                }
            }
        }

        #region /------- IClonable Members --------/
        
        public object Clone()
        {
            SRTree tmp = new SRTree();
            
            if (this.LeftList != null)
                tmp.LeftList = this.LeftList.Clone() as ClusteredArrayList;
            if (this.RightList != null)
                tmp.RightList = this.RightList.Clone() as ClusteredArrayList;

            return tmp;
        }
        
        #endregion
    }
}
