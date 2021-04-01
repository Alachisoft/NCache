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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.DataStructures
{
     [Serializable]
    public sealed class NCLinkedListNode<T>
    {
        // Fields
        internal T item;
        internal NCLinkedListNode<T> next;
        internal NCLinkedListNode<T> prev;

        // Methods
        public NCLinkedListNode(T value)
        {
            this.item = value;
        }

        internal NCLinkedListNode(NCLinkedList<T> list, T value)
        {
            this.item = value;
        }

        internal void Invalidate()
        {
            this.next = null;
            this.prev = null;
        }

        public NCLinkedListNode<T> Next
        {
            get
            {
                if ((this.next != null))
                {
                    return this.next;
                }
                return null;
            }
        }

        public NCLinkedListNode<T> Previous
        {
            get
            {
                if ((this.prev != null))
                {
                    return this.prev;
                }
                return null;
            }
        }

        public T Value
        {
            get
            {
                return this.item;
            }
            set
            {
                this.item = value;
            }
        }
    }
}
