// $Id: List.java,v 1.6 2004/07/05 14:17:35 belaban Exp $
using System;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NGroups.Util
{
	/// <summary> Doubly-linked list. Elements can be added at head or tail and removed from head/tail.
	/// This class is tuned for element access at either head or tail, random access to elements
	/// is not very fast; in this case use Vector. Concurrent access is supported: a thread is blocked
	/// while another thread adds/removes an object. When no objects are available, removal returns null.
	/// </summary>
	/// <author>  Bela Ban
	/// </author>
	[Serializable]
	internal class List : ICloneable, ICompactSerializable
	{
		public System.Collections.ArrayList Contents
		{
			get
			{
				System.Collections.ArrayList retval = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(_size));
				Element el;
				
				lock (mutex)
				{
					el = head;
					while (el != null)
					{
						retval.Add(el.obj);
						el = el.next;
					}
				}
				return retval;
			}
			
		}
		protected internal Element head = null, tail = null;
		protected internal int _size = 0;
		protected internal object mutex = new object();
		
		[Serializable]
		protected internal class Element
		{
			private void  InitBlock(List enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private List enclosingInstance;
			public List Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal object obj = null;
			internal Element next = null;
			internal Element prev = null;
			
			internal Element(List enclosingInstance, object o)
			{
				InitBlock(enclosingInstance);
				obj = o;
			}
		}
		
		
		public List()
		{
		}
		
		
		/// <summary>Adds an object at the tail of the list.</summary>
		public virtual void  add(object obj)
		{
			Element el = new Element(this, obj);
			
			lock (mutex)
			{
				if (head == null)
				{
					head = el;
					tail = head;
					_size = 1;
				}
				else
				{
					el.prev = tail;
					tail.next = el;
					tail = el;
					_size++;
				}
			}
		}
		
		/// <summary>Adds an object at the head of the list.</summary>
		public virtual void  addAtHead(object obj)
		{
			Element el = new Element(this, obj);
			
			lock (mutex)
			{
				if (head == null)
				{
					head = el;
					tail = head;
					_size = 1;
				}
				else
				{
					el.next = head;
					head.prev = el;
					head = el;
					_size++;
				}
			}
		}
		
		
		/// <summary>Removes an object from the tail of the list. Returns null if no elements available</summary>
		public object remove()
		{
			Element retval = null;
			
			lock (mutex)
			{
				if (tail == null)
					return null;
				retval = tail;
				if (head == tail)
				{
					// last element
					head = null;
					tail = null;
				}
				else
				{
					tail.prev.next = null;
					tail = tail.prev;
					retval.prev = null;
				}
				
				_size--;
			}
			return retval.obj;
		}
		
		
		/// <summary>Removes an object from the head of the list. Returns null if no elements available </summary>
		public object removeFromHead()
		{
			Element retval = null;
			
			lock (mutex)
			{
				if (head == null)
					return null;
				retval = head;
				if (head == tail)
				{
					// last element
					head = null;
					tail = null;
				}
				else
				{
					head = head.next;
					head.prev = null;
					retval.next = null;
				}
				_size--;
			}
			return retval.obj;
		}
		
		
		/// <summary>Returns element at the tail (if present), but does not remove it from list.</summary>
		public object peek()
		{
			lock (mutex)
			{
				return tail != null?tail.obj:null;
			}
		}
		
		
		/// <summary>Returns element at the head (if present), but does not remove it from list.</summary>
		public object peekAtHead()
		{
			lock (mutex)
			{
				return head != null?head.obj:null;
			}
		}
		
		
		/// <summary>Removes element <code>obj</code> from the list, checking for equality using the <code>equals</code>
		/// operator. Only the first duplicate object is removed. Returns the removed object.
		/// </summary>
		public object removeElement(object obj)
		{
			Element el = null;
			object retval = null;
			
			lock (mutex)
			{
				el = head;
				while (el != null)
				{
					if (el.obj.Equals(obj))
					{
						retval = el.obj;
						if (head == tail)
						{
							// only 1 element left in the list
							head = null;
							tail = null;
						}
						else if (el.prev == null)
						{
							// we're at the head
							head = el.next;
							head.prev = null;
							el.next = null;
						}
						else if (el.next == null)
						{
							// we're at the tail
							tail = el.prev;
							tail.next = null;
							el.prev = null;
						}
						else
						{
							// we're somewhere in the middle of the list
							el.prev.next = el.next;
							el.next.prev = el.prev;
							el.next = null;
							el.prev = null;
						}
						_size--;
						break;
					}
					
					el = el.next;
				}
			}
			return retval;
		}
		
		
		public void  removeAll()
		{
			lock (mutex)
			{
				_size = 0;
				head = null;
				tail = null;
			}
		}
		
		
		public int size()
		{
			return _size;
		}
		
		public override string ToString()
		{
			System.Text.StringBuilder ret = new System.Text.StringBuilder("[");
			Element el = head;
			
			while (el != null)
			{
				if (el.obj != null)
				{
					ret.Append(el.obj + " ");
				}
				el = el.next;
			}
			ret.Append(']');
			return ret.ToString();
		}
		
		
		public string dump()
		{
			System.Text.StringBuilder ret = new System.Text.StringBuilder("[");
			for (Element el = head; el != null; el = el.next)
			{
				ret.Append(el.obj + " ");
			}
			
			return ret.ToString() + ']';
		}
		
		
		public System.Collections.IEnumerator elements()
		{
			return new ListEnumerator(this, head);
		}
		
		
		public bool contains(object obj)
		{
			Element el = head;
			
			while (el != null)
			{
				if (el.obj != null && el.obj.Equals(obj))
					return true;
				el = el.next;
			}
			return false;
		}
		
		
		public List copy()
		{
			List retval = new List();
			
			lock (mutex)
			{
				for (Element el = head; el != null; el = el.next)
					retval.add(el.obj);
			}
			return retval;
		}
		
		
		public object Clone()
		{
			return copy();
		}
		
		
		internal class ListEnumerator : System.Collections.IEnumerator
		{
			private void  InitBlock(List enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private object tempAuxObj;
			public bool MoveNext()
			{
				bool result = hasMoreElements();
				if (result)
				{
					tempAuxObj = nextElement();
				}
				return result;
			}
			public void  Reset()
			{
				tempAuxObj = null;
			}
			public object Current
			{
				get
				{
					return tempAuxObj;
				}
				
			}
			private List enclosingInstance;
			public List Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal Element curr = null;
			
			internal ListEnumerator(List enclosingInstance, Element start)
			{
				InitBlock(enclosingInstance);
				curr = start;
			}
			
			public bool hasMoreElements()
			{
				return curr != null;
			}
			
			public object nextElement()
			{
				object retval;
				
				if (curr == null)
					throw new System.ArgumentOutOfRangeException();
				retval = curr.obj;
				curr = curr.next;
				return retval;
			}
		}

		#region ICompactSerializable Members

		void ICompactSerializable.Deserialize(CompactReader reader)
		{
			object obj;
			int new_size = reader.ReadInt32();

			if (new_size == 0)
				return;

			for (int i = 0; i < new_size; i++)
			{
				obj = reader.ReadObject();
				add(obj);
			}
		}

		void ICompactSerializable.Serialize(CompactWriter writer)
		{
			Element el;
			lock (mutex)
			{
				el = head;
				writer.Write(_size);
				for (int i = 0; i < _size; i++)
				{
					writer.WriteObject(el.obj);
					el = el.next;
				}
			}
		}

		#endregion
	}
}