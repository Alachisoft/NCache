
using System;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Common.DataStructures
{
    ///<summary>
    /// The RedBlackNode class encapsulates a node in the tree
    ///</summary>
    public class RedBlackNode<T> : ISizableIndex, ITreeNode
    {
        /// <summary> Tree node color. </summary>
        public const byte RED = 0xff;
        /// <summary> Tree node color. </summary>
        public const byte BLACK = 0x00;

        /// <summary> key provided by the calling class. </summary>
        private T _key;

        /// <summary> the data or value associated with the key. </summary>
        private HashVector _value;

        /// <summary> color - used to balance the tree. </summary>
        private byte _color;

        /// <summary> Left node. </summary>
        private RedBlackNode<T> _leftNode;

        /// <summary> Right node. </summary>
        private RedBlackNode<T> _rightNode;

        /// <summary> Parent node. </summary>
        private RedBlackNode<T> _parentNode;

        private RedBlackNodeReference<T> _rbReference;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RedBlackNode()
        {
            Color = RED;
            Data = new HashVector();
            _rbReference = new RedBlackNodeReference<T>(this);
        }

        ///<summary>
        ///Key
        ///</summary>
        public T Key
        {
            get { return _key; }
            set { _key = value; }
        }


        ///<summary>
        ///Data
        ///</summary>
        public HashVector Data
        {
            get { return _value; }
            set { _value = value; }
        }

        public void Insert(object key, object value)
        {
            Data.Add(key, null);
        }

        ///<summary>
        ///Color
        ///</summary>
        public byte Color
        {
            get { return _color; }
            set { _color = value; }
        }
        ///<summary>
        ///Left
        ///</summary>
        public RedBlackNode<T> Left
        {
            get { return _leftNode; }
            set { _leftNode = value; }
        }

        ///<summary>
        /// Right
        ///</summary>
        public RedBlackNode<T> Right
        {
            get { return _rightNode; }
            set { _rightNode = value; }
        }

        /// <summary>
        /// Parent node
        /// </summary>
        public RedBlackNode<T> Parent
        {
            get { return _parentNode; }
            set { _parentNode = value; }
        }

        public RedBlackNodeReference<T> RBNodeReference
        {
            get { return _rbReference; }
            set { _rbReference = value; }
        }

        public long IndexInMemorySize
        {
            get { return _value.BucketCount * MemoryUtil.NetHashtableOverHead; }
        }

        ITreeNode ITreeNode.Parent
        {
            get
            {
                return _parentNode;
            }           
        }

        ITreeNode ITreeNode.Left
        {
            get
            {
                return _leftNode;
            }
           
        }

        ITreeNode ITreeNode.Right
        {
            get
            {
                return _rightNode;
            }
            
        }

        object ITreeNode.Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = (T)value;
            }
        }

        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = (HashVector)value;
            }
        }
    }
}
