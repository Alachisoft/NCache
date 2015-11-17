///<summary>
///A red-black tree must satisfy these properties:
///
///1. The root is black. 
///2. All leaves are black. 
///3. Red nodes can only have black children. 
///4. All paths from a node to its leaves contain the same number of black nodes.
///</summary>

using System.Collections;
using System.Text;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class RedBlack<T> : ISizableIndex where T:IComparable
	{
        //REGEX is the comparison based on the regular expression.
        //It is used for LIKE type comparisons.
        //IREGEX is the inverse comparison based on the regular expression.
        //It is used for NOT LIKE type of comparisons.
        public enum COMPARE
        {
            EQ, NE, LT, GT, LTEQ, GTEQ, REGEX, IREGEX
        }

        // the number of nodes contained in the tree
		private int             intCount;
        
		// a simple randomized hash code. The hash code could be used as a key
        // if it is "unique" enough. Note: The IComparable interface would need to 
        // be replaced with int.

		// identifies the owner of the tree
		
		// the tree
		private RedBlackNode<T>	rbTree;
        
		//  sentinelNode is convenient way of indicating a leaf node.
        private RedBlackNode<T> _sentinelNode = new RedBlackNode<T>();
        
		// the node that was last found; used to optimize searches
		private RedBlackNode<T>	lastNodeFound;

        //  Tree Type 
        private AttributeTypeSize _typeSize;
        // Tree Node Keys Size
        private long _rbNodeKeySize;
        // Tree Data Size
        private long _rbNodeDataSize;
		

        //whether a duplicate key.

        //used for logging
        string _cacheName;

        public RedBlack()
        {

            // set up the sentinel node. the sentinel node is the key to a successfull
            // implementation and for understanding the red-black tree properties.
            _sentinelNode.Left = _sentinelNode.Right = _sentinelNode;
            _sentinelNode.Parent = null;
            _sentinelNode.Color = RedBlackNode<T>.BLACK;
            rbTree = _sentinelNode;
            lastNodeFound = _sentinelNode;
        }

        public RedBlack(string cacheName, AttributeTypeSize size)
            : this()
        {
            _cacheName = cacheName;
            _typeSize = size;
        }

        public RedBlackNode<T> SentinelNode
        {
            get { return _sentinelNode; }
        }
		
		///<summary>
		/// Add
		/// args: ByVal key As T, ByVal data As Object
		/// key is object that implements IComparable interface
		/// performance tip: change to use use int type (such as the hashcode)
		///</summary>
        public object Add(T key, object data)
		{
            bool collision = false;
            RedBlackNodeReference<T> keyNodeRfrnce = null;
            try
            {
                if (key == null || data == null)
                    throw (new RedBlackException("RedBlackNode<T> key and data must not be null"));

                // traverse tree - find where node belongs
                int result = 0;
                // create new node
                RedBlackNode<T> node = new RedBlackNode<T>();
                RedBlackNode<T> temp = rbTree;				// grab the rbTree node of the tree

                while (temp != _sentinelNode)
                {	// find Parent
                    node.Parent = temp;
                    if(key is string)
                        result = key.ToString().ToLower().CompareTo(temp.Key.ToString().ToLower());
                    else
                        result = key.CompareTo(temp.Key);
                    if (result == 0)
                    {
                        collision = true; //data with the same key.
                        break;
                    }

                    if (result > 0)
                    {
                        temp = temp.Right;
                        collision = false;
                    }
                    else
                    {
                        temp = temp.Left;
                        collision = false;
                    }
                }

                if (collision)
                {
                    long prevSize = temp.IndexInMemorySize;
                    temp.Insert(data, null);//.Data[data] = null;
                    keyNodeRfrnce = temp.RBNodeReference;

                    _rbNodeDataSize += temp.IndexInMemorySize - prevSize;
                }
                else
                {
                    // setup node
                    node.Key = key;
                    node.Insert(data, null);//.Data.Add(data, null);
                    node.Left = _sentinelNode;
                    node.Right = _sentinelNode;

                    if (_typeSize != AttributeTypeSize.Variable)
                        _rbNodeKeySize += MemoryUtil.GetTypeSize(_typeSize);
                    else
                        _rbNodeKeySize += MemoryUtil.GetStringSize(key);

                    _rbNodeDataSize += node.IndexInMemorySize;

                    // insert node into tree starting at parent's location
                    if (node.Parent != null)
                    {
                        if (key is string)
                            result = node.Key.ToString().ToLower().CompareTo(node.Parent.Key.ToString().ToLower());
                        else
                            result = node.Key.CompareTo(node.Parent.Key);
                        
                        if (result > 0)
                            node.Parent.Right = node;
                        else
                            node.Parent.Left = node;
                    }
                    else
                        rbTree = node;					// first node added

                    RestoreAfterInsert(node);           // restore red-black properities

                    lastNodeFound = node;

                    intCount = intCount + 1;

                    keyNodeRfrnce= node.RBNodeReference;
                }
            }
            catch (Exception ex)
            {
            }

            return keyNodeRfrnce;
		}
        ///<summary>
        /// RestoreAfterInsert
        /// Additions to red-black trees usually destroy the red-black 
        /// properties. Examine the tree and restore. Rotations are normally 
        /// required to restore it
        ///</summary>
		private void RestoreAfterInsert(RedBlackNode<T> x)
		{   
            // x and y are used as variable names for brevity, in a more formal
            // implementation, you should probably change the names

			RedBlackNode<T> y;

			// maintain red-black tree properties after adding x
			while(x != rbTree && x.Parent.Color == RedBlackNode<T>.RED)
			{
				// Parent node is .Colored red; 
				if(x.Parent == x.Parent.Parent.Left)	// determine traversal path			
				{										// is it on the Left or Right subtree?
					y = x.Parent.Parent.Right;			// get uncle
					if(y!= null && y.Color == RedBlackNode<T>.RED)
					{	// uncle is red; change x's Parent and uncle to black
						x.Parent.Color			= RedBlackNode<T>.BLACK;
						y.Color					= RedBlackNode<T>.BLACK;
						// grandparent must be red. Why? Every red node that is not 
						// a leaf has only black children 
						x.Parent.Parent.Color	= RedBlackNode<T>.RED;	
						x						= x.Parent.Parent;	// continue loop with grandparent
					}	
					else
					{
						// uncle is black; determine if x is greater than Parent
						if(x == x.Parent.Right) 
						{	// yes, x is greater than Parent; rotate Left
							// make x a Left child
							x = x.Parent;
							RotateLeft(x);
						}
						// no, x is less than Parent
						x.Parent.Color			= RedBlackNode<T>.BLACK;	// make Parent black
						x.Parent.Parent.Color	= RedBlackNode<T>.RED;		// make grandparent black
						RotateRight(x.Parent.Parent);					// rotate right
					}
				}
				else
				{	// x's Parent is on the Right subtree
					// this code is the same as above with "Left" and "Right" swapped
					y = x.Parent.Parent.Left;
					if(y!= null && y.Color == RedBlackNode<T>.RED)
					{
						x.Parent.Color			= RedBlackNode<T>.BLACK;
						y.Color					= RedBlackNode<T>.BLACK;
						x.Parent.Parent.Color	= RedBlackNode<T>.RED;
						x						= x.Parent.Parent;
					}
					else
					{
						if(x == x.Parent.Left)
						{
							x = x.Parent;
							RotateRight(x);
						}
						x.Parent.Color			= RedBlackNode<T>.BLACK;
						x.Parent.Parent.Color	= RedBlackNode<T>.RED;
						RotateLeft(x.Parent.Parent);
					}
				}																													
			}
			rbTree.Color = RedBlackNode<T>.BLACK;		// rbTree should always be black
		}
		
		///<summary>
		/// RotateLeft
		/// Rebalance the tree by rotating the nodes to the left
		///</summary>
		public void RotateLeft(RedBlackNode<T> x)
		{
			// pushing node x down and to the Left to balance the tree. x's Right child (y)
			// replaces x (since y > x), and y's Left child becomes x's Right child 
			// (since it's < y but > x).
            
			RedBlackNode<T> y = x.Right;			// get x's Right node, this becomes y

			// set x's Right link
			x.Right = y.Left;					// y's Left child's becomes x's Right child

			// modify parents
			if(y.Left != _sentinelNode) 
				y.Left.Parent = x;				// sets y's Left Parent to x

            if(y != _sentinelNode)
			    y.Parent = x.Parent;			// set y's Parent to x's Parent

			if(x.Parent != null)		
			{	// determine which side of it's Parent x was on
				if(x == x.Parent.Left)			
					x.Parent.Left = y;			// set Left Parent to y
				else
					x.Parent.Right = y;			// set Right Parent to y
			} 
			else 
				rbTree = y;						// at rbTree, set it to y

			// link x and y 
			y.Left = x;							// put x on y's Left 
			if(x != _sentinelNode)						// set y as x's Parent
				x.Parent = y;		
		}
		///<summary>
		/// RotateRight
		/// Rebalance the tree by rotating the nodes to the right
		///</summary>
		public void RotateRight(RedBlackNode<T> x)
		{
			// pushing node x down and to the Right to balance the tree. x's Left child (y)
			// replaces x (since x < y), and y's Right child becomes x's Left child 
			// (since it's < x but > y).
            
			RedBlackNode<T> y = x.Left;			// get x's Left node, this becomes y

			// set x's Right link
			x.Left = y.Right;					// y's Right child becomes x's Left child

			// modify parents
			if(y.Right != _sentinelNode) 
				y.Right.Parent = x;				// sets y's Right Parent to x

            if(y != _sentinelNode)
                y.Parent = x.Parent;			// set y's Parent to x's Parent

			if(x.Parent != null)				// null=rbTree, could also have used rbTree
			{	// determine which side of it's Parent x was on
				if(x == x.Parent.Right)			
					x.Parent.Right = y;			// set Right Parent to y
				else
					x.Parent.Left = y;			// set Left Parent to y
			} 
			else 
				rbTree = y;						// at rbTree, set it to y

			// link x and y 
			y.Right = x;						// put x on y's Right
			if(x != _sentinelNode)				// set y as x's Parent
				x.Parent = y;		
		}

		
		///<summary>
		/// GetData
		/// Gets the data object associated with the specified key
		///<summary>
		public object GetData(T key, COMPARE compareType)
		{
			int result;
            ArrayList keyList = new ArrayList();
            RedBlackNode<T> treeNode = rbTree;     // begin at root
            IDictionaryEnumerator en = this.GetEnumerator();
            string pattern;
            WildcardEnabledRegex regex;
            HashVector finalTable = null;
            HashVector skippedKeys = null;
            bool isStringValue = false;

            if (key is string)
                isStringValue = true;

            switch(compareType)
            {
                case COMPARE.EQ:
                    // traverse tree until node is found
                    while (treeNode != _sentinelNode)
                    {
                        if (isStringValue && treeNode.Key is string)
                            result = treeNode.Key.ToString().ToLower().CompareTo(key.ToString().ToLower());
                        else
                            result = treeNode.Key.CompareTo(key);
				        if(result == 0)
				        {
					        lastNodeFound = treeNode;
                            keyList.AddRange(treeNode.Data.Keys);
					        //return treeNode.Data;
                            return keyList;
				        }
				        if(result > 0) //treenode is Greater then the one we are looking. Move to Left branch 
					        treeNode = treeNode.Left;
				        else
					        treeNode = treeNode.Right; //treenode is Less then the one we are looking. Move to Right branch.
			        }
                    break;

                case COMPARE.NE:
                    // traverse tree until node is found
                    finalTable = new HashVector();
                    
                    while (en.MoveNext())
                    {
                        if (isStringValue && en.Key is string)
                            result = en.Key.ToString().ToLower().CompareTo(key.ToString().ToLower());
                        else
                            result = ((IComparable)en.Key).CompareTo(key);

                        if (result != 0)
                        {
                            HashVector tmp = en.Value as HashVector;
                            IDictionaryEnumerator ide = tmp.GetEnumerator();
                    
                            while (ide.MoveNext())
                                finalTable[ide.Key] = ide.Value;
                        }
                    }

                    return new ArrayList(finalTable.Keys);//keyList;

                    break;

                case COMPARE.GT:
                    finalTable = new HashVector();
					while (en.MoveNext())
					{
                        if (isStringValue && en.Key is string)
                            result = en.Key.ToString().ToLower().CompareTo(key.ToString().ToLower());
                        else
                            result = ((IComparable)en.Key).CompareTo(key);

                        if (result > 0)
                        {
                            HashVector tmp = en.Value as HashVector;
                            IDictionaryEnumerator ide = tmp.GetEnumerator();
                            
                            while (ide.MoveNext())
                                finalTable[ide.Key] = ide.Value;

                        }
					}

                    return new ArrayList(finalTable.Keys);//keyList;
                    
                    break;

                case COMPARE.LT:
                    finalTable = new HashVector();
                    while (en.MoveNext())
                    {
                        if (isStringValue && en.Key is string)
                            result = en.Key.ToString().ToLower().CompareTo(key.ToString().ToLower());
                        else
                            result = ((IComparable)en.Key).CompareTo(key);

                        if (result < 0)
                        {
                            HashVector tmp = en.Value as HashVector;
                            IDictionaryEnumerator ide = tmp.GetEnumerator();
                            while (ide.MoveNext())
                                finalTable[ide.Key] = ide.Value;

                        }
                    }

                    return new ArrayList(finalTable.Keys);//keyList;
                    
                    break;

                case COMPARE.GTEQ:
                    finalTable = new HashVector();

					while (en.MoveNext())
					{
                        if (isStringValue && en.Key is string)
                            result = en.Key.ToString().ToLower().CompareTo(key.ToString().ToLower());
                        else
                            result = ((IComparable)en.Key).CompareTo(key);

                        if (result >= 0)
                        {
                            HashVector tmp = en.Value as HashVector;
                            IDictionaryEnumerator ide = tmp.GetEnumerator();
                            while (ide.MoveNext())
                                finalTable[ide.Key] = ide.Value;

                        }
					}

                    return new ArrayList(finalTable.Keys);//keyList;
                    
                    break;

                case COMPARE.LTEQ:
                    finalTable = new HashVector();
                    while (en.MoveNext())
                    {
                        if (isStringValue && en.Key is string)
                            result = en.Key.ToString().ToLower().CompareTo(key.ToString().ToLower());
                        else
                            result = ((IComparable)en.Key).CompareTo(key);

                        if (result <= 0)
                        {
                            HashVector tmp = en.Value as HashVector;
                            IDictionaryEnumerator ide = tmp.GetEnumerator();
                            while (ide.MoveNext())
                                finalTable[ide.Key] = ide.Value;

                        }
                        else break;
                    }

                    return new ArrayList(finalTable.Keys);//keyList;
                    
                    break;

                case COMPARE.REGEX:
                    finalTable = new HashVector();
                    pattern = key as string;
                    regex = new WildcardEnabledRegex(pattern);
                    while (en.MoveNext())
                    {
                        if (en.Key is string)
                        {
                            if (regex.IsMatch((string)en.Key.ToString().ToLower()))
                            {
                                HashVector tmp = en.Value as HashVector;
                                IDictionaryEnumerator ide = tmp.GetEnumerator();
                             
                                while (ide.MoveNext())
                                    finalTable[ide.Key] = ide.Value;
                            }
                        }
                    }

                    return new ArrayList(finalTable.Keys);//keyList;
                    
                    break;

                case COMPARE.IREGEX:
                    finalTable = new HashVector();
                    pattern = key as string;
                    regex = new WildcardEnabledRegex(pattern);
                    skippedKeys = new HashVector();
                    while (en.MoveNext())
                    {
                        if (en.Key is string)
                        {
                            if (regex.IsMatch((string)en.Key.ToString().ToLower()))
                            {
                                HashVector tmp = en.Value as HashVector;
                                IDictionaryEnumerator ide = tmp.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    skippedKeys[ide.Key] = ide.Value;
                                }
                            }
                            else
                            {
                                HashVector tmp = en.Value as HashVector;
                                IDictionaryEnumerator ide = tmp.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    finalTable[ide.Key] = ide.Value;
                                }
                            }
                        }
                    }

                    ArrayList list = new ArrayList(finalTable.Keys);// keyList;
                    
                    for (int idx = list.Count - 1; idx >= 0; idx--)
                    {
                        if (skippedKeys.ContainsKey(list[idx]))
                        {
                            list.RemoveAt(idx);
                        }
                    }

                    return list;

                    break;
            }
			
            return keyList;
		}
		


		///<summary>
		/// return true if a specifeid key exists
		///<summary>
		public bool Contains(T key)
		{
			int result;
			RedBlackNode<T> treeNode = rbTree;     // begin at root
            
			// traverse tree until node is found
			while(treeNode != _sentinelNode)
			{
                result = treeNode.Key.CompareTo(key);                
				if(result == 0)
				{
					lastNodeFound = treeNode;
					return true;
				}

                if (result > 0) //treenode is Greater then the one we are looking. Move to Left branch 
                    treeNode = treeNode.Left;
                else
                    treeNode = treeNode.Right; //treenode is Less then the one we are looking. Move to Right branch.
			}
			return false;
		}

		///<summary>
		/// GetMinKey
		/// Returns the minimum key value
		///<summary>
		public T MinKey
		{
			get 
			{
				RedBlackNode<T> treeNode = rbTree;
				if(treeNode == null || treeNode == _sentinelNode) return default(T);
		
				// traverse to the extreme left to find the smallest key
				while(treeNode.Left != _sentinelNode)
					treeNode = treeNode.Left;
		
				lastNodeFound = treeNode;
				return treeNode.Key;
			}
		}

		///<summary>
		/// GetMaxKey
		/// Returns the maximum key value
		///<summary>
		public T MaxKey
		{
			get
			{
				RedBlackNode<T> treeNode = rbTree;
				if(treeNode == null || treeNode == _sentinelNode)
					throw(new RedBlackException("RedBlack tree is empty"));

				// traverse to the extreme right to find the largest key
				while(treeNode.Right != _sentinelNode)
					treeNode = treeNode.Right;

				lastNodeFound = treeNode;
				return treeNode.Key;
			}
		}

		
		///<summary>
		/// GetEnumerator
		/// return an enumerator that returns the tree nodes in order
		///<summary>
		public RedBlackEnumerator GetEnumerator()
		{
            // elements is simply a generic name to refer to the 
            // data objects the nodes contain
			return Elements(true);      
		}

		///<summary>
		/// Keys
		/// if(ascending is true, the keys will be returned in ascending order, else
		/// the keys will be returned in descending order.
		///<summary>
		public RedBlackEnumerator Keys()
		{
			return Keys(true);
		}

		public RedBlackEnumerator Keys(bool ascending)
		{
			return new RedBlackEnumerator(rbTree, ascending, _sentinelNode);
		}
		
		///<summary>
		/// Elements
		/// Returns an enumeration of the data objects.
		/// if(ascending is true, the objects will be returned in ascending order,
		/// else the objects will be returned in descending order.
		///<summary>
		public RedBlackEnumerator Elements()
		{
			return Elements(true);
		}

		public RedBlackEnumerator Elements(bool ascending)
		{
			return new RedBlackEnumerator(rbTree, ascending, _sentinelNode);
		}

		///<summary>
		/// IsEmpty
		/// Is the tree empty?
		///<summary>
		public bool IsEmpty
		{
			get { return (rbTree == null); }
		}

        public void Remove(object indexKey)
        {
            Remove(indexKey, null);
        }

		///<summary>
		/// Remove
		/// removes the key and data object (delete)
		///<summary>
		public bool Remove(object cacheKey, object node)
		{
            bool isNodeRemoved = false;
            RedBlackNodeReference<T> keyNodeReference = (RedBlackNodeReference<T>)node;
            RedBlackNode<T> keyNode = keyNodeReference.RBReference;
			try
			{
                    if (cacheKey != null && keyNode.Data.Count > 1)
                    {
                        if (keyNode.Data.Contains(cacheKey))
                        {
                            long initialSize = keyNode.IndexInMemorySize;
                            keyNode.Data.Remove(cacheKey);
                            isNodeRemoved = false;
                            _rbNodeDataSize += keyNode.IndexInMemorySize - initialSize;
                        }
                    }
                    else
                    {
                        if (_typeSize != AttributeTypeSize.Variable)
                            _rbNodeKeySize -= MemoryUtil.GetTypeSize(_typeSize);
                        else
                            _rbNodeKeySize -= MemoryUtil.GetStringSize(keyNode.Key);

                        _rbNodeDataSize -= keyNode.IndexInMemorySize;

                        Delete(keyNode);
                        isNodeRemoved = true;
                    }
				
			}
			catch(Exception)
			{
				throw;
			}
			
            if(isNodeRemoved)
                intCount = intCount - 1;

            return isNodeRemoved;
		}

        public void Remove(T indexKey, object cacheKey)
        {
            bool isNodeRemoved = false;
            if (indexKey == null)
                throw (new RedBlackException("RedBlackNode<T> key is null"));

            try
            {
                // find node
                int result;
                RedBlackNode<T> node;

                // see if node to be deleted was the last one found
                if(indexKey is string)
                    result = indexKey.ToString().ToLower().CompareTo(lastNodeFound.Key.ToString().ToLower());
                else
                    result = indexKey.CompareTo(lastNodeFound.Key);
                if (result == 0)
                    node = lastNodeFound;
                else
                {	// not found, must search		
                    node = rbTree;
                    while (node != _sentinelNode)
                    {
                        if (indexKey is string)
                            result = indexKey.ToString().ToLower().CompareTo(node.Key.ToString().ToLower());
                        else
                            result = indexKey.CompareTo(node.Key);
                       
                        if (result == 0)
                            break;
                        if (result < 0)
                            node = node.Left;
                        else
                            node = node.Right;
                    }

                    if (node == _sentinelNode)
                    {
                        return;				// key not found
                        
                    }
                }

                try
                {
                    if (cacheKey != null && node.Data.Count > 1)
                    {
                        if (node.Data.Contains(cacheKey))
                        {
                            node.Data.Remove(cacheKey);

                            isNodeRemoved = false;
                        }
                    }
                    else
                    {
                        if (_typeSize != AttributeTypeSize.Variable)
                            _rbNodeKeySize -= MemoryUtil.GetTypeSize(_typeSize);
                        else
                            _rbNodeKeySize -= MemoryUtil.GetStringSize(node.Key);

                        _rbNodeDataSize -= node.IndexInMemorySize;

                        Delete(node);

                        isNodeRemoved = true;
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
            catch (Exception)
            {
                throw;
            }

            if (isNodeRemoved)
                intCount = intCount - 1;
        }
		///<summary>
		/// Delete
		/// Delete a node from the tree and restore red black properties
		///<summary>
		private void Delete(RedBlackNode<T> z)
		{
			// A node to be deleted will be: 
			//		1. a leaf with no children
			//		2. have one child
			//		3. have two children
			// If the deleted node is red, the red black properties still hold.
			// If the deleted node is black, the tree needs rebalancing

			RedBlackNode<T> x = new RedBlackNode<T>();	// work node to contain the replacement node
			RedBlackNode<T> y;					// work node 

			// find the replacement node (the successor to x) - the node one with 
			// at *most* one child. 
			if(z.Left == _sentinelNode || z.Right == _sentinelNode) 
				y = z;						// node has sentinel as a child
			else 
			{
				// z has two children, find replacement node which will 
				// be the leftmost node greater than z
				y = z.Right;				        // traverse right subtree	
				while(y.Left != _sentinelNode)		// to find next node in sequence
					y = y.Left;
			}

			// at this point, y contains the replacement node. it's content will be copied 
			// to the valules in the node to be deleted

			// x (y's only child) is the node that will be linked to y's old parent. 
            if(y.Left != _sentinelNode)
                x = y.Left;					
            else
                x = y.Right;					

			// replace x's parent with y's parent and
			// link x to proper subtree in parent
			// this removes y from the chain
			x.Parent = y.Parent;
			if(y.Parent != null)
				if(y == y.Parent.Left)
					y.Parent.Left = x;
				else
					y.Parent.Right = x;
			else
				rbTree = x;			// make x the root node

			// copy the values from y (the replacement node) to the node being deleted.
			// note: this effectively deletes the node. 
			if(y != z) 
			{
				z.Key	= y.Key;
				z.Data	= y.Data; //un-commented by [Asif Imam] 12 Jun,08
                z.RBNodeReference = y.RBNodeReference;
                z.RBNodeReference.RBReference = z;
			}

			if(y.Color == RedBlackNode<T>.BLACK)
				RestoreAfterDelete(x);

			lastNodeFound = _sentinelNode;
		}

        ///<summary>
        /// RestoreAfterDelete
        /// Deletions from red-black trees may destroy the red-black 
        /// properties. Examine the tree and restore. Rotations are normally 
        /// required to restore it
        ///</summary>
		private void RestoreAfterDelete(RedBlackNode<T> x)
		{
			// maintain Red-Black tree balance after deleting node 			

			RedBlackNode<T> y;

			while(x != rbTree && x.Color == RedBlackNode<T>.BLACK) 
			{
				if(x == x.Parent.Left)			// determine sub tree from parent
				{
					y = x.Parent.Right;			// y is x's sibling 
					if(y.Color == RedBlackNode<T>.RED) 
					{	// x is black, y is red - make both black and rotate
						y.Color			= RedBlackNode<T>.BLACK;
						x.Parent.Color	= RedBlackNode<T>.RED;
						RotateLeft(x.Parent);
						y = x.Parent.Right;
					}
					if(y.Left.Color == RedBlackNode<T>.BLACK && 
						y.Right.Color == RedBlackNode<T>.BLACK) 
					{	// children are both black
						y.Color = RedBlackNode<T>.RED;		// change parent to red
						x = x.Parent;					// move up the tree
					} 
					else 
					{
						if(y.Right.Color == RedBlackNode<T>.BLACK) 
						{
							y.Left.Color	= RedBlackNode<T>.BLACK;
							y.Color			= RedBlackNode<T>.RED;
							RotateRight(y);
							y				= x.Parent.Right;
						}
						y.Color			= x.Parent.Color;
						x.Parent.Color	= RedBlackNode<T>.BLACK;
						y.Right.Color	= RedBlackNode<T>.BLACK;
						RotateLeft(x.Parent);
						x = rbTree;
					}
				} 
				else 
				{	// right subtree - same as code above with right and left swapped
					y = x.Parent.Left;
					if(y.Color == RedBlackNode<T>.RED) 
					{
						y.Color			= RedBlackNode<T>.BLACK;
						x.Parent.Color	= RedBlackNode<T>.RED;
						RotateRight (x.Parent);
						y = x.Parent.Left;
					}
					if(y.Right.Color == RedBlackNode<T>.BLACK && 
						y.Left.Color == RedBlackNode<T>.BLACK) 
					{
						y.Color = RedBlackNode<T>.RED;
						x		= x.Parent;
					} 
					else 
					{
						if(y.Left.Color == RedBlackNode<T>.BLACK) 
						{
							y.Right.Color	= RedBlackNode<T>.BLACK;
							y.Color			= RedBlackNode<T>.RED;
							RotateLeft(y);
							y				= x.Parent.Left;
						}
						y.Color			= x.Parent.Color;
						x.Parent.Color	= RedBlackNode<T>.BLACK;
						y.Left.Color	= RedBlackNode<T>.BLACK;
						RotateRight(x.Parent);
						x = rbTree;
					}
				}
			}
			x.Color = RedBlackNode<T>.BLACK;
		}
		
		///<summary>
		/// RemoveMin
		/// removes the node with the minimum key
		///<summary>
		public void RemoveMin()
		{
            if(rbTree == null)
                throw(new RedBlackException("RedBlackNode<T> is null"));
            
             Remove(MinKey);
		}
		///<summary>
		/// RemoveMax
		/// removes the node with the maximum key
		///<summary>
		public void RemoveMax()
		{
            if(rbTree == null)
                throw(new RedBlackException("RedBlackNode<T> is null"));
            
            Remove(MaxKey);
		}
		///<summary>
		/// Clear
		/// Empties or clears the tree
		///<summary>
		public void Clear ()
		{
            rbTree      = _sentinelNode;
            intCount    = 0;

            _rbNodeDataSize = 0;
            _rbNodeKeySize = 0;
		}
		///<summary>
		/// Size
		/// returns the size (number of nodes) in the tree
		///<summary>
		public int Count
		{
			// number of keys
			get { return intCount; }
		}
		///<summary>
		/// Equals
		///<summary>
		public override bool Equals(object obj)
		{
			if(obj == null)
				return false;
			
			if(!(obj is RedBlackNode<T> ))
				return false;
			
			if(this == obj)
				return true;
			
			return (ToString().Equals(((RedBlackNode<T>)(obj)).ToString()));
			
		}
		///<summary>
		/// HashCode
		///<summary>
		public override int GetHashCode()
		{
			return 0;
		}
		///<summary>
		/// ToString
		///<summary>
		public override string ToString()
		{
			//return strIdentifier.ToString();
			return "";
		}


        public long IndexInMemorySize
        {
            get { return _rbNodeKeySize + _rbNodeDataSize; }
        }   
	}
}
