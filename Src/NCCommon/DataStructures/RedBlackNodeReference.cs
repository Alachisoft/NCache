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
	public class RedBlackNodeReference<T>: INodeReference
	{
		/// <summary> Red Black node reference. </summary>
        private RedBlackNode<T> _rbNode;

        public RedBlackNodeReference()
        {
        }

		/// <summary>
		/// Default constructor.
		/// </summary>
        public RedBlackNodeReference(RedBlackNode<T> rbNode)
		{
            _rbNode = rbNode;
		}

        public RedBlackNode<T> RBReference
        {
            get { return _rbNode; }
            set { _rbNode = value; }
        }


        public object GetNode()
        {
            return _rbNode;
        }

        public object GetKey()
        {
            return _rbNode.Key;
        }



	}
}
