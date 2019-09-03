
using System;
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures
{
	///<summary>
	/// The RedBlackNode class encapsulates a node in the tree
	///</summary>
	public class RedBlackNodeReference<T>:INodeReference
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
