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
using System.Runtime.Serialization;
using System.Collections;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Threading;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;


namespace Alachisoft.NCache.Common.DataStructures
{
   
    public class NCLinkedList<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, ICollection, IEnumerable, IDeserializationCallback, ICompactSerializable
    {
        // Fields
        private object _syncRoot;
        internal int count;
        private const string CountName = "Count";
        internal NCLinkedListNode<T> head;
        private SerializationInfo siInfo;
        private const string ValuesName = "Data";
        private const string VersionName = "Version";

        // Methods
        public NCLinkedList()
        {
        }

        public NCLinkedList(System.Collections.Generic.IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            foreach (T local in collection)
            {
                this.AddLast(local);
            }
        }

        protected NCLinkedList(SerializationInfo info, StreamingContext context)
        {
            this.siInfo = info;
        }

        public NCLinkedListNode<T> AddAfter(NCLinkedListNode<T> node, T value)
        {
            this.ValidateNode(node);
            NCLinkedListNode<T> newNode = new NCLinkedListNode<T>(value);
            this.InternalInsertNodeBefore(node.Next, newNode);
            return newNode;
        }

        public void AddAfter(NCLinkedListNode<T> node, NCLinkedListNode<T> newNode)
        {
            this.ValidateNode(node);
            this.ValidateNewNode(newNode);
            this.InternalInsertNodeBefore(node.Next, newNode);
        }

        public void AddBefore(NCLinkedListNode<T> node, NCLinkedListNode<T> newNode)
        {
            this.ValidateNode(node);
            this.ValidateNewNode(newNode);
            this.InternalInsertNodeBefore(node, newNode);
            if (node == this.head)
            {
                this.head = newNode;
            }
        }

        public NCLinkedListNode<T> AddBefore(NCLinkedListNode<T> node, T value)
        {
            this.ValidateNode(node);
            NCLinkedListNode<T> newNode = new NCLinkedListNode<T>(value);
            this.InternalInsertNodeBefore(node, newNode);
            if (node == this.head)
            {
                this.head = newNode;
            }
            return newNode;
        }

        public void AddFirst(NCLinkedListNode<T> node)
        {
            this.ValidateNewNode(node);
            if (this.head == null)
            {
                this.InternalInsertNodeToEmptyList(node);
            }
            else
            {
                this.InternalInsertNodeBefore(this.head, node);
                this.head = node;
            }
        }

        public NCLinkedListNode<T> AddFirst(T value)
        {
            NCLinkedListNode<T> newNode = new NCLinkedListNode<T>(value);
            if (this.head == null)
            {
                this.InternalInsertNodeToEmptyList(newNode);
                return newNode;
            }
            this.InternalInsertNodeBefore(this.head, newNode);
            this.head = newNode;
            return newNode;
        }

        public NCLinkedListNode<T> AddLast(T value)
        {
            NCLinkedListNode<T> newNode = new NCLinkedListNode<T>(value);
            if (this.head == null)
            {
                this.InternalInsertNodeToEmptyList(newNode);
                return newNode;
            }
            this.InternalInsertNodeBefore(this.head, newNode);
            return newNode;
        }

        public void AddLast(NCLinkedListNode<T> node)
        {
            this.ValidateNewNode(node);
            if (this.head == null)
            {
                this.InternalInsertNodeToEmptyList(node);
            }
            else
            {
                this.InternalInsertNodeBefore(this.head, node);
            }
        }

        public void AppendLinkedList(NCLinkedList<T> secondList)
        {
            if (this.head == null)
            {
                this.head = secondList.head;
                this.count = this.count + secondList.count;
            }
            else
            {
                secondList.head.prev.next = this.head;
                this.head.prev.next = secondList.head;

                this.head.prev = secondList.head.prev;
                this.count = this.count + secondList.count;
            }
        }

        public void Clear()
        {
            NCLinkedListNode<T> head = this.head;
            while (head != null)
            {
                NCLinkedListNode<T> node2 = head;
                head = head.Next;
                node2.Invalidate();
            }
            this.head = null;
            this.count = 0;
        }

        public bool Contains(T value)
        {
            return (this.Find(value) != null);
        }

        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if ((index < 0) || (index > array.Length))
            {
                throw new ArgumentOutOfRangeException("index", "out of range");
            }
            if ((array.Length - index) < this.Count)
            {
                throw new ArgumentException("Insufficient Space");
            }
            NCLinkedListNode<T> head = this.head;
            if (head != null)
            {
                do
                {
                    array[index++] = head.item;
                    head = head.next;
                }
                while (head != this.head);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
        {
            return new Enumerator((NCLinkedList<T>)this);
        }

        public void AddRangeInList(System.Collections.ICollection keys)
        {
            if (keys == null)
                throw new ArgumentNullException("keys cannot be null.");
            IEnumerator _enum = keys.GetEnumerator();
            object obj;
            NCLinkedListNode<T> node;
            while (_enum.MoveNext())
            {
                node = new NCLinkedListNode<T>((T)_enum.Current);
                this.AddLast(node);
            }
        }

        public NCLinkedList<T> Clone()
        {
            IEnumerator _enum = this.GetEnumerator();
            NCLinkedList<T> list = new NCLinkedList<T>();
            while (_enum.MoveNext())
            {
                list.AddLast((T)_enum.Current);
            }

            return list;
        }

        private void InternalInsertNodeBefore(NCLinkedListNode<T> node, NCLinkedListNode<T> newNode)
        {
            newNode.next = node;
            newNode.prev = node.prev;
            node.prev.next = newNode;
            node.prev = newNode;
            this.count++;
        }

        private void InternalInsertNodeToEmptyList(NCLinkedListNode<T> newNode)
        {
            newNode.next = newNode;
            newNode.prev = newNode;
            this.head = newNode;
            this.count++;
        }

        internal void InternalRemoveNode(NCLinkedListNode<T> node)
        {
            if (node.next == node)
            {
                this.head = null;
            }
            else
            {
                node.next.prev = node.prev;
                node.prev.next = node.next;
                if (this.head == node)
                {
                    this.head = node.next;
                }
            }
            node.Invalidate();
            this.count--;
        }

        public virtual void OnDeserialization(object sender)
        {
            if (this.siInfo != null)
            {
                int num = this.siInfo.GetInt32("Version");
                if (this.siInfo.GetInt32("Count") != 0)
                {
                    T[] localArray = (T[])this.siInfo.GetValue("Data", typeof(T[]));
                    if (localArray == null)
                    {
                        throw new SerializationException("Missing values");
                    }
                    for (int i = 0; i < localArray.Length; i++)
                    {
                        this.AddLast(localArray[i]);
                    }
                }
                else
                {
                    this.head = null;
                }
                this.siInfo = null;
            }
        }

        public bool Remove(T value)
        {
            NCLinkedListNode<T> node = this.Find(value);
            if (node != null)
            {
                this.InternalRemoveNode(node);
                return true;
            }
            return false;
        }

        public bool Remove(NCLinkedListNode<T> node)
        {
            if (node != null)
            {
                this.InternalRemoveNode(node);
                return true;
            }
            return false;
        }

        public NCLinkedListNode<T> Find(T value)
        {
            NCLinkedListNode<T> head = this.head;
            System.Collections.Generic.EqualityComparer<T> comparer = System.Collections.Generic.EqualityComparer<T>.Default;
            if (head != null)
            {
                if (value != null)
                {
                    do
                    {
                        if (comparer.Equals(head.item, value))
                        {
                            return head;
                        }
                        head = head.next;
                    }
                    while (head != this.head);
                }
                else
                {
                    do
                    {
                        if (head.item == null)
                        {
                            return head;
                        }
                        head = head.next;
                    }
                    while (head != this.head);
                }
            }
            return null;
        }

        void System.Collections.Generic.ICollection<T>.Add(T value)
        {
            this.AddLast(value);
        }

        #region	/                 --- ICompactSerializable ---           /

        public void Deserialize(CompactReader reader)
        {
            T obj;
            int c = reader.ReadInt32();
            for (int i = 0; i < c; i++)
            {
                obj = reader.ReadObjectAs<T>();
                this.AddLast(obj);
            }
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(count);
            
            IEnumerator e = this.GetEnumerator();
            while (e.MoveNext())
            {
                writer.WriteObject(e.Current);
            }
        }

        #endregion

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("Count", this.count);
            if (this.count != 0)
            {
                T[] array = new T[this.Count];
                this.CopyTo(array, 0);
                info.AddValue("Data", array, typeof(T[]));
            }
        }


        internal void ValidateNewNode(NCLinkedListNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

        }

        internal void ValidateNode(NCLinkedListNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

        }

        // Properties
        public int Count
        {
            get
            {
                return this.count;
            }
        }

        public NCLinkedListNode<T> First
        {
            get
            {
                return this.head;
            }
        }

        public NCLinkedListNode<T> Last
        {
            get
            {
                if (this.head != null)
                {
                    return this.head.prev;
                }
                return null;
            }
        }

        bool System.Collections.Generic.ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
                }
                return this._syncRoot;
            }
        }


        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : System.Collections.Generic.IEnumerator<T>, IDisposable, IEnumerator, ISerializable, IDeserializationCallback
        {
            private const string LinkedListName = "LinkedList";
            private const string CurrentValueName = "Current";
            private const string VersionName = "Version";
            private const string IndexName = "Index";
            private NCLinkedList<T> list;
            private NCLinkedListNode<T> node;
            private T current;
            private int index;
            private SerializationInfo siInfo;
            internal Enumerator(NCLinkedList<T> list)
            {
                this.list = list;
                this.node = list.head;
                this.current = default(T);
                this.index = 0;
                this.siInfo = null;
            }

            internal Enumerator(SerializationInfo info, StreamingContext context)
            {
                this.siInfo = info;
                this.list = null;
                //this.version = 0;
                this.node = null;
                this.current = default(T);
                this.index = 0;
            }

            public T Current
            {
                get
                {
                    return this.current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if ((this.index == 0) || (this.index == (this.list.Count + 1)))
                    {
                        throw new InvalidOperationException("Invalid enumeration operation");
                    }
                    return this.current;
                }
            }

            public bool MoveNext()
            {
                if (this.node == null)
                {
                    this.index = this.list.Count + 1;
                    return false;
                }
                this.index++;
                this.current = this.node.item;
                this.node = this.node.next;
                if (this.node == this.list.head)
                {
                    this.node = null;
                }
                return true;
            }

            void IEnumerator.Reset()
            {
                this.current = default(T);
                this.node = this.list.head;
                this.index = 0;
            }

            public void Dispose()
            {
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException("info");
                }
                info.AddValue("LinkedList", this.list);
                info.AddValue("Current", this.current);
                info.AddValue("Index", this.index);
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                if (this.list == null)
                {
                    if (this.siInfo == null)
                    {
                        throw new SerializationException("Serialization invalid on deserialize");
                    }
                    this.list = (NCLinkedList<T>)this.siInfo.GetValue("LinkedList", typeof(NCLinkedList<T>));
                    this.current = (T)this.siInfo.GetValue("Current", typeof(T));
                    this.index = this.siInfo.GetInt32("Index");
                    if (this.list.siInfo != null)
                    {
                        this.list.OnDeserialization(sender);
                    }
                    if (this.index == (this.list.Count + 1))
                    {
                        this.node = null;
                    }
                    else
                    {
                        this.node = this.list.First;
                        if ((this.node != null) && (this.index != 0))
                        {
                            for (int i = 0; i < this.index; i++)
                            {
                                this.node = this.node.next;
                            }
                            if (this.node == this.list.First)
                            {
                                this.node = null;
                            }
                        }
                    }
                    this.siInfo = null;
                }
            }
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }
    }

}
