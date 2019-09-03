using System;
using System.Collections;
	
namespace Alachisoft.NCache.Common.DataStructures
{
	///<summary>
	/// The RedBlackEnumerator class returns the keys or data objects of the treap in
	/// sorted order. 
	///</summary>
	public class RedBlackEnumerator : IDictionaryEnumerator
	{
		// the treap uses the stack to order the nodes
		private Stack stack;
		// return the keys
		// return in ascending order (true) or descending (false)
		private bool ascending;
		
		// key
		private object ordKey;

		// the data or value associated with the key
		private object objValue;

        private ITreeNode _sentinelNode;      


        #region /  --- IDictionary Enumerator --- /

        ///<summary>
		///Key
		///</summary>
		/*public IComparable*/ object IDictionaryEnumerator.Key
		{
			get
            {
				return ordKey;
			}
			
		}

		///<summary>
		///Data
		///</summary>
		/*public*/ object IDictionaryEnumerator.Value
		{
			get
            {
				return objValue;
			}
			
		}


        DictionaryEntry IDictionaryEnumerator.Entry
        {
            get
            {
                return new DictionaryEntry();
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return null;
            }
        }

        void IEnumerator.Reset()
        {
        }

        #endregion

        public RedBlackEnumerator() 
        {
		}
		///<summary>
		/// Determine order, walk the tree and push the nodes onto the stack
		///</summary>
		public RedBlackEnumerator(ITreeNode tnode, bool ascending, ITreeNode sentinelNode) 
        {

            stack = new Stack();
            this.ascending = ascending;
            _sentinelNode = sentinelNode;
			
            // use depth-first traversal to push nodes into stack
            // the lowest node will be at the top of the stack
            if(ascending)
			{   // find the lowest node
				while(tnode != _sentinelNode)
				{
					stack.Push(tnode);
					tnode = tnode.Left;
				}
			}
			else
			{
                // the highest node will be at top of stack
				while(tnode != _sentinelNode)
				{
					stack.Push(tnode);
					tnode = tnode.Right;
				}
			}
			
		}
		///<summary>
		/// HasMoreElements
		///</summary>
		public bool HasMoreElements()
		{
            bool result = stack != null && stack.Count > 0;
            return result;
		}

		///<summary>
		/// NextElement
		///</summary>
		public object NextElement()
		{
			if(stack.Count == 0)
				throw(new RedBlackException("Element not found"));
			
			// the top of stack will always have the next item
			// get top of stack but don't remove it as the next nodes in sequence
			// may be pushed onto the top
			// the stack will be popped after all the nodes have been returned
			ITreeNode node = (ITreeNode) stack.Peek();	//next node in sequence
			
            if(ascending)
            {
                if(node.Right == _sentinelNode)
                {	
                    // yes, top node is lowest node in subtree - pop node off stack 
                    ITreeNode tn = (ITreeNode) stack.Pop();
                    // peek at right node's parent 
                    // get rid of it if it has already been used
                    while(HasMoreElements()&& ((ITreeNode) stack.Peek()).Right == tn)
                        tn = (ITreeNode) stack.Pop();
                }
                else
                {
                    // find the next items in the sequence
                    // traverse to left; find lowest and push onto stack
                    ITreeNode tn = node.Right;
                    while(tn != _sentinelNode)
                    {
                        stack.Push(tn);
                        tn = tn.Left;
                    }
                }
            }
            else            // descending, same comments as above apply
            {
                if(node.Left == _sentinelNode)
                {
                    // walk the tree
                    ITreeNode tn = (ITreeNode) stack.Pop();
                    while(HasMoreElements() && ((ITreeNode)stack.Peek()).Left == tn)
                        tn = (ITreeNode) stack.Pop();
                }
                else
                {
                    // determine next node in sequence
                    // traverse to left subtree and find greatest node - push onto stack
                    ITreeNode tn = node.Left;
                    while(tn != _sentinelNode)
                    {
                        stack.Push(tn);
                        tn = tn.Right;
                    }
                }
            }
			
            ordKey = node.Key;
            objValue = node.Value;
          	
			return node.Key;
		}
		///<summary>
		/// MoveNext
		/// For .NET compatibility
		///</summary>
		public bool MoveNext()
		{
			if(HasMoreElements())
			{
				NextElement();
				return true;
			}
			return false;
		}
	}
}
