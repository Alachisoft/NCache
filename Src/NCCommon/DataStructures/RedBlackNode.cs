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
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures
{
	///<summary>
	/// The RedBlackNode class encapsulates a node in the tree
	///</summary>
	public class RedBlackNode:ISizableIndex
	{
        /// <summary> Tree node color. </summary>
		public const int	RED		= 0;
		/// <summary> Tree node color. </summary>
		public const int	BLACK	= 1;

		/// <summary> key provided by the calling class. </summary>
		private IComparable		_key;

		/// <summary> the data or value associated with the key. </summary>
		private Hashtable  		_value;

		/// <summary> color - used to balance the tree. </summary>
		private int				_color;

		/// <summary> Left node. </summary>
		private RedBlackNode	_leftNode;

		/// <summary> Right node. </summary>
		private RedBlackNode	_rightNode;

		/// <summary> Parent node. </summary>
        private RedBlackNode	_parentNode;

        private RedBlackNodeReference _rbReference;

        /// <summary> Max Count of values in Hashtable  </summary>
        private long _maxItemCount;

        		
		/// <summary>
		/// Default constructor.
		/// </summary>
		public RedBlackNode()
		{
			Color = RED;
            Data = new Hashtable();
            _rbReference = new RedBlackNodeReference(this);
		}

		///<summary>
		///Key
		///</summary>
		public IComparable Key
		{
			get { return _key; }
			set { _key = value; }
		}

		///<summary>
		///Data
		///</summary>
		public Hashtable Data
		{
			get { return _value; }
			set { _value = value; }
		}

        ///<summary>
        ///Insert Value
        ///</summary>
        public void Insert(object key, object value)
        {
            Data.Add(key, null);
            if (Data.Count > _maxItemCount)
                _maxItemCount = Data.Count;
        }

        ///<summary>
		///Color
		///</summary>
		public int Color
		{
			get { return _color; }
			set { _color = value; }
		}
		///<summary>
		///Left
		///</summary>
		public RedBlackNode Left
		{
			get { return _leftNode; }			
			set { _leftNode = value; }
		}

		///<summary>
		/// Right
		///</summary>
		public RedBlackNode Right
		{
			get { return _rightNode; }			
			set { _rightNode = value; }
		}

		/// <summary>
		/// Parent node
		/// </summary>
        public RedBlackNode Parent
        {
            get { return _parentNode; }			
            set { _parentNode = value; }
        }

        public RedBlackNodeReference RBNodeReference
        {
            get { return _rbReference; }
            set { _rbReference = value; }
        }

        public long IndexInMemorySize
        {
            get { return _maxItemCount * MemoryUtil.NetHashtableOverHead; }
        }
	}
}
